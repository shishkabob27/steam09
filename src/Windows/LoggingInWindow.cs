using SDL_Sharp;

public class LoggingInWindow : SteamWindow
{
	public LoggingInWindow(Steam steam, string title, int width, int height, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0) : base(steam, title, width, height, resizable, minimumWidth, minimumHeight)
	{
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);
	}

	public override void Draw()
	{
		base.Draw();

		panel.DrawText($"Connecting Steam account: {steam.CurrentUser.AccountName}...", 30, 52, new Color(255, 255, 255, 255));

		SDL.RenderPresent(renderer);
	}
}