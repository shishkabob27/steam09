using SDL_Sharp;

public class GameCategoryToggleControl : UIControl
{
	public bool Open = true;

	public GameCategoryToggleControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0, string text = "") : base(parent, renderer, controlName, x, y, width, height)
	{
		this.width = 14;
		this.height = 9;
	}

	public override void Draw()
	{
		base.Draw();

		if (mouseOver)
		{
			if (Open) parent.DrawTexture(parent.categoryCollapsedMouseOverTexture, x, y);
			else parent.DrawTexture(parent.categoryExpandedMouseOverTexture, x, y);
		}
		else
		{
			if (Open) parent.DrawTexture(parent.categoryCollapsedTexture, x, y);
			else parent.DrawTexture(parent.categoryExpandedTexture, x, y);
		}
	}
}