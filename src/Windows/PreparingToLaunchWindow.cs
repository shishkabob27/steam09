using System.Drawing;
using KGUI;

public class PreparingToLaunchWindow : SteamWindow
{
	Game game;
	LaunchConfig launchConfig;

	float time = 0;
	bool done = false;

	public PreparingToLaunchWindow(Steam steam, string uuid) : base(steam, uuid)
	{
	}

	public void SetGame(Game game)
	{
		this.game = game;
	}

	public void SetLaunchConfig(LaunchConfig launchConfig)
	{
		this.launchConfig = launchConfig;
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		time += deltaTime;

		if (time > 1 && !done)
		{
			client.LaunchGameProcess(game, launchConfig);
			WindowManager.Instance.CloseWindow(this);
			done = true;
		}
	}

	public override void Draw()
	{
		base.Draw();

		int stage = (int)Math.Min((time * 3) + 1, 3);
		panel.DrawText(Localization.GetString($"SteamUI_JoinDialog_PreparingToPlay{stage}").Replace("%s1", game.Name), 28, 48, Color.FromArgb(230, 236, 224));
	}
}