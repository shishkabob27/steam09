using SDL_Sharp;

public class LabelControl : UIControl
{
	public FontAlignment Alignment;
	public LabelControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0, string text = "", FontAlignment alignment = FontAlignment.Left) : base(parent, renderer, controlName, x, y, width, height)
	{
		this.text = Localization.GetString(text);
		this.Alignment = alignment;
	}

	public override void Draw()
	{
		base.Draw();

		int textX = x;
		int textY = y + (height / 2) - 5;
		if (Alignment == FontAlignment.Right)
		{
			textX += width;
		}
		else if (Alignment == FontAlignment.Center)
		{
			textX += width / 2;
		}
		parent.DrawText(text, textX, textY, new Color(255, 255, 255, 255), false, false, 8, Alignment);
	}
}