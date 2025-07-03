using SDL_Sharp;

public class ScrollbarControl : ButtonControl
{
	public ScrollbarControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0, int index = 0) : base(parent, renderer, controlName, x, y, width, height)
	{
	}

	public override void Draw()
	{
		//content
		SDL.SetRenderDrawColor(renderer, 104, 106, 101, 255);
		Rect rect = new Rect(x + 1, y + 1, width - 2, height - 2);
		SDL.RenderFillRect(renderer, ref rect);

		//border
		SDL.SetRenderDrawColor(renderer, 88, 88, 88, 255);
		SDL.RenderDrawLine(renderer, x + 1, y, x + width - 2, y); // top
		SDL.RenderDrawLine(renderer, x, y + 1, x, y + height - 2); // left
		SDL.RenderDrawLine(renderer, x + 1, y + height - 1, x + width - 2, y + height - 1); // bottom
		SDL.RenderDrawLine(renderer, x + width - 1, y + 1, x + width - 1, y + height - 2); // right

		//corners
		SDL.SetRenderDrawColor(renderer, 100, 101, 97, 255);
		SDL.RenderDrawPoint(renderer, x + 1, y + 1);
		SDL.RenderDrawPoint(renderer, x + width - 2, y + 1);
		SDL.RenderDrawPoint(renderer, x + 1, y + height - 2);
		SDL.RenderDrawPoint(renderer, x + width - 2, y + height - 2);
	}
}