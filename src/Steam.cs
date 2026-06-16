using System.Diagnostics;
using System.Runtime.Versioning;
using KGUI;
using Microsoft.Win32;
using SDL;
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
		SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_EVENTS | SDL_InitFlags.SDL_INIT_AUDIO);
		SDL3_ttf.TTF_Init();

		if (OperatingSystem.IsWindows())
		{
			SetupRegistry();
			TaskIcon = new TaskIcon();
			TaskIcon.Initialize();
		}

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
		SDL3_ttf.TTF_Quit();
		SDL3.SDL_Quit();
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
		if (WindowManager.Instance.GetWindows().OfType<MainWindow>().Any()) //if main window has already been shown, don't show it again
		{
			WindowManager.Instance.HighlightWindow<MainWindow>();
			return;
		}

		//create it next loop
		mainwindowState = 1;
	}

	public void QuitApplication()
	{
		Shutdown();
		Environment.Exit(0);
	}

	public void Loop()
	{
		const int targetFPS = 60;
		const float targetFrameTime = 1.0f / targetFPS;
		float frameStartTime;

		while (true)
		{
			Update();
			Draw();

			unsafe
			{
				SDL_Event e;
				while (SDL3.SDL_PollEvent(&e))
				{
					foreach (BaseWindow window in WindowManager.Instance.GetWindows())
					{
						window.HandleSDLEvent(e);
					}
				}
			}
		}
	}

	public void Update()
	{
		UIThread.ProcessPending();

		now = SDL3.SDL_GetTicks() / 1000f;
		float deltaTime = now - lastTime;
		lastTime = now;

		TaskIcon?.ProcessMessages();

		manager.RunWaitCallbacks(System.TimeSpan.FromSeconds(0));

		// Process any queued window creation requests from background threads
		SteamGuardAuthenticator.ProcessWindowCreationQueue();

		if (loginWindowState == 1)
		{
			WindowManager.Instance.CreateWindow(new LoginWindow(this, "login_window"));
			loginWindowState = 2;
		}

		if (mainwindowState == 1)
		{
			//hide logging in window
			WindowManager.Instance.CloseWindow<LoggingInWindow>();

			// //create main window
			WindowManager.Instance.CreateWindow(new MainWindow(this, "main_window"));
			mainwindowState = 2;
		}

		WindowManager.Instance.Update(deltaTime);

		//if no windows exists and the user is not logged in, quit
		if (WindowManager.Instance.GetWindows().Count == 0 && CurrentUser == null)
		{
			QuitApplication();
		}
	}

	public void Draw()
	{
		WindowManager.Instance.Draw();
	}
}