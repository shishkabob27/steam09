using SDL_Sharp;

public class ScrollbarButtonControl : ButtonControl
{
	bool up = false;

	public ScrollbarButtonControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0, int index = 0, bool up = false) : base(parent, renderer, controlName, x, y, width, height)
	{
		this.up = up;
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

		//arrow
		SDL.SetRenderDrawColor(renderer, 216, 222, 211, 255);

		// this kinda sucks
		if (up)
		{
			SDL.RenderDrawLine(renderer, x + 7, y + 5, x + 7, y + 5);
			SDL.RenderDrawLine(renderer, x + 6, y + 6, x + 8, y + 6);
			SDL.RenderDrawLine(renderer, x + 5, y + 7, x + 9, y + 7);
			SDL.RenderDrawLine(renderer, x + 4, y + 8, x + 10, y + 8);
		}
		else
		{
			SDL.RenderDrawLine(renderer, x + 4, y + 5, x + 10, y + 5);
			SDL.RenderDrawLine(renderer, x + 5, y + 6, x + 9, y + 6);
			SDL.RenderDrawLine(renderer, x + 6, y + 7, x + 8, y + 7);
			SDL.RenderDrawLine(renderer, x + 7, y + 8, x + 7, y + 8);
		}
	}
}