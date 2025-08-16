using SDL_Sharp;

public class ButtonControl : UIControl
{
	public int style;

	public ButtonControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0, string text = "", int style = 0) : base(parent, renderer, controlName, x, y, width, height)
	{
		this.text = Localization.GetString(text);
		this.style = style;

		//double clicking a button should still trigger a click event
		OnDoubleClick += () =>
		{
			OnClick?.Invoke();
		};
	}

	public override void Draw()
	{
		base.Draw();

		//content
		if (style == 0)
		{
			if (enabled) SDL.SetRenderDrawColor(renderer, 125, 128, 120, 255);
			else SDL.SetRenderDrawColor(renderer, 104, 106, 101, 255);
		}
		else
		{
			if (enabled) SDL.SetRenderDrawColor(renderer, 85, 88, 82, 255);
			else SDL.SetRenderDrawColor(renderer, 70, 70, 70, 255);
		}
		Rect rect = new Rect(x + 1, y + 1, width - 2, height - 2);
		SDL.RenderFillRect(renderer, ref rect);

		//border
		if (style == 0)
		{
			if (mouseDown) SDL.SetRenderDrawColor(renderer, 196, 181, 80, 255);
			else if (enabled) SDL.SetRenderDrawColor(renderer, 7, 4, 12, 255);
			else SDL.SetRenderDrawColor(renderer, 79, 80, 79, 255);
		}
		else
		{
			if (mouseDown) SDL.SetRenderDrawColor(renderer, 196, 181, 80, 255);
			else if (enabled) SDL.SetRenderDrawColor(renderer, 7, 4, 12, 255);
			else SDL.SetRenderDrawColor(renderer, 53, 53, 55, 255);
		}
		SDL.RenderDrawLine(renderer, x + 1, y, x + width - 2, y); // top
		SDL.RenderDrawLine(renderer, x, y + 1, x, y + height - 2); // left
		SDL.RenderDrawLine(renderer, x + 1, y + height - 1, x + width - 2, y + height - 1); // bottom
		SDL.RenderDrawLine(renderer, x + width - 1, y + 1, x + width - 1, y + height - 2); // right

		//corners
		if (style == 0)
		{
			if (!enabled) SDL.SetRenderDrawColor(renderer, 91, 93, 89, 255);
			else if (mouseDown) SDL.SetRenderDrawColor(renderer, 196, 181, 80, 255);
			else SDL.SetRenderDrawColor(renderer, 65, 66, 66, 255);
		}
		else
		{
			if (!enabled) SDL.SetRenderDrawColor(renderer, 61, 61, 62, 255);
			else if (mouseDown) SDL.SetRenderDrawColor(renderer, 196, 181, 80, 255);
			else SDL.SetRenderDrawColor(renderer, 45, 46, 47, 255);
		}
		SDL.RenderDrawPoint(renderer, x + 1, y + 1);
		SDL.RenderDrawPoint(renderer, x + width - 2, y + 1);
		SDL.RenderDrawPoint(renderer, x + 1, y + height - 2);
		SDL.RenderDrawPoint(renderer, x + width - 2, y + height - 2);

		//text
		Color textColor;
		if (!enabled) textColor = new Color(121, 126, 121, 255);
		else if (mouseOver) textColor = new Color(196, 181, 80, 255);
		else textColor = new Color(255, 255, 255, 255);
		int textX = x + 10;
		int textY = y + (height / 2) - 5;
		if (mouseDown)
		{
			textX += 2;
			textY += 1;
		}
		parent.DrawText(text, textX, textY, textColor);
	}
}