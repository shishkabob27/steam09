using SteamKit2;

public static class AppManifest
{
	const string StateFlagsInstalled = "4";
	const string StateFlagsDownloading = "1026";

	public static string GetPath(int appId) =>
		Utils.GetAbsolutePath(Path.Combine("steamapps", $"appmanifest_{appId}.acf"));

	public static bool Exists(int appId) => File.Exists(GetPath(appId));

	public static KeyValue? TryLoad(int appId)
	{
		var path = GetPath(appId);
		if (!File.Exists(path))
		{
			return null;
		}

		return KeyValue.LoadAsText(path);
	}

	public static void Delete(int appId)
	{
		var path = GetPath(appId);
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}

	static KeyValue GetAppState(KeyValue manifest)
	{
		if (manifest.Name == "AppState")
		{
			return manifest;
		}

		return manifest["AppState"];
	}

	public static bool IsAppInstalled(int appId, string installFolderName)
	{
		if (!Exists(appId))
		{
			return false;
		}

		string installDir = Utils.GetAbsolutePath(Path.Combine("steamapps", "common", installFolderName));
		if (!Directory.Exists(installDir))
		{
			return false;
		}

		var manifest = TryLoad(appId);
		if (manifest == null)
		{
			return false;
		}

		var appState = GetAppState(manifest);
		if (appState == KeyValue.Invalid)
		{
			return false;
		}

		var installedDepots = appState["InstalledDepots"];
		return installedDepots != KeyValue.Invalid && installedDepots.Children.Count > 0;
	}

	public static bool HasPartialInstall(int appId, string installFolderName) => Exists(appId) && !IsAppInstalled(appId, installFolderName);

	public static Dictionary<uint, ulong> GetInstalledDepots(int appId)
	{
		var result = new Dictionary<uint, ulong>();
		var manifest = TryLoad(appId);
		if (manifest == null)
		{
			return result;
		}

		var appState = GetAppState(manifest);
		if (appState == KeyValue.Invalid)
		{
			return result;
		}

		var installedDepots = appState["InstalledDepots"];
		if (installedDepots == KeyValue.Invalid)
		{
			return result;
		}

		foreach (var depot in installedDepots.Children)
		{
			if (!uint.TryParse(depot.Name, out var depotId))
			{
				continue;
			}

			var manifestIdStr = depot["manifest"]?.Value;
			if (manifestIdStr != null && ulong.TryParse(manifestIdStr, out var manifestId))
			{
				result[depotId] = manifestId;
			}
		}

		return result;
	}

	public static void WriteDownloading(int appId, string name, string installFolderName, ulong bytesToDownload, ulong bytesDownloaded, ulong lastOwner)
	{
		WriteManifest(appId, name, installFolderName, buildId: 0, lastOwner, bytesToDownload, bytesDownloaded, StateFlagsDownloading, installedDepots: null);
	}

	public static void UpdateDownloadProgress(int appId, ulong bytesDownloaded, ulong bytesToDownload)
	{
		var manifest = TryLoad(appId);
		if (manifest == null)
		{
			return;
		}

		var appState = GetAppState(manifest);
		if (appState == KeyValue.Invalid)
		{
			return;
		}

		SetChildValue(appState, "BytesDownloaded", bytesDownloaded.ToString());
		SetChildValue(appState, "BytesToDownload", bytesToDownload.ToString());
		SetChildValue(appState, "LastUpdated", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

		appState.SaveToFile(GetPath(appId), false);
	}

	public static void Write(int appId, string name, string installFolderName, uint buildId, ulong lastOwner, Dictionary<uint, ulong> installedDepots)
	{
		string installDir = Utils.GetAbsolutePath(Path.Combine("steamapps", "common", installFolderName));
		ulong sizeOnDisk = CalculateInstallSize(installDir);

		var appState = WriteManifest(appId, name, installFolderName, buildId, lastOwner, bytesToDownload: 0, bytesDownloaded: 0, StateFlagsInstalled, installedDepots);
		SetChildValue(appState, "SizeOnDisk", sizeOnDisk.ToString());
		appState.SaveToFile(GetPath(appId), false);
	}

	static KeyValue WriteManifest(int appId, string name, string installFolderName, uint buildId, ulong lastOwner, ulong bytesToDownload, ulong bytesDownloaded, string stateFlags, Dictionary<uint, ulong>? installedDepots)
	{
		var appState = new KeyValue("AppState");

		void AddValue(string key, string value) => appState.Children.Add(new KeyValue(key) { Value = value });

		AddValue("appid", appId.ToString());
		AddValue("Universe", "1");
		AddValue("name", name);
		AddValue("StateFlags", stateFlags);
		AddValue("installdir", installFolderName);
		AddValue("LastUpdated", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
		AddValue("UpdateResult", "0");
		AddValue("SizeOnDisk", "0");
		AddValue("buildid", buildId.ToString());
		AddValue("LastOwner", lastOwner.ToString());
		AddValue("BytesToDownload", bytesToDownload.ToString());
		AddValue("BytesDownloaded", bytesDownloaded.ToString());
		AddValue("AutoUpdateBehavior", "0");
		AddValue("AllowOtherDownloadsWhileRunning", "0");
		AddValue("ScheduledAutoUpdate", "0");

		var userConfig = new KeyValue("UserConfig");
		userConfig.Children.Add(new KeyValue("language") { Value = "english" });
		appState.Children.Add(userConfig);

		var installedDepotsKv = new KeyValue("InstalledDepots");
		var mountedDepotsKv = new KeyValue("MountedDepots");

		if (installedDepots != null)
		{
			foreach (var (depotId, manifestId) in installedDepots.OrderBy(x => x.Key))
			{
				var depotKv = new KeyValue(depotId.ToString());
				depotKv.Children.Add(new KeyValue("manifest") { Value = manifestId.ToString() });
				installedDepotsKv.Children.Add(depotKv);
				mountedDepotsKv.Children.Add(new KeyValue(depotId.ToString()) { Value = manifestId.ToString() });
			}
		}

		appState.Children.Add(installedDepotsKv);
		appState.Children.Add(mountedDepotsKv);

		Directory.CreateDirectory(Path.GetDirectoryName(GetPath(appId))!);
		appState.SaveToFile(GetPath(appId), false);

		return appState;
	}

	static void SetChildValue(KeyValue parent, string name, string value)
	{
		var child = parent.Children.FirstOrDefault(c => c.Name == name);
		if (child != null)
		{
			child.Value = value;
		}
		else
		{
			parent.Children.Add(new KeyValue(name) { Value = value });
		}
	}

	static ulong CalculateInstallSize(string installDir)
	{
		if (!Directory.Exists(installDir))
		{
			return 0;
		}

		ulong size = 0;

		foreach (var file in Directory.EnumerateFiles(installDir, "*", SearchOption.AllDirectories))
		{
			var relativePath = Path.GetRelativePath(installDir, file).Replace('\\', '/');
			if (relativePath.StartsWith(".DepotDownloader/", StringComparison.Ordinal))
			{
				continue;
			}

			size += (ulong)new FileInfo(file).Length;
		}

		return size;
	}
}
