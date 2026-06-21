using System.Drawing;
using System.Xml.Serialization;

namespace KGUI.Controls
{
	public class TabItemControl : UIControl
	{
		public bool selected = false;

		[XmlAttribute("default")]
		public bool Default = false;

		public TabItemControl(UIControl parent) : base(parent)
		{
			height = 22;

			OnClick = (c) =>
			{
				if (parent is TabListControl tabList)
				{
					tabList.SetTabSelected(this);
				}
			};
		}

		public override void Draw()
		{
			base.Draw();

			//content
			Color backgroundColor = Color.FromArgb(70, 70, 70);
			if (selected) backgroundColor = Color.FromArgb(104, 106, 101);
			DrawBox(1, 1, width - 2, height - 1, backgroundColor);

			//border
			Color borderColor = Color.FromArgb(116, 116, 116);
			DrawLine(1, 0, width - 2, 0, borderColor); // top
			DrawLine(0, 1, 0, height - 1, borderColor); // left
			DrawLine(width - 1, 1, width - 1, height - 1, borderColor); // right

			//corners
			if (!selected)
			{
				Color cornerColor = Color.FromArgb(93, 93, 93);
				DrawPoint(1, 1, cornerColor);
				DrawPoint(width - 2, 1, cornerColor);
			}

			//text
			Color textColor;
			if (selected || mouseOver) textColor = Color.FromArgb(196, 181, 80);
			else textColor = Color.FromArgb(216, 222, 211);
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
}