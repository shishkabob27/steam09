using SDL_Sharp;

public class PopupButtonControl : UIControl
{

	public PopupButtonControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0, string text = "", int style = 0) : base(parent, renderer, controlName, x, y, width, height)
	{
		this.height = 24;
		this.text = text;
	}

	public override void Draw()
	{
		base.Draw();

		if (mouseOver) parent.DrawBox(x, y, width, height, new Color(147, 134, 59, 255));

		parent.DrawText(text, x + 6, y + 7, new Color(255, 255, 255, 255));
	}
}