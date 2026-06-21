using System.Threading.Tasks;
using KGUI;
using KGUI.Controls;
using SteamKit2;
using SteamKit2.Authentication;

public class LoginWindow : SteamWindow
{
	TextEntryControl UsernameEdit;
	TextEntryControl PasswordEdit;

	CheckboxControl RememberPasswordCheckbox;

	ButtonControl LoginButton;

	bool isLoggingIn = false;
	private AuthSession _authSession;

	public LoginWindow(Steam client, string uuid) : base(client, uuid)
	{
		UsernameEdit = panel.GetControlByID<TextEntryControl>("UserNameEdit");
		PasswordEdit = panel.GetControlByID<TextEntryControl>("PasswordEdit");
	
		LoginButton = panel.GetControlByID<ButtonControl>("LoginButton");

		RememberPasswordCheckbox = panel.GetControlByID<CheckboxControl>("RememberPasswordCheckbox");

		Steam.Instance.manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
		Steam.Instance.manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		UsernameEdit.enabled = !isLoggingIn;
		PasswordEdit.enabled = !isLoggingIn;
		LoginButton.enabled = !isLoggingIn && !string.IsNullOrWhiteSpace(UsernameEdit.text) && !string.IsNullOrWhiteSpace(PasswordEdit.text);
		RememberPasswordCheckbox.enabled = !isLoggingIn;
	}

	void OnCancel()
	{
		WindowManager.Instance.CloseWindow(this);
	}

	void OnCreateNewAccount()
	{
		System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://store.steampowered.com/join/") { UseShellExecute = true });
	}

	void OnLostPassword()
	{
		System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://help.steampowered.com/en/wizard/HelpWithLogin") { UseShellExecute = true });
	}

	void AttemptSteamLogin()
	{
		Console.WriteLine($"Attempting login for user '{UsernameEdit.text}'");
		isLoggingIn = true;
		Steam.Instance.steamClient.Connect();
	}

	void OnDisconnected(SteamClient.DisconnectedCallback callback)
	{
		isLoggingIn = false;
	}

	async void OnConnected(SteamClient.ConnectedCallback callback)
	{
		// Run authentication on background thread to avoid blocking UI
		_ = Task.Run(async () =>
		{
			try
			{
				Console.WriteLine("Connected to Steam, starting authentication...");

				_authSession = await Steam.Instance.steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails()
				{
					Username = UsernameEdit.text,
					Password = PasswordEdit.text,
					Authenticator = new SteamGuardAuthenticator(),
					IsPersistentSession = RememberPasswordCheckbox.selected,
				});

				var pollResponse = await _authSession.PollingWaitForResultAsync();

				WindowManager.Instance.CloseWindow<SteamGuardWindow>();

				// Logon to Steam with the access token we have received
				// Note that we are using RefreshToken for logging on here
				Steam.Instance.steamUser.LogOn(new SteamUser.LogOnDetails
				{
					Username = pollResponse.AccountName,
					AccessToken = pollResponse.RefreshToken,
					ShouldRememberPassword = RememberPasswordCheckbox.selected,
					LoginID = 0x73743039,
				});

				Steam.Instance.CurrentUser = new User(0, pollResponse.AccountName, "", "", pollResponse.RefreshToken);

				WindowManager.Instance.CloseWindow(this);
			}
			catch (System.Exception e)
			{
				Console.WriteLine(e);
				isLoggingIn = false;
			}
		});
	}
}