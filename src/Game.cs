using Newtonsoft.Json.Linq;

public class Game
{
	public string Name { get; set; }
	public int AppID { get; set; }
	public GameStatus Status { get; set; } = GameStatus.NotInstalled;
	public float InstallProgress { get; set; } = 0; // set by ContentDownloader
	public JObject AppInfo { get; set; }

	public bool IsFavorite { get; set; } = false;

	public Game()
	{
	}

	public string GetStatusString()
	{
		switch (Status)
		{
			case GameStatus.NotInstalled:
				return "Not installed";
			case GameStatus.Downloading:
				return "Downloading: " + InstallProgress.ToString("F0") + "%";
			case GameStatus.Installed:
				return "100% - Ready";
			case GameStatus.UpdatePending:
				return "Update pending";
		}

		return "Unknown";
	}

	public string GetDeveloper()
	{
		if (AppInfo == null)
		{
			return "Unknown";
		}

		JToken developer = AppInfo["extended"]?["developer"];

		if (developer == null || developer.ToString() == "null")
		{
			return "Unknown";
		}

		return developer.ToString();
	}

	public string GetInstallDir()
	{
		if (AppInfo == null)
		{
			return AppID.ToString();
		}

		if (AppInfo["config"] == null)
		{
			Console.WriteLine($"{AppID} - AppInfo config is null");
			return AppID.ToString();
		}

		return AppInfo["config"]["installdir"].ToString();
	}

	/// <summary>
	/// Returns the estimated size of the game in bytes
	/// </summary>
	/// <returns>Estimated size in bytes</returns>
	public long GetEstimatedSize()
	{
		long TotalDownloadSize = 0; // in bytes

		if (AppInfo == null || AppInfo["depots"] == null)
		{
			return 0;
		}

		foreach (JProperty depot in AppInfo["depots"])
		{
			try
			{

				//if sharedinstall is 1, then ignore it
				if (depot.Value["sharedinstall"] != null && depot.Value["sharedinstall"].ToString() == "1")
				{
					continue;
				}

				//TODO: check what dlc user has
				//check if depot has dlcappid, if yes then ignore it for now
				if (depot.Value["dlcappid"] != null)
				{
					continue;
				}

				if (depot.Value["config"] != null)
				{
					//check if there is an oslist and if it contains windows, if not then ignore it
					if (depot.Value["config"]["oslist"] != null && !depot.Value["config"]["oslist"].ToString().Contains("windows"))
					{
						continue;
					}

					//check if there is a language in the config, if yes and it is not english, then ignore it
					if (depot.Value["config"]["language"] != null && depot.Value["config"]["language"].ToString() != "" && depot.Value["config"]["language"].ToString() != "english")
					{
						continue;
					}
				}


				//get the size of the depot
				JToken size = depot.Value["manifests"]["public"]["size"];
				TotalDownloadSize += long.Parse(size.ToString());
			}
			catch (Exception e)
			{
			}
		}

		return TotalDownloadSize;
	}

	//returns <executablePath, arguments, description>
	public List<Tuple<string, string, string>> GetLaunchConfigs()
	{
		List<Tuple<string, string, string>> launchConfigs = new List<Tuple<string, string, string>>();

		if (AppInfo == null)
		{
			return launchConfigs;
		}

		foreach (JObject launch in AppInfo["config"]["launch"])
		{
			try
			{
				//if this launch config is the only one, then add it
				if (AppInfo["config"]?["launch"]?.Count() == 1)
				{
					launchConfigs.Add(new Tuple<string, string, string>(launch["executable"].ToString(), launch["arguments"]?.ToString() ?? "", string.Empty));
					return launchConfigs;
				}

				//check if os matches
				if (launch["config"] != null && launch["config"]["oslist"] != null && !launch["config"]["oslist"].ToString().Contains("windows")) continue;

				//check if osarch matches
				if (launch["config"] != null && launch["config"]["osarch"] != null && launch["config"]["osarch"].ToString() != "64") continue;

				//TODO: allow betas
				//check if the betakey exists, if yes, ignore it
				if (launch["config"] != null && launch["config"]["betakey"] != null) continue;

				//check if the executable exists
				if (launch["executable"] == null) continue;

				launchConfigs.Add(new Tuple<string, string, string>(launch["executable"].ToString(), launch["arguments"]?.ToString() ?? "", launch["description"]?.ToString() ?? $"Play {Name}"));
			}
			catch (Exception e)
			{
				Console.WriteLine("Error getting launch config: " + e.Message);
			}
		}

		return launchConfigs;
	}

	//This should only be called on init
	public bool IsInstalled()
	{
		//check if the install dir exists
		string installDir = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "steamapps/common/" + GetInstallDir().ToLower()));
		if (!Directory.Exists(installDir)) return false;

		//check if <appid>.installed exists
		string installedFile = Path.Combine("steamapps", $"{AppID}.installed");
		if (!File.Exists(installedFile)) return false;

		return true;
	}
}