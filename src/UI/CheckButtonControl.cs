using SDL_Sharp;

public class CheckButtonControl : UIControl
{
	public bool selected = false;

	public Action<bool> OnSelected;

	public CheckButtonControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0, string text = "") : base(parent, renderer, controlName, x, y, width, height)
	{
		this.width = 22;
		this.height = 22;
		this.text = text;
		this.width = 22 + 25 + parent.steamFont8.MeasureText(text);

		OnClick += () =>
		{
			selected = !selected;
			OnSelected?.Invoke(selected);
		};
		OnDoubleClick += () => OnClick();
	}

	public override void Draw()
	{
		base.Draw();

		if (enabled)
		{
			if (mouseDown) parent.DrawTextureSheet(parent.checkboxTexture, x, y, 4, 0, 22, 22);
			else if (selected && focused) parent.DrawTextureSheet(parent.checkboxTexture, x, y, 5, 0, 22, 22);
			else if (selected) parent.DrawTextureSheet(parent.checkboxTexture, x, y, 3, 0, 22, 22);
			else if (focused) parent.DrawTextureSheet(parent.checkboxTexture, x, y, 6, 0, 22, 22);
			else parent.DrawTextureSheet(parent.checkboxTexture, x, y, 1, 0, 22, 22);
		}
		else
		{
			if (selected) parent.DrawTextureSheet(parent.checkboxTexture, x, y, 2, 0, 22, 22);
			else parent.DrawTextureSheet(parent.checkboxTexture, x, y, 0, 0, 22, 22);
		}

		//text
		Color textColor;
		if (!enabled) textColor = new Color(121, 126, 121, 255);
		else if (mouseOver) textColor = new Color(196, 181, 80, 255);
		else textColor = new Color(255, 255, 255, 255);
		int textX = x + 28;
		int textY = y + (height / 2) - 4;
		parent.DrawText(text, textX, textY, textColor);
	}
}