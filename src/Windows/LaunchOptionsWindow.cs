using SDL_Sharp;

public class LaunchOptionsWindow : SteamWindow
{
	Game game;
	List<Tuple<string, string, string>> launchConfigs;
	int selectedLaunchConfig = -1;

	ButtonControl launchButton;
	ButtonControl cancelButton;

	List<RadioButtonControl> radioButtons = new List<RadioButtonControl>();

	public LaunchOptionsWindow(Steam steam, string title, int width, int height, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0) : base(steam, title, width, height, resizable, minimumWidth, minimumHeight)
	{
		launchButton = new ButtonControl(panel, renderer, "launchbutton", 0, 0, 98, 24, "Launch", 1);
		cancelButton = new ButtonControl(panel, renderer, "cancelbutton", 0, 0, 98, 24, "Cancel", 1);
		panel.AddControl(launchButton);
		panel.AddControl(cancelButton);

		launchButton.OnClick += Launch;
		cancelButton.OnClick += () =>
		{
			steam.PendingWindowsToRemove.Add(this);
		};
	}

	public void SetGame(Game game)
	{
		this.game = game;
	}

	public void SetLaunchConfigs(List<Tuple<string, string, string>> launchConfigs)
	{
		this.launchConfigs = launchConfigs;

		foreach (Tuple<string, string, string> launchConfig in launchConfigs)
		{
			RadioButtonControl radioButton = new RadioButtonControl(panel, renderer, $"launchconfig_{launchConfig.Item3}", 20, 32 + radioButtons.Count * 30, text: launchConfig.Item3);
			radioButtons.Add(radioButton);
			panel.AddControl(radioButton);
			radioButton.OnSelected += (selected) =>
			{
				selectedLaunchConfig = radioButtons.IndexOf(radioButton);
			};
		}
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		if (selectedLaunchConfig == -1)
		{
			radioButtons[0].selected = true;
			selectedLaunchConfig = 0;
		}

		//Align buttons to the bottom right of the window
		cancelButton.x = mWidth - cancelButton.width - 10;
		cancelButton.y = mHeight - cancelButton.height - 10;

		launchButton.x = cancelButton.x - launchButton.width - 10;
		launchButton.y = mHeight - launchButton.height - 10;
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

		launchButton.Draw();
		cancelButton.Draw();

		SDL.RenderPresent(renderer);
	}

	public void Launch()
	{
		steam.BeginLaunchGame(game, launchConfigs[selectedLaunchConfig]);
		steam.PendingWindowsToRemove.Add(this);
	}
}