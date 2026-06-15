using System.Drawing;

namespace KGUI
{
	public class SolidBackgroundControl : UIControl
	{
		public SolidBackgroundControl(UIControl parent) : base(parent)
		{
			AcceptMouseEvents = false;
		}
		
		public override void Draw()
		{
			if (BackgroundColor != "")
			{
				string[] colors = BackgroundColor.Replace(" ", "").Split(',');
				if (colors.Length == 3)
				{
					DrawBox(0, 0, width, height, Color.FromArgb(255, int.Parse(colors[0]), int.Parse(colors[1]), int.Parse(colors[2])));
				}
				else
				{
					Console.WriteLine($"Invalid background color for {ID} ({GetType().Name}): {BackgroundColor}");
				}
			}
		}
	}
}