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

	public ReadOnlyCollection<SteamApps.LicenseListCallback.License> AppLicenses { get; private set; }
	public Dictionary<uint, ulong> PackageTokens { get; } = [];

	private void LicenseListCallback(SteamApps.LicenseListCallback licenseList)
	{
		if (licenseList.Result != EResult.OK)
		{
			Console.WriteLine("Unable to get license list: {0} ", licenseList.Result);

			return;
		}

		Console.WriteLine("Got {0} licenses for account!", licenseList.LicenseList.Count);
		AppLicenses = licenseList.LicenseList;

		foreach (var license in licenseList.LicenseList)
		{
			if (license.AccessToken > 0)
			{
				PackageTokens.TryAdd(license.PackageID, license.AccessToken);
			}
		}
	}

	public async Task DownloadGame(int appID)
	{
		Console.WriteLine("Downloading game: " + appID);
		//check if any game is downloading
		//TODO: curently downloading multiple games can cause files to download in other games install directory
		if (Games.Any(g => g.Status == GameStatus.Downloading))
		{
			Console.WriteLine("Another game is already downloading");
			return;
		}

		Game game = Games.Find(g => g.AppID == appID);
		if (game == null)
		{
			Console.WriteLine("Game not found");
			return;
		}

		if (game.Status == GameStatus.Installed)
		{
			Console.WriteLine("Game is already installed");
			return;
		}
		else if (game.Status == GameStatus.UpdatePending)
		{
			Console.WriteLine("Game is already updating");
			return;
		}
		else if (game.Status == GameStatus.Downloading)
		{
			Console.WriteLine("Game is already downloading");
			return;
		}

		game.Status = GameStatus.Downloading;

		string installDir = Path.Combine("steamapps", "common", game.GetInstallDir());
		ContentDownloader.Config.InstallDirectory = installDir;

		try
		{
			await ContentDownloader.DownloadAppAsync((uint)appID, new List<(uint, ulong)> { }, "public", Util.GetSteamOS(), Util.GetSteamArch(), "english", false, false, game);
			game.Status = GameStatus.Installed;

			//create dummy file to indicate game is installed
			File.Create(Path.Combine("steamapps", $"{appID}.installed"));
		}
		catch (Exception e)
		{
			game.Status = GameStatus.NotInstalled;
			Console.WriteLine("Failed to download game: " + e.Message);
			Console.WriteLine(e.StackTrace);
		}

		//reload game list
		MainWindow? mainWindow = WindowManager.Instance.GetWindows().Find(w => w is MainWindow) as MainWindow;
		mainWindow?.ReloadGameList = true;
	}


	//Will launch the game if there is only one launch config, otherwise it will show the launch options window
	public void StartGame(Game game)
	{
		List<Tuple<string, string, string>> launchConfigs = game.GetLaunchConfigs();
		if (launchConfigs.Count == 0)
		{
			Console.WriteLine("No launch config found");
			return;
		}

		//if there is only one launch config, launch it directly
		if (launchConfigs.Count == 1)
		{
			BeginLaunchGame(game, launchConfigs[0]);
			return;
		}

		LaunchOptionsWindow launchOptionsWindow = new(this, "launchoptionswindow_" + game.AppID);
		launchOptionsWindow.SetTitle(Localization.GetString("Steam_GameLaunchOptions_Title").Replace("%game%", game.Name));
		launchOptionsWindow.SetGame(game);
		launchOptionsWindow.SetLaunchConfigs(launchConfigs);
		WindowManager.Instance.CreateWindow(launchOptionsWindow);
	}

	public void BeginLaunchGame(Game game, Tuple<string, string, string> launchConfig)
	{
		PreparingToLaunchWindow preparingToLaunchWindow = new PreparingToLaunchWindow(this, "preparingtolaunchwindow_" + game.AppID);
		preparingToLaunchWindow.SetTitle(Localization.GetString("Steam_GameLaunchOptions_Title").Replace("%game%", game.Name));
		preparingToLaunchWindow.SetGame(game);
		preparingToLaunchWindow.SetLaunchConfig(launchConfig);
		WindowManager.Instance.CreateWindow(preparingToLaunchWindow);
	}

	//Will launch the game with the given launch config
	public async void LaunchGameProcess(Game game, Tuple<string, string, string> launchConfig)
	{
		SetupGameEnvironmentVariables(game.AppID);

		Console.WriteLine($"Launching \"{Path.GetFullPath(Environment.CurrentDirectory + "/steamapps/common/" + game.GetInstallDir().ToLower() + "/" + launchConfig.Item1)}\" with arguments: \"{launchConfig.Item2}\"");

		ProcessStartInfo startInfo = new ProcessStartInfo();

		//Get absolute path of install dir
		string installDir = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "steamapps/common/" + game.GetInstallDir().ToLower()));
		startInfo.WorkingDirectory = installDir;
		startInfo.FileName = installDir + "/" + launchConfig.Item1;
		startInfo.Arguments = launchConfig.Item2;
		startInfo.WindowStyle = ProcessWindowStyle.Normal;
		startInfo.UseShellExecute = false;

		try
		{
			Process.Start(startInfo);
		}
		catch (Exception e)
		{
			Console.WriteLine("Failed to start game: " + e.Message);
			Console.WriteLine(e.StackTrace);
		}
	}

	async void GetGames()
	{
		//make http request to get games
		string response;
		HttpClient client = new HttpClient();
		client.DefaultRequestHeaders.Add("User-Agent", "steam09");

		try
		{
			Console.WriteLine("Retrieving games from steam");
			response = await client.GetStringAsync($"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?steamid={steamUser.SteamID.ConvertToUInt64()}&include_appinfo=true&key=" + CurrentUser.WebAPIKey);
		}
		catch (Exception e)
		{
			Console.WriteLine("Failed to get games: " + e.Message);

			Console.WriteLine("Falling back to cache");
			//check if cache exists
			if (System.IO.File.Exists("appcache/games.json"))
			{
				response = System.IO.File.ReadAllText("appcache/games.json");
			}
			else
			{
				Console.WriteLine("No cache found");
				response = "{}";
			}
		}

		//save cache
		Console.WriteLine("Saving game list to cache");
		System.IO.File.WriteAllText("appcache/games.json", response);

		//parse json
		dynamic games = JsonConvert.DeserializeObject(response);
		if (games?.response?.games == null)
		{
			Console.WriteLine("No games found");
			mainwindowState = 1; // start main window
			return;
		}

		foreach (var game in games.response.games)
		{
			Games.Add(new Game
			{
				Name = game.name,
				AppID = game.appid,
			});

			Directory.CreateDirectory("appcache/librarycache/" + game.appid);
		}

		//get app info
		try
		{
			List<SteamApps.PICSRequest> appRequests = new List<SteamApps.PICSRequest>();
			foreach (Game game in Games)
			{
				//check if game has app_info.json, if yes then skip it
				if (System.IO.File.Exists($"appcache/librarycache/{game.AppID}/app_info.json"))
				{
					//parse app_info.json
					string cachedAppInfo = System.IO.File.ReadAllText($"appcache/librarycache/{game.AppID}/app_info.json");
					game.AppInfo = JObject.Parse(cachedAppInfo);
				}
				else
				{
					appRequests.Add(new SteamApps.PICSRequest((uint)game.AppID));
				}
			}

			if (appRequests.Count > 0)
			{
				Console.WriteLine("Getting app info for " + appRequests.Count + " games");

				var appInfo = await steamApps.PICSGetProductInfo(appRequests, []);

				foreach (var appResult in appInfo.Results)
				{
					foreach (var app in appResult.Apps)
					{
						if (app.Value != null)
						{
							//write a readable app info to file
							string appInfoString = Utils.SerializeAppInfoFileReadable(app.Value);
							System.IO.File.WriteAllText($"appcache/librarycache/{app.Value.ID}/app_info.json", appInfoString);
							Games.Find(g => g.AppID == app.Value.ID).AppInfo = JObject.Parse(appInfoString);
						}
					}
				}
			}
		}
		catch (Exception e)
		{
			Console.WriteLine("Failed to get app info: " + e.Message);
			Console.WriteLine(e.StackTrace);
		}

		//check if games are installed
		foreach (Game game in Games)
		{
			game.Status = game.IsInstalled() ? GameStatus.Installed : GameStatus.NotInstalled;
		}

		mainwindowState = 1;
	}
}