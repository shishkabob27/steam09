using SDL_Sharp;
using SDL_Sharp.Image;

public class TabList : UIControl
{
	public List<TabItem> Children = new List<TabItem>();

	public TabList(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0) : base(parent, renderer, controlName, x, y, width, height)
	{
		acceptMouseButtons = false;
	}

	public override void Update()
	{
		base.Update();

		//layout children horizontally
		int x = this.x + 2;
		foreach (var child in Children)
		{
			child.y = y - child.height;
			child.x = x;
			x += child.width + 2;
		}
	}

	public override void Draw()
	{
		base.Draw();

		foreach (var child in Children)
		{
			child.Draw();
		}
	}

	public void SetTabSelected(string tabName, bool selected)
	{
		foreach (var child in Children)
		{
			if (child.ControlName == tabName) child.selected = selected;
			else child.selected = false;
		}

		OnTabSelected?.Invoke(tabName);
	}

	public Action<string> OnTabSelected;
}