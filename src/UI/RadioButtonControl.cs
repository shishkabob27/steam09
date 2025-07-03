using SDL_Sharp;

public class RadioButtonControl : UIControl
{
	public bool selected = false;

	public Action<bool> OnSelected;

	public RadioButtonControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0, string text = "") : base(parent, renderer, controlName, x, y, width, height)
	{
		this.width = 22;
		this.height = 22;
		this.text = text;
		this.width = 22 + 25 + parent.steamFont8.MeasureText(text);

		OnClick += () =>
		{
			//TODO: support for multiple groups of radio buttons
			foreach (UIControl control in parent.controls)
			{
				if (control is RadioButtonControl)
				{
					RadioButtonControl radioButton = (RadioButtonControl)control;
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

		if (enabled)
		{
			if (mouseDown) parent.DrawTextureSheet(parent.radioButtonTexture, x, y, 2, 0, 22, 22);
			else if (selected && focused) parent.DrawTextureSheet(parent.radioButtonTexture, x, y, 3, 0, 22, 22);
			else if (selected) parent.DrawTextureSheet(parent.radioButtonTexture, x, y, 4, 0, 22, 22);
			else if (focused) parent.DrawTextureSheet(parent.radioButtonTexture, x, y, 6, 0, 22, 22);
			else parent.DrawTextureSheet(parent.radioButtonTexture, x, y, 5, 0, 22, 22);
		}
		else
		{
			if (selected) parent.DrawTextureSheet(parent.radioButtonTexture, x, y, 1, 0, 22, 22);
			else parent.DrawTextureSheet(parent.radioButtonTexture, x, y, 0, 0, 22, 22);
		}

		//text
		Color textColor;
		if (!enabled) textColor = new Color(121, 126, 121, 255);
		else if (mouseOver || focused) textColor = new Color(196, 181, 80, 255);
		else textColor = new Color(255, 255, 255, 255);
		int textX = x + 25;
		int textY = y + (height / 2) - 5;
		parent.DrawText(text, textX, textY, textColor);
	}
}