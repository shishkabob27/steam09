using SDL_Sharp;
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
		BrowserView = BrowserRenderer.CreateView(1920, 1080);

		BrowserView.OnFinishLoading += (_, _, _) =>
		{
			OnFinishLoading?.Invoke();
		};

		BrowserView.OnChangeCursor += (cursor) =>
		{
			if (cursor == ULCursor.Pointer)
			{
				SDL.SetCursor(SDL.CreateSystemCursor(SystemCursor.Arrow));
			}
			else if (cursor == ULCursor.IBeam)
			{
				SDL.SetCursor(SDL.CreateSystemCursor(SystemCursor.IBeam));
			}
			else if (cursor == ULCursor.Hand)
			{
				SDL.SetCursor(SDL.CreateSystemCursor(SystemCursor.Hand));
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
		scrollEvent.DeltaX = scrollX * 50;
		scrollEvent.DeltaY = scrollY * 50;
		BrowserView.FireScrollEvent(scrollEvent);
	}

	public void OnKeyDown(Keycode key, KeyModifier mod)
	{
		Console.WriteLine(key);

		ULKeyEventModifiers modifiers = 0;
		if (mod.HasFlag(KeyModifier.LeftShift))
		{
			modifiers |= ULKeyEventModifiers.ShiftKey;
		}
		if (mod.HasFlag(KeyModifier.LeftCtrl))
		{
			modifiers |= ULKeyEventModifiers.CtrlKey;
		}
		if (mod.HasFlag(KeyModifier.LeftAlt))
		{
			modifiers |= ULKeyEventModifiers.AltKey;
		}
		if (mod.HasFlag(KeyModifier.LeftGui))
		{
			modifiers |= ULKeyEventModifiers.MetaKey;
		}

		int keyCode = ULKeyCodes.GK_T;

		ULKeyEvent keyEvent = ULKeyEvent.Create(ULKeyEventType.KeyDown, modifiers, keyCode, keyCode, key.ToString(), key.ToString(), false, false, false);
		BrowserView.FireKeyEvent(keyEvent);
	}

	public void OnKeyUp(Keycode key, KeyModifier mod)
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

	public void Draw(SDL_Sharp.Renderer renderer, Rect destRect)
	{
		BrowserRenderer.Render();

		unsafe
		{
			byte* basePtr = BrowserView.Surface.Value.Bitmap.LockPixels();
			int pitch = (int)BrowserView.Surface.Value.Bitmap.RowBytes;

			Surface* surface = SDL.CreateRGBSurfaceFrom(basePtr, (int)BrowserView.Width, (int)BrowserView.Height, 32, pitch, 0x00ff0000, 0x0000ff00, 0x000000ff, 0xff000000);

			Texture texture = SDL.CreateTextureFromSurface(renderer, surface);

			SDL.RenderCopy(renderer, texture, null, &destRect);

			SDL.FreeSurface(surface);
			SDL.DestroyTexture(texture);

			BrowserView.Surface.Value.Bitmap.UnlockPixels();
		}
	}
}