using System.Drawing;
using System.Xml.Serialization;
using SDL;

namespace KGUI.Controls
{
	public class ListViewControl : UIControl
	{
		[XmlAttribute("verticalPadding")] public int VerticalPadding = 2; //top and bottom padding
		[XmlAttribute("horizontalPadding")] public int HorizontalPadding = 0; //left and right padding
		[XmlAttribute("gap")] public int Gap = 2; // gap between children
		[XmlAttribute("enableScrollbar")] public bool EnableScrollbar = true;
		[XmlAttribute("accountForChildrenOfItems")] public bool AccountForChildrenOfItems = false;

		public const int SCROLL_SPEED = 40;

		const int SCROLLBAR_WIDTH = 21;

		private int _currentScroll = 0;
		public int CurrentScroll { get { return _currentScroll; } }

		private UIControl _SelectedItem = null;
		public UIControl SelectedItem { get { return _SelectedItem; } }

		public ScrollbarButtonControl scrollbarButtonUp;
		public ScrollbarButtonControl scrollbarButtonDown;

		public ScrollbarControl scrollbarControl;

		private bool isDraggingScrollbar = false;
		public bool IsDraggingScrollbar { get { return isDraggingScrollbar; } }

		private int dragStartMouseY = 0;
		private int dragStartScroll = 0;

		private int _rowLayoutBatchDepth = 0;
		private bool _rowLayoutDirty = false;

		private bool IsLeftMouseDown()
		{

			SDL_MouseButtonFlags mouseState;
			unsafe {
				mouseState = SDL3.SDL_GetGlobalMouseState(null, null);
			}
			return mouseState.HasFlag(SDL_MouseButtonFlags.SDL_BUTTON_LMASK);
		}

		public ListViewControl(UIControl parent) : base(parent)
		{
			AcceptMouseEvents = false;

			OnScroll = (control, delta) =>
			{
				_currentScroll += delta > 0 ? -SCROLL_SPEED : SCROLL_SPEED;
			};

			scrollbarButtonUp = new ScrollbarButtonControl(this);
			scrollbarButtonUp.SetSize(15, 14);
			scrollbarButtonUp.Up = true;
			scrollbarButtonDown = new ScrollbarButtonControl(this);
			scrollbarButtonDown.SetSize(15, 14);
			scrollbarButtonDown.Up = false;
			scrollbarControl = new ScrollbarControl(this);
			scrollbarControl.SetSize(15, height);
			scrollbarButtonUp.ManualDraw = true;
			scrollbarButtonDown.ManualDraw = true;
			scrollbarControl.ManualDraw = true;
			AddChild(scrollbarButtonUp);
			AddChild(scrollbarButtonDown);
			AddChild(scrollbarControl);

			scrollbarButtonUp.OnClick += (control) =>
			{
				_currentScroll -= SCROLL_SPEED;
				ClampScroll();
			};
			scrollbarButtonDown.OnClick += (control) =>
			{
				_currentScroll += SCROLL_SPEED;
				ClampScroll();
			};
		}

		void ClampScroll()
		{
			if (_currentScroll + height > CalculateContentHeight())
			{
				_currentScroll = CalculateContentHeight() - height;
			}

			if (_currentScroll < 0) _currentScroll = 0;
		}

		void OnRowStructureChanged()
		{
			if (_rowLayoutBatchDepth > 0)
				_rowLayoutDirty = true;
			else
				ApplyRowWidthsAndNotifyChildren();
		}

		void ApplyRowWidthsAndNotifyChildren()
		{
			int prevInner = int.MinValue;
			for (int pass = 0; pass < 8; pass++)
			{
				bool narrow = EnableScrollbar && CalculateContentHeight() > height;
				int inner = width - (narrow ? SCROLLBAR_WIDTH : 0);
				if (inner == prevInner) break;
				prevInner = inner;
				foreach (var child in _children.Where(c => !c.ManualDraw))
				{
					if (child.width != inner)
					{
						child.width = inner;
						child.OnListViewRowWidthChanged();
					}
				}
			}
		}

		public void BeginBatchChildChanges()
		{
			_rowLayoutBatchDepth++;
		}

		public void EndBatchChildChanges()
		{
			if (_rowLayoutBatchDepth <= 0) return;
			_rowLayoutBatchDepth--;
			if (_rowLayoutBatchDepth != 0) return;
			if (_rowLayoutDirty)
			{
				_rowLayoutDirty = false;
				ApplyRowWidthsAndNotifyChildren();
			}
		}

		public override void Update()
		{
			base.Update();

			if (_rowLayoutBatchDepth == 0)
				ApplyRowWidthsAndNotifyChildren();

			ClampScroll();

			int childY = VerticalPadding;
			childY -= _currentScroll;
			foreach (var child in _children.Where(x => !x.ManualDraw))
			{
				child.x = 0;

				//only update y if the child is visible
				if (child.visible)
				{
					child.y = childY;
					childY += CalculateChildHeight(child) + (child == _children.LastOrDefault(x => !x.ManualDraw) ? 0 : Gap);
				}
			}

			//Scrollbar update
			if (IsScrollbarAvailable())
			{
				scrollbarButtonUp.enabled = true;
				scrollbarButtonDown.enabled = true;
				scrollbarControl.enabled = true;

				//buttons
				scrollbarButtonUp.x = width - 18;
				scrollbarButtonUp.y = 3;
				scrollbarButtonDown.x = width - 18;
				scrollbarButtonDown.y = height - 18;

				scrollbarControl.x = width - 17;

				//scrollbar things
				{
					if (isDraggingScrollbar)
					{
						//set mouseover/mouseDown to false on everything, prevents false mouse events when letting go of the scrollbar
						// foreach (var control in parent.controls)
						// {
						// 	control.mouseOver = false;
						// 	control.mouseDown = false;
						// }
					}

					scrollbarControl.x = width - 18;

					int ystart = 21;
					int yend = height - 22;
					int trackHeight = yend - ystart;

					int contentHeight = CalculateContentHeight();
					int visibleHeight = this.height;

					int thumbHeight = Math.Max(14, (int)(trackHeight * visibleHeight / contentHeight));

					int maxScroll = contentHeight - visibleHeight;
					int scrollbarPosition = ystart;
					if (maxScroll > 0)
					{
						scrollbarPosition = ystart + (_currentScroll * (trackHeight - thumbHeight)) / maxScroll;
					}

					scrollbarControl.y = scrollbarPosition;
					scrollbarControl.height = thumbHeight;

					if (!isDraggingScrollbar && scrollbarControl.mouseDown)
					{
						isDraggingScrollbar = true;
						dragStartMouseY = GetRelativeMouseY();
						dragStartScroll = _currentScroll;
					}
					else if (isDraggingScrollbar && !IsLeftMouseDown())
					{
						isDraggingScrollbar = false;
					}

					if (isDraggingScrollbar)
					{
						int mouseDelta = GetRelativeMouseY() - dragStartMouseY;
						int maxScrollbarTravel = trackHeight - thumbHeight;
						if (maxScrollbarTravel > 0)
						{
							int scrollDelta = (mouseDelta * maxScroll) / maxScrollbarTravel;
							_currentScroll = dragStartScroll + scrollDelta;
						}
					}
				}
			}
			else
			{
				scrollbarButtonUp.enabled = false;
				scrollbarButtonDown.enabled = false;
				scrollbarControl.enabled = false;
			}
		}

		public override void Draw()
		{
			base.Draw();

			//draw scrollbar
			if (IsScrollbarAvailable())
			{
				//background
				DrawBox(width - SCROLLBAR_WIDTH, 0, SCROLLBAR_WIDTH, height, Color.FromArgb(70, 70, 70));

				//buttons
				scrollbarButtonUp.Draw();
				scrollbarButtonDown.Draw();

				//scrollbar
				scrollbarControl.Draw();
			}
		}

		public override void AddChild(UIControl child)
		{
			base.AddChild(child);

			child.OnClick += OnChildClick;
			if (!child.ManualDraw)
				OnRowStructureChanged();
		}
		public override void RemoveChild(UIControl child)
		{
			bool wasRow = !child.ManualDraw;
			base.RemoveChild(child);
			child.Reposition(0, 0, width, height);

			child.OnClick -= OnChildClick;
			if (wasRow)
				OnRowStructureChanged();
		}

		private void OnChildClick(UIControl child)
		{
			SelectItem(child);
		}

		public void SelectItem(UIControl child)
		{
			_SelectedItem = child;
		}

		public bool IsSelectedItem(UIControl child)
		{
			return _SelectedItem == child;
		}

		public void MoveItemToTop(UIControl child)
		{
			_children.Remove(child);
			_children.Insert(0, child);
			if (!child.ManualDraw)
				OnRowStructureChanged();
		}

		public void Sort(Comparison<UIControl> comparison)
		{
			//ignore scrollbar stuff
			List<UIControl> childrenToNotSort = _children.Where(x => x.ManualDraw).ToList();
			foreach (var child in childrenToNotSort)
			{
				_children.Remove(child);
			}
			_children.Sort(comparison);
			foreach (var child in childrenToNotSort)
			{
				_children.Add(child);
			}
			OnRowStructureChanged();
		}

		public T? GetFirstItem<T>(Func<T, bool> predicate) where T : UIControl
		{
			return _children.FirstOrDefault(x => x is T && predicate((T)x)) as T;
		}

		public T? GetFirstItem<T>() where T : UIControl
		{
			return _children.FirstOrDefault(x => x is T) as T;
		}

		public T? GetLastItem<T>(Func<T, bool> predicate) where T : UIControl
		{
			return _children.LastOrDefault(x => x is T && predicate((T)x)) as T;
		}

		public T? GetLastItem<T>() where T : UIControl
		{
			return _children.LastOrDefault(x => x is T) as T;
		}

		public int GetItemCount()
		{
			return _children.Where(x => !x.ManualDraw).Count();
		}

		public bool IsScrollbarAvailable()
		{
			return EnableScrollbar && CalculateContentHeight() > height;
		}

		public int CalculateContentHeight()
		{
			int height = VerticalPadding * 2;
			foreach (var child in _children.Where(x => !x.ManualDraw && x.visible))
			{
				height += CalculateChildHeight(child) + Gap;
			}
			return height - Gap;
		}

		public int CalculateChildHeight(UIControl child)
		{
			int childHeight = child.height;
			if (AccountForChildrenOfItems)
			{
				foreach (var subchild in child.Children.Where(x => !x.ManualDraw && x.visible))
				{
					childHeight += CalculateChildHeight(subchild); //todo: account for gap
				}
			}
			return childHeight;
		}

		public bool IsAtTop()
		{
			return _currentScroll <= 0;
		}

		public bool IsAtBottom()
		{
			return _currentScroll + height >= CalculateContentHeight();
		}

		public void ScrollToBottom()
		{
			_currentScroll = CalculateContentHeight() - height;
		}

		public void ScrollTo(int amount)
		{
			_currentScroll = amount;
			ClampScroll();
		}

		public void Clear(bool resetScroll = true)
		{
			List<UIControl> childrenToClear = _children.Where(x => !x.ManualDraw).ToList(); // don't clear scrollbar stuff
			foreach (var child in childrenToClear)
			{
				child.Destroy();
				RemoveChild(child);
			}

			if (resetScroll) _currentScroll = 0;
		}
	}
}