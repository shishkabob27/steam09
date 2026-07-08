
using System.Drawing;

namespace KGUI.Controls
{
	public class CheckboxControl : UIControl
	{
		public bool selected = false;

		public Action<bool> OnSelected;

		private static readonly string CHECKBOX_TEXTURE = Assets.GetAssetPath("graphics/checkbox.png");

		public CheckboxControl(UIControl parent) : base(parent)
		{
			this.text = Localization.GetString(text);

			OnClick += (control) =>
			{
				selected = !selected;
				OnSelected?.Invoke(selected);
			};
			OnDoubleClick += (control) => OnClick(this);
		}

		public override void Draw()
		{
			base.Draw();

			if (enabled)
			{
				if (mouseDown && selected) DrawTextureSheet(CHECKBOX_TEXTURE, 0, 0, 4, 0, 22, 22);
				else if (selected && focused) DrawTextureSheet(CHECKBOX_TEXTURE, 0, 0, 5, 0, 22, 22);
				else if (selected) DrawTextureSheet(CHECKBOX_TEXTURE, 0, 0, 3, 0, 22, 22);
				else if (focused) DrawTextureSheet(CHECKBOX_TEXTURE, 0, 0, 6, 0, 22, 22);
				else DrawTextureSheet(CHECKBOX_TEXTURE, 0, 0, 1, 0, 22, 22);
			}
			else
			{
				if (selected) DrawTextureSheet(CHECKBOX_TEXTURE, 0, 0, 2, 0, 22, 22);
				else DrawTextureSheet(CHECKBOX_TEXTURE, 0, 0, 0, 0, 22, 22);
			}
				
			//text
			Color textColor;
			if (!enabled) textColor = Color.FromArgb(121, 126, 121);
			else if (mouseOver) textColor = Color.FromArgb(196, 181, 80);
			else textColor = Color.FromArgb(255, 255, 255);
			int textX = x + 28;
			int textY = y + (height / 2) - 4;
			parent.DrawText(text, textX, textY, textColor);
		}
	}
}