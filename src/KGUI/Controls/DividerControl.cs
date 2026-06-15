using System.Drawing;
using KGUI;

namespace KGUI
{
	public class DividerControl : UIControl
	{
		public DividerControl(UIControl parent) : base(parent)
		{
		}

		public override void Draw()
		{
			DrawLine(0, 0, width, 0, Color.FromArgb(45, 45, 43));
			DrawLine(0, 1, width, 1, Color.FromArgb(110, 110, 108));
		}
	}
}