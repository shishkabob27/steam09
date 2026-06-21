using System.Drawing;
using SDL;

namespace KGUI.Controls
{
	public class ScrollbarControl : ButtonControl
	{
		public ScrollbarControl(UIControl parent) : base(parent)
		{
		}

		public override void Draw()
		{
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
		}
	}
}