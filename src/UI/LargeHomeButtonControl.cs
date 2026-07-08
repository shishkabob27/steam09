using System.Drawing;
using System.Xml.Serialization;
using KGUI;
using KGUI.Controls;

public class LargeHomeButtonControl : ButtonControl
{
	[XmlAttribute("homeIndex")]
	public int homeIndex = 0;

	private static readonly string BOTTOM_BUTTONS_TEXTURE = Assets.GetAssetPath("graphics/bottom_buttons.png");

	public LargeHomeButtonControl(UIControl parent) : base(parent)
	{
		this.text = Localization.GetString(text);
	}

	public override void Draw()
	{
		if (mouseDown) DrawTextureSheet(BOTTOM_BUTTONS_TEXTURE, 0, 0, homeIndex, 2, 100, 52);
		else if (mouseOver) DrawTextureSheet(BOTTOM_BUTTONS_TEXTURE, 0, 0, homeIndex, 1, 100, 52);
		else DrawTextureSheet(BOTTOM_BUTTONS_TEXTURE, 0, 0, homeIndex, 0, 100, 52);

		int textX = 51;
		int textY = 41;
		Color textColor = Color.FromArgb(143, 146, 141);
		switch (homeIndex)
		{
			case 0:

				DrawText(Localization.GetString("Steam_News"), textX, textY, textColor, alignment: FontAlignment.Center);
				break;
			case 1:
				DrawText(Localization.GetString("Steam_Friends"), textX, textY, textColor, alignment: FontAlignment.Center);
				break;
			case 2:
				DrawText(Localization.GetString("Steam_Servers"), textX, textY, textColor, alignment: FontAlignment.Center);
				break;
			case 3:
				DrawText(Localization.GetString("Steam_Settings"), textX, textY, textColor, alignment: FontAlignment.Center);
				break;
			case 4:
				textX++;
				DrawText(Localization.GetString("Steam_Support"), textX, textY, textColor, alignment: FontAlignment.Center);
				break;
		}
	}
}