using DepotDownloader;
using KGUI;

public class DownloadManager
{
	readonly Queue<int> _downloadQueue = new Queue<int>();
	CancellationTokenSource? _downloadCts;
	int? _currentDownloadAppId;
	bool _processingQueue;

	public async Task DownloadGame(int appID)
	{
		Console.WriteLine("Downloading game: " + appID);

		Game? game = Steam.Instance.Games.Find(g => g.AppID == appID);
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

		if (_downloadQueue.Contains(appID) || _currentDownloadAppId == appID)
		{
			Console.WriteLine("Game is already queued or downloading");
			return;
		}

		_downloadQueue.Enqueue(appID);
		game.Status = GameStatus.Queued;
		NotifyGameUpdate(game);

		if (_downloadCts != null)
		{
			Console.WriteLine("Stopping current download to process queue");
			_downloadCts.Cancel();
			return;
		}

		await ProcessDownloadQueue();
	}

	async Task ProcessDownloadQueue()
	{
		if (_processingQueue)
		{
			return;
		}

		_processingQueue = true;

		try
		{
			while (_downloadQueue.Count > 0)
			{
				int appID = _downloadQueue.Dequeue();

				Game? game = Steam.Instance.Games.Find(g => g.AppID == appID);
				if (game == null)
				{
					Console.WriteLine("Game not found");
					continue;
				}

				if (game.Status == GameStatus.Installed)
				{
					Console.WriteLine("Game is already installed");
					continue;
				}

				_currentDownloadAppId = appID;

				game.Status = GameStatus.Downloading;
				game.DownloadStatus = DownloadStatus.Processing;
				if (!game.HasPartialDownload)
				{
					game.InstallProgress = 0;
				}

				string installDir = Utils.GetAbsolutePath(Path.Combine("steamapps", "common", game.InstallFolderName));
				ContentDownloader.Config.InstallDirectory = installDir;
				ContentDownloader.Config.GameInstallDirectory = game.InstallFolderName;

				_downloadCts = new CancellationTokenSource();
				ulong lastOwner = Steam.Instance.steamClient?.SteamID?.ConvertToUInt64() ?? 0;
				AppManifest.WriteDownloading(appID, game.Name, game.InstallFolderName, game.EstimatedSize, 0, lastOwner);

				try
				{
					await ContentDownloader.DownloadAppAsync((uint)appID, new List<(uint, ulong)> { }, "public", Util.GetSteamOS(), Util.GetSteamArch(), "english", false, false, game, _downloadCts.Token);
					game.Status = GameStatus.Installed;
					game.InstallProgress = 100;
					game.DownloadStatus = DownloadStatus.None;
					game.HasPartialDownload = false;
				}
				catch (OperationCanceledException)
				{
					if (game.InstallProgress > 0)
					{
						ulong bytesDownloaded = (ulong)(game.EstimatedSize * game.InstallProgress / 100f);
						AppManifest.UpdateDownloadProgress(appID, bytesDownloaded, game.EstimatedSize);
						game.HasPartialDownload = true;
					}
					else
					{
						AppManifest.Delete(appID);
						game.HasPartialDownload = false;
					}

					game.Status = GameStatus.Queued;
					game.DownloadStatus = DownloadStatus.None;
					_downloadQueue.Enqueue(appID);
					Console.WriteLine("Download cancelled for game: " + appID);
				}
				catch (Exception e)
				{
					if (game.InstallProgress <= 0)
					{
						AppManifest.Delete(appID);
					}
					else
					{
						ulong bytesDownloaded = (ulong)(game.EstimatedSize * game.InstallProgress / 100f);
						AppManifest.UpdateDownloadProgress(appID, bytesDownloaded, game.EstimatedSize);
					}

					game.RefreshPartialDownloadState();
					game.Status = GameStatus.NotInstalled;
					game.DownloadStatus = DownloadStatus.None;
					Console.WriteLine("Failed to download game: " + e.Message);
					Console.WriteLine(e.StackTrace);
				}
				finally
				{
					_currentDownloadAppId = null;
					_downloadCts.Dispose();
					_downloadCts = null;

					NotifyGameUpdate(game);
				}
			}
		}
		finally
		{
			_processingQueue = false;

			if (_downloadQueue.Count > 0 && _downloadCts == null)
			{
				await ProcessDownloadQueue();
			}
		}
	}

	static void NotifyGameUpdate(Game game)
	{
		MainWindow? mainWindow = WindowManager.Instance.GetWindows().Find(w => w is MainWindow) as MainWindow;
		mainWindow?.QueueGameUpdate(game);
	}
}
