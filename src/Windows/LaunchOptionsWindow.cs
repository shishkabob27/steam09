using KGUI;

public class LaunchOptionsWindow : SteamWindow
{
	Game game;
	List<LaunchConfig> launchConfigs;
	int selectedLaunchConfig = -1;

	ButtonControl launchButton;
	ButtonControl cancelButton;

	List<RadioButtonControl> radioButtons = new List<RadioButtonControl>();

	public LaunchOptionsWindow(Steam steam, string uuid) : base(steam, uuid)
	{
		launchButton = panel.GetControlByID<ButtonControl>("launchbutton");
		cancelButton = panel.GetControlByID<ButtonControl>("cancelbutton");

		launchButton.text = Localization.GetString("Steam_Launch");
		cancelButton.text = Localization.GetString("vgui_Cancel");

		launchButton.OnClick += Launch;
		cancelButton.OnClick += (control) =>
		{
			WindowManager.Instance.CloseWindow(this);
		};
	}

	public void SetGame(Game game)
	{
		this.game = game;
	}

	public void SetLaunchConfigs(List<LaunchConfig> launchConfigs)
	{
		this.launchConfigs = launchConfigs;

		foreach (LaunchConfig launchConfig in launchConfigs)
		{
			RadioButtonControl radioButton = new RadioButtonControl(panel.RootControl);
			radioButton.x = 20;
			radioButton.y = 32 + radioButtons.Count * 30;
			radioButton.width = 300;
			radioButton.height = 24;
			radioButton.text = launchConfig.Description;
			radioButtons.Add(radioButton);
			panel.RootControl.AddChild(radioButton);
			radioButton.OnSelected += (selected) =>
			{
				selectedLaunchConfig = radioButtons.IndexOf(radioButton);
			};
		}
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		if (radioButtons.Count == 0)
			return;

		if (selectedLaunchConfig == -1)
		{
			radioButtons[0].selected = true;
			selectedLaunchConfig = 0;
		}
	}

	public override void Draw()
	{
		base.Draw();

		int y = 40;
		foreach (RadioButtonControl radioButton in radioButtons)
		{
			radioButton.Draw();
			y += 30;
		}
	}

	public void Launch(UIControl control)
	{
		client.BeginLaunchGame(game, launchConfigs[selectedLaunchConfig]);
		WindowManager.Instance.CloseWindow(this);
	}
}