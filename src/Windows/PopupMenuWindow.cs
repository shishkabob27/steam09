using KGUI;
using KGUI.Controls;
using SDL;

public class PopupMenuWindow : SteamWindow
{
	public override bool isPopupWindow { get { return true; } }

	ListViewControl list;

	public PopupMenuWindow(Steam steam, string uuid) : base(steam, uuid)
	{
		list = panel.GetControlByID<ListViewControl>("list");
		//move window to mouse position
		float mX, mY = 0;
		unsafe
		{
			SDL3.SDL_GetGlobalMouseState(&mX, &mY);
		}

		SetWindowPosition((int)mX, (int)mY);
	}

	public void AddItem(string text, Action<PopupButtonControl>? onClick)
	{
		PopupButtonControl button = new(list);
		button.text = text;
		list.AddChild(button);

		if (onClick != null)
		{
			button.OnClick += (b) =>
			{
				onClick(button);
				WindowManager.Instance.CloseWindow(this);
			};
		}

		AdjustSize();
	}

	public void AddSeparator()
	{
		DividerControl divider = new DividerControl(list);
		divider.paddingX = 5;
		divider.height = 3;
		list.AddChild(divider);

		AdjustSize();
	}

	void AdjustSize()
	{
		int maxWidth = 0;
		foreach (var child in list.Children)
		{
			int childWidth = child.MeasureTextWidth(child.text) + PopupButtonControl.TextPaddingX * 2;
			if (childWidth > maxWidth) maxWidth = childWidth;
		}
		SetWindowSize(maxWidth + (WindowBorderSize * 2), list.CalculateContentHeight() + (WindowBorderSize * 2));
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		if (!KeyboardFocus)
		{
			WindowManager.Instance.CloseWindow(this);
		}
	}
}