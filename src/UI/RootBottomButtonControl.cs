using SDL_Sharp;

public class RootBottomButtonControl : ButtonControl
{
	public Texture texture;
	int index;
	public RootBottomButtonControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0, string text = "", int style = 0, int index = 0) : base(parent, renderer, controlName, x, y, width, height, text, style)
	{
		this.index = index;
	}

	public override void Draw()
	{
		if (mouseDown) parent.DrawTextureSheet(texture, x, y, index, 2, 100, 52);
		else if (mouseOver) parent.DrawTextureSheet(texture, x, y, index, 1, 100, 52);
		else parent.DrawTextureSheet(texture, x, y, index, 0, 100, 52);
	}
}