using KGUI;
using KGUI.Controls;
using SDL;

public class SteamGuardWindow : SteamWindow
{
	LabelControl promptLabel;
	ButtonControl enterButton;
	TextEntryControl codeEdit;
	SteamGuardAuthenticator? authenticator;
	bool isConfirmationMode = false;

	public SteamGuardWindow(Steam steam, string uuid) : base(steam, uuid)
	{
		promptLabel = panel.GetControlByID<LabelControl>("PromptLabel");
		codeEdit = panel.GetControlByID<TextEntryControl>("CodeEdit");
		enterButton = panel.GetControlByID<ButtonControl>("EnterButton");

		// Default to code entry mode
		SetCodeEntryMode();
	}

	public void SetAuthenticator(SteamGuardAuthenticator authenticator)
	{
		this.authenticator = authenticator;
	}

	public void SetDeviceCodeMode()
	{
		promptLabel.text = "Please enter the code from your authenticator";
		SetCodeEntryMode();
	}

	public void SetEmailMode(string email)
	{
		promptLabel.text = $"Please enter the code sent to {email}";
		SetCodeEntryMode();
	}

	public void SetConfirmationMode()
	{
		promptLabel.text = "Please approve this login on your Steam Mobile App...";
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

	void OnEnterCode()
	{
		if (codeEdit.text.Length != 5)
		{
			return;
		}

		// Send the code to the authenticator
		authenticator?.OnCodeEntered(codeEdit.text);

		WindowManager.Instance.CloseWindow(this);
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

		WindowManager.Instance.CloseWindow(this);
	}

	public override void OnKeyDown(SDL_Keycode key, SDL_Keymod mod)
	{
		base.OnKeyDown(key, mod);

		// Handle Escape key to cancel
		if (key == SDL_Keycode.SDLK_ESCAPE)
		{
			OnCancelPressed();
		}
	}
}