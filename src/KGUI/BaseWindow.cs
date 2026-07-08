using System.Drawing;
using SDL;
using System.Xml;
using System.Reflection;
using System.Xml.Serialization;
using System.Security.Cryptography.X509Certificates;

namespace KGUI
{
	public class BaseWindow
	{
		public WindowEnvironment windowEnvironment;
		public BackendWindow backendWindow;

		public string UUID = ""; //used to determine if the window is the same as another window

		private bool _initialized = false;

		public UIPanel panel;

		[XmlAttribute("title", typeof(string))]
		public string titleDefinition = "";

		[XmlAttribute("width", typeof(int))]
		public int widthDefinition;

		[XmlAttribute("height", typeof(int))]
		public int heightDefinition;

		[XmlAttribute("resizable", typeof(bool))]
		public bool resizableDefinition;

		[XmlAttribute("minimumWidth", typeof(int))]
		public int minimumWidthDefinition;

		[XmlAttribute("minimumHeight", typeof(int))]
		public int minimumHeightDefinition;

		public bool MouseFocus = true;
		public bool KeyboardFocus = true;

		public virtual bool isPopupWindow { get { return false; } }

		private bool accountForWindowDecorations = true;

		private bool useCustomWindowDecorations = true;

		public bool isFadingIn = true;
		public bool isFadingOut = false;
		public float windowOpacity = 0.0f;
		int updateCount = 0;

		//DEBUG
		#if DEBUG
		double updateFrameTime = 0;
		double drawFrameTime = 0;
		bool debugFrameTime = false;
		#endif

		//todo: styles
		public int TitleBarHeight = 21;
		public int WindowBorderSize = 1;

		public BaseWindow(string uuid)
		{
			this.UUID = uuid;
		}

		public void Initialize(WindowEnvironment windowEnv, BackendWindow backendWindow)
		{
			this.windowEnvironment = windowEnv;
			this.backendWindow = backendWindow;

			backendWindow.CreateWindow(this, titleDefinition, widthDefinition, heightDefinition, resizableDefinition, minimumWidthDefinition, minimumHeightDefinition, useCustomWindowDecorations, isPopupWindow);
		
			panel = new UIPanel(this);
			LoadLayout(Assets.GetAssetPath("windows/" + GetType().Name + ".xml"));

			int initialWidth = widthDefinition;
			int initialHeight = heightDefinition;
			if (accountForWindowDecorations && useCustomWindowDecorations)
			{
				initialWidth += WindowBorderSize * 2;
				initialHeight += WindowBorderSize + TitleBarHeight;
			}
			backendWindow.SetWindowSize(initialWidth, initialHeight);
			backendWindow.SetWindowPositionCentered();
			
			backendWindow.SetWindowOpacity(0.0f);

			backendWindow.SetWindowResizable(resizableDefinition, minimumWidthDefinition, minimumHeightDefinition);

			_initialized = true;

			PostInitialize();
		}

		public virtual void PostInitialize()
		{
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
					if (xmlAttribute == null) continue;

					string xmlName = xmlAttribute?.AttributeName ?? field.Name;
					if (!string.Equals(xmlName, attribute.Name, StringComparison.OrdinalIgnoreCase)) continue;

					object convertedValue = Convert.ChangeType(attribute.InnerText, field.FieldType);
					field.SetValue(this, convertedValue);
				}

				foreach (PropertyInfo property in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
				{
					if (!property.CanWrite) continue;
					XmlAttributeAttribute? xmlAttribute = property.GetCustomAttribute<XmlAttributeAttribute>();
					if (xmlAttribute == null) continue;

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
			UpdateFade(deltaTime);

			#if DEBUG
			System.Diagnostics.Stopwatch? watch = null;
			if (debugFrameTime)
				watch = System.Diagnostics.Stopwatch.StartNew();
			#endif

			panel.Update(deltaTime);

			#if DEBUG
			if (debugFrameTime)
			{
				watch?.Stop();
				updateFrameTime = watch?.Elapsed.TotalMilliseconds ?? 0;
			}
			#endif
		}

		void UpdateFade(float deltaTime)
		{
			const float FADE_SPEED = 8.0f;
			if (updateCount < 3) { updateCount++; return; }
			if (isFadingIn)
			{
				windowOpacity += deltaTime * FADE_SPEED;
				if (windowOpacity >= 1.0f || isPopupWindow)
				{
					isFadingIn = false;
					windowOpacity = 1.0f;
					backendWindow.FocusWindow();
				}
			}
			else if (isFadingOut)
			{
				windowOpacity -= deltaTime * FADE_SPEED;
				if (windowOpacity <= 0.0f || isPopupWindow)
				{
					isFadingOut = false;
					windowOpacity = 0.0f;
				}
			}

			backendWindow?.SetWindowOpacity(windowOpacity);
		}

		public virtual void PreDraw()
		{
			if (!_initialized) return;
			
			backendWindow?.BeginDraw();

			//background
			backendWindow?.Renderer.RenderBox(0, 0, backendWindow.GetWindowWidth(), backendWindow.GetWindowHeight(), Color.FromArgb(70, 70, 70));

			if (useCustomWindowDecorations)
			{
				DrawWindowDecorations();
			}

			#if DEBUG
			System.Diagnostics.Stopwatch? watch = null;
			if (debugFrameTime)
				watch = System.Diagnostics.Stopwatch.StartNew();
			#endif

			panel.Draw();
			#if DEBUG
			if (debugFrameTime)
			{
				watch?.Stop();
				drawFrameTime = watch?.Elapsed.TotalMilliseconds ?? 0;
			}
			#endif
		}

		void DrawWindowDecorations()
		{
			// //draw window border
			backendWindow.Renderer.RenderBoxBorder(0, 0, backendWindow.GetWindowWidth(), backendWindow.GetWindowHeight(), Color.FromArgb(104, 106, 101));

			//title bar
			backendWindow.Renderer.RenderBox(0, 0, backendWindow.GetWindowWidth(), 21, Color.FromArgb(90, 106, 80));

			//corners
			backendWindow.Renderer.RenderPoint(0, 0, Color.FromArgb(0, 0, 0, 0));
			backendWindow.Renderer.RenderPoint(backendWindow.GetWindowWidth() - 1, 0, Color.FromArgb(0, 0, 0, 0));
			backendWindow.Renderer.RenderPoint(0, backendWindow.GetWindowHeight() - 1, Color.FromArgb(0, 0, 0, 0));
			backendWindow.Renderer.RenderPoint(backendWindow.GetWindowWidth() - 1, backendWindow.GetWindowHeight() - 1, Color.FromArgb(0, 0, 0, 0));
		}

		public virtual void Draw()
		{
		}

		public void PostDraw()
		{
			if (!_initialized) return;
			if (useCustomWindowDecorations && !isPopupWindow)
			{
				//minimize button
				Color minimizeColor = Color.FromArgb(216, 222, 211);
				Rect minimizeMouseRect = new Rect { X = backendWindow.GetWindowWidth() - 33, Y = 0, W = 15, H = 21 };
				if (panel.MouseX >= minimizeMouseRect.X && panel.MouseY >= minimizeMouseRect.Y && panel.MouseX <= minimizeMouseRect.X + minimizeMouseRect.W && panel.MouseY <= minimizeMouseRect.Y + minimizeMouseRect.H) minimizeColor = Color.FromArgb(141, 146, 121);
				backendWindow.Renderer.RenderBox(backendWindow.GetWindowWidth() - 29, 14, 7, 2, minimizeColor);

				//close button
				int closeButtonX = backendWindow.GetWindowWidth() - 15;
				int closeButtonY = 7;
				Color closeColor = Color.FromArgb(216, 222, 211);
				Rect closeMouseRect = new Rect { X = backendWindow.GetWindowWidth() - 18, Y = 0, W = 15, H = 21 };
				if (panel.MouseX >= closeMouseRect.X && panel.MouseY >= closeMouseRect.Y && panel.MouseX <= closeMouseRect.X + closeMouseRect.W && panel.MouseY <= closeMouseRect.Y + closeMouseRect.H) closeColor = Color.FromArgb(141, 146, 121);
				for (int i = 0; i < 8; i++)
				{
					backendWindow.Renderer.RenderBox(closeButtonX + i, closeButtonY + i, 2, 2, closeColor);
					backendWindow.Renderer.RenderBox(closeButtonX + 7 - i, closeButtonY + i, 2, 2, closeColor);
				}

				//title
				string titleToDraw = backendWindow.GetTitle();
				#if DEBUG
				if (debugFrameTime)
					titleToDraw += $" - { updateFrameTime.ToString("F2") } ms , { drawFrameTime.ToString("F2") } ms";
				#endif

				panel.DrawText(titleToDraw, 6, 6, Color.FromArgb(216, 222, 211));
			}

			backendWindow.EndDraw();
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
			if (isPopupWindow) return WindowBorderSize;
			return useCustomWindowDecorations && accountForWindowDecorations ? WindowBorderSize : 0;
		}

		public int GetInternalY()
		{
			if (isPopupWindow) return WindowBorderSize;
			return useCustomWindowDecorations && accountForWindowDecorations ? TitleBarHeight : 0;
		}

		public int GetInternalWidth()
		{
			if (isPopupWindow) return backendWindow.GetWindowWidth() - (WindowBorderSize * 2);
			return useCustomWindowDecorations && accountForWindowDecorations ? backendWindow.GetWindowWidth() - (WindowBorderSize * 2) : backendWindow.GetWindowWidth();
		}

		public int GetInternalHeight()
		{
			if (isPopupWindow) return backendWindow.GetWindowHeight() - (WindowBorderSize * 2);
			return useCustomWindowDecorations && accountForWindowDecorations ? backendWindow.GetWindowHeight() - TitleBarHeight - WindowBorderSize : backendWindow.GetWindowHeight();
		}

		public virtual void OnMouseMotion(int x, int y)
		{
			panel.HandleMouseMotionEvent(x, y);

		}

		public virtual void OnMouseScroll(int scrollX, int scrollY)
		{
			panel.HandleMouseWheelEvent(scrollY);
		}

		public virtual void OnMouseDown(int x, int y, int button)
		{
		}

		public virtual void OnMouseUp(int x, int y, int button)
		{
			if (!useCustomWindowDecorations || isPopupWindow) return;

			Rect minimizeRect = new Rect{ X = backendWindow.GetWindowWidth() - 33, Y = 0, W = 15, H = 21};
			Rect closeRect = new Rect{ X = backendWindow.GetWindowWidth() - 18, Y = 0, W = 15, H = 21};
			if (x >= minimizeRect.X && y >= minimizeRect.Y && x <= minimizeRect.X + minimizeRect.W && y <= minimizeRect.Y + minimizeRect.H)
			{
				backendWindow.MinimizeWindow();
			}
			else if (x >= closeRect.X && y >= closeRect.Y && x <= closeRect.X + closeRect.W && y <= closeRect.Y + closeRect.H)
			{
				windowEnvironment.RemoveWindow(this);
			}
		}

		public virtual void OnKeyDown(SDL_Keycode key, SDL_Keymod modifier)
		{
			panel.HandleKeyDownEvent(key, modifier);
		}

		public virtual void OnKeyUp(SDL_Keycode key, SDL_Keymod modifier)
		{
			panel.HandleKeyUpEvent(key, modifier);
		}
	}
}