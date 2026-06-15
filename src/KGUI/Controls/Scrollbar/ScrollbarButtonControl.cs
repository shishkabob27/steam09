using System.Drawing;
using SDL;

namespace KGUI
{
	public class ScrollbarButtonControl : ButtonControl
	{
		public bool Up = false;
		//unsafe SDL_Texture* texture;

		public ScrollbarButtonControl(UIControl parent) : base(parent)
		{
			// unsafe {
			// 	texture = LoadTexture(Assets.GetAssetPath("graphics/scrollbar_button_" + (this.Up ? "up" : "down") + ".png"));
			// }
		}

		public override void Draw()
		{
			// DrawTexture(texture, 0, 0);

			//content
			DrawBox(1, 1, width - 2, height - 2, Color.FromArgb(104, 106, 101));

			//border
			Color borderColor = Color.FromArgb(88, 88, 88);
			DrawLine(1, 0, width - 2, 0, borderColor); // top
			DrawLine(0, 1, 0, height - 2, borderColor); // left
			DrawLine(1, height - 1, width - 2, height - 1, borderColor); // bottom
			DrawLine(width - 1, 1, width - 1, height - 2, borderColor); // right

			//corners
			Color cornerColor = Color.FromArgb(100, 101, 97);
			DrawPoint(1, 1, cornerColor);
			DrawPoint(width - 2, 1, cornerColor);
			DrawPoint(1, height - 2, cornerColor);
			DrawPoint(width - 2, height - 2, cornerColor);

			//arrow
			Color arrowColor = Color.FromArgb(216, 222, 211);

			// this kinda sucks
			if (Up)
			{
				DrawLine(7, 5, 7, 5, arrowColor);
				DrawLine(6, 6, 8, 6, arrowColor);
				DrawLine(5, 7, 9, 7, arrowColor);
				DrawLine(4, 8, 10, 8, arrowColor);
			}
			else
			{
				DrawLine(4, 5, 10, 5, arrowColor);
				DrawLine(5, 6, 9, 6, arrowColor);
				DrawLine(6, 7, 8, 7, arrowColor);
				DrawLine(7, 8, 7, 8, arrowColor);
			}
		}
	}
}