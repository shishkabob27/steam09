using System.Drawing;
using System.Xml.Serialization;
using SDL;

namespace KGUI.Controls
{
	public class ButtonControl : UIControl
	{
		[XmlAttribute("style")]
		public int style;

		unsafe SDL_Texture* texture;

		public unsafe ButtonControl(UIControl parent) : base(parent)
		{
			this.text = Localization.GetString(text);

			//texture = LoadTexture(Assets.GetAssetPath("graphics/button.png"));

			//double clicking a button should still trigger a click event
			OnDoubleClick += (control) =>
			{
				OnClick?.Invoke(control);
			};
		}

		public unsafe override void Draw()
		{
			base.Draw();

			// {
			// 	DrawTexture9Grid(texture, 0, 0, width, height, 6, 6, 6, 6, 1.0f);
			// }

			Color fillColor;
			if (style == 0)
			{
				if (enabled) fillColor = Color.FromArgb( 125, 128, 120);
				else fillColor = Color.FromArgb(104, 106, 101);
			}
			else
			{
				if (enabled) fillColor = Color.FromArgb(85, 88, 82);
				else fillColor = Color.FromArgb(70, 70, 70);
			}

			DrawBox(1, 1, width-2, height-2, fillColor);

			// Rect rect = new Rect(x + 1, y + 1, width - 2, height - 2);
			// SDL.RenderFillRect(renderer, ref rect);

			//border
			Color borderColor;
			if (style == 0)
			{
				if (mouseDown) borderColor = Color.FromArgb(196, 181, 80);
				else if (enabled) borderColor = Color.FromArgb(7, 4, 12);
				else borderColor = Color.FromArgb(79, 80, 79);
			}
			else
			{
				if (mouseDown) borderColor = Color.FromArgb(196, 181, 80);
				else if (enabled) borderColor = Color.FromArgb(7, 4, 12);
				else borderColor = Color.FromArgb(53, 53, 55);
			}


			DrawLine(1, 0, width - 2, 0, borderColor); // top
			DrawLine(0, 1, 0, height - 2, borderColor); // left
			DrawLine(1, 0 + height - 1, width - 2, height - 1, borderColor); // bottom
			DrawLine(0 + width - 1, 1,width - 1, height - 2, borderColor); // right

			//corners
			Color cornerColor;
			if (style == 0)
			{
				if (!enabled) cornerColor = Color.FromArgb(91, 93, 89);
				else if (mouseDown) cornerColor = Color.FromArgb(196, 181, 80);
				else cornerColor = Color.FromArgb(65, 66, 66);
			}
			else
			{
				if (!enabled) cornerColor = Color.FromArgb(61, 61, 62);
				else if (mouseDown) cornerColor = Color.FromArgb(196, 181, 80);
				else cornerColor = Color.FromArgb(45, 46, 47);
			}
			DrawPoint(x + 1, y + 1, cornerColor);
			DrawPoint(x + width - 2, y + 1, cornerColor);
			DrawPoint(x + 1, y + height - 2, cornerColor);
			DrawPoint(x + width - 2, y + height - 2, cornerColor);

			//text
			Color textColor;
			if (!enabled) textColor = Color.FromArgb(121, 126, 121);
			else if (mouseOver) textColor = Color.FromArgb(196, 181, 80);
			else textColor = Color.White;
			int textX = 10;
			int textY = (height / 2) - 5;
			if (mouseDown)
			{
				textX += 2;
				textY += 1;
			}
			DrawText(text, textX, textY, textColor);
		}

		public override void Destroy()
		{
			base.Destroy();
			unsafe {
				SDL3.SDL_DestroyTexture(texture);
			}
		}
	}
}