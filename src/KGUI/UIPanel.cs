//Holds and manages UIControls
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using SDL;

namespace KGUI
{
	public class UIPanel
	{
		public BaseWindow window;
		public SteamFont7? steamFont7;
		public SteamFont8? steamFont8;

		public RootControl RootControl;

		public Dictionary<string, UIControl> IDControls = new Dictionary<string, UIControl>();

		static Dictionary<string, Type> _typeCache = new();

		//track double clicks
		float timeSinceLastClick = 0;
		UIControl currentlyHoveredControl = null;
		UIControl lastClickedControl = null;
		UIControl lastDoubleClickedControl = null;
		const float DOUBLE_CLICK_TIME = 0.3f;

		bool leftMouseDown = false;
		bool rightMouseDown = false;
		bool prevLeftMouseDown = false;
		bool prevRightMouseDown = false;

		int mouseX;
		int mouseY;
		public int MouseX { get { return mouseX; } }
		public int MouseY { get { return mouseY; } }

		public UIPanel(BaseWindow window)
		{
			this.window = window;

			unsafe
			{
				SDL_Surface* surface7 = SDL3_image.IMG_Load(Assets.GetAssetPath("fonts/steamfont7.png"));
				if (surface7 != null)
				{
					SDL_Texture* texture7 = SDL3.SDL_CreateTextureFromSurface(window.renderer, surface7);
					steamFont7 = new SteamFont7(texture7);
					SDL3.SDL_DestroySurface(surface7);
				}

				SDL_Surface* surface8 = SDL3_image.IMG_Load(Assets.GetAssetPath("fonts/steamfont8.png"));
				if (surface8 != null)
				{
					SDL_Texture* texture8 = SDL3.SDL_CreateTextureFromSurface(window.renderer, surface8);
					steamFont8 = new SteamFont8(texture8);
					SDL3.SDL_DestroySurface(surface8);
				}
			}

			RootControl = new(null)
			{
				width = window.GetInternalWidth(),
				height = window.GetInternalHeight(),
				x = window.GetInternalX(),
				y = window.GetInternalY()
			};
			RootControl.SetPanel(this);

			BuildTypeCache();
		}

		static void BuildTypeCache()
		{
			_typeCache = AppDomain.CurrentDomain
				.GetAssemblies()
				.SelectMany(a =>
				{
					try { return a.GetTypes(); }
					catch { return Array.Empty<Type>(); } // dynamic/unloaded assemblies safety
				})
				.GroupBy(t => t.Name) // or FullName if you want stricter matching
				.ToDictionary(g => g.Key, g => g.First());
		}


		public void LoadLayout(XmlNode rootNode)
		{
			foreach (var child in rootNode.ChildNodes)
			{
				if (!(child is XmlNode node)) continue;

				InitControlFromLayout(node, RootControl);
			}
		}

		void InitControlFromLayout(XmlNode node, UIControl parentControl)
		{
			Type? type;
			if (!_typeCache.TryGetValue(node.Name, out type))
			{
				throw new Exception("Control type " + node.Name + " not found");
			}
			
			UIControl control = (UIControl)Activator.CreateInstance(type, [parentControl]);
			foreach (XmlAttribute attribute in node.Attributes)
			{
				if (attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
				{
					AssignEventHandlerFromAttribute(attribute, control);
				}
				else
				{
					ApplyAttributeToControl(attribute, control);
				}
			}

			parentControl.AddChild(control);

			if (control.ID != "")
			{
				if (IDControls.ContainsKey(control.ID))
				{
					throw new Exception("Control with ID " + control.ID + " already exists");
				}

				IDControls[control.ID] = control;
			}

			foreach (XmlNode childNode in node.ChildNodes)
			{
				InitControlFromLayout(childNode, control);
			}
			control.OnChildrenLayoutComplete();
		}

		void AssignEventHandlerFromAttribute(XmlAttribute attribute, UIControl control)
		{
			string eventFieldName = "On" + char.ToUpper(attribute.Name[2]) + attribute.Name.Substring(3);
			var evtField = control.GetType().GetField(eventFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			if (evtField == null)
				throw new Exception($"Event '{eventFieldName}' does not exist on type '{control.GetType().Name}'");

			var handlerName = attribute.Value.Trim();

			// Try to find a method on 'window' with that name, any signature
			var allMethods = window.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			MethodInfo? foundMethod = null;
			foreach (var method in allMethods)
			{
				if (string.Equals(method.Name, handlerName, StringComparison.Ordinal))
				{
					foundMethod = method;
					break;
				}
			}
			if (foundMethod == null)
				throw new Exception($"Handler method '{handlerName}' not found on window '{window.GetType().Name}' for event '{eventFieldName}'");

			var fieldType = evtField.FieldType;
			Delegate del;
			try
			{
				if (fieldType == typeof(Action<UIControl>) &&foundMethod.GetParameters().Length == 0)
				{
					Action<UIControl> wrapper = _ => foundMethod.Invoke(window, null);
					del = wrapper;
				}
				else
				{
					del = Delegate.CreateDelegate(fieldType, window, foundMethod, false);
				}

				if (del == null)
				{
					throw new Exception($"Failed to create delegate of type '{fieldType.Name}' for method '{handlerName}'");
				}
			}
			catch
			{
				throw new Exception($"Cannot bind event '{eventFieldName}' to '{handlerName}': incompatible signature");
			}

			// Combine with any existing
			var existing = evtField.GetValue(control) as Delegate;
			if (existing != null)
			{
				del = Delegate.Combine(existing, del);
			}

			evtField.SetValue(control, del);
		}

		void ApplyAttributeToControl(XmlAttribute attribute, UIControl control)
		{
			FieldInfo? field = control.GetType()
			.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.FirstOrDefault(currentField =>
			{
				XmlAttributeAttribute? xmlAttribute = currentField.GetCustomAttribute<XmlAttributeAttribute>();
				if (xmlAttribute == null) return false;
				string xmlName = string.IsNullOrWhiteSpace(xmlAttribute.AttributeName) ? currentField.Name : xmlAttribute.AttributeName;
				return string.Equals(xmlName, attribute.Name, StringComparison.OrdinalIgnoreCase);
			});
			if (field == null) return;
			object convertedValue = Convert.ChangeType(attribute.InnerText, field.FieldType);
			field.SetValue(control, convertedValue);
		}

		public unsafe void DrawText(string text, int x, int y, Color color, bool bold = false, bool underline = false, int fontSize = 8, FontAlignment alignment = FontAlignment.Left)
		{
			if (fontSize == 7 && steamFont7 != null)
			{
				steamFont7.RenderText(window.renderer, text, x, y, color, bold, underline, alignment);
			}
			else if (fontSize == 8 && steamFont8 != null)
			{
				steamFont8.RenderText(window.renderer, text, x, y, color, bold, underline, alignment);
			}
		}

		public void DrawTextWrapped(string text, int x, int y, int maxWidth, Color color, bool bold = false, bool underline = false, int fontSize = 8, FontAlignment alignment = FontAlignment.Left, int lineHeight = 12)
		{
			FontRenderer fontRenderer = fontSize == 7 ? steamFont7 : steamFont8;
			if (fontRenderer == null) return;
			unsafe {
				fontRenderer.RenderTextWrapped(window.renderer, text, x, y, maxWidth, color, bold, underline, alignment, lineHeight);
			}
		}

		public int MeasureTextWrappedHeight(string text, int maxWidth, bool bold = false, bool underline = false, int fontSize = 8, int lineHeight = 12)
		{
			FontRenderer fontRenderer = fontSize == 7 ? steamFont7 : steamFont8;
			if (fontRenderer == null) return 0;
			return fontRenderer.MeasureWrappedHeight(text, maxWidth, bold, lineHeight);
		}

		public int MeasureText(string text, bool bold = false, int fontSize = 8)
		{
			if (fontSize == 7 && steamFont7 != null)
			{
				return steamFont7.MeasureText(text, bold);
			}
			else if (fontSize == 8 && steamFont8 != null)
			{
				return steamFont8.MeasureText(text, bold);
			}
			return 0;
		}

		public void SetFocus(UIControl control)
		{
			if (lastClickedControl != null)
			{
				lastClickedControl.focused = false;
				lastClickedControl.OnUnfocused?.Invoke(lastClickedControl);
			}
			lastClickedControl = control;
			control.focused = true;
			control.OnFocused?.Invoke(control);
		}

		public virtual void Update(float deltaTime)
		{
			timeSinceLastClick += deltaTime;
			
			RootControl.Reposition(window.GetInternalX(), window.GetInternalY(), window.GetInternalWidth(), window.GetInternalHeight());

			float x, y;
			SDL_MouseButtonFlags mouseState;
			unsafe {
				mouseState = SDL3.SDL_GetGlobalMouseState(&x, &y);
			}

			prevLeftMouseDown = leftMouseDown;
			prevRightMouseDown = rightMouseDown;
			leftMouseDown = mouseState.HasFlag(SDL_MouseButtonFlags.SDL_BUTTON_LMASK) && window.MouseFocus;
			rightMouseDown = mouseState.HasFlag(SDL_MouseButtonFlags.SDL_BUTTON_RMASK) && window.MouseFocus;

			UIControl? topmostControl = GetTopmostControlAt(mouseX, mouseY);

			// If the control under the mouse has changed since the last frame
			if (topmostControl != currentlyHoveredControl)
			{
				if (currentlyHoveredControl != null)
				{
					currentlyHoveredControl.mouseOver = false;
				}

				if (topmostControl != null)
				{
					topmostControl.mouseOver = true;
				}

				currentlyHoveredControl = topmostControl;
			}

			if (leftMouseDown)
			{
				if (lastClickedControl != null)
				{
					//release previous click
					lastClickedControl.mouseDown = false;

					if (lastClickedControl != topmostControl)
					{
						lastClickedControl.OnUnfocused?.Invoke(lastClickedControl);
						lastClickedControl.focused = false;
					}
				}
				if (lastClickedControl != topmostControl) lastClickedControl = topmostControl; // set as the new clicked control
				if (lastClickedControl != null) lastClickedControl.mouseDown = true; // click the new control
				
				if (!prevLeftMouseDown) // we just clicked this frame
				{
					if (lastClickedControl != null)
					{
						bool isDoubleClick = lastDoubleClickedControl == lastClickedControl && timeSinceLastClick <= DOUBLE_CLICK_TIME;
						lastClickedControl.OnClick?.Invoke(lastClickedControl);
						if (isDoubleClick)
						{
							lastClickedControl.OnDoubleClick?.Invoke(lastClickedControl);
						} 
						lastDoubleClickedControl = lastClickedControl;
						timeSinceLastClick = 0;
					}

					if (lastClickedControl != null && !lastClickedControl.focused)
					{
						lastClickedControl?.OnFocused?.Invoke(lastClickedControl);
						lastClickedControl.focused = true;
					}
				}
			}

			if (!leftMouseDown && prevLeftMouseDown)
			{
				if (lastClickedControl != null)
				{
					lastClickedControl.mouseDown = false;
				}
			}


				foreach (var child in RootControl.Children)
			{
				//if (!child.enabled) continue;
				child.Update();
				child.UpdateChildren();
			}
		}

		public void Draw()
		{
			//clip
			ClipDrawToWindow();
			//this sucks
			foreach (var child in RootControl.Children)
			{
				if (!child.visible) continue;
				ClipDrawToControl(child);
				child.DrawBackground();
			}

			foreach (var child in RootControl.Children)
			{
				if (!child.visible) continue;

				ClipDrawToControl(child);
				child.Draw();
				child.DrawChildren();
			}

			
			foreach (var child in RootControl.Children)
			{
				if (!child.visible) continue;
				ClipDrawToControl(child, border: true);
				child.DrawBorder();
			}
			ClearClip();
		}

		public void ClipDrawToWindow()
		{
			SDL_Rect clipRect = new SDL_Rect();
			clipRect.x = window.GetInternalX();
			clipRect.y = window.GetInternalY();
			clipRect.w = window.GetInternalWidth();
			clipRect.h = window.GetInternalHeight();
			unsafe {
				SDL3.SDL_SetRenderClipRect(window.renderer, &clipRect);
			}
		}

		public void ClipDrawToControl(UIControl control, bool border = false)
		{
			SDL_Rect clipRect = new SDL_Rect();
			clipRect.x = control.GetAbsoluteX();
			clipRect.y = control.GetAbsoluteY();
			clipRect.w = control.width;
			clipRect.h = control.height;
			if (border)
			{
				clipRect.x -= 1;
				clipRect.y -= 1;
				clipRect.w += 2;
				clipRect.h += 2;
			}
			unsafe {
				SDL3.SDL_SetRenderClipRect(window.renderer, &clipRect);
			}
		}

		public void ClearClip()
		{
			unsafe {
				SDL3.SDL_SetRenderClipRect(window.renderer, null);
			}
		}

		public UIControl? GetTopmostControlAt(int mx, int my)
		{
			return GetTopmostControlAt(mx, my, includeNonMouseControls: false);
		}

		public UIControl? GetTopmostControlAt(int mx, int my, bool includeNonMouseControls)
		{
			UIControl? best = null;
			void Walk(UIControl control, int depth)
			{
				if (!control.visible || !control.enabled)
					return;

				int ax = control.GetAbsoluteX();
				int ay = control.GetAbsoluteY();
				bool inside = mx >= ax && mx < ax + control.width &&
							  my >= ay && my < ay + control.height;

				if (!inside)
					return;

				foreach (var child in control.Children)
					Walk(child, depth + 1);

				if (!includeNonMouseControls && !control.AcceptMouseEvents)
					return;

				if (best == null || control.zIndex > best.zIndex)
					best = control;
			}

			foreach (var rootChild in RootControl.Children)
				Walk(rootChild, 0);

			return best;
		}

		public List<UIControl> GetControlsAt(int mx, int my, bool includeNonMouseControls)
		{
			List<(UIControl control, int depth)> hits = new List<(UIControl control, int depth)>();
			void Walk(UIControl control, int depth)
			{
				if (!control.visible || !control.enabled)
					return;

				int ax = control.GetAbsoluteX();
				int ay = control.GetAbsoluteY();
				bool inside = mx >= ax && mx < ax + control.width &&
							  my >= ay && my < ay + control.height;

				if (!inside)
					return;

				foreach (var child in control.Children)
					Walk(child, depth + 1);

				if (!includeNonMouseControls && !control.AcceptMouseEvents)
					return;

				hits.Add((control, depth));
			}

			foreach (var rootChild in RootControl.Children)
				Walk(rootChild, 0);

			return hits
				.OrderByDescending(hit => hit.control.zIndex)
				.ThenByDescending(hit => hit.depth)
				.Select(hit => hit.control)
				.ToList();
		}

		public UIControl? GetScrollTargetAt(int mx, int my)
		{
			HashSet<UIControl> checkedControls = new HashSet<UIControl>();
			List<UIControl> hoveredControls = GetControlsAt(mx, my, includeNonMouseControls: true);

			foreach (var hoveredControl in hoveredControls)
			{
				UIControl? currentControl = hoveredControl;
				while (currentControl != null)
				{
					if (checkedControls.Contains(currentControl))
					{
						currentControl = currentControl.parent;
						continue;
					}

					checkedControls.Add(currentControl);
					if (currentControl.enabled && currentControl.visible && currentControl.OnScroll != null)
					{
						return currentControl;
					}
					currentControl = currentControl.parent;
				}
			}
			return null;
		}

		public UIControl? GetControlByID(string id)
		{
			if (!IDControls.TryGetValue(id, out UIControl? value)) return null;
			return value;
		}

		public T? GetControlByID<T>(string id) where T : UIControl
		{
			if (!IDControls.TryGetValue(id, out UIControl? value)) return null;
			if (value == null) return null;
			return value as T;
		}

		public void HandleSDLEvent(SDL_Event e)
		{
			//handle mouse events
			if (e.Type == SDL_EventType.SDL_EVENT_MOUSE_MOTION)
			{
				mouseX = (int)e.motion.x;
				mouseY = (int)e.motion.y;

			}
			else if (e.Type == SDL_EventType.SDL_EVENT_KEY_DOWN)
			{
				lastClickedControl?.OnKeyDown?.Invoke(lastClickedControl, e.key.key, e.key.mod);
			}
			else if (e.Type == SDL_EventType.SDL_EVENT_KEY_UP)
			{
				lastClickedControl?.OnKeyUp?.Invoke(lastClickedControl, e.key.key, e.key.mod);
			}
			else if (e.Type == SDL_EventType.SDL_EVENT_MOUSE_WHEEL)
			{
				UIControl? scrollTarget = GetScrollTargetAt(mouseX, mouseY);
				scrollTarget?.OnScroll?.Invoke(scrollTarget, (int)e.wheel.y);
			}
		}

	}
}