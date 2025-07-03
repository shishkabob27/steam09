using SDL_Sharp;

public class DividerControl : UIControl
{
	public DividerControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0) : base(parent, renderer, controlName, x, y, width, height)
	{
	}

	public override void Draw()
	{
		base.Draw();
		SDL.SetRenderDrawColor(renderer, 45, 45, 43, 255);
		SDL.RenderDrawLine(renderer, x, y, x + width, y);

		SDL.SetRenderDrawColor(renderer, 110, 110, 108, 255);
		SDL.RenderDrawLine(renderer, x, y + 1, x + width, y + 1);
	}
}