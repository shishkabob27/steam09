using System.Drawing;
using KGUI;
using KGUI.Controls;

public class PreparingToLaunchWindow : SteamWindow
{
	Game game;
	LaunchConfig launchConfig;

	float time = 0;
	bool done = false;

	public PreparingToLaunchWindow(Steam steam, string uuid) : base(steam, uuid) {}

	public void SetGame(Game game, LaunchConfig launchConfig)
	{
		this.game = game;
		this.launchConfig = launchConfig;

		Steam.Instance.OnSelectedLaunchConfigChanged(game, launchConfig);

		backendWindow.SetTitle(Localization.GetString("Steam_GameLaunchOptions_Title").Replace("%game%", game.Name));
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		if (game == null) return;

		if (game.LaunchAppTaskStatus != LaunchAppTaskStatus.DelayLaunch)
			return;
			
		time += deltaTime;

		if (time > 1 && !done)
		{
			client.LaunchGameProcess(game, launchConfig);
			windowEnvironment.RemoveWindow(this);
			done = true;
		}
	}

	public override void Draw()
	{
		base.Draw();
		
		if (game == null) return;

		int stage = (int)Math.Min((time * 3) + 1, 3);
		panel.DrawText(Localization.GetString($"SteamUI_JoinDialog_PreparingToPlay{stage}").Replace("%s1", game.Name), 28, 48, Color.FromArgb(230, 236, 224));
	}
}