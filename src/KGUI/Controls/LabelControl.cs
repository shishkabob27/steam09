using System.Drawing;
using System.Xml.Serialization;
using SDL;

namespace KGUI.Controls
{
	public class LabelControl : UIControl
	{
		[XmlAttribute("textAlignment")]
		public string TextAlignment = "west";
		
		public LabelControl(UIControl parent) : base(parent)
		{
		}

		public unsafe override void Draw()
		{
			base.Draw();
			
			int outputWidth = MeasureTextWidth(this.text);
			int outputHeight = MeasureTextWrappedHeight(this.text, this.width);
			int drawX = 0;
			if (TextAlignment == "center")
			{
				drawX = (this.width - outputWidth) / 2;
			}
			else if (TextAlignment == "east")
			{
				drawX = this.width - outputWidth;
			}
			
			int drawY = (this.height - outputHeight) / 2;

			DrawText(this.text, drawX, drawY, Color.FromArgb(216, 222, 211));
		}
	}
}