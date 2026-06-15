using System.Drawing;
using System.Collections.Concurrent;
using SDL;

namespace KGUI
{
	public class RemoteImageControl : UIControl
    {
        unsafe SDL_Texture* imageTexture;
        bool readyToDraw = false;
        bool pendingFirstDrawFetch;
        int fetchGeneration;
        string? pendingTexturePath;
        readonly object pendingTextureLock = new();
        static readonly HttpClient imageHttpClient = new();
        static readonly ConcurrentDictionary<string, Task<bool>> inProgressDownloads = new();

        public Color backgroundColor = Color.Transparent;

        public int imageWidth = 0;
        public int imageHeight = 0;

        public bool Ready { get { return readyToDraw; } }

        string imageUrl = "";

		const string cacheFolder = "webcache";

        public RemoteImageControl(UIControl parent) : base(parent)
		{
			if (!Directory.Exists(cacheFolder))
			{
				Directory.CreateDirectory(cacheFolder);
			}
		}

        public void SetImageUrl(string imageUrl)
        {
            string newUrl = imageUrl ?? "";
            if (this.imageUrl == newUrl)
                return;

            this.imageUrl = newUrl;
            fetchGeneration++;


            readyToDraw = false;
            lock (pendingTextureLock)
                pendingTexturePath = null;


            unsafe
            {
                if (imageTexture != null)
                {
                    SDL3.SDL_DestroyTexture(imageTexture);
                    imageTexture = null;
                }
            }


            pendingFirstDrawFetch = !string.IsNullOrWhiteSpace(newUrl);
        }

        void AttemptFetchImage()
        {
            //clear and url args
            string testUrl = imageUrl.Split('?')[0];
            //only download actual png
            if (!testUrl.EndsWith(".png")
            && !testUrl.EndsWith(".jpg")
            && !testUrl.EndsWith(".jpeg")
            && !testUrl.EndsWith(".gif")
            && !testUrl.EndsWith(".webp"))
            {
                Console.WriteLine("RemoteImageControl: Skipping download of image: " + imageUrl);
                return;
            }


            string cachePath = Path.Combine(cacheFolder, cleanURL(imageUrl) + ".png");

            // load the cached image immediately if available and not still downloading
            if (File.Exists(cachePath) && !inProgressDownloads.ContainsKey(cachePath))
            {
                unsafe
                {
                    imageTexture = LoadTexture(cachePath);
                    if (imageTexture == null)
                    {
                        Console.WriteLine("ImageControl: Error loading image: " + cachePath);
                        return;
                    }
                    readyToDraw = true;
                }
            }
            else
            {
                readyToDraw = false;
                int gen = fetchGeneration;
                _ = QueueImageWhenReadyAsync(imageUrl, cachePath, gen);
            }
        }

        private async Task QueueImageWhenReadyAsync(string imageUrl, string cachePath, int generation)
        {
            try
            {
                if (await EnsureCacheFileAsync(imageUrl, cachePath))
                {
                    lock (pendingTextureLock)
                    {
                        if (generation == fetchGeneration)
                            pendingTexturePath = cachePath;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ImageControl: Error downloading image: " + imageUrl);
                Console.WriteLine("Error: " + e.Message);
            }
        }

        private static Task<bool> EnsureCacheFileAsync(string imageUrl, string cachePath)
        {
            if (File.Exists(cachePath))
            {
                return Task.FromResult(true);
            }

            return inProgressDownloads.GetOrAdd(cachePath, _ => DownloadAndCacheImageAsync(imageUrl, cachePath));
        }

        private static async Task<bool> DownloadAndCacheImageAsync(string imageUrl, string cachePath)
        {
            try
            {
                var response = await imageHttpClient.GetAsync(imageUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                Directory.CreateDirectory(cacheFolder);
                await File.WriteAllBytesAsync(cachePath, imageBytes);
                return true;
            }
            finally
            {
                inProgressDownloads.TryRemove(cachePath, out _);
            }
        }

        public unsafe override void Draw()
        {
            base.Draw();

            if (pendingFirstDrawFetch)
            {
                pendingFirstDrawFetch = false;
                if (!string.IsNullOrWhiteSpace(imageUrl))
                    AttemptFetchImage();
            }

            string? texturePathToLoad = null;
            lock (pendingTextureLock)
            {
                texturePathToLoad = pendingTexturePath;
                pendingTexturePath = null;
            }

            if (!string.IsNullOrWhiteSpace(texturePathToLoad))
            {
                var loadedTexture = LoadTexture(texturePathToLoad);
                if (loadedTexture != null)
                {
                    imageTexture = loadedTexture;
                    readyToDraw = true;
                }
            }

            if (imageTexture != null && readyToDraw)
            {
                if (backgroundColor != Color.Transparent)
                {
                    DrawBox(0, 0, width, height, backgroundColor);
                }
                DrawTextureRect(imageTexture, 0, 0, width, height);
            }
        }

        private string cleanURL(string url)
        {
            return url.Replace("https://", "").Replace("http://", "").Replace("//", "/").Replace("?", "_").Replace("&", "_").Replace("=", "_").Replace(".", "_").Replace(":", "_").Replace(";", "_").Replace(",", "_").Replace(" ", "_").Replace("|", "_").Replace("\\", "_").Replace("/", "_").Replace("*", "_").Replace("?", "_").Replace("&", "_").Replace("=", "_").Replace(".", "_").Replace(":", "_").Replace(";", "_").Replace(",", "_").Replace(" ", "_").Replace("|", "_").Replace("\\", "_").Replace("/", "_").Replace("*", "_");
        }

        public override void Destroy()
        {
            base.Destroy();
            unsafe {
                SDL3.SDL_DestroyTexture(imageTexture);
            }
        }
    }
}