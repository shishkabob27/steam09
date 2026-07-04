using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using DepotDownloader;
using KGUI;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamKit2;

public partial class Steam
{
	public List<Game> Games = new List<Game>();

	List<Tuple<Process, Game>> runningGameProcesses = new List<Tuple<Process, Game>>();
	float timeSinceLastProcessCheck = 0;
	const float processCheckInterval = 1.5f;

	public DownloadManager DownloadManager = new DownloadManager();

	public LibraryDataCache LibraryDataCache = new LibraryDataCache();

	private async void LicenseListCallback(SteamApps.LicenseListCallback licenseList)
	{
		if (licenseList.Result != EResult.OK)
		{
			Console.WriteLine("Unable to get license list: {0} ", licenseList.Result);
			//TODO: read from cache if available
			return;
		}

		Console.WriteLine("Got {0} licenses for account! Waiting for package info...", licenseList.LicenseList.Count);
		LibraryDataCache.AppLicenses = licenseList.LicenseList;

		List<SteamApps.PICSRequest> PackageRequests = new List<SteamApps.PICSRequest>();
		foreach (var license in licenseList.LicenseList)
		{
			if (!LibraryDataCache.PackageTokens.ContainsKey(license.PackageID))
			{
				LibraryDataCache.PackageTokens.Add(license.PackageID, license.AccessToken);
			}
			PackageRequests.Add(new SteamApps.PICSRequest(license.PackageID, license.AccessToken));
		}

		var packageInfo = await steamApps.PICSGetProductInfo([], PackageRequests);

		if (!packageInfo.Complete) 
		{
			throw new Exception("Package info response incomplete: ");
			//TODO: handle incomplete response
		}

		List<uint> appids = new List<uint>();

		foreach (var appResult in packageInfo.Results)
		{
			foreach (var app in appResult.Packages)
			{
				if (app.Value != null)
				{
					LibraryDataCache.PackageInfo[app.Value.ID] = app.Value;
					if (app.Value.KeyValues["common"]["appids"] != null)
					{
						KeyValue appidsToken = app.Value.KeyValues["appids"];
						foreach (var appid in appidsToken.Children)
						{
							appids.Add(appid.AsUnsignedInteger());
						}
					}
				}
			}
		}

		Console.WriteLine($"Retrieved info for {appids.Distinct().Count()} unique apps. Waiting for access tokens...");

		//get access tokens for apps
		var accessTokens = await steamApps.PICSGetAccessTokens(appids, []);
		foreach (var token in accessTokens.AppTokens)
		{
			LibraryDataCache.AppTokens[token.Key] = token.Value;
		}

		Console.WriteLine("Got access tokens for apps! Waiting for app info...");

		var appInfo = await steamApps.PICSGetProductInfo(appids.Select(id => new SteamApps.PICSRequest(id, LibraryDataCache.AppTokens.TryGetValue(id, out ulong token) ? token : 0)).ToList(), []);

		List<uint> allRetrievedAppIds = [];

		foreach (var appResult in appInfo.Results)
		{
			foreach (var app in appResult.Apps)
			{
				if (app.Value != null)
				{
					LibraryDataCache.AppInfo[app.Value.ID] = app.Value;
					allRetrievedAppIds.Add(app.Value.ID);

					//update app if it exist
					if (Games.Any(g => g.AppID == app.Value.ID))
					{
						Games.Find(g => g.AppID == app.Value.ID)?.ParseAppInfo(app.Value.KeyValues);
						continue;
					}
					else
					{						
						Game game = new()
						{ 
							AppID = app.Value.ID, 
						};
						game.ParseAppInfo(app.Value.KeyValues);
						Games.Add(game);
					}
				}
			}
		}

		//save to users games.json
		string gamesJson = JsonConvert.SerializeObject(allRetrievedAppIds, Formatting.None);
		File.WriteAllText(Utils.GetAbsolutePath($"userdata/{steamClient?.SteamID?.ConvertToUInt64() ?? 0}/owned_apps.json"), gamesJson);

		foreach (Game game in Games)
		{
			game.Status = game.IsInstalled() ? GameStatus.Installed : GameStatus.NotInstalled;
			game.RefreshPartialDownloadState();
		}

		Console.WriteLine("App info retrieved for all apps! Waiting for main window.");

		if (mainwindowState == 0) // if main window is not loaded, load it
		{
			mainwindowState = 1;
		}
	}

	//Will launch the game if there is only one launch config, otherwise it will show the launch options window
	public void StartGame(Game game)
	{
		if (game.LaunchConfigs.Count == 0)
		{
			Console.WriteLine("No launch config found");
			return;
		}

		//if there is only one launch config, launch it directly
		if (game.LaunchConfigs.Count == 1)
		{
			BeginLaunchGame(game, game.LaunchConfigs[0]);
			return;
		}

		game.LaunchAppTaskStatus = LaunchAppTaskStatus.ShowLaunchOption;
		LaunchOptionsWindow launchOptionsWindow = new(this, "launchoptionswindow_" + game.AppID);
		launchOptionsWindow.SetTitle(Localization.GetString("Steam_GameLaunchOptions_Title").Replace("%game%", game.Name));
		launchOptionsWindow.SetGame(game);
		launchOptionsWindow.SetLaunchConfigs(game.LaunchConfigs);
		WindowManager.Instance.CreateWindow(launchOptionsWindow);
	}

	public void BeginLaunchGame(Game game, LaunchConfig launchConfig)
	{
		game.LaunchAppTaskStatus = LaunchAppTaskStatus.DelayLaunch;
		PreparingToLaunchWindow preparingToLaunchWindow = new PreparingToLaunchWindow(this, "preparingtolaunchwindow_" + game.AppID);
		preparingToLaunchWindow.SetTitle(Localization.GetString("Steam_GameLaunchOptions_Title").Replace("%game%", game.Name));
		preparingToLaunchWindow.SetGame(game, launchConfig);
		WindowManager.Instance.CreateWindow(preparingToLaunchWindow);
	}

	public void OnSelectedLaunchConfigChanged(Game game, LaunchConfig launchConfig)
	{
		game.LaunchAppTaskStatus = LaunchAppTaskStatus.RunningInstallScript;
		RunInstallScripts(game);
	}

	public async Task RunInstallScripts(Game game)
	{
		if (Environment.OSVersion.Platform != PlatformID.Win32NT)
		{
			CompleteInstallScript(game);
			return;
		}

		KeyValue? appInfo = LibraryDataCache.GetAppInfo(game.AppID);
		if (appInfo == null)
		{
			CompleteInstallScript(game);
			return;
		}

		var installScript = appInfo["install"];
		if (installScript == KeyValue.Invalid)
		{
			CompleteInstallScript(game);
			return;
		}
		
		var RunProcesses = installScript["Run Process"];
		if (RunProcesses != KeyValue.Invalid)
		{
			foreach (var runProcess in RunProcesses.Children)
			{
				string? ProcessName = runProcess.Name;
				if (ProcessName == null)
					continue;

				bool installRequired = false;

				var preReqRunKey = runProcess["HasRunKey"];
				string? preReqRun = null;
				if (preReqRunKey != KeyValue.Invalid)
				{
					preReqRun = preReqRunKey.AsString();
					if (!string.IsNullOrEmpty(preReqRun))
					{
						preReqRun = preReqRun.Replace("HKEY_LOCAL_MACHINE\\", "", StringComparison.OrdinalIgnoreCase);
						installRequired = !GetRunKeyRegistryValue(preReqRun, ProcessName);
					}
				}

				if (!installRequired)
					continue;

				game.LaunchAppTaskStatus = LaunchAppTaskStatus.RunningInstallScript;

				var processKey = runProcess["Process 1"];
				if (processKey == KeyValue.Invalid)
					continue;
				string? processPath = processKey.AsString();
				if (string.IsNullOrEmpty(processPath))
					continue;
				processPath = InsertEnvironmentVariables(processPath, game);

				string? arguments = "";
				var argumentsKey = runProcess["Command 1"];
				if (argumentsKey != KeyValue.Invalid)
					arguments = argumentsKey.AsString() ?? "";
				arguments = InsertEnvironmentVariables(arguments, game);

				Console.WriteLine($"{ProcessName}: Running process: {processPath} with arguments: {arguments}");

				int installSuccess = RunInstallProcess(processPath, arguments);

				//set run key value to 1
				if (!string.IsNullOrEmpty(preReqRun))
				{
					Registry.LocalMachine.SetValue($"{preReqRun}" + ProcessName, installSuccess == 0 ? "1" : "0", RegistryValueKind.DWord);
				}
			}
		}

		CompleteInstallScript(game);
	}

	void CompleteInstallScript(Game game)
	{
		game.LaunchAppTaskStatus = LaunchAppTaskStatus.DelayLaunch;
	}

	static void SetRunKeyRegistryValue(string preReqRun, string processName, int installSuccess)
	{
		Registry.LocalMachine.SetValue($"{preReqRun}" + processName, installSuccess == 0 ? "1" : "0", RegistryValueKind.DWord);
	}

	static bool GetRunKeyRegistryValue(string preReqRun, string processName)
	{
		RegistryView[] viewsToTry = { RegistryView.Registry64, RegistryView.Registry32 };
		foreach (var view in viewsToTry)
		{
			if (RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view)?.OpenSubKey(preReqRun)?.GetValue(processName)?.ToString() == "1")
			{
				return true;
			}
		}
		return false;
	}

	static int RunInstallProcess(string processPath, string arguments)
	{
		ProcessStartInfo startInfo = new()
		{
			FileName = processPath,
			Arguments = arguments,
			WindowStyle = ProcessWindowStyle.Normal,
			UseShellExecute = false
		};

		try
		{
			Process? process = Process.Start(startInfo);
			if (process != null)
			{
				process.WaitForExit();
				return process.ExitCode;
			}
			return 1;
		}
		catch (Exception e)
		{
			Console.WriteLine("Failed to start process, attempting to start with shell execute: " + e.Message);
			
			//attempt to start with shell execute
			startInfo.UseShellExecute = true;
			try
			{
				Process? process = Process.Start(startInfo);
				if (process != null)
				{
					process.WaitForExit();
					return process.ExitCode;
				}
				return 1;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Failed to start process with shell execute: " + ex.Message);
				Console.WriteLine(ex.StackTrace);
				return 1;
			}
		}
	}

	static string InsertEnvironmentVariables(string? text, Game game)
	{
		if (string.IsNullOrEmpty(text)) return string.Empty;

		text = text.Replace("%INSTALLDIR%", Utils.GetAbsolutePath("steamapps/common/" + game.InstallFolderName), StringComparison.OrdinalIgnoreCase);
		text = text.Replace("%ROOTDRIVE%", Environment.SystemDirectory?[0].ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
		text = text.Replace("%APPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StringComparison.OrdinalIgnoreCase);
		text = text.Replace("%USER_MYDOCS%", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), StringComparison.OrdinalIgnoreCase);
		text = text.Replace("%COMMON_MYDOCS%", Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), StringComparison.OrdinalIgnoreCase);
		text = text.Replace("%LOCAL_APPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StringComparison.OrdinalIgnoreCase);
		text = text.Replace("%WinDir%", Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase);
		text = text.Replace("%STEAMPATH%", Utils.GetAbsolutePath(""), StringComparison.OrdinalIgnoreCase);
		return text;
	}

	//Will launch the game with the given launch config
	public async void LaunchGameProcess(Game game, LaunchConfig launchConfig)
	{
		game.LaunchAppTaskStatus = LaunchAppTaskStatus.CreatingProcess;
		SetupGameEnvironmentVariables(game.AppID);

		Console.WriteLine($"Launching \"{Utils.GetAbsolutePath("steamapps/common/" + game.InstallFolderName + "/" + launchConfig.Executable)}\" with arguments: \"{launchConfig.Arguments}\"");

		ProcessStartInfo startInfo = new ProcessStartInfo();

		//Get absolute path of install dir
		string installDir = Utils.GetAbsolutePath("steamapps/common/" + game.InstallFolderName);
		startInfo.WorkingDirectory = installDir + Path.DirectorySeparatorChar + launchConfig.WorkingDirectory;
		startInfo.FileName = installDir + "/" + launchConfig.Executable;
		startInfo.Arguments = launchConfig.Arguments;
		startInfo.WindowStyle = ProcessWindowStyle.Normal;
		startInfo.UseShellExecute = false;

		try
		{
			Process? process = Process.Start(startInfo);
			if (process != null)
			{
				runningGameProcesses.Add(new Tuple<Process, Game>(process, game));
				game.LaunchAppTaskStatus = LaunchAppTaskStatus.Completed;
			}
		}
		catch (Exception e)
		{
			Console.WriteLine("Failed to start game: " + e.Message);
			Console.WriteLine(e.StackTrace);
		}
	}

	void WatchGameProcesses(float deltaTime)
	{
		timeSinceLastProcessCheck += deltaTime;
		if (timeSinceLastProcessCheck < processCheckInterval) return;

		timeSinceLastProcessCheck = 0;

		for (int i = runningGameProcesses.Count - 1; i >= 0; i--)
		{
			if (runningGameProcesses[i].Item1.HasExited)
			{
				runningGameProcesses[i].Item2.LaunchAppTaskStatus = LaunchAppTaskStatus.None;
				runningGameProcesses.RemoveAt(i);
			}
		}
	}
}