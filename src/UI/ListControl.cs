using SDL_Sharp;
using SDL_Sharp.Image;

public class ListControl : UIControl
{

	public List<UIControl> Children = new List<UIControl>();

	public int Gap = 0; // gap between children

	public int scroll = 0;

	public ScrollbarButtonControl scrollbarButtonUp;
	public ScrollbarButtonControl scrollbarButtonDown;

	public ScrollbarControl scrollbarControl;

	private bool isDraggingScrollbar = false;
	private int dragStartMouseY = 0;
	private int dragStartScroll = 0;

	private bool IsLeftMouseDown()
	{
		MouseButtonMask mouseState = SDL.GetGlobalMouseState(out int x, out int y);
		return mouseState.HasFlag(MouseButtonMask.Left);
	}

	public ListControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0) : base(parent, renderer, controlName, x, y, width, height)
	{
		acceptMouseButtons = false;

		OnScroll = (delta) =>
		{
			scroll += delta > 0 ? -40 : 40;
		};

		scrollbarButtonUp = new ScrollbarButtonControl(parent, renderer, "scrollbarButtonUp", x + width - 18, 0, 15, 14, 0, true);
		scrollbarButtonDown = new ScrollbarButtonControl(parent, renderer, "scrollbarButtonDown", x + width - 18, 0, 15, 14, 1, false);
		scrollbarControl = new ScrollbarControl(parent, renderer, "scrollbarControl", x + width - 18, y, 15, height);
		parent.AddControl(scrollbarButtonUp);
		parent.AddControl(scrollbarButtonDown);
		parent.AddControl(scrollbarControl);

		scrollbarButtonUp.OnClick += () =>
		{
			scroll -= 40;
		};
		scrollbarButtonDown.OnClick += () =>
		{
			scroll += 40;
		};

		parent.AddControl(scrollbarControl);
	}

	void ClampScroll()
	{
		if (scroll + height > CalculateContentHeight())
		{
			scroll = CalculateContentHeight() - height;
		}

		if (scroll < 0) scroll = 0;
	}

	public override void Update()
	{
		base.Update();

		ClampScroll();

		int initialY = y;
		y -= scroll;
		foreach (var child in Children)
		{
			//set position
			child.x = this.x;
			child.y = this.y;
			child.width = this.width;
			y += child.height + (child == Children.Last() ? 0 : Gap);
		}
		y = initialY;

		if (parent.MouseY < y || parent.MouseY > y + height || parent.MouseX < x || parent.MouseX > x + width)
		{
			foreach (var child in Children)
			{
				child.mouseOver = false;
				child.mouseDown = false;
			}
		}
		else if (ShouldDrawScrollbar() && parent.MouseX >= x + width - 21 && parent.MouseX <= x + width) // if mouse is over the scrollbar, also don't allow any children to be mouse over or down
		{
			foreach (var child in Children)
			{
				child.mouseOver = false;
				child.mouseDown = false;
			}
		}

		//Scrollbar update
		if (ShouldDrawScrollbar())
		{
			scrollbarButtonUp.enabled = true;
			scrollbarButtonDown.enabled = true;
			scrollbarControl.enabled = true;

			//buttons
			scrollbarButtonUp.x = x + width - 18;
			scrollbarButtonUp.y = y + 3;
			scrollbarButtonDown.x = x + width - 18;
			scrollbarButtonDown.y = y + height - 18;

			scrollbarControl.x = x + width - 18;

			//scrollbar things
			{
				if (isDraggingScrollbar)
				{
					//set mouseover/mouseDown to false on everything, prevents false mouse events when letting go of the scrollbar
					foreach (var control in parent.controls)
					{
						control.mouseOver = false;
						control.mouseDown = false;
					}
				}

				scrollbarControl.x = x + width - 18;

				int ystart = this.y + 21;
				int yend = this.y + this.height - 22;
				int trackHeight = yend - ystart;

				int contentHeight = CalculateContentHeight();
				int visibleHeight = this.height;

				int thumbHeight = Math.Max(14, (int)(trackHeight * visibleHeight / contentHeight));

				int maxScroll = contentHeight - visibleHeight;
				int scrollbarPosition = ystart;
				if (maxScroll > 0)
				{
					scrollbarPosition = ystart + (scroll * (trackHeight - thumbHeight)) / maxScroll;
				}

				scrollbarControl.y = scrollbarPosition;
				scrollbarControl.height = thumbHeight;

				if (!isDraggingScrollbar && scrollbarControl.mouseDown)
				{
					isDraggingScrollbar = true;
					dragStartMouseY = parent.MouseY;
					dragStartScroll = scroll;
				}
				else if (isDraggingScrollbar && !IsLeftMouseDown())
				{
					isDraggingScrollbar = false;
				}

				if (isDraggingScrollbar)
				{
					int mouseDelta = parent.MouseY - dragStartMouseY;
					int maxScrollbarTravel = trackHeight - thumbHeight;
					if (maxScrollbarTravel > 0)
					{
						int scrollDelta = (mouseDelta * maxScroll) / maxScrollbarTravel;
						scroll = dragStartScroll + scrollDelta;
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

		//DEBUG: draw line at 0 
		//parent.DrawBox(x, y - scroll, width, 1, new Color(255, 0, 0, 255));

		//clip
		Rect clipRect = new Rect(x, y, width, height);
		SDL.RenderSetClipRect(renderer, ref clipRect);

		//draw children
		foreach (var child in Children)
		{
			child.Draw();
		}

		//draw scrollbar
		if (ShouldDrawScrollbar())
		{
			//background
			parent.DrawBox(x + width - 21, y, 21, height, new Color(70, 70, 70, 255));

			//buttons
			scrollbarButtonUp.Draw();
			scrollbarButtonDown.Draw();

			//scrollbar
			scrollbarControl.Draw();
		}

		//unclip
		unsafe
		{
			SDL.RenderSetClipRect(renderer, null);
		}

		//debug draw line at end of content
		//parent.DrawBox(x, CalculateContentHeight() - scroll + y, width, 1, new Color(255, 0, 0, 255));
	}

	public void Sort(Comparison<UIControl> comparison)
	{
		Children.Sort(comparison);
	}

	public bool ShouldDrawScrollbar()
	{
		return CalculateContentHeight() > height;
	}

	public int CalculateContentHeight()
	{
		int height = 0;
		foreach (var child in Children)
		{
			height += child.height + Gap;
		}
		return height - Gap;
	}

	public bool IsAtBottom()
	{
		return scroll + height >= CalculateContentHeight();
	}

	public void ScrollToBottom()
	{
		scroll = CalculateContentHeight() - height;
	}

	public void Clear(bool resetScroll = true)
	{
		Children.Clear();
		if (resetScroll) scroll = 0;
	}
}