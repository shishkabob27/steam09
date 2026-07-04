using System.Collections.ObjectModel;
using SteamKit2;

public class LibraryDataCache
{
    public ReadOnlyCollection<SteamApps.LicenseListCallback.License> AppLicenses { get; set; }
	public Dictionary<uint, ulong> PackageTokens { get;  set; } = [];
	public Dictionary<uint, ulong> AppTokens { get;  set; } = new Dictionary<uint, ulong>();
}