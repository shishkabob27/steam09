using System.Collections.ObjectModel;
using SteamKit2;

public class LibraryDataCache
{
    public ReadOnlyCollection<SteamApps.LicenseListCallback.License> AppLicenses { get; set; }
	public Dictionary<uint, ulong> PackageTokens { get;  set; } = [];
	public Dictionary<uint, ulong> AppTokens { get;  set; } = new Dictionary<uint, ulong>();
	public Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> AppInfo { get; set; } = [];
	public Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> PackageInfo { get; set; } = [];

	public KeyValue? GetAppInfo(uint appId)
	{
		return AppInfo.TryGetValue(appId, out var appInfo) ? appInfo.KeyValues : null;
	}

	public KeyValue? GetPackageInfo(uint packageId)
	{
		return PackageInfo.TryGetValue(packageId, out var packageInfo) ? packageInfo.KeyValues : null;
	}
}