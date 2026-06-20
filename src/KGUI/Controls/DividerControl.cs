using System.Drawing;
using KGUI;

namespace KGUI
{
	public class DividerControl : UIControl
	{
		public int paddingX = 0;
		public DividerControl(UIControl parent) : base(parent)
		{
		}

		public override void Draw()
		{
			DrawLine(paddingX, 0, width - paddingX, 0, Color.FromArgb(45, 45, 43));
			DrawLine(paddingX, 1, width - paddingX, 1, Color.FromArgb(110, 110, 108));
		}
	}
}