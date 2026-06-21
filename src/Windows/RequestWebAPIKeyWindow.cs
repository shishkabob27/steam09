using KGUI;
using KGUI.Controls;

public class RequestWebAPIKeyWindow : SteamWindow
{
	ButtonControl enterButton;
	TextEntryControl webApiKeyEdit;

	public RequestWebAPIKeyWindow(Steam steam, string uuid) : base(steam, uuid)
	{
		SetTitle("STEAM - " + steam.CurrentUser.AccountName);
		enterButton = panel.GetControlByID<ButtonControl>("enterButton");
		webApiKeyEdit = panel.GetControlByID<TextEntryControl>("apiKeyTextBox");
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

	void OnRetrieveButtonClick()
	{
		System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://steamcommunity.com/dev/apikey") { UseShellExecute = true });
	}

	void OnEnterButtonClick()
	{
		if (webApiKeyEdit.text.Length != 32)
		{
			return;
		}

		WindowManager.Instance.CloseWindow(this);

		client.CurrentUser.WebAPIKey = webApiKeyEdit.text;
		client.ModifyLoginUser(client.CurrentUser);
		client.ContinueLogin();
	}
}