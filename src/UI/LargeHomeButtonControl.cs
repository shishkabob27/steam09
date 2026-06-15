using System.Drawing;
using System.Xml.Serialization;
using KGUI;
using SDL;

public class LargeHomeButtonControl : ButtonControl
{
	[XmlAttribute("homeIndex")]
	public int homeIndex = 0;

	unsafe SDL_Texture* texture;

	public LargeHomeButtonControl(UIControl parent) : base(parent)
	{
		this.text = Localization.GetString(text);

		unsafe {
			texture = LoadTexture(Assets.GetAssetPath("graphics/bottom_buttons.png"));
		}
	}

	public unsafe override void Draw()
	{
		if (mouseDown) DrawTextureSheet(texture, 0, 0, homeIndex, 2, 100, 52);
		else if (mouseOver) DrawTextureSheet(texture, 0, 0, homeIndex, 1, 100, 52);
		else DrawTextureSheet(texture, 0, 0, homeIndex, 0, 100, 52);

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

	public override void Destroy()
	{
		base.Destroy();
		unsafe {
			SDL3.SDL_DestroyTexture(texture);
		}
	}
}