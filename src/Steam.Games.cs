using System.Collections.ObjectModel;
using System.Diagnostics;
using DepotDownloader;
using KGUI;
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

	public ReadOnlyCollection<SteamApps.LicenseListCallback.License> AppLicenses { get; private set; }
	public Dictionary<uint, ulong> PackageTokens { get;  set; } = [];
	public Dictionary<uint, ulong> AppTokens { get;  set; } = new Dictionary<uint, ulong>();

	private async void LicenseListCallback(SteamApps.LicenseListCallback licenseList)
	{
		if (licenseList.Result != EResult.OK)
		{
			Console.WriteLine("Unable to get license list: {0} ", licenseList.Result);
			//TODO: read from cache if available
			return;
		}

		Console.WriteLine("Got {0} licenses for account! Waiting for package info...", licenseList.LicenseList.Count);
		AppLicenses = licenseList.LicenseList;

		List<SteamApps.PICSRequest> PackageRequests = new List<SteamApps.PICSRequest>();
		foreach (var license in licenseList.LicenseList)
		{
			if (!PackageTokens.ContainsKey(license.PackageID))
			{
				PackageTokens.Add(license.PackageID, license.AccessToken);
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
			AppTokens[token.Key] = token.Value;
		}

		Console.WriteLine("Got access tokens for apps! Waiting for app info...");

		var appInfo = await steamApps.PICSGetProductInfo(appids.Select(id => new SteamApps.PICSRequest(id, AppTokens.TryGetValue(id, out ulong token) ? token : 0)).ToList(), []);

		List<string> allRetrievedAppIds = new List<string>();

		foreach (var appResult in appInfo.Results)
		{
			foreach (var app in appResult.Apps)
			{
				if (app.Value != null)
				{
					allRetrievedAppIds.Add(app.Value.ID.ToString());

					Directory.CreateDirectory(Utils.GetAbsolutePath($"appcache/librarycache/{app.Value.ID}"));
					app.Value.KeyValues.SaveToFile(Utils.GetAbsolutePath($"appcache/librarycache/{app.Value.ID}/appinfo.vdf"), false);

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
							AppID = (int)app.Value.ID, 
						};
						game.ParseAppInfo(app.Value.KeyValues);
						Games.Add(game);
					}
				}
			}
		}

		//save to users games.json
		string gamesJson = JsonConvert.SerializeObject(allRetrievedAppIds, Formatting.None);
		File.WriteAllText(Utils.GetAbsolutePath($"userdata/{steamClient?.SteamID?.ConvertToUInt64() ?? 0}/games.json"), gamesJson);

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

		LaunchOptionsWindow launchOptionsWindow = new(this, "launchoptionswindow_" + game.AppID);
		launchOptionsWindow.SetTitle(Localization.GetString("Steam_GameLaunchOptions_Title").Replace("%game%", game.Name));
		launchOptionsWindow.SetGame(game);
		launchOptionsWindow.SetLaunchConfigs(game.LaunchConfigs);
		WindowManager.Instance.CreateWindow(launchOptionsWindow);
	}

	public void BeginLaunchGame(Game game, LaunchConfig launchConfig)
	{
		PreparingToLaunchWindow preparingToLaunchWindow = new PreparingToLaunchWindow(this, "preparingtolaunchwindow_" + game.AppID);
		preparingToLaunchWindow.SetTitle(Localization.GetString("Steam_GameLaunchOptions_Title").Replace("%game%", game.Name));
		preparingToLaunchWindow.SetGame(game);
		preparingToLaunchWindow.SetLaunchConfig(launchConfig);
		WindowManager.Instance.CreateWindow(preparingToLaunchWindow);
	}

	//Will launch the game with the given launch config
	public async void LaunchGameProcess(Game game, LaunchConfig launchConfig)
	{
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
				game.IsRunning = true;
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
				runningGameProcesses[i].Item2.IsRunning = false;
				runningGameProcesses.RemoveAt(i);
			}
		}
	}
}