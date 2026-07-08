//Holds and manages UIControls
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using SDL;
using KGUI.Controls;
using System.Numerics;

namespace KGUI
{
	public class UIPanel
	{
		public BaseWindow window;

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

			RootControl = new(null)
			{
				width = window.widthDefinition,
				height = window.heightDefinition,
				x = 0,
				y = 0
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
					catch { return []; }
				})
				.GroupBy(t => t.Name)
				.ToDictionary(g => g.Key, g => g.First());
		}


		public void LoadLayout(XmlNode rootNode)
		{
			foreach (var child in rootNode.ChildNodes)
			{
				if (child is not XmlNode node) continue;

				InitControlFromLayout(node, RootControl);
			}
		}

		void InitControlFromLayout(XmlNode node, UIControl parentControl)
		{
			if (node.Name == "#comment") return;
			
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

			control.OnAttributesDeserialized();

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
			object convertedValue = null;
			Type targetType = field.FieldType;
			Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
			if (underlyingType.IsEnum)
			{
				if (int.TryParse(attribute.InnerText, out int enumInt))
					convertedValue = Enum.ToObject(underlyingType, enumInt);
				else
					convertedValue = Enum.Parse(underlyingType, attribute.InnerText, true);
			}
			else
			{
				convertedValue = Convert.ChangeType(attribute.InnerText, underlyingType);
			}

			field.SetValue(control, convertedValue);
		}

		public Vector2 GetTextureSize(string texturePath)
		{
			return window.backendWindow.Renderer.GetTextureSize(texturePath);
		}

		public void DrawText(string text, int x, int y, Color color, bool bold = false, bool underline = false, int fontSize = 8, FontAlignment alignment = FontAlignment.Left)
		{
			window.backendWindow.Renderer.DrawText(text, x, y, color, bold, underline, fontSize, alignment);
		}

		public void DrawTextWrapped(string text, int x, int y, int maxWidth, Color color, bool bold = false, bool underline = false, int fontSize = 8, FontAlignment alignment = FontAlignment.Left, int lineHeight = 12)
		{
			window.backendWindow.Renderer.DrawTextWrapped(text, x, y, maxWidth, color, bold, underline, fontSize, alignment, lineHeight);
		}

		public int MeasureTextWrappedHeight(string text, int maxWidth, bool bold = false, bool underline = false, int fontSize = 8, int lineHeight = 12)
		{
			return window.backendWindow.Renderer.MeasureTextWrappedHeight(text, maxWidth, bold, underline, fontSize, lineHeight);
		}

		public int MeasureTextWidth(string text, bool bold = false, int fontSize = 8)
		{
			return window.backendWindow.Renderer.MeasureTextWidth(text, bold, fontSize);
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
					if (lastClickedControl != topmostControl)
					{
						lastClickedControl.mouseDown = false;
						lastClickedControl.OnUnfocused?.Invoke(lastClickedControl);
						lastClickedControl.focused = false;
					}
				}

				if (lastClickedControl != topmostControl) lastClickedControl = topmostControl; // set as the new clicked control
				if (lastClickedControl != null && !lastClickedControl.mouseDown) lastClickedControl.OnMouseDown?.Invoke(1);
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

			if (rightMouseDown)
			{
				if (!prevRightMouseDown) // we just right-clicked this frame
				{
					if (topmostControl != null)
					{
						topmostControl.OnRightClick?.Invoke(topmostControl);
						topmostControl.OnMouseDown?.Invoke(2);
					}
				}
			}

			if (!leftMouseDown && prevLeftMouseDown)
			{
				if (lastClickedControl != null)
				{
					lastClickedControl.mouseDown = false;
					lastClickedControl.OnMouseUp?.Invoke(1);
				}
			}

			if (!rightMouseDown && prevRightMouseDown)
			{
				if (topmostControl != null)
				{
					topmostControl.OnMouseUp?.Invoke(2);
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
			ClipDrawToWindow();
			DrawControlAndChildren(RootControl);
			ClearClip();
		}

		void DrawControlAndChildren(UIControl control)
		{
			if (control.ManualDraw || !control.visible) return;
			if (control.parent != null)
			{
				if (control.x + control.width < 0 || control.x > control.parent.width || control.y + control.height < 0 || control.y > control.parent.height) return;
			}

			Rect previousClip = window.backendWindow.Renderer.GetClipRect();

			if (ClipDrawToControl(control, previousClip))
			{
				control.Draw();
				foreach (var child in control.Children)
				{
					DrawControlAndChildren(child);
				}
			}

			window.backendWindow.Renderer.SetClipRect(previousClip);
		}

		public void ClipDrawToWindow()
		{
			window.backendWindow.Renderer.SetClipRect(new Rect(window.GetInternalX(), window.GetInternalY(), window.GetInternalWidth(), window.GetInternalHeight()));
		}

		public bool ClipDrawToControl(UIControl control, Rect previousClip, bool border = false)
		{
			Rect requestedClip = new Rect(control.GetAbsoluteX(), control.GetAbsoluteY(), control.width, control.height);

			if (border)
			{
				requestedClip.X -= 1;
				requestedClip.Y -= 1;
				requestedClip.W += 2;
				requestedClip.H += 2;
			}

			int x1 = Math.Max(requestedClip.X, previousClip.X);
			int y1 = Math.Max(requestedClip.Y, previousClip.Y);
			int x2 = Math.Min(requestedClip.X + requestedClip.W, previousClip.X + previousClip.W);
			int y2 = Math.Min(requestedClip.Y + requestedClip.H, previousClip.Y + previousClip.H);

			Rect finalClip = new Rect(x1, y1, Math.Max(0, x2 - x1), Math.Max(0, y2 - y1));

			window.backendWindow.Renderer.SetClipRect(finalClip);

			return finalClip.W > 0 && finalClip.H > 0;
		}

		public void ClearClip()
		{
			window.backendWindow.Renderer.SetClipRect(null);
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

		public void HandleMouseMotionEvent(int newMouseX, int newMouseY)
		{
			mouseX = newMouseX;
			mouseY = newMouseY;
		}

		public void HandleKeyDownEvent(SDL_Keycode key, SDL_Keymod mod)
		{
			lastClickedControl?.OnKeyDown?.Invoke(lastClickedControl, key, mod);
		}

		public void HandleKeyUpEvent(SDL_Keycode key, SDL_Keymod mod)
		{
			lastClickedControl?.OnKeyUp?.Invoke(lastClickedControl, key, mod);
		}

		public void HandleMouseWheelEvent(int scrollY)
		{
			UIControl? scrollTarget = GetScrollTargetAt(mouseX, mouseY);
			scrollTarget?.OnScroll?.Invoke(scrollTarget, scrollY);
		}
	}
}