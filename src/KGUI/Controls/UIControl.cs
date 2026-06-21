using System.Diagnostics;
using System.Drawing;
using SDL;
using System.Xml;
using System.Xml.Serialization;

namespace KGUI.Controls
{
	public class UIControl
	{
		public UIControl parent;
		protected List<UIControl> _children = new List<UIControl>();
		public List<UIControl> Children { get { return _children; } }

		[XmlAttribute("id")]
		public string ID = "";

		[XmlAttribute("borderTop")]
		public bool BorderTop = false;

		[XmlAttribute("borderBottom")]
		public bool BorderBottom = false;

		[XmlAttribute("borderLeft")]
		public bool BorderLeft = false;

		[XmlAttribute("borderRight")]
		public bool BorderRight = false;

		[XmlAttribute("borderColor")]
		public string BorderColor = "";

		[XmlAttribute("backgroundColor")]
		public string BackgroundColor = "";


		[XmlAttribute("x")]
		public int x; // relative to parent
		[XmlAttribute("y")]
		public int y; // relative to parent
		[XmlAttribute("z")]
		public int zIndex = 0;
		[XmlAttribute("width")]
		public int width;
		[XmlAttribute("height")]
		public int height;

		[XmlAttribute("pinnedTop")]
		public int pinnedTop = -1;
		[XmlAttribute("pinnedBottom")]
		public int pinnedBottom = -1;
		[XmlAttribute("pinnedLeft")]
		public int pinnedLeft = -1;
		[XmlAttribute("pinnedRight")]
		public int pinnedRight = -1;

		//if false, mouse events will not be sent to this control, except for scroll events
		public bool AcceptMouseEvents = true;

		public bool ManualDraw = false;

		public bool visible = true;
		public bool enabled = true;

		public bool focused = false;

		public bool mouseOver = false;
		public bool mouseDown = false;

		[XmlAttribute("text")]
		public string text = "";

		public UIControl(UIControl parent)
		{
			this.parent = parent;
		}

		public virtual void OnListViewRowWidthChanged() { }

		public virtual void OnChildrenLayoutComplete() { }

		public virtual void OnAttributesDeserialized() { }

		public virtual void Update()
		{
			if (pinnedTop != -1)
			{
				if (pinnedTop != -1) y = pinnedTop;
				else y = parent.height - pinnedTop - height;
			}
			if (pinnedBottom != -1)
			{
				if (pinnedTop != -1) height = (parent.height - pinnedBottom) - y;
				else y = parent.height - pinnedBottom - height;
			}
			if (pinnedLeft != -1)
			{
				if (pinnedLeft != -1) x = pinnedLeft;
				else x = parent.width - pinnedLeft - width;
			}
			if (pinnedRight != -1)
			{
				if (pinnedLeft != -1) width = (parent.width - pinnedRight) - x;
				else x = parent.width - pinnedRight - width;
			}
		}

		public void UpdateChildren()
		{
			foreach (var child in _children)
			{
				child.Update();
				child.UpdateChildren();
			}
		}

		public virtual void DrawBackground()
		{
			if (BackgroundColor != "")
			{
				//split background color by comma or space
				string[] colors = BackgroundColor.Replace(" ", "").Split(',');
				if (colors.Length == 1)
				{
					string colorStr = colors[0];
					if (colorStr.StartsWith("#"))
					{
						DrawBox(0, 0, width, height, ColorTranslator.FromHtml(colorStr));
					}
				}
				else if (colors.Length == 3)
				{
					DrawBox(0, 0, width, height, Color.FromArgb(255, int.Parse(colors[0]), int.Parse(colors[1]), int.Parse(colors[2])));
				}
				else if (colors.Length == 4)
				{
					DrawBox(0, 0, width, height, Color.FromArgb(int.Parse(colors[3]), int.Parse(colors[0]), int.Parse(colors[1]), int.Parse(colors[2])));
				}
			}
		}

		public virtual void Draw()
		{
		}

		public void DrawBorder()
		{
			Color borderColor = Color.Transparent;
			if (BorderColor != "")
			{
				//split border color by comma or space
				string[] colors = BorderColor.Replace(" ", "").Split(',');
				if (colors.Length == 3)
				{
					borderColor = Color.FromArgb(255, int.Parse(colors[0]), int.Parse(colors[1]), int.Parse(colors[2]));
				}
			}

			if (BorderTop) DrawLine(0, 1, width, 1, borderColor);
			if (BorderBottom) DrawLine(0, height - 1, width, height - 1, borderColor);
			if (BorderLeft) DrawLine(1, 0, 1, height, borderColor);
			if (BorderRight) DrawLine(width, 0, width, height, borderColor);
		}

		public virtual void Destroy()
		{
			foreach (var child in _children.ToList())
			{
				child.Destroy();
				RemoveChild(child);
			}
			_children.Clear();
		}

		public virtual void AddChild(UIControl child)
		{
			_children.Add(child);
			child.parent = this;
		}

		public virtual void RemoveChild(UIControl child)
		{
			_children.Remove(child);
			child.parent = null;
		}

		public virtual void OrderChildren(Func<UIControl, int> orderFunc)
		{
			_children = _children.OrderBy(orderFunc).ToList();
		}

		public virtual void Reposition(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public int GetAbsoluteX()
		{
			//get the absolute x coordinate by moving up to the root control
			UIControl currentParent = parent;
			int absoluteX = x;
			while (currentParent != null && currentParent != currentParent.parent)
			{
				absoluteX += currentParent.x;
				currentParent = currentParent.parent;
			}
			return absoluteX;
		}

		public int GetAbsoluteY()
		{
			//get the absolute y coordinate by moving up to the root control
			UIControl currentParent = parent;
			int absoluteY = y;
			while (currentParent != null && currentParent != currentParent.parent)
			{
				absoluteY += currentParent.y;
				currentParent = currentParent.parent;
			}
			return absoluteY;
		}

		//Reposition with respect to the the parent's coordinates
		public void Reposition(int x, int y, int width, int height)
		{
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
		}

		public void SetSize(int width, int height)
		{
			this.width = width;
			this.height = height;
		}

		public virtual unsafe SDL_Texture* LoadTexture(string path)
		{
			return parent.LoadTexture(path);
		}

		public virtual unsafe void DrawTexture(SDL_Texture* texture, int x, int y)
		{
			if (texture == null) return;
			parent.DrawTexture(texture, this.x + x, this.y + y);
		}

		public virtual unsafe void DrawTextureRect(SDL_Texture* texture, int x, int y, int width, int height)
		{
			parent.DrawTextureRect(texture, this.x + x, this.y + y, width, height);
		}

		public virtual unsafe void DrawTexture9Grid(SDL_Texture* texture, int x, int y, int width, int height, int top, int left, int bottom, int right, float scale)
		{
			parent.DrawTexture9Grid(texture, this.x + x, this.y + y, width, height, top, left, bottom, right, scale);
		}

		public virtual unsafe void DrawTextureSheet(SDL_Texture* texture, int x, int y, int indexX, int indexY, int cellWidth, int cellHeight)
		{
			parent.DrawTextureSheet(texture, this.x + x, this.y + y, indexX, indexY, cellWidth, cellHeight);
		}

		public virtual void DrawPoint(int x, int y, Color color)
		{
			parent.DrawPoint(this.x + x, this.y + y, color);
		}

		public virtual void DrawLine(int x1, int y1, int x2, int y2, Color color)
		{
			parent.DrawLine(this.x + x1, this.y + y1, this.x + x2, this.y + y2, color);
		}

		public virtual void DrawBox(int x, int y, int width, int height, Color color)
		{
			parent.DrawBox(this.x + x, this.y + y, width, height, color);
		}

		public virtual void DrawBoxBorder(int x, int y, int width, int height, Color color)
		{
			parent.DrawBoxBorder(this.x + x, this.y + y, width, height, color);
		}

		public virtual void DrawText(string text, int x, int y, Color color, bool bold = false, bool underline = false, int fontSize = 8, FontAlignment alignment = FontAlignment.Left)
		{
			parent.DrawText(text, this.x + x, this.y + y, color, bold, underline, fontSize, alignment);
		}

		public virtual void DrawTextWrapped(string text, int x, int y, int maxWidth, Color color, bool bold = false, bool underline = false, int fontSize = 8, FontAlignment alignment = FontAlignment.Left, int lineHeight = 12)
		{
			parent.DrawTextWrapped(text, this.x + x, this.y + y, maxWidth, color, bold, underline, fontSize, alignment, lineHeight);
		}

		public virtual int MeasureTextWrappedHeight(string text, int maxWidth, bool bold = false, bool underline = false, int fontSize = 8, int lineHeight = 12)
		{
			return parent.MeasureTextWrappedHeight(text, maxWidth, bold, underline, fontSize, lineHeight);
		}

		public virtual int MeasureTextWidth(string text, bool bold = false, int fontSize = 8)
		{
			return parent.MeasureTextWidth(text, bold, fontSize);
		}

		public virtual int GetRelativeMouseX()
		{
			return parent.GetRelativeMouseX() - x;
		}
		public virtual int GetRelativeMouseY()
		{
			return parent.GetRelativeMouseY() - y;
		}

		public Action<UIControl> OnClick;
		public Action<int> OnMouseDown;
		public Action<int> OnMouseUp;
		public Action<UIControl> OnDoubleClick;
		public Action<UIControl> OnRightClick;
		public Action<UIControl, int> OnScroll;
		public Action<UIControl, SDL_Keycode, SDL_Keymod> OnKeyDown;
		public Action<UIControl, SDL_Keycode, SDL_Keymod> OnKeyUp;
		public Action<UIControl> OnFocused;
		public Action<UIControl> OnUnfocused;
	}
}