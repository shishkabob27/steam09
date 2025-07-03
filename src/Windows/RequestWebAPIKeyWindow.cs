using SDL_Sharp;

public class RequestWebAPIKeyWindow : SteamWindow
{
	ButtonControl enterButton;
	ButtonControl retrieveButton;
	TextEntryControl webApiKeyEdit;

	public RequestWebAPIKeyWindow(Steam steam, string title, int width, int height, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0) : base(steam, title, width, height, resizable, minimumWidth, minimumHeight)
	{
		retrieveButton = new ButtonControl(panel, renderer, "retrieveButton", 20, 110, 125, 24, "Retrieve key", 1);
		enterButton = new ButtonControl(panel, renderer, "enterButton", 155, 110, 125, 24, "Enter", 1);

		webApiKeyEdit = new TextEntryControl(panel, renderer, "webApiKeyEdit", 20, 80, 260, 24)
		{
			OnEnterPressed = OnEnterPressed,
			maxLength = 32
		};

		enterButton.OnClick += OnEnterPressed;
		retrieveButton.OnClick += () =>
		{
			System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://steamcommunity.com/dev/apikey") { UseShellExecute = true });
		};

		panel.AddControl(enterButton);
		panel.AddControl(retrieveButton);
		panel.AddControl(webApiKeyEdit);
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		if (webApiKeyEdit.text.Length == 32)
		{
			enterButton.enabled = true;
		}
		else
		{
			enterButton.enabled = false;
		}
	}

	public override void Draw()
	{
		base.Draw();

		panel.DrawText("Please enter your Steam Web API key", 28, 48, new Color(230, 236, 224, 255));

		webApiKeyEdit.Draw();
		enterButton.Draw();
		retrieveButton.Draw();

		SDL.RenderPresent(renderer);
	}

	void OnEnterPressed()
	{
		if (webApiKeyEdit.text.Length != 32)
		{
			return;
		}

		steam.PendingWindowsToRemove.Add(this);

		steam.CurrentUser.WebAPIKey = webApiKeyEdit.text;
		steam.ModifyLoginUser(steam.CurrentUser);
		steam.ContinueLogin();
	}
}