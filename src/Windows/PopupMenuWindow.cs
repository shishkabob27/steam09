using SDL_Sharp;

public class PopupMenuWindow : SteamWindow
{
	public override bool isPopupWindow { get { return true; } }

	public ListControl list;

	public PopupMenuWindow(Steam steam, string title, int width, int height, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0) : base(steam, title, width, height, resizable, minimumWidth, minimumHeight)
	{
		//move window to mouse position
		int mouseX, mouseY;
		SDL.GetGlobalMouseState(out mouseX, out mouseY);
		SetWindowPosition(mouseX, mouseY);

		list = new ListControl(panel, renderer, "list", 0, 0, 120, 120);
		panel.AddControl(list);
	}

	public void AddItem(string text, Action onClick)
	{
		PopupButtonControl button = new PopupButtonControl(panel, renderer, $"button_{text}", 0, 0, 120, 20, text);
		list.Children.Add(button);
		panel.AddControl(button);
		button.OnClick += onClick;
	}

	public void AddSeparator()
	{
		list.Children.Add(new DividerControl(panel, renderer, "divider", 4, 0, 112, 1));
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		if (!KeyboardFocus)
		{
			steam.PendingWindowsToRemove.Add(this);
		}

		//resize window to fit list
		SetWindowSize(mWidth, list.CalculateContentHeight());

		list.x = 0;
		list.y = 0;
		list.width = mWidth;
		list.height = mHeight;
	}

	public override void Draw()
	{
		base.Draw();

		panel.DrawBox(0, 0, mWidth, mHeight, new Color(46, 49, 44, 255));

		//draw list
		list.Draw();

		//draw window border
		SDL.SetRenderDrawColor(renderer, 100, 106, 100, 255);
		Rect borderRect = new Rect(0, 0, mWidth, mHeight);
		SDL.RenderDrawRect(renderer, ref borderRect);

		SDL.RenderPresent(renderer);
	}
}