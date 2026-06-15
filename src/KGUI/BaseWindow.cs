using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using SDL;
using System.Xml;
using System.Reflection;
using System.Xml.Serialization;

namespace KGUI
{
	public class BaseWindow
	{
		public string UUID = ""; //used to determine if the window is the same as another window

		protected unsafe SDL_Window* window;
		public SDL_WindowID windowID;
		public unsafe SDL_Renderer* renderer;

		public UIPanel panel;

		[XmlAttribute("title", typeof(string))]
		public string title;

		[XmlAttribute("width", typeof(int))]
		public int mWidth;

		[XmlAttribute("height", typeof(int))]
		public int mHeight;

		[XmlAttribute("resizable", typeof(bool))]
		public bool resizable;

		[XmlAttribute("minimumWidth", typeof(int))]
		public int minimumWidth;

		[XmlAttribute("minimumHeight", typeof(int))]
		public int minimumHeight;

		bool mMouseFocus;
		bool mKeyboardFocus;
		bool mMinimized;
		bool mShown;

		public bool MouseFocus { get { return mMouseFocus; } }
		public bool KeyboardFocus { get { return mKeyboardFocus; } }

		public virtual bool isPopupWindow { get { return false; } }

		private const int TITLE_BAR_HEIGHT = 21;
		private const int RESIZE_CORNER_SIZE = 4;
		private bool accountForWindowDecorations = true;

		private bool useCustomWindowDecorations = true;

		unsafe delegate* unmanaged[Cdecl]<SDL_Window*, SDL_Point*, nint, SDL_HitTestResult> hitTestCallbackDelegate;
		GCHandle hitTestGCHandle;

		unsafe delegate* unmanaged[Cdecl]<IntPtr, SDL_Event*, SDLBool> eventWatchCallbackDelegate;
		GCHandle eventWatchGCHandle;

		public bool isFadingIn = true;
		public bool isFadingOut = false;
		public float windowOpacity = 0.0f;

		// public unsafe SDL_Texture* windowBackgroundTexture; //9-slice texture
		// public unsafe SDL_Texture* minimizeButtonTexture;
		// public unsafe SDL_Texture* maximizeButtonTexture;
		// public unsafe SDL_Texture* closeButtonTexture;

		//DEBUG
		public double updateFrameTime = 0;
		public double drawFrameTime = 0;

		//todo: styles
		private int TitleBarHeight = 21;
		private int WindowBorderSize = 1;

		public unsafe BaseWindow(string uuid)
		{
			if (GetType() == typeof(BaseWindow))
			{
				throw new Exception("BaseWindow cannot be instantiated directly");
			}

			this.UUID = uuid;

			SDL_WindowFlags windowFlags = SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS | SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS;
			if (useCustomWindowDecorations)
			{
				windowFlags |= SDL_WindowFlags.SDL_WINDOW_BORDERLESS | SDL_WindowFlags.SDL_WINDOW_TRANSPARENT;
			}

			// if (isPopupWindow)
			// {
			// 	windowFlags |= WindowFlags.SkipTaskbar;
			// }
			window = SDL3.SDL_CreateWindow((Utf8String)title, mWidth, mHeight, windowFlags);
			if (window == null)
			{
				Console.WriteLine("Failed to create window: " + SDL3.SDL_GetError());
				return;
			}

			this.windowID = SDL3.SDL_GetWindowID(window);
			mMouseFocus = true;
			mKeyboardFocus = true;

			renderer = SDL3.SDL_CreateRenderer(window, (byte*)null);
			if (renderer == null)
			{
				Console.WriteLine("Failed to create renderer: " + SDL3.SDL_GetError());
				return;
			}
			

			panel = new UIPanel(this);
			LoadLayout(Assets.GetAssetPath("windows/" + GetType().Name + ".xml"));

			SDL3.SDL_SetWindowTitle(window, (Utf8String)title);
			int initialWidth = mWidth;
			int initialHeight = mHeight;
			if (accountForWindowDecorations && useCustomWindowDecorations)
			{
				initialWidth += WindowBorderSize * 2;
				initialHeight += WindowBorderSize + TitleBarHeight;
			}
			SDL3.SDL_SetWindowSize(window, initialWidth, initialHeight);
			SDL3.SDL_SetWindowResizable(window, resizable);
			if (minimumWidth > 0 && minimumHeight > 0) SDL3.SDL_SetWindowMinimumSize(window, minimumWidth, minimumHeight);

			//set window position to center of screen
			unsafe
			{
				SDL3.SDL_SetWindowPosition(window, (int)SDL3.SDL_WINDOWPOS_CENTERED, (int)SDL3.SDL_WINDOWPOS_CENTERED);
			}
		
			//ensure renderer targets the window
			unsafe
			{
				SDL3.SDL_SetRenderTarget(renderer, null);
			}
			
			mShown = true;
			SDL3.SDL_ShowWindow(window);

			eventWatchCallbackDelegate = &EventWatchCallback;
			eventWatchGCHandle = GCHandle.Alloc(this);
			SDL3.SDL_AddEventWatch(eventWatchCallbackDelegate, GCHandle.ToIntPtr(eventWatchGCHandle));

			// Set up window hit test callback
			if (useCustomWindowDecorations)
			{
				hitTestCallbackDelegate = &HitTestCallback;
				hitTestGCHandle = GCHandle.Alloc(this);
				SDL3.SDL_SetWindowHitTest(window, hitTestCallbackDelegate, (nint)hitTestGCHandle);
			}

			// SDL_Surface* windowBackgroundSurface = SDL3_image.IMG_Load(Assets.GetAssetPath("graphics/window.png"));
			// if (windowBackgroundSurface == null)
			// {
			// 	Console.WriteLine("Failed to create window surface: " + SDL3.SDL_GetError());
			// 	return;
			// }

			// windowBackgroundTexture = SDL3.SDL_CreateTextureFromSurface(renderer, windowBackgroundSurface);
			// if (windowBackgroundTexture == null)
			// {
			// 	Console.WriteLine("Failed to create window texture: " + SDL3.SDL_GetError());
			// 	return;
			// }
			
			// SDL_Surface* minimizeButtonSurface = SDL3_image.IMG_Load(Assets.GetAssetPath("graphics/minimize_button.png"));
			// if (minimizeButtonSurface == null)
			// {
			// 	Console.WriteLine("Failed to create minimize button surface: " + SDL3.SDL_GetError());
			// 	return;
			// }
			// minimizeButtonTexture = SDL3.SDL_CreateTextureFromSurface(renderer, minimizeButtonSurface);
			// if (minimizeButtonTexture == null)
			// {
			// 	Console.WriteLine("Failed to create minimize button texture: " + SDL3.SDL_GetError());
			// 	return;
			// }

			// SDL_Surface* maximizeButtonSurface = SDL3_image.IMG_Load(Assets.GetAssetPath("graphics/maximize_button.png"));
			// if (maximizeButtonSurface == null)
			// {
			// 	Console.WriteLine("Failed to create maximize button surface: " + SDL3.SDL_GetError());
			// 	return;
			// }
			// maximizeButtonTexture = SDL3.SDL_CreateTextureFromSurface(renderer, maximizeButtonSurface);
			// if (maximizeButtonTexture == null)
			// {
			// 	Console.WriteLine("Failed to create maximize button texture: " + SDL3.SDL_GetError());
			// 	return;
			// }

			// SDL_Surface* closeButtonSurface = SDL3_image.IMG_Load(Assets.GetAssetPath("graphics/close_button.png"));
			// if (closeButtonSurface == null)
			// {
			// 	Console.WriteLine("Failed to create close button surface: " + SDL3.SDL_GetError());
			// 	return;
			// }
			// closeButtonTexture = SDL3.SDL_CreateTextureFromSurface(renderer, closeButtonSurface);
			// if (closeButtonTexture == null)
			// {
			// 	Console.WriteLine("Failed to create close button texture: " + SDL3.SDL_GetError());
			// 	return;
			// }

			// SDL3.SDL_DestroySurface(windowBackgroundSurface);
			// SDL3.SDL_DestroySurface(minimizeButtonSurface);
			// SDL3.SDL_DestroySurface(maximizeButtonSurface);
			// SDL3.SDL_DestroySurface(closeButtonSurface);
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
		unsafe static SDL_HitTestResult HitTestCallback(SDL_Window* window, SDL_Point* point, nint data)
		{
			if (data == nint.Zero)
				return SDL_HitTestResult.SDL_HITTEST_NORMAL;

			GCHandle handle = GCHandle.FromIntPtr(data);
			BaseWindow instance = (BaseWindow)handle.Target!;

			int x = point->x;
			int y = point->y;

			// check minimize and close buttons (they should be normal so clicks work)
			SDL_FRect minimizeRect = new SDL_FRect { x = instance.mWidth - 33, y = 0, w = 15, h = 21 };
			SDL_FRect closeRect = new SDL_FRect { x = instance.mWidth - 18, y = 0, w = 15, h = 21 };
			
			if (x >= minimizeRect.x && y >= minimizeRect.y && x <= minimizeRect.x + minimizeRect.w && y <= minimizeRect.y + minimizeRect.h)
			{
				return SDL_HitTestResult.SDL_HITTEST_NORMAL;
			}
			
			if (x >= closeRect.x && y >= closeRect.y && x <= closeRect.x + closeRect.w && y <= closeRect.y + closeRect.h)
			{
				return SDL_HitTestResult.SDL_HITTEST_NORMAL;
			}

			// titlebar is draggable
			if (y < TITLE_BAR_HEIGHT)
			{
				return SDL_HitTestResult.SDL_HITTEST_DRAGGABLE;
			}

			// check resize corners and edges if window is resizable
			bool isResizable = SDL3.SDL_GetWindowFlags(window).HasFlag(SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
			if (isResizable)
			{
				// corners
				if (x < RESIZE_CORNER_SIZE && y < RESIZE_CORNER_SIZE)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_TOPLEFT;
				if (x >= instance.mWidth - RESIZE_CORNER_SIZE && y < RESIZE_CORNER_SIZE)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_TOPRIGHT;
				if (x < RESIZE_CORNER_SIZE && y >= instance.mHeight - RESIZE_CORNER_SIZE)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_BOTTOMLEFT;
				if (x >= instance.mWidth - RESIZE_CORNER_SIZE && y >= instance.mHeight - RESIZE_CORNER_SIZE)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_BOTTOMRIGHT;

				if (x >= instance.mWidth - 21 && y >= instance.mHeight - 21)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_BOTTOMRIGHT;

				// edges
				if (x < RESIZE_CORNER_SIZE)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_LEFT;
				if (x >= instance.mWidth - RESIZE_CORNER_SIZE)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_RIGHT;
				if (y < RESIZE_CORNER_SIZE)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_TOP;
				if (y >= instance.mHeight - RESIZE_CORNER_SIZE)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_BOTTOM;
			}

			// rest of window is normal
			return SDL_HitTestResult.SDL_HITTEST_NORMAL;
		}

		public void SetWindowPosition(int x, int y)
		{
			unsafe
			{
				SDL3.SDL_SetWindowPosition(window, x, y);
			}
		}

		public void SetWindowPositionCentered()
		{
			unsafe
			{
				SDL3.SDL_SetWindowPosition(window, (int)SDL3.SDL_WINDOWPOS_CENTERED, (int)SDL3.SDL_WINDOWPOS_CENTERED);
			}
		}

		public void SetWindowSize(int width, int height, bool UpdateWindow = true)
		{
			unsafe
			{
				int totalWidth = width;
				int totalHeight = height;
				if (accountForWindowDecorations && useCustomWindowDecorations)
				{
					totalWidth += WindowBorderSize * 2;
					totalHeight += WindowBorderSize + TitleBarHeight;
				}
				if (UpdateWindow) SDL3.SDL_SetWindowSize(window, totalWidth, totalHeight);
				mWidth = totalWidth;
				mHeight = totalHeight;
			}
		}

		public void SetTitle(string title)
		{
			this.title = title;
			unsafe
			{
				SDL3.SDL_SetWindowTitle(window, (Utf8String)title);
			}
		}

		public void LoadLayout(string layoutPath)
		{
			if (!File.Exists(layoutPath))
			{
				throw new Exception("Failed to load layout: " + layoutPath + " does not exist");
			}

			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(layoutPath);
			if (xmlDocument.DocumentElement == null) throw new Exception("Failed to load layout: " + layoutPath);
			XmlNode rootNode = xmlDocument.DocumentElement;
			if (rootNode.Name != GetType().Name) throw new Exception("Failed to load layout: " + layoutPath + " is not a " + GetType().Name);

			//apply attributes to this window
			foreach (XmlAttribute attribute in rootNode.Attributes)
			{
				foreach (FieldInfo field in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
				{
					XmlAttributeAttribute? xmlAttribute = field.GetCustomAttribute<XmlAttributeAttribute>();
					string xmlName = xmlAttribute?.AttributeName ?? field.Name;
					if (!string.Equals(xmlName, attribute.Name, StringComparison.OrdinalIgnoreCase)) continue;

					object convertedValue = Convert.ChangeType(attribute.InnerText, field.FieldType);
					field.SetValue(this, convertedValue);
				}

				foreach (PropertyInfo property in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
				{
					if (!property.CanWrite) continue;
					XmlAttributeAttribute? xmlAttribute = property.GetCustomAttribute<XmlAttributeAttribute>();
					string xmlName = xmlAttribute?.AttributeName ?? property.Name;
					if (!string.Equals(xmlName, attribute.Name, StringComparison.OrdinalIgnoreCase)) continue;

					object convertedValue = Convert.ChangeType(attribute.InnerText, property.PropertyType);
					property.SetValue(this, convertedValue);
				}
			}

			//load layout from root node
			panel.LoadLayout(rootNode);
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

			unsafe
			{
				SDL3.SDL_SetWindowOpacity(window, windowOpacity);
			}

			panel.Update(deltaTime);
		}

		public unsafe virtual void PreDraw()
		{
			//if window is not shown, dont draw
			if (!mShown)
			{
				return;
			}

			if (isPopupWindow) return;

			//ensure renderer targets the window
			SDL3.SDL_SetRenderTarget(renderer, null);

			//set clear color and clear renderer
			SDL3.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 0);
			SDL3.SDL_RenderClear(renderer);

			//draw 9-slice background texture
			if (useCustomWindowDecorations)
			{
				// SDL_FRect destRect = new SDL_FRect { x = 0, y = 0, w = mWidth, h = mHeight };
				// SDL3.SDL_RenderTexture9Grid(
				// 	renderer,
				// 	windowBackgroundTexture,
				// 	null,
				// 	8.0f, 8.0f, 27.0f, 8.0f, 0.0f,
				// 	&destRect
				// );

				SDL3.SDL_SetRenderDrawColor(renderer, 70, 70, 70, 255);
				SDL_FRect contentRect = new SDL_FRect { x = 0, y = 0, w = mWidth, h = mHeight };
				SDL3.SDL_RenderFillRect(renderer, &contentRect);

				//draw window border
				SDL3.SDL_SetRenderDrawColor(renderer, 104, 106, 101, 255);
				SDL_FRect borderRect = new SDL_FRect { x = 0, y = 0, w = mWidth, h = mHeight };
				SDL3.SDL_RenderRect(renderer, &borderRect);

				//draw window title bar
				SDL3.SDL_SetRenderDrawColor(renderer, 90, 106, 80, 255);
				SDL_FRect titleBarRect = new SDL_FRect { x = 0, y = 0, w = mWidth, h = 21 };
				SDL3.SDL_RenderFillRect(renderer, &titleBarRect);
				//panel.RootControl.DrawBox(0, 0, mWidth, 21, Color.FromArgb(255, 90, 106, 80));

				//title
				//panel.DrawText(title, 6, 6, Color.FromArgb(255, 216, 222, 211));

				//corners
				SDL3.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 0);
				SDL3.SDL_RenderPoint(renderer, 0, 0);
				SDL3.SDL_RenderPoint(renderer, mWidth - 1, 0);
				SDL3.SDL_RenderPoint(renderer, 0, mHeight - 1);
				SDL3.SDL_RenderPoint(renderer, mWidth - 1, mHeight - 1);

			}

			panel.Draw();
		}

		public virtual void Draw()
		{
		}

		public unsafe void PostDraw()
		{
			if (useCustomWindowDecorations)
			{
				// //controls
				// SDL_FRect minimizeRect = new SDL_FRect { x = mWidth - 58, y = 6, w = 16, h = 16 };
				// SDL3.SDL_RenderTexture(renderer, minimizeButtonTexture, null, &minimizeRect);
				// SDL_FRect maximizeRect = new SDL_FRect { x = mWidth - 40, y = 6, w = 16, h = 16 };
				// SDL3.SDL_RenderTexture(renderer, maximizeButtonTexture, null, &maximizeRect);
				// SDL_FRect closeRect = new SDL_FRect { x = mWidth - 22, y = 6, w = 16, h = 16 };
				// SDL3.SDL_RenderTexture(renderer, closeButtonTexture, null, &closeRect);

				//minimize button
				Color minimizeColor = Color.FromArgb(216, 222, 211);
				SDL_FRect minimizeMouseRect = new SDL_FRect { x = mWidth - 33, y = 0, w = 15, h = 21 };
				if (panel.MouseX >= minimizeMouseRect.x && panel.MouseY >= minimizeMouseRect.y && panel.MouseX <= minimizeMouseRect.x + minimizeMouseRect.w && panel.MouseY <= minimizeMouseRect.y + minimizeMouseRect.h) minimizeColor = Color.FromArgb(141, 146, 121);
				panel.RootControl.DrawBox(mWidth - 29 - GetInternalX(), 14 - GetInternalY(), 7, 2, minimizeColor);

				//close button
				int closeButtonX = mWidth - 15;
				int closeButtonY = 7;
				Color closeColor = Color.FromArgb(216, 222, 211);
				SDL_FRect closeMouseRect = new SDL_FRect { x = mWidth - 18, y = 0, w = 15, h = 21 };
				if (panel.MouseX >= closeMouseRect.x && panel.MouseY >= closeMouseRect.y && panel.MouseX <= closeMouseRect.x + closeMouseRect.w && panel.MouseY <= closeMouseRect.y + closeMouseRect.h) closeColor = Color.FromArgb(141, 146, 121);
				for (int i = 0; i < 8; i++)
				{
					panel.RootControl.DrawBox(closeButtonX + i - GetInternalX(), closeButtonY + i - GetInternalY(), 2, 2, closeColor);
					panel.RootControl.DrawBox(closeButtonX + 7 - i - GetInternalX(), closeButtonY + i - GetInternalY(), 2, 2, closeColor);
				}

				//title
				panel.DrawText(title, 6, 6, Color.FromArgb(216, 222, 211));
			}

			unsafe
			{
				SDL3.SDL_RenderPresent(renderer);
			}
		}

		public void ForceUpdateDrawLoop()
		{
			Update(0.016f);
			PreDraw();
			Draw();
			PostDraw();
		}

		public int GetInternalX()
		{
			return useCustomWindowDecorations && accountForWindowDecorations ? WindowBorderSize : 0;
		}

		public int GetInternalY()
		{
			return useCustomWindowDecorations && accountForWindowDecorations ? TitleBarHeight : 0;
		}

		public int GetInternalWidth()
		{
			return useCustomWindowDecorations && accountForWindowDecorations ? mWidth - (WindowBorderSize * 2) : mWidth;
		}

		public int GetInternalHeight()
		{
			return useCustomWindowDecorations && accountForWindowDecorations ? mHeight - TitleBarHeight : mHeight;
		}

		public void CloseWindow()
		{
			unsafe
			{
				// clear hit test callback before destroying window
				SDL3.SDL_SetWindowHitTest(window, null, nint.Zero);
				if (hitTestGCHandle.IsAllocated)
				{
					hitTestGCHandle.Free();
				}
				// SDL3.SDL_DestroyTexture(windowBackgroundTexture);
				// SDL3.SDL_DestroyTexture(minimizeButtonTexture);
				// SDL3.SDL_DestroyTexture(maximizeButtonTexture);
				// SDL3.SDL_DestroyTexture(closeButtonTexture);
				SDL3.SDL_DestroyWindow(window);
				SDL3.SDL_DestroyRenderer(renderer);
			}
		}

		public void HideWindow()
		{
			unsafe
			{
				SDL3.SDL_HideWindow(window);
			}
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

		public virtual void OnKeyDown(SDL_Keycode key, SDL_Keymod modifier)
		{
		}

		public virtual void OnKeyUp(SDL_Keycode key, SDL_Keymod modifier)
		{
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
		unsafe static SDLBool EventWatchCallback(IntPtr userdata, SDL_Event* sdlEvent)
		{
			BaseWindow instance = GCHandle.FromIntPtr(userdata).Target as BaseWindow
				?? throw new InvalidCastException("userdata GCHandle.Target is not a BaseWindow");
			if (instance.windowID != sdlEvent->window.windowID) return true;

			switch ((SDL_EventType)sdlEvent->type)
			{
				case SDL_EventType.SDL_EVENT_WINDOW_SAFE_AREA_CHANGED:
				case SDL_EventType.SDL_EVENT_WINDOW_EXPOSED:
					instance.ForceUpdateDrawLoop();
					break;				
				case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
				case SDL_EventType.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
					instance.SetWindowSize(sdlEvent->window.data1, sdlEvent->window.data2, false);
					break;
			}
			return true;
		}

		public unsafe void HandleSDLEvent(SDL_Event e)
		{
			//If an event was detected for this window
			if (e.window.windowID != SDL3.SDL_GetWindowID(window)) return;

			//Caption update flag
			bool updateCaption = false;
			if (e.type >= (uint)SDL_EventType.SDL_EVENT_WINDOW_FIRST && e.type <= (uint)SDL_EventType.SDL_EVENT_WINDOW_LAST)
			{
				switch (e.window.type)
				{
					//Window appeared
					case SDL_EventType.SDL_EVENT_WINDOW_SHOWN:
						mShown = true;
						break;

					//Window disappeared
					case SDL_EventType.SDL_EVENT_WINDOW_HIDDEN:
						mShown = false;
						break;

					case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
						if (e.window.data1 <= 0 || e.window.data2 <= 0) break; //ignore minimize events that set size to 0,0
						mWidth = e.window.data1;
						mHeight = e.window.data2;
						SDL3.SDL_RenderPresent(renderer);
						break;

					//Get new dimensions and repaint
					case SDL_EventType.SDL_EVENT_WINDOW_DISPLAY_CHANGED:
						if (e.window.data1 <= 0 || e.window.data2 <= 0) break; //ignore minimize events that set size to 0,0
						mWidth = e.window.data1;
						mHeight = e.window.data2;

						SDL3.SDL_RenderPresent(renderer);
						break;

					//Repaint on expose
					case SDL_EventType.SDL_EVENT_WINDOW_EXPOSED:
						SDL3.SDL_RenderPresent(renderer);
						break;

					//Mouse enter
					case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER:
						mMouseFocus = true;
						updateCaption = true;
						break;

					//Mouse exit
					case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
						mMouseFocus = false;
						updateCaption = true;
						break;

					//Keyboard focus gained
					case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
						mKeyboardFocus = true;
						updateCaption = true;
						break;

					//Keyboard focus lost
					case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
						mKeyboardFocus = false;
						updateCaption = true;
						break;

					//Window minimized
					case SDL_EventType.SDL_EVENT_WINDOW_MINIMIZED:
						mMinimized = true;
						break;

					//Window maxized
					case SDL_EventType.SDL_EVENT_WINDOW_MAXIMIZED:
						mMinimized = false;
						break;

					//Window restored
					case SDL_EventType.SDL_EVENT_WINDOW_RESTORED:
						mMinimized = false;
						break;

					case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
						WindowManager.Instance.CloseWindow(this);
						break;
				}
			}
			else
			{
				switch (e.Type)
				{
					case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
						panel.HandleSDLEvent(e);
						break;
					case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
						panel.HandleSDLEvent(e);
						OnMouseDown((int)e.motion.x, (int)e.motion.y, (int)e.button.button);
						break;
					case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
						//minimize button
						SDL_FRect minimizeRect = new SDL_FRect{ x = mWidth - 33, y = 0, w = 15, h = 21};
						SDL_FRect closeRect = new SDL_FRect{ x = mWidth - 18, y = 0, w = 15, h = 21};
						if (e.motion.x >= minimizeRect.x && e.motion.y >= minimizeRect.y && e.motion.x <= minimizeRect.x + minimizeRect.w && e.motion.y <= minimizeRect.y + minimizeRect.h)
						{
							SDL3.SDL_MinimizeWindow(window);
						}
						else if (e.motion.x >= closeRect.x && e.motion.y >= closeRect.y && e.motion.x <= closeRect.x + closeRect.w && e.motion.y <= closeRect.y + closeRect.h)
						{
							WindowManager.Instance.CloseWindow(this);
						}

						panel.HandleSDLEvent(e);
						OnMouseUp((int)e.motion.x, (int)e.motion.y, (int)e.button.button);
						break;
					case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
						panel.HandleSDLEvent(e);
						OnMouseScroll((int)e.wheel.x, (int)e.wheel.y);
						break;
					case SDL_EventType.SDL_EVENT_KEY_DOWN:
						panel.HandleSDLEvent(e);
						OnKeyDown(e.key.key, e.key.mod);
						break;
					case SDL_EventType.SDL_EVENT_KEY_UP:
						panel.HandleSDLEvent(e);
						OnKeyUp(e.key.key, e.key.mod);
						break;
				}
			}
		}

		public void FocusWindow()
		{
			//if window is minimized, restore it
			unsafe
			{
				if (mMinimized) SDL3.SDL_RestoreWindow(window);

				mShown = true;
				SDL3.SDL_ShowWindow(window);

				//Move window forward
				SDL3.SDL_RaiseWindow(window);
			}
		}
	}
}