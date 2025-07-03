// This file is subject to the terms and conditions defined
// in file 'LICENSE', which is part of this source code package.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QRCoder;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.CDN;
using SteamKit2.Internal;

namespace DepotDownloader
{
	class Steam3Session
	{
		public bool IsLoggedOn { get; private set; }

		public ReadOnlyCollection<SteamApps.LicenseListCallback.License> Licenses => Steam.Instance.AppLicenses;

		public Dictionary<uint, ulong> AppTokens { get; } = [];
		public Dictionary<uint, ulong> PackageTokens => Steam.Instance.PackageTokens;
		public Dictionary<uint, byte[]> DepotKeys { get; } = [];
		public ConcurrentDictionary<(uint, string), TaskCompletionSource<SteamContent.CDNAuthToken>> CDNAuthTokens { get; } = [];
		public Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> AppInfo { get; } = [];
		public Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> PackageInfo { get; } = [];
		public Dictionary<string, byte[]> AppBetaPasswords { get; } = [];

		public SteamClient steamClient => Steam.Instance.steamClient;
		public SteamUser steamUser => Steam.Instance.steamUser;
		public SteamContent steamContent => Steam.Instance.steamContent;
		public SteamApps steamApps => Steam.Instance.steamApps;
		public SteamCloud steamCloud => Steam.Instance.steamCloud;
		public PublishedFile steamPublishedFile => Steam.Instance.steamPublishedFile;

		readonly CallbackManager callbacks;

		readonly bool authenticatedUser = true;
		bool bConnecting;
		bool bAborted;
		bool bExpectingDisconnectRemote;
		bool bDidDisconnect;
		bool bIsConnectionRecovery;
		int connectionBackoff;
		int seq; // more hack fixes
		AuthSession authSession;
		readonly CancellationTokenSource abortedToken = new();

		// input
		readonly SteamUser.LogOnDetails logonDetails;

		public Steam3Session(SteamUser.LogOnDetails details)
		{
			//this.logonDetails = details;
			//this.authenticatedUser = details.Username != null || ContentDownloader.Config.UseQrCode;

			var clientConfiguration = SteamConfiguration.Create(config =>
				config
					.WithHttpClientFactory(static purpose => HttpClientFactory.CreateHttpClient())
			);

			//this.steamClient = new SteamClient(clientConfiguration);

			//this.steamUser = Steam.Instance.steamClient.GetHandler<SteamUser>();
			//this.steamApps = this.steamClient.GetHandler<SteamApps>();
			//this.steamCloud = this.steamClient.GetHandler<SteamCloud>();
			//var steamUnifiedMessages = Steam.Instance.steamClient.GetHandler<SteamUnifiedMessages>();
			//this.steamPublishedFile = steamUnifiedMessages.CreateService<PublishedFile>();
			//this.steamContent = Steam.Instance.steamClient.GetHandler<SteamContent>();

			//this.callbacks = new CallbackManager(this.steamClient);

			//this.callbacks.Subscribe<SteamClient.ConnectedCallback>(ConnectedCallback);
			//this.callbacks.Subscribe<SteamClient.DisconnectedCallback>(DisconnectedCallback);
			//this.callbacks.Subscribe<SteamUser.LoggedOnCallback>(LogOnCallback);
			//this.callbacks.Subscribe<SteamApps.LicenseListCallback>(LicenseListCallback);

			Console.Write("Connecting to Steam3...");
			//Connect();
		}

		public delegate bool WaitCondition();

		private readonly Lock steamLock = new();

		public bool WaitUntilCallback(Action submitter, WaitCondition waiter)
		{
			while (!bAborted && !waiter())
			{
				lock (steamLock)
				{
					submitter();
				}

				var seq = this.seq;
				do
				{
					lock (steamLock)
					{
						callbacks.RunWaitCallbacks(TimeSpan.FromSeconds(1));
					}
				} while (!bAborted && this.seq == seq && !waiter());
			}

			return bAborted;
		}

		public bool WaitForCredentials()
		{
			if (IsLoggedOn || bAborted)
				return IsLoggedOn;

			WaitUntilCallback(() => { }, () => IsLoggedOn);

			return IsLoggedOn;
		}

		public async Task TickCallbacks()
		{
			var token = abortedToken.Token;

			try
			{
				while (!token.IsCancellationRequested)
				{
					await callbacks.RunWaitCallbackAsync(token);
				}
			}
			catch (OperationCanceledException)
			{
				//
			}
		}

		public async Task RequestAppInfo(uint appId, bool bForce = false)
		{
			if ((AppInfo.ContainsKey(appId) && !bForce) || bAborted)
				return;

			var appTokens = await steamApps.PICSGetAccessTokens([appId], []);

			if (appTokens.AppTokensDenied.Contains(appId))
			{
				Console.WriteLine("Insufficient privileges to get access token for app {0}", appId);
			}

			foreach (var token_dict in appTokens.AppTokens)
			{
				this.AppTokens[token_dict.Key] = token_dict.Value;
			}

			var request = new SteamApps.PICSRequest(appId);

			if (AppTokens.TryGetValue(appId, out var token))
			{
				request.AccessToken = token;
			}

			var appInfoMultiple = await steamApps.PICSGetProductInfo([request], []);

			foreach (var appInfo in appInfoMultiple.Results)
			{
				foreach (var app_value in appInfo.Apps)
				{
					var app = app_value.Value;

					Console.WriteLine("Got AppInfo for {0}", app.ID);
					AppInfo[app.ID] = app;
				}

				foreach (var app in appInfo.UnknownApps)
				{
					AppInfo[app] = null;
				}
			}
		}

		public async Task RequestPackageInfo(IEnumerable<uint> packageIds)
		{
			var packages = packageIds.ToList();
			packages.RemoveAll(PackageInfo.ContainsKey);

			if (packages.Count == 0 || bAborted)
				return;

			var packageRequests = new List<SteamApps.PICSRequest>();

			foreach (var package in packages)
			{
				var request = new SteamApps.PICSRequest(package);

				if (PackageTokens.TryGetValue(package, out var token))
				{
					request.AccessToken = token;
				}

				packageRequests.Add(request);
			}

			var packageInfoMultiple = await steamApps.PICSGetProductInfo([], packageRequests);

			foreach (var packageInfo in packageInfoMultiple.Results)
			{
				foreach (var package_value in packageInfo.Packages)
				{
					var package = package_value.Value;
					PackageInfo[package.ID] = package;
				}

				foreach (var package in packageInfo.UnknownPackages)
				{
					PackageInfo[package] = null;
				}
			}
		}

		public async Task<bool> RequestFreeAppLicense(uint appId)
		{
			try
			{
				var resultInfo = await steamApps.RequestFreeLicense(appId);

				return resultInfo.GrantedApps.Contains(appId);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to request FreeOnDemand license for app {appId}: {ex.Message}");
				return false;
			}
		}

		public async Task RequestDepotKey(uint depotId, uint appid = 0)
		{
			if (DepotKeys.ContainsKey(depotId) || bAborted)
				return;

			var depotKey = await steamApps.GetDepotDecryptionKey(depotId, appid);

			Console.WriteLine("Got depot key for {0} result: {1}", depotKey.DepotID, depotKey.Result);

			if (depotKey.Result != EResult.OK)
			{
				return;
			}

			DepotKeys[depotKey.DepotID] = depotKey.DepotKey;
		}


		public async Task<ulong> GetDepotManifestRequestCodeAsync(uint depotId, uint appId, ulong manifestId, string branch)
		{
			if (bAborted)
				return 0;

			var requestCode = await steamContent.GetManifestRequestCode(depotId, appId, manifestId, branch);

			if (requestCode == 0)
			{
				Console.WriteLine($"No manifest request code was returned for depot {depotId} from app {appId}, manifest {manifestId}");

				if (!authenticatedUser)
				{
					Console.WriteLine("Suggestion: Try logging in with -username as old manifests may not be available for anonymous accounts.");
				}
			}
			else
			{
				Console.WriteLine($"Got manifest request code for depot {depotId} from app {appId}, manifest {manifestId}, result: {requestCode}");
			}

			return requestCode;
		}

		public async Task RequestCDNAuthToken(uint appid, uint depotid, Server server)
		{
			var cdnKey = (depotid, server.Host);
			var completion = new TaskCompletionSource<SteamContent.CDNAuthToken>();

			if (bAborted || !CDNAuthTokens.TryAdd(cdnKey, completion))
			{
				return;
			}

			DebugLog.WriteLine(nameof(Steam3Session), $"Requesting CDN auth token for {server.Host}");

			var cdnAuth = await steamContent.GetCDNAuthToken(appid, depotid, server.Host);

			Console.WriteLine($"Got CDN auth token for {server.Host} result: {cdnAuth.Result} (expires {cdnAuth.Expiration})");

			if (cdnAuth.Result != EResult.OK)
			{
				return;
			}

			completion.TrySetResult(cdnAuth);
		}

		public async Task CheckAppBetaPassword(uint appid, string password)
		{
			var appPassword = await steamApps.CheckAppBetaPassword(appid, password);

			Console.WriteLine("Retrieved {0} beta keys with result: {1}", appPassword.BetaPasswords.Count, appPassword.Result);

			foreach (var entry in appPassword.BetaPasswords)
			{
				AppBetaPasswords[entry.Key] = entry.Value;
			}
		}

		public async Task<KeyValue> GetPrivateBetaDepotSection(uint appid, string branch)
		{
			if (!AppBetaPasswords.TryGetValue(branch, out var branchPassword)) // Should be filled by CheckAppBetaPassword
			{
				return new KeyValue();
			}

			AppTokens.TryGetValue(appid, out var accessToken); // Should be filled by RequestAppInfo

			var privateBeta = await steamApps.PICSGetPrivateBeta(appid, accessToken, branch, branchPassword);

			Console.WriteLine($"Retrieved private beta depot section for {appid} with result: {privateBeta.Result}");

			return privateBeta.DepotSection;
		}

		public async Task<PublishedFileDetails> GetPublishedFileDetails(uint appId, PublishedFileID pubFile)
		{
			var pubFileRequest = new CPublishedFile_GetDetails_Request { appid = appId };
			pubFileRequest.publishedfileids.Add(pubFile);

			var details = await steamPublishedFile.GetDetails(pubFileRequest);

			if (details.Result == EResult.OK)
			{
				return details.Body.publishedfiledetails.FirstOrDefault();
			}

			throw new Exception($"EResult {(int)details.Result} ({details.Result}) while retrieving file details for pubfile {pubFile}.");
		}


		public async Task<SteamCloud.UGCDetailsCallback> GetUGCDetails(UGCHandle ugcHandle)
		{
			var callback = await steamCloud.RequestUGCDetails(ugcHandle);

			if (callback.Result == EResult.OK)
			{
				return callback;
			}
			else if (callback.Result == EResult.FileNotFound)
			{
				return null;
			}

			throw new Exception($"EResult {(int)callback.Result} ({callback.Result}) while retrieving UGC details for {ugcHandle}.");
		}
	}
}