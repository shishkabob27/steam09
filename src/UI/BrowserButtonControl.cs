using SDL_Sharp;

public class BrowserButtonControl : ButtonControl
{
	public Texture texture;
	int index;

	public BrowserButtonControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0, int index = 0) : base(parent, renderer, controlName, x, y, width, height)
	{
		this.index = index;
		zIndex = 2;
	}

	public override void Draw()
	{
		if (!enabled) parent.DrawTextureSheet(texture, x, y, 0, index, 16, 16);
		else if (mouseDown) parent.DrawTextureSheet(texture, x, y, 3, index, 16, 16);
		else if (mouseOver) parent.DrawTextureSheet(texture, x, y, 2, index, 16, 16);
		else parent.DrawTextureSheet(texture, x, y, 1, index, 16, 16);
	}
}