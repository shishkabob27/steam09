using SDL;
using SteamKit2.Internal;
using UltralightNet;
using UltralightNet.AppCore;

public class Browser
{
	public UltralightNet.Renderer BrowserRenderer;
	public View BrowserView;

	int lastX = 0;
	int lastY = 0;

	int lastResizeX = 0;
	int lastResizeY = 0;

	public Action OnFinishLoading;

	~Browser()
	{
		BrowserRenderer.Dispose();
		BrowserView.Dispose();
	}

	public void Initialize()
	{
		AppCoreMethods.SetPlatformFontLoader();

		// Create Renderer
		var cfg = new ULConfig();
		BrowserRenderer = ULPlatform.CreateRenderer(cfg);

		// Create View
		BrowserView = BrowserRenderer.CreateView(640, 480);

		BrowserView.OnFinishLoading += (_, _, _) =>
		{
			OnFinishLoading?.Invoke();
		};

		BrowserView.OnChangeCursor += (cursor) =>
		{
			unsafe
			{				
				if (cursor == ULCursor.Pointer)
				{
					SDL3.SDL_SetCursor(SDL3.SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT));
				}
				else if (cursor == ULCursor.IBeam)
				{
					SDL3.SDL_SetCursor(SDL3.SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_TEXT));
				}
				else if (cursor == ULCursor.Hand)
				{
					SDL3.SDL_SetCursor(SDL3.SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_POINTER));
				}
			}
		};
	}

	public void LoadURL(string url)
	{
		BrowserView.URL = url;
	}

	public void Back()
	{
		BrowserView.GoBack();
	}

	public void Forward()
	{
		BrowserView.GoForward();
	}

	public void Reload()
	{
		BrowserView.Reload();
	}

	public void Stop()
	{
		BrowserView.Stop();
	}

	public bool CanGoBack()
	{
		return BrowserView.CanGoBack;
	}

	public bool CanGoForward()
	{
		return BrowserView.CanGoForward;
	}

	public void Resize(int width, int height)
	{
		if (width == lastResizeX && height == lastResizeY) return;

		BrowserView.Resize((uint)width, (uint)height);

		lastResizeX = width;
		lastResizeY = height;
	}

	public void OnMouseMove(int x, int y)
	{
		if (x == lastX && y == lastY) return;

		BrowserView.FireMouseEvent(new ULMouseEvent()
		{
			Type = ULMouseEventType.MouseMoved,
			X = x,
			Y = y
		});

		lastX = x;
		lastY = y;
	}

	public void OnMouseDown(int button)
	{
		ULMouseEvent mouseEvent = new ULMouseEvent();
		mouseEvent.Type = ULMouseEventType.MouseDown;
		mouseEvent.X = lastX;
		mouseEvent.Y = lastY;

		if (button == 1)
		{
			mouseEvent.Button = ULMouseEventButton.Left;
		}
		else if (button == 2)
		{
			mouseEvent.Button = ULMouseEventButton.Right;
		}
		else if (button == 3)
		{
			mouseEvent.Button = ULMouseEventButton.Middle;
		}
		else if (button == 4)
		{
			Back();
			return;
		}
		else if (button == 5)
		{
			Forward();
			return;
		}

		BrowserView.FireMouseEvent(mouseEvent);
	}

	public void OnMouseUp(int button)
	{
		ULMouseEvent mouseEvent = new ULMouseEvent();
		mouseEvent.Type = ULMouseEventType.MouseUp;
		mouseEvent.X = lastX;
		mouseEvent.Y = lastY;

		if (button == 1)
		{
			mouseEvent.Button = ULMouseEventButton.Left;
		}
		else if (button == 2)
		{
			mouseEvent.Button = ULMouseEventButton.Right;
		}
		else if (button == 3)
		{
			mouseEvent.Button = ULMouseEventButton.Middle;
		}
		BrowserView.FireMouseEvent(mouseEvent);
	}
	public void OnMouseScroll(int scrollX, int scrollY)
	{
		ULScrollEvent scrollEvent = new ULScrollEvent();
		scrollEvent.Type = ULScrollEventType.ByPixel;
		scrollEvent.DeltaX = scrollX * 100;
		scrollEvent.DeltaY = scrollY * 100;
		BrowserView.FireScrollEvent(scrollEvent);
	}

	public void OnKeyDown(SDL_Keycode key, SDL_Keymod mod)
	{
		Console.WriteLine(key);

		ULKeyEventModifiers modifiers = 0;
		if (mod.HasFlag(SDL_Keymod.SDL_KMOD_LSHIFT))
		{
			modifiers |= ULKeyEventModifiers.ShiftKey;
		}
		if (mod.HasFlag(SDL_Keymod.SDL_KMOD_LCTRL))
		{
			modifiers |= ULKeyEventModifiers.CtrlKey;
		}
		if (mod.HasFlag(SDL_Keymod.SDL_KMOD_LALT))
		{
			modifiers |= ULKeyEventModifiers.AltKey;
		}
		if (mod.HasFlag(SDL_Keymod.SDL_KMOD_LGUI))
		{
			modifiers |= ULKeyEventModifiers.MetaKey;
		}

		int keyCode = ULKeyCodes.GK_T;

		ULKeyEvent keyEvent = ULKeyEvent.Create(ULKeyEventType.KeyDown, modifiers, keyCode, keyCode, key.ToString(), key.ToString(), false, false, false);
		BrowserView.FireKeyEvent(keyEvent);
	}

	public void OnKeyUp(SDL_Keycode key, SDL_Keymod mod)
	{
		Console.WriteLine(key);

		int keyCode = ULKeyCodes.GK_T;

		ULKeyEvent keyEvent = ULKeyEvent.Create(ULKeyEventType.KeyUp, 0, keyCode, keyCode, key.ToString(), key.ToString(), false, false, false);
		BrowserView.FireKeyEvent(keyEvent);
	}

	public void Update()
	{
		BrowserRenderer.Update();
	}

	public unsafe void Draw(SDL_Renderer* renderer, SDL_FRect destRect)
	{
		BrowserRenderer.Render();

		if (!BrowserView.Surface.HasValue)
			return;

		var bitmap = BrowserView.Surface.Value.Bitmap;
		int width = (int)bitmap.Width;
		int height = (int)bitmap.Height;
		byte* pixels = bitmap.LockPixels();
		int rowBytes = (int)bitmap.RowBytes;

		if (pixels == null || width <= 0 || height <= 0)
		{
			if (pixels != null) bitmap.UnlockPixels();
			return;
		}

		SDL_Surface* surface = SDL3.SDL_CreateSurfaceFrom(width, height, SDL3.SDL_PIXELFORMAT_BGRA32, (nint)pixels, rowBytes);

		if (surface != null)
		{
			SDL_Texture* texture = SDL3.SDL_CreateTextureFromSurface(renderer, surface);
			if (texture != null)
			{
				SDL3.SDL_RenderTexture(renderer, texture, null, &destRect);
				SDL3.SDL_DestroyTexture(texture);
			}
			SDL3.SDL_DestroySurface(surface);
		}

		bitmap.UnlockPixels();
	}
}