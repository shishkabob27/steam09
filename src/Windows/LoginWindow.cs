using System.Threading.Tasks;
using SDL_Sharp;
using SteamKit2;
using SteamKit2.Authentication;

public class LoginWindow : SteamWindow
{
	TextEntryControl UsernameEdit;
	TextEntryControl PasswordEdit;

	CheckButtonControl RememberPasswordButton;

	ButtonControl LoginButton;
	ButtonControl CancelButton;

	ButtonControl CreateNewAccountButton;
	ButtonControl LostPasswordButton;


	bool isLoggingIn = false;
	public AuthSession authSession;

	public LoginWindow(Steam steam, string title, int width, int height, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0) : base(steam, title, width, height, resizable, minimumWidth, minimumHeight)
	{
		panel.LoadUILayout("SteamLoginDialog.res");

		Steam.Instance.manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
		Steam.Instance.manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

		UsernameEdit = panel.GetControl<TextEntryControl>("UserNameEdit");
		PasswordEdit = panel.GetControl<TextEntryControl>("PasswordEdit");
		RememberPasswordButton = panel.GetControl<CheckButtonControl>("SavePasswordCheck");
		LoginButton = panel.GetControl<ButtonControl>("LoginButton");
		CancelButton = panel.GetControl<ButtonControl>("CancelButton");
		CreateNewAccountButton = panel.GetControl<ButtonControl>("CreateNewAccountButton");
		LostPasswordButton = panel.GetControl<ButtonControl>("LostPasswordButton");

		LoginButton.OnClick += AttemptSteamLogin;
		CancelButton.OnClick += () => Steam.Instance.PendingWindowsToRemove.Add(this);

		CreateNewAccountButton.OnClick += () => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://store.steampowered.com/join/") { UseShellExecute = true });
		LostPasswordButton.OnClick += () => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://help.steampowered.com/en/wizard/HelpWithLogin") { UseShellExecute = true });
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);


		UsernameEdit.enabled = !isLoggingIn;
		PasswordEdit.enabled = !isLoggingIn;
		LoginButton.enabled = !isLoggingIn && !string.IsNullOrWhiteSpace(UsernameEdit.text) && !string.IsNullOrWhiteSpace(PasswordEdit.text);
		RememberPasswordButton.enabled = !isLoggingIn;
	}

	public override void Draw()
	{
		base.Draw();

		SDL.RenderPresent(renderer);
	}

	void AttemptSteamLogin()
	{
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
				authSession = await Steam.Instance.steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails()
				{
					Username = UsernameEdit.text,
					Password = PasswordEdit.text,
					Authenticator = new SteamGuardAuthenticator(),
					IsPersistentSession = RememberPasswordButton.selected,
				});

				var pollResponse = await authSession.PollingWaitForResultAsync();

				//find steamguard window and des
				foreach (var window in Steam.Instance.Windows)
				{
					if (window is SteamGuardWindow)
					{
						Steam.Instance.PendingWindowsToRemove.Add(window);
					}
				}

				// Logon to Steam with the access token we have received
				// Note that we are using RefreshToken for logging on here
				Steam.Instance.steamUser.LogOn(new SteamUser.LogOnDetails
				{
					Username = pollResponse.AccountName,
					AccessToken = pollResponse.RefreshToken,
					ShouldRememberPassword = RememberPasswordButton.selected,
					LoginID = 0x73743039,
				});

				Steam.Instance.CurrentUser = new User(0, pollResponse.AccountName, "", "", pollResponse.RefreshToken);

				Steam.Instance.PendingWindowsToRemove.Add(this);
			}
			catch (System.Exception e)
			{
				Console.WriteLine(e);
				isLoggingIn = false;
			}
		});
	}
}