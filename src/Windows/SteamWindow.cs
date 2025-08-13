using SDL_Sharp;

public class SteamWindow
{
	public Steam steam;

	protected Window window;
	public Renderer renderer;

	public UIPanel panel;

	int windowID;
	public string title = "";


	protected int mWidth;
	protected int mHeight;

	bool mMouseFocus;
	bool mKeyboardFocus;
	bool mFullScreen;
	bool mMinimized;
	bool mShown;

	public bool MouseFocus { get { return mMouseFocus; } }
	public bool KeyboardFocus { get { return mKeyboardFocus; } }

	public virtual bool isPopupWindow { get { return false; } }

	private const int TITLE_BAR_HEIGHT = 21;
	private const int RESIZE_CORNER_SIZE = 20;

	private HitTest hitTestCallback;

	public bool isFadingIn = true;
	public bool isFadingOut = false;
	public float windowOpacity = 0.0f;

	public SteamWindow(Steam steam, string title, int width, int height, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0)
	{
		this.steam = steam;

		WindowFlags windowFlags = WindowFlags.Shown | WindowFlags.InputFocus | WindowFlags.MouseFocus | WindowFlags.Borderless;

		if (isPopupWindow)
		{
			windowFlags |= WindowFlags.PopupMenu;
		}

		window = SDL.CreateWindow(title, SDL.WINDOWPOS_UNDEFINED, SDL.WINDOWPOS_UNDEFINED, width, height, windowFlags);
		this.title = title;

		mMouseFocus = true;
		mKeyboardFocus = true;
		mWidth = width;
		mHeight = height;

		renderer = SDL.CreateRenderer(window, -1, RendererFlags.Accelerated | RendererFlags.PresentVsync);
		panel = new UIPanel(this);

		mShown = true;

		if (resizable) SDL.SetWindowResizable(window, true);
		if (minimumWidth > 0 && minimumHeight > 0) SDL.SetWindowMinimumSize(window, minimumWidth, minimumHeight);

		// Set up window hit test callback
		unsafe
		{
			hitTestCallback = (Window win, Point* area, void* data) =>
			{
				if (area->Y < TITLE_BAR_HEIGHT && area->X < mWidth - 35)
				{
					return HitTestResult.Draggable;
				}

				// Bottom right corner resize area
				if (resizable)
				{
					if (area->X >= mWidth - RESIZE_CORNER_SIZE && area->Y >= mHeight - RESIZE_CORNER_SIZE)
					{
						return HitTestResult.ResizeBottomRight;
					}
				}

				return HitTestResult.Normal;
			};
			if (!isPopupWindow) SDL.SetWindowHitTest(window, hitTestCallback, null);
		}
	}

	public void SetWindowTitle(string title)
	{
		this.title = title;
		SDL.SetWindowTitle(window, title);
	}

	public void SetWindowPosition(int x, int y)
	{
		SDL.SetWindowPosition(window, x, y);
	}

	public void SetWindowSize(int width, int height)
	{
		SDL.SetWindowSize(window, width, height);
	}

	public virtual void Update(float deltaTime)
	{
		//if window is not shown, dont update
		if (!mShown)
		{
			return;
		}

		const float FADE_SPEED = 8.0f;
		if (isFadingIn)
		{
			windowOpacity += deltaTime * FADE_SPEED;
			if (windowOpacity >= 1.0f)
			{
				isFadingIn = false;
				windowOpacity = 1.0f;
				FocusWindow();
			}
		}
		else if (isFadingOut)
		{
			windowOpacity -= deltaTime * FADE_SPEED;
			if (windowOpacity <= 0.0f)
			{
				isFadingOut = false;
				windowOpacity = 0.0f;
			}
		}

		SDL.SetWindowOpacity(window, windowOpacity);

		panel.Update(deltaTime);
	}

	public virtual void Draw()
	{
		//if window is not shown, dont draw
		if (!mShown)
		{
			return;
		}

		if (isPopupWindow) return;

		//draw window content
		SDL.SetRenderDrawColor(renderer, 70, 70, 70, 255);
		Rect contentRect = new Rect(0, 0, mWidth, mHeight);
		SDL.RenderFillRect(renderer, ref contentRect);

		//draw window border
		SDL.SetRenderDrawColor(renderer, 104, 106, 101, 255);
		Rect borderRect = new Rect(0, 0, mWidth, mHeight);
		SDL.RenderDrawRect(renderer, ref borderRect);

		//draw window title bar
		panel.DrawBox(0, 0, mWidth, 21, new Color(90, 106, 80, 255));

		//title
		panel.DrawText(title, 6, 6, new Color(216, 222, 211, 255));

		//corners
		SDL.SetRenderDrawColor(renderer, 0, 0, 0, 0);
		SDL.RenderDrawPoint(renderer, 0, 0);
		SDL.RenderDrawPoint(renderer, mWidth - 1, 0);
		SDL.RenderDrawPoint(renderer, 0, mHeight - 1);
		SDL.RenderDrawPoint(renderer, mWidth - 1, mHeight - 1);

		//minimize button
		Color minimizeColor = new Color(216, 222, 211, 255);
		Rect minimizeMouseRect = new Rect(mWidth - 33, 0, 15, 21);
		if (panel.MouseX >= minimizeMouseRect.X && panel.MouseY >= minimizeMouseRect.Y && panel.MouseX <= minimizeMouseRect.X + minimizeMouseRect.Width && panel.MouseY <= minimizeMouseRect.Y + minimizeMouseRect.Height) minimizeColor = new Color(141, 146, 121, 255);
		panel.DrawBox(mWidth - 29, 14, 7, 2, minimizeColor);

		//close button
		int closeButtonX = mWidth - 15;
		int closeButtonY = 7;
		Color closeColor = new Color(216, 222, 211, 255);
		Rect closeMouseRect = new Rect(mWidth - 18, 0, 15, 21);
		if (panel.MouseX >= closeMouseRect.X && panel.MouseY >= closeMouseRect.Y && panel.MouseX <= closeMouseRect.X + closeMouseRect.Width && panel.MouseY <= closeMouseRect.Y + closeMouseRect.Height) closeColor = new Color(141, 146, 121, 255);
		for (int i = 0; i < 8; i++)
		{
			panel.DrawBox(closeButtonX + i, closeButtonY + i, 2, 2, closeColor);
			panel.DrawBox(closeButtonX + 7 - i, closeButtonY + i, 2, 2, closeColor);
		}
	}

	public void CloseWindow()
	{
		SDL.DestroyWindow(window);
		SDL.DestroyRenderer(renderer);
	}

	public void HideWindow()
	{
		SDL.HideWindow(window);
		mShown = false;
	}

	public virtual void OnMouseScroll(int scrollX, int scrollY)
	{
	}

	public virtual void OnMouseDown(int x, int y, int button)
	{
	}

	public virtual void OnMouseUp(int x, int y, int button)
	{
	}

	public virtual void OnKeyDown(SDL_Sharp.Keycode key, SDL_Sharp.KeyModifier modifier)
	{
	}

	public virtual void OnKeyUp(SDL_Sharp.Keycode key, SDL_Sharp.KeyModifier modifier)
	{
	}

	public void HandleSDLEvent(SDL_Sharp.Event e)
	{
		//If an event was detected for this window
		if (e.Window.WindowID != SDL.GetWindowID(window)) return;

		//Caption update flag
		bool updateCaption = false;
		if (e.Type == SDL_Sharp.EventType.WindowEvent)
		{
			switch (e.Window.Evt)
			{
				//Window appeared
				case SDL_Sharp.WindowEventID.Shown:
					mShown = true;
					break;

				//Window disappeared
				case SDL_Sharp.WindowEventID.Hidden:
					mShown = false;
					break;

				case SDL_Sharp.WindowEventID.Resized:
					mWidth = e.Window.Data1;
					mHeight = e.Window.Data2;
					SDL.RenderPresent(renderer);
					break;

				//Get new dimensions and repaint
				case SDL_Sharp.WindowEventID.SizeChanged:
					mWidth = e.Window.Data1;
					mHeight = e.Window.Data2;
					SDL.RenderPresent(renderer);
					break;

				//Repaint on expose
				case SDL_Sharp.WindowEventID.Exposed:
					SDL.RenderPresent(renderer);
					break;

				//Mouse enter
				case SDL_Sharp.WindowEventID.Enter:
					mMouseFocus = true;
					updateCaption = true;
					break;

				//Mouse exit
				case SDL_Sharp.WindowEventID.Leave:
					mMouseFocus = false;
					updateCaption = true;
					break;

				//Keyboard focus gained
				case SDL_Sharp.WindowEventID.FocusGained:
					mKeyboardFocus = true;
					updateCaption = true;
					break;

				//Keyboard focus lost
				case SDL_Sharp.WindowEventID.FocusLost:
					mKeyboardFocus = false;
					updateCaption = true;
					break;

				//Window minimized
				case SDL_Sharp.WindowEventID.Minimized:
					mMinimized = true;
					break;

				//Window maxized
				case SDL_Sharp.WindowEventID.Maximized:
					mMinimized = false;
					break;

				//Window restored
				case SDL_Sharp.WindowEventID.Restored:
					mMinimized = false;
					break;

				case SDL_Sharp.WindowEventID.Close:
					steam.PendingWindowsToRemove.Add(this);
					break;
			}
		}
		else
		{
			switch (e.Type)
			{
				case SDL_Sharp.EventType.MouseMotion:
					panel.HandleSDLEvent(e);
					break;
				case SDL_Sharp.EventType.MouseButtonDown:
					panel.HandleSDLEvent(e);
					OnMouseDown(e.Motion.X, e.Motion.Y, (int)e.Button.Button);
					break;
				case SDL_Sharp.EventType.MouseButtonUp:
					//minimize button
					Rect minimizeRect = new Rect(mWidth - 33, 0, 15, 21);
					Rect closeRect = new Rect(mWidth - 18, 0, 15, 21);
					if (e.Motion.X >= minimizeRect.X && e.Motion.Y >= minimizeRect.Y && e.Motion.X <= minimizeRect.X + minimizeRect.Width && e.Motion.Y <= minimizeRect.Y + minimizeRect.Height)
					{
						SDL.MinimizeWindow(window);
					}
					else if (e.Motion.X >= closeRect.X && e.Motion.Y >= closeRect.Y && e.Motion.X <= closeRect.X + closeRect.Width && e.Motion.Y <= closeRect.Y + closeRect.Height)
					{
						steam.PendingWindowsToRemove.Add(this);
					}

					panel.HandleSDLEvent(e);
					OnMouseUp(e.Motion.X, e.Motion.Y, (int)e.Button.Button);
					break;
				case SDL_Sharp.EventType.MouseWheel:
					panel.HandleSDLEvent(e);
					OnMouseScroll(e.Wheel.X, e.Wheel.Y);
					break;
				case SDL_Sharp.EventType.KeyDown:
					panel.HandleSDLEvent(e);
					OnKeyDown(e.Keyboard.Keysym.Sym, e.Keyboard.Keysym.Mod);
					break;
				case SDL_Sharp.EventType.KeyUp:
					panel.HandleSDLEvent(e);
					OnKeyUp(e.Keyboard.Keysym.Sym, e.Keyboard.Keysym.Mod);
					break;
			}
		}
	}

	public void FocusWindow()
	{
		//if window is minimized, restore it
		if (mMinimized) SDL.RestoreWindow(window);

		mShown = true;
		SDL.ShowWindow(window);

		//Move window forward
		SDL.RaiseWindow(window);
	}
}