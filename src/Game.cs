using DepotDownloader;
using Newtonsoft.Json.Linq;
using SteamKit2;

public class Game
{
	public string Name { get; set; }
	public int AppID { get; set; }
	public string Type { get; set; }
	public GameStatus Status { get; set; } = GameStatus.NotInstalled;
	public DownloadStatus DownloadStatus { get; set; } = DownloadStatus.None;
	public float InstallProgress { get; set; } = 0; // set by ContentDownloader
	public bool HasPartialDownload { get; set; } = false;
	public bool IsRunning { get; set; } = false;

	public string Developer { get; set; } = "Unknown";
	public string InstallFolderName { get; set; } = string.Empty;
	public ulong EstimatedSize { get; set; } = 0; // in bytes, calculated from app info depots

	public List<LaunchConfig> LaunchConfigs { get; set; } = new List<LaunchConfig>();

	public string Homepage { get; set; } = string.Empty;
	public Tuple<string, string> ManualUrl { get; set; } = new Tuple<string, string>(string.Empty, string.Empty); // display name, url

	public string IconUrl { get; set; } = string.Empty;

	public bool IsFavorite { get; set; } = false;

	public Game()
	{
	}

	public void ParseAppInfo(KeyValue appInfo)
	{
		//name
		if (appInfo["common"] != null && appInfo["common"]["name"] != null)
		{
			Name = appInfo["common"]["name"].Value ?? $"App {AppID}";
		}
		else
		{
			Name = $"App {AppID}";
		}

		//developer
		if (appInfo["extended"] != null && appInfo["extended"]["developer"] != null)
		{
			Developer = appInfo["extended"]["developer"].Value ?? "Unknown";
			if (Developer == "null") Developer = "Unknown";
			if (string.IsNullOrEmpty(Developer)) Developer = "Unknown";
		}

		//type
		if (appInfo["common"] != null && appInfo["common"]["type"] != null)
		{
			Type = appInfo["common"]["type"].Value ?? "Unknown";
			if (Type == "null") Type = "Unknown";
			if (string.IsNullOrEmpty(Type)) Type = "Unknown";
		}
		else
		{
			Type = "Unknown";
		}

		//install folder name
		if (appInfo["config"] != null && appInfo["config"]["installdir"] != null)
		{
			InstallFolderName = appInfo["config"]["installdir"].Value ?? AppID.ToString();
			if (InstallFolderName == "null") InstallFolderName = AppID.ToString();
		}

		EstimatedSize = GetEstimatedSize(appInfo);

		LaunchConfigs = GetLaunchConfigs(appInfo);

		//homepage
		if (appInfo["extended"] != null && appInfo["extended"]["homepage"] != null)
		{
			Homepage = appInfo["extended"]["homepage"].Value ?? string.Empty;
			if (Homepage == "null") Homepage = string.Empty;
		}

		//manual
		if (appInfo["extended"] != null && appInfo["extended"]["gamemanualurl"] != null)
		{
			string url = appInfo["extended"]["gamemanualurl"].Value ?? string.Empty;
			if (string.IsNullOrEmpty(url))
			{
				ManualUrl = new Tuple<string, string>(Localization.GetString("Steam_Game_NoManual"), null);
			}
			else
			{
				if (url == "null") url = string.Empty;
				ManualUrl = new Tuple<string, string>(Localization.GetString("Steam_Game_DefaultManual").Replace("%game%", Name), url);
			}
		}

		//icon url
		if (appInfo["common"] != null && appInfo["common"]["icon"] != null)
		{
			string iconHash = appInfo["common"]["icon"].Value ?? string.Empty;
			if (iconHash != "null" && !string.IsNullOrEmpty(iconHash))
			{
				IconUrl = $"https://cdn.cloudflare.steamstatic.com/steamcommunity/public/images/apps/{AppID}/{iconHash}.jpg";
			}
		}
	}

	public string GetStatusString()
	{
		if (IsRunning)
		{
			return "Running";
		}
		switch (Status)
		{
			case GameStatus.NotInstalled:
				return HasPartialDownload ? "Paused" : Localization.GetString("Steam_NotInstalled");
			case GameStatus.Queued:
				return "Queued";
			case GameStatus.Downloading:
				return "Downloading: " + InstallProgress.ToString("F0") + "%";
			case GameStatus.Installed:
				return Localization.GetString("Steam_GameReady");
			case GameStatus.UpdatePending:
				return "Update pending";
		}

		return "Unknown";
	}


	/// <summary>
	/// Returns the estimated size of the game in bytes
	/// </summary>
	/// <returns>Estimated size in bytes</returns>
	ulong GetEstimatedSize(KeyValue appInfo)
	{
		ulong TotalDownloadSize = 0; // in bytes

		if (appInfo["depots"] == null)
		{
			return 0;
		}

		foreach (KeyValue depot in appInfo["depots"].Children)
		{
			try
			{				
				//if sharedinstall is 1, then ignore it, shardinstall 2 still count but we should check if that depot is already installed
				//eg. goldsrc games
				if (depot["sharedinstall"] != null && depot["sharedinstall"].AsInteger() == 1)
				{
					continue;
				}

				//TODO: check what dlc user has
				if (depot["dlcappid"] != null && depot["dlcappid"].AsInteger() != 0)
				{
					continue;
				}

				if (depot["config"] != null)
				{
					string oslist = depot["config"]["oslist"]?.AsString() ?? "";
					if (!string.IsNullOrEmpty(oslist) && !oslist.ToLower().Contains(Util.GetSteamOS())) // if there is no oslist, then we assume it is for all OSs
					{
						continue;
					}

					string language = depot["config"]["language"]?.AsString() ?? "";
					if (!string.IsNullOrEmpty(language) && language.ToLower() != "english") // if there is no language, then we assume it is for all languages
					{
						continue;
					}
				}

				//get the size of the depot
				ulong size = depot["manifests"]["public"]["size"].AsUnsignedLong();
				TotalDownloadSize += size;
			}
			catch (Exception e)
			{
				Console.WriteLine($"Error calculating size for depot {depot.Name}: " + e.Message + "\n" + e.StackTrace);
			}
		}

		return TotalDownloadSize;
	}

	//returns <executablePath, arguments, description>
	public List<LaunchConfig> GetLaunchConfigs(KeyValue appInfo)
	{
		List<LaunchConfig> launchConfigs = new List<LaunchConfig>();

		if (appInfo["config"] == null)
		{
			Console.WriteLine($"GetLaunchConfigs: AppInfo config is null for {Name}");
			return launchConfigs;
		}

		foreach (KeyValue launch in appInfo["config"]["launch"].Children)
		{
			try
			{
				//if this launch config is the only one, then add it
				if (appInfo["config"]?["launch"]?.Children.Count == 1)
				{
					launchConfigs.Add(new LaunchConfig(launch["executable"].AsString() ?? "", launch["arguments"]?.AsString() ?? "", launch["workingdir"]?.AsString() ?? "", string.Empty));
					return launchConfigs;
				}

				//check if os matches
				if (launch["config"] != null)
				{
					string oslist = launch["config"]["oslist"]?.AsString() ?? "";
					string osarch = launch["config"]["osarch"]?.AsString() ?? "";
					string betakey = launch["config"]["betakey"]?.AsString() ?? "";
					
					if (!string.IsNullOrEmpty(oslist) && !oslist.Contains(Util.GetSteamOS())) continue;

					//check if osarch matches
					if (!string.IsNullOrEmpty(osarch) && osarch.ToLower() != Util.GetSteamArch()) continue;

					//TODO: allow betas
					//check if the betakey exists, if yes, ignore it
					if (!string.IsNullOrEmpty(betakey)) continue;
				}

				//check if the executable exists
				if (launch["executable"] == null || string.IsNullOrEmpty(launch["executable"].AsString())) continue;

				launchConfigs.Add(new LaunchConfig(launch["executable"].AsString() ?? "", launch["arguments"]?.AsString() ?? "", launch["workingdir"]?.AsString() ?? "", launch["description"]?.AsString() ?? Localization.GetString("Steam_LaunchOption_Game").Replace("%game%", Name)));
			}
			catch (Exception e)
			{
				Console.WriteLine("Error getting launch config: " + e.Message);
			}
		}

		return launchConfigs;
	}

	public bool HasPartialDownloadFiles()
	{
		return AppManifest.HasPartialInstall(AppID, InstallFolderName);
	}

	public void RefreshPartialDownloadState()
	{
		HasPartialDownload = !IsInstalled() && HasPartialDownloadFiles();
	}

	//This should only be called on init
	public bool IsInstalled()
	{
		return AppManifest.IsAppInstalled(AppID, InstallFolderName);
	}
}

public class LaunchConfig
{
	public string Executable { get; set; }
	public string Arguments { get; set; }
	public string WorkingDirectory { get; set; }
	public string Description { get; set; }

	public LaunchConfig(string executable, string arguments, string workingDirectory, string description)
	{
		Executable = executable;
		Arguments = arguments;
		WorkingDirectory = workingDirectory;
		Description = description;
	}
}