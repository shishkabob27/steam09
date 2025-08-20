using SDL_Sharp;

public class SteamGuardWindow : SteamWindow
{
	ButtonControl enterButton;
	TextEntryControl codeEdit;
	SteamGuardAuthenticator? authenticator;
	string promptText = "Please enter the code from your authenticator";
	bool isConfirmationMode = false;

	public SteamGuardWindow(Steam steam, string title, int width, int height, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0) : base(steam, title, width, height, resizable, minimumWidth, minimumHeight)
	{
		enterButton = new ButtonControl(panel, renderer, "enterButton", 155, 110, 125, 24, "Enter", 1);

		codeEdit = new TextEntryControl(panel, renderer, "codeEdit", 20, 80, 260, 24)
		{
			OnEnterPressed = OnEnterPressed,
			maxLength = 5
		};

		enterButton.OnClick += OnEnterPressed;

		// Add all controls
		panel.AddControl(enterButton);
		panel.AddControl(codeEdit);

		// Default to code entry mode
		SetCodeEntryMode();
	}

	public void SetAuthenticator(SteamGuardAuthenticator authenticator)
	{
		this.authenticator = authenticator;
	}

	public void SetDeviceCodeMode()
	{
		promptText = "Please enter the code from your authenticator";
		SetCodeEntryMode();
	}

	public void SetEmailMode(string email)
	{
		promptText = $"Please enter the code sent to {email}";
		SetCodeEntryMode();
	}

	public void SetConfirmationMode()
	{
		promptText = "Please approve this login on your Steam Mobile App...";
		isConfirmationMode = true;

		// Hide code entry controls
		enterButton.enabled = false;
		codeEdit.enabled = false;
		enterButton.visible = false;
		codeEdit.visible = false;
	}

	private void SetCodeEntryMode()
	{
		isConfirmationMode = false;

		enterButton.enabled = false;
		codeEdit.enabled = true;
		enterButton.visible = true;
		codeEdit.visible = true;
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		if (!isConfirmationMode && codeEdit.text.Length == 5)
		{
			enterButton.enabled = true;
		}
		else if (!isConfirmationMode)
		{
			enterButton.enabled = false;
		}
	}

	public override void Draw()
	{
		base.Draw();

		panel.DrawText(promptText, 28, 48, new Color(230, 236, 224, 255));

		SDL.RenderPresent(renderer);
	}

	void OnEnterPressed()
	{
		if (codeEdit.text.Length != 5)
		{
			return;
		}

		// Send the code to the authenticator
		authenticator?.OnCodeEntered(codeEdit.text);

		steam.PendingWindowsToRemove.Add(this);
	}

	void OnCancelPressed()
	{
		if (isConfirmationMode)
		{
			authenticator?.OnConfirmationCancelled();
		}
		else
		{
			authenticator?.OnCodeCancelled();
		}

		steam.PendingWindowsToRemove.Add(this);
	}

	public override void OnKeyDown(Keycode key, KeyModifier mod)
	{
		base.OnKeyDown(key, mod);

		// Handle Escape key to cancel
		if (key == Keycode.Escape)
		{
			OnCancelPressed();
		}
	}
}