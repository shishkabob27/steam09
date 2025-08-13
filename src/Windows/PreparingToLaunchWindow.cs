using SDL_Sharp;

public class PreparingToLaunchWindow : SteamWindow
{
	Game game;
	Tuple<string, string, string> launchConfig;

	float time = 0;
	bool done = false;
	public PreparingToLaunchWindow(Steam steam, string title, int width, int height, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0) : base(steam, title, width, height, resizable, minimumWidth, minimumHeight)
	{
	}

	public void SetGame(Game game)
	{
		this.game = game;
	}

	public void SetLaunchConfig(Tuple<string, string, string> launchConfig)
	{
		this.launchConfig = launchConfig;
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		time += deltaTime;

		if (time > 1 && !done)
		{
			steam.LaunchGameProcess(game, launchConfig);
			steam.PendingWindowsToRemove.Add(this);
			done = true;
		}
	}

	public override void Draw()
	{
		base.Draw();

		int stage = (int)Math.Min((time * 3) + 1, 3);
		panel.DrawText(Localization.GetString($"SteamUI_JoinDialog_PreparingToPlay{stage}").Replace("%s1", game.Name), 28, 48, new Color(230, 236, 224, 255));

		SDL.RenderPresent(renderer);
	}
}