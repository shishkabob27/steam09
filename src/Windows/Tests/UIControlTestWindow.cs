using SDL_Sharp;

public class UIControlTestWindow : SteamWindow
{
	ButtonControl button;

	public UIControlTestWindow(Steam steam, string title, int width, int height, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0) : base(steam, title, width, height, resizable, minimumWidth, minimumHeight)
	{
		button = new ButtonControl(panel, renderer, "button", 100, 100, 100, 30);
		panel.AddControl(button);
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);
	}

	public override void Draw()
	{
		base.Draw();

		button.Draw();

		SDL.RenderPresent(renderer);
	}
}