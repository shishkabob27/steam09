using System.Drawing;
using KGUI;

public class PopupButtonControl : UIControl
{
	public const int TextPaddingX = 6;
	public PopupButtonControl(UIControl parent) : base(parent)
	{
		this.height = 24;
	}

	public override void Draw()
	{
		base.Draw();

		if (mouseOver) DrawBox(0, 0, width, height, Color.FromArgb(147, 134, 59));
		DrawText(text, TextPaddingX, 7, Color.FromArgb(255, 255, 255));
	}
}