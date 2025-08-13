using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;
using SDL_Sharp;
using SDL_Sharp.Loader;
using SDL_Sharp.Ttf;
using SteamKit2;
using SteamKit2.Internal;

public partial class Steam
{
	public static Steam Instance { get; private set; }

	int loginWindowState = 0;
	int mainwindowState = 0; //0 = not shown, 1 = loading, 2 = shown

	float now = 0;
	float lastTime = 0;

	public TaskIcon TaskIcon;

	public List<SteamWindow> Windows = new List<SteamWindow>();
	public List<SteamWindow> PendingWindows = new List<SteamWindow>();
	public List<SteamWindow> PendingWindowsToRemove = new List<SteamWindow>();

	public SteamClient steamClient;
	public SteamUser steamUser;
	public SteamApps steamApps;
	public SteamFriends steamFriends;
	public SteamContent steamContent;
	public SteamCloud steamCloud;
	public PublishedFile steamPublishedFile;
	public CallbackManager manager;

	public User CurrentUser;

	public void Initialize()
	{
		Instance = this;

		CheckIfAlreadyRunning();

		CreateDirectories();

		//SDL init
		SdlLoader.LoadDefault();
		SDL.Init(SdlInitFlags.Video | SdlInitFlags.Events);
		TTF.Init();

		if (OperatingSystem.IsWindows())
		{
			SetupRegistry();
		}

		TaskIcon = new TaskIcon();
		TaskIcon.Initialize();

		// Initialize Steam
		steamClient = new SteamClient();

		manager = new CallbackManager(steamClient);
		manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
		manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
		manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

		manager.Subscribe<SteamUser.AccountInfoCallback>(OnAccountInfo);
		manager.Subscribe<SteamFriends.FriendsListCallback>(OnFriendsList);
		manager.Subscribe<SteamFriends.ProfileInfoCallback>(OnFriendProfileInfo);
		manager.Subscribe<SteamFriends.PersonaStateCallback>(OnFriendPersonaState);
		manager.Subscribe<SteamFriends.PersonaChangeCallback>(OnFriendPersonaChange);
		manager.Subscribe<SteamFriends.FriendMsgCallback>(OnFriendMsg);
		manager.Subscribe<SteamFriends.FriendMsgHistoryCallback>(OnFriendMsgHistory);
		manager.Subscribe<SteamApps.PICSProductInfoCallback>(OnPICSProductInfo);
		manager.Subscribe<SteamApps.LicenseListCallback>(LicenseListCallback);

		steamUser = steamClient.GetHandler<SteamUser>();
		steamApps = steamClient.GetHandler<SteamApps>();
		steamFriends = steamClient.GetHandler<SteamFriends>();
		steamContent = steamClient.GetHandler<SteamContent>();
		steamCloud = steamClient.GetHandler<SteamCloud>();
		steamPublishedFile = steamClient.GetHandler<SteamUnifiedMessages>().CreateService<PublishedFile>();

		Localization.Initialize("en");

		//if no cached login, show login window
		if (!AttemptCachedLogin())
		{
			loginWindowState = 1;
		}
	}

	void CreateDirectories()
	{
		Directory.CreateDirectory("appcache");
		Directory.CreateDirectory("appcache/librarycache");

		Directory.CreateDirectory("config");
		Directory.CreateDirectory("config/avatarcache");

		Directory.CreateDirectory("steamapps");
		Directory.CreateDirectory("steamapps/common");
	}

	void CheckIfAlreadyRunning()
	{
		Process[] processes = Process.GetProcessesByName("steam09");
		if (processes.Length > 1)
		{
			//check if the process has the same file path
			if (processes[0].MainModule.FileName == Process.GetCurrentProcess().MainModule.FileName)
			{
				Console.WriteLine("Steam09 is already running");
				Thread.Sleep(1000);
				Environment.Exit(0);
			}
		}
	}

	[SupportedOSPlatform("windows")]
	void SetupRegistry()
	{
		string steam09Path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

		Registry.LocalMachine.CreateSubKey("Software\\WOW6432Node\\Valve\\Steam").SetValue("InstallPath", steam09Path);
		Registry.LocalMachine.CreateSubKey("Software\\WOW6432Node\\Valve\\Steam").SetValue("SteamPID", Process.GetCurrentProcess().Id);

		Registry.CurrentUser.CreateSubKey("Software\\Classes\\steam\\Shell\\Open\\Command").SetValue("", $"{steam09Path}\\steam09.exe %1");

		Registry.CurrentUser.CreateSubKey("Software\\Valve\\Steam").SetValue("SteamExe", $"{steam09Path}\\steam09.exe");
		Registry.CurrentUser.CreateSubKey("Software\\Valve\\Steam").SetValue("SteamPath", steam09Path);

		Registry.CurrentUser.CreateSubKey("Software\\Valve\\Steam\\ActiveProcess").SetValue("pid", Process.GetCurrentProcess().Id);
		Registry.CurrentUser.CreateSubKey("Software\\Valve\\Steam\\ActiveProcess").SetValue("SteamClientDll", $"{steam09Path}\\steamclient.dll");
		Registry.CurrentUser.CreateSubKey("Software\\Valve\\Steam\\ActiveProcess").SetValue("SteamClientDll64", $"{steam09Path}\\steamclient64.dll");
	}

	void SetupGameEnvironmentVariables(int appID)
	{
		Environment.SetEnvironmentVariable("SteamAppId", appID.ToString());
		Environment.SetEnvironmentVariable("SteamGameId", appID.ToString());
	}

	public void Shutdown()
	{
		TaskIcon?.Cleanup();
		TTF.Quit();
		SDL.Quit();
	}

	public void ShowMainWindow()
	{
		//check if user is logged in
		if (CurrentUser == null)
		{
			return;
		}

		if (mainwindowState == 1) //if main window has already been shown, don't show it again
		{
			return;
		}

		foreach (SteamWindow window in Windows)
		{
			if (window is MainWindow)
			{
				window.FocusWindow();
				return;
			}
		}

		mainwindowState = 1;
	}

	public void QuitApplication()
	{
		Shutdown();
		Environment.Exit(0);
	}

	public void Loop()
	{
		while (true)
		{
			Update();
			Draw();

			SDL_Sharp.Event e;
			while (SDL.PollEvent(out e) != 0)
			{
				foreach (SteamWindow window in Windows)
				{
					window.HandleSDLEvent(e);
				}
			}

			manager.RunWaitCallbacks(System.TimeSpan.FromSeconds(0));
		}
	}

	public void Update()
	{
		now = (float)SDL.GetTicks64() / 1000f;
		float deltaTime = now - lastTime;
		lastTime = now;

		TaskIcon?.ProcessMessages();

		// Process any queued window creation requests from background threads
		SteamGuardAuthenticator.ProcessWindowCreationQueue();

		if (loginWindowState == 1)
		{
			PendingWindows.Add(new LoginWindow(this, Localization.GetString("Steam_Login_Title"), 420, 300, false));
			loginWindowState = 2;
		}

		if (mainwindowState == 1)
		{
			//hide logging in window
			foreach (SteamWindow window in Windows)
			{
				if (window is LoggingInWindow)
				{
					PendingWindowsToRemove.Add(window);
				}
			}

			//create main window
			PendingWindows.Add(new MainWindow(this, Localization.GetString("Steam_Root_Title").Replace("%account%", CurrentUser.AccountName), 1000, 660, true, 640, 480));
			mainwindowState = 2;
		}

		foreach (SteamWindow window in PendingWindows)
		{
			Windows.Add(window);
		}
		PendingWindows.Clear();

		List<SteamWindow> ActualWindowsToRemove = new List<SteamWindow>();
		foreach (SteamWindow window in PendingWindowsToRemove)
		{
			//check if window is already faded out
			if (window.windowOpacity <= 0.0f)
			{
				window.CloseWindow();
				Windows.Remove(window);
				ActualWindowsToRemove.Add(window);
				continue;
			}

			window.isFadingOut = true;
		}

		foreach (SteamWindow window in ActualWindowsToRemove)
		{
			PendingWindowsToRemove.Remove(window);
		}

		foreach (SteamWindow window in Windows)
		{
			window.Update(deltaTime);
		}

		//if no windows exists and the user is not logged in, quit
		if (Windows.Count == 0 && CurrentUser == null)
		{
			QuitApplication();
		}
	}

	public void Draw()
	{
		foreach (SteamWindow window in Windows)
		{
			window?.Draw();
		}
	}
}