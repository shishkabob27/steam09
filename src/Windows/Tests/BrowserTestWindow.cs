using SDL_Sharp;
using UltralightNet;
using UltralightNet.AppCore;

public class BrowserTestWindow : SteamWindow
{
	UltralightNet.Renderer BrowserRenderer;
	View BrowserView;


	int lastX = 0;
	int lastY = 0;

	bool LeftMouseDown = false;
	bool RightMouseDown = false;
	bool MiddleMouseDown = false;

	public BrowserTestWindow(Steam steam, string title, int width, int height, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0) : base(steam, title, width, height, resizable, minimumWidth, minimumHeight)
	{
		AppCoreMethods.SetPlatformFontLoader();

		// Create Renderer
		var cfg = new ULConfig();
		BrowserRenderer = ULPlatform.CreateRenderer(cfg);

		// Create View
		BrowserView = BrowserRenderer.CreateView(1980, 1024);

		// Load URL

		bool loaded = false;

		BrowserView.OnFinishLoading += (_, _, _) =>
		{
			loaded = true;
		};

		BrowserView.URL = "https://store.steampowered.com/";
		BrowserView.OnChangeURL += (url) =>
		{
			Console.WriteLine(url);
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

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		BrowserView.Resize((uint)mWidth - 2, (uint)mHeight - 22);

		int x, y;
		MouseButtonMask button = SDL.GetMouseState(out x, out y);

		if (x != lastX || y != lastY)
		{
			ULMouseEvent mouseEvent = new ULMouseEvent();
			mouseEvent.Type = ULMouseEventType.MouseMoved;
			mouseEvent.X = x - 2;
			mouseEvent.Y = y - 20;
			BrowserView.FireMouseEvent(mouseEvent);
			lastX = x - 2;
			lastY = y;
		}

		BrowserRenderer.Update();
	}

	public override void OnMouseDown(int x, int y, int button)
	{
		ULMouseEvent mouseEvent = new ULMouseEvent();
		mouseEvent.X = x - 2;
		mouseEvent.Y = y - 20;
		mouseEvent.Type = ULMouseEventType.MouseDown;

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

	public override void OnMouseUp(int x, int y, int button)
	{
		ULMouseEvent mouseEvent = new ULMouseEvent();
		mouseEvent.X = x - 2;
		mouseEvent.Y = y - 20;
		mouseEvent.Type = ULMouseEventType.MouseUp;

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
	public override void OnMouseScroll(int scrollX, int scrollY)
	{
		ULScrollEvent scrollEvent = new ULScrollEvent();
		scrollEvent.Type = ULScrollEventType.ByPixel;
		scrollEvent.DeltaX = scrollX * 50;
		scrollEvent.DeltaY = scrollY * 50;
		BrowserView.FireScrollEvent(scrollEvent);
	}

	public override void OnKeyDown(Keycode key, KeyModifier mod)
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

		int keyCode = ULKeyCodes.GK_M;

		ULKeyEvent keyEvent = ULKeyEvent.Create(ULKeyEventType.KeyDown, modifiers, keyCode, keyCode, key.ToString(), key.ToString(), false, false, false);
		BrowserView.FireKeyEvent(keyEvent);
	}

	public override void OnKeyUp(Keycode key, KeyModifier mod)
	{
		Console.WriteLine(key);

		int keyCode = ULKeyCodes.GK_M;

		ULKeyEvent keyEvent = ULKeyEvent.Create(ULKeyEventType.KeyUp, 0, keyCode, keyCode, key.ToString(), key.ToString(), false, false, false);
		BrowserView.FireKeyEvent(keyEvent);
	}

	public override void Draw()
	{
		base.Draw();

		BrowserRenderer.Render();

		unsafe
		{
			byte* basePtr = BrowserView.Surface.Value.Bitmap.LockPixels();
			int pitch = (int)BrowserView.Surface.Value.Bitmap.RowBytes; // Number of bytes per row

			Surface* surface = SDL.CreateRGBSurfaceFrom(basePtr, (int)BrowserView.Width, (int)BrowserView.Height, 32, pitch, 0x00ff0000, 0x0000ff00, 0x000000ff, 0xff000000);

			Texture texture = SDL.CreateTextureFromSurface(renderer, surface);

			Rect destRect = new Rect();
			destRect.X = 1;
			destRect.Y = 21;
			destRect.Width = (int)BrowserView.Width;
			destRect.Height = (int)BrowserView.Height;

			SDL.RenderCopy(renderer, texture, null, &destRect);

			SDL.FreeSurface(surface);
			SDL.DestroyTexture(texture);

			BrowserView.Surface.Value.Bitmap.UnlockPixels();
		}


		SDL.RenderPresent(renderer);
	}
}