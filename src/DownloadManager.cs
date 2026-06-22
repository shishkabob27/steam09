using DepotDownloader;
using KGUI;

public class DownloadManager
{
	readonly Queue<int> _downloadQueue = new Queue<int>();
	CancellationTokenSource? _downloadCts;
	int? _currentDownloadAppId;
	int? _pauseRequestedAppId;
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
					if (_pauseRequestedAppId == appID)
					{
						_pauseRequestedAppId = null;
						SetGamePaused(game, appID);
						Console.WriteLine("Download paused for game: " + appID);
					}
					else
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
						_downloadQueue.Enqueue(appID);
						Console.WriteLine("Download cancelled for game: " + appID);
					}

					game.DownloadStatus = DownloadStatus.None;
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

	public void PauseDownload(int appID)
	{
		Game? game = Steam.Instance.Games.Find(g => g.AppID == appID);
		if (game == null)
		{
			Console.WriteLine("Game not found");
			return;
		}

		if (_currentDownloadAppId == appID && _downloadCts != null)
		{
			Console.WriteLine("Pausing active download for game: " + appID);
			_pauseRequestedAppId = appID;
			_downloadCts.Cancel();
			return;
		}

		if (RemoveFromQueue(appID))
		{
			Console.WriteLine("Removed queued download for game: " + appID);
			game.Status = GameStatus.NotInstalled;
			game.DownloadStatus = DownloadStatus.None;
			game.RefreshPartialDownloadState();
			NotifyGameUpdate(game);
			return;
		}

		Console.WriteLine("Game is not downloading or queued: " + appID);
	}

	static void SetGamePaused(Game game, int appID)
	{
		ulong bytesDownloaded = game.InstallProgress > 0
			? (ulong)(game.EstimatedSize * game.InstallProgress / 100f)
			: 0;

		if (AppManifest.Exists(appID))
		{
			AppManifest.UpdateDownloadProgress(appID, bytesDownloaded, game.EstimatedSize);
		}
		else if (bytesDownloaded > 0)
		{
			ulong lastOwner = Steam.Instance.steamClient?.SteamID?.ConvertToUInt64() ?? 0;
			AppManifest.WriteDownloading(appID, game.Name, game.InstallFolderName, game.EstimatedSize, bytesDownloaded, lastOwner);
		}

		game.Status = GameStatus.NotInstalled;
		game.DownloadStatus = DownloadStatus.None;
		game.RefreshPartialDownloadState();
	}

	bool RemoveFromQueue(int appID)
	{
		if (!_downloadQueue.Contains(appID))
		{
			return false;
		}

		var remaining = _downloadQueue.Where(id => id != appID).ToList();
		_downloadQueue.Clear();
		foreach (int id in remaining)
		{
			_downloadQueue.Enqueue(id);
		}

		return true;
	}

	static void NotifyGameUpdate(Game game)
	{
		MainWindow? mainWindow = WindowManager.Instance.GetWindows().Find(w => w is MainWindow) as MainWindow;
		mainWindow?.QueueGameUpdate(game);
	}
}
