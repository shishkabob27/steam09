using SDL_Sharp;
using SDL_Sharp.Image;

public class TabItem : UIControl
{
	public bool selected = false;

	TabList tabList;

	public TabItem(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0, string text = "", TabList tabList = null) : base(parent, renderer, controlName, x, y, width, height)
	{
		this.text = text;
		this.tabList = tabList;

		OnClick = () =>
		{
			tabList.SetTabSelected(controlName, true);
		};
	}

	public override void Draw()
	{
		base.Draw();

		//content
		if (selected) SDL.SetRenderDrawColor(renderer, 104, 106, 101, 255);
		else SDL.SetRenderDrawColor(renderer, 70, 70, 70, 255);
		Rect rect = new Rect(x + 1, y + 1, width - 2, height - 1);
		SDL.RenderFillRect(renderer, ref rect);

		//border
		SDL.SetRenderDrawColor(renderer, 116, 116, 116, 255);
		SDL.RenderDrawLine(renderer, x + 1, y, x + width - 2, y); // top
		SDL.RenderDrawLine(renderer, x, y + 1, x, y + height - 1); // left
		SDL.RenderDrawLine(renderer, x + width - 1, y + 1, x + width - 1, y + height - 1); // right

		//corners
		if (!selected)
		{
			SDL.SetRenderDrawColor(renderer, 93, 93, 93, 255);
			SDL.RenderDrawPoint(renderer, x + 1, y + 1);
			SDL.RenderDrawPoint(renderer, x + width - 2, y + 1);
		}

		//text
		Color textColor;
		if (selected) textColor = new Color(196, 181, 80, 255);
		else textColor = new Color(216, 222, 211, 255);
		int textX = x + 9;
		int textY = y + (height / 2) - 5;
		if (mouseDown)
		{
			textX += 2;
			textY += 1;
		}
		parent.DrawText(text, textX, textY, textColor);
	}
}