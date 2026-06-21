using System.Drawing;
using SDL;

namespace KGUI.Controls
{	
	public class RadioButtonControl : UIControl
	{
		public bool selected = false;

		public Action<bool> OnSelected;

		unsafe SDL_Texture* radioButtonTexture;

		public RadioButtonControl(UIControl parent) : base(parent)
		{
			//this.width = 22;
			//this.height = 22;
			//this.text = text;
			//this.width = 22 + 25 + parent.steamFont8.MeasureText(text);

			unsafe
			{
				radioButtonTexture = LoadTexture(Assets.GetAssetPath("graphics/radiobutton.png"));
			}


			OnClick += (control) =>
			{
				//TODO: support for multiple groups of radio buttons
				foreach (UIControl parentControl in parent.Children)
				{
					if (parentControl is RadioButtonControl)
					{
						RadioButtonControl radioButton = (RadioButtonControl)parentControl;
						radioButton.selected = false;
					}
				}

				selected = true;
				OnSelected?.Invoke(selected);
			};
		}

		public override void Draw()
		{
			base.Draw();

			unsafe
			{				
				if (enabled)
				{
					if (mouseDown) DrawTextureSheet(radioButtonTexture, 0, 0, 2, 0, 22, 22);
					else if (selected && focused) DrawTextureSheet(radioButtonTexture, 0, 0, 3, 0, 22, 22);
					else if (selected) DrawTextureSheet(radioButtonTexture, 0, 0, 4, 0, 22, 22);
					else if (focused) DrawTextureSheet(radioButtonTexture, 0, 0, 6, 0, 22, 22);
					else DrawTextureSheet(radioButtonTexture, 0, 0, 5, 0, 22, 22);
				}
				else
				{
					if (selected) parent.DrawTextureSheet(radioButtonTexture, 0, 0, 1, 0, 22, 22);
					else parent.DrawTextureSheet(radioButtonTexture, 0, 0, 0, 0, 22, 22);
				}
			}

			//text
			Color textColor;
			if (!enabled) textColor = Color.FromArgb(121, 126, 121);
			else if (mouseOver || focused) textColor = Color.FromArgb(196, 181, 80);
			else textColor = Color.FromArgb(255, 255, 255);
			int textX = x + 25;
			int textY = y + (height / 2) - 5;
			parent.DrawText(text, textX, textY, textColor);
		}
	}
}