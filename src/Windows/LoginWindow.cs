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

	DividerControl divider;

	ButtonControl CreateNewAccountButton;
	ButtonControl LostPasswordButton;


	bool isLoggingIn = false;
	public AuthSession authSession;

	public LoginWindow(Steam steam, string title, int width, int height, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0) : base(steam, title, width, height, resizable, minimumWidth, minimumHeight)
	{
		UsernameEdit = new(panel, renderer, "UsernameEdit", 121, 68, 238, 24);
		PasswordEdit = new(panel, renderer, "PasswordEdit", 121, 102, 238, 24)
		{
			isPassword = true
		};

		RememberPasswordButton = new(panel, renderer, "RememberPasswordButton", 118, 132, text: "Remember my password");

		LoginButton = new(panel, renderer, "LoginButton", 120, 164, 80, 24, "Login", 1);
		CancelButton = new(panel, renderer, "CancelButton", 205, 164, 84, 24, "Cancel", 1);

		divider = new(panel, renderer, "Divider", 20, 202, 380, 3);

		CreateNewAccountButton = new(panel, renderer, "CreateNewAccountButton", 228, 220, 174, 24, "Create a new account...", 1);
		LostPasswordButton = new(panel, renderer, "LostPasswordButton", 228, 252, 174, 24, "Retrieve a lost account...", 1);

		CreateNewAccountButton.OnClick += () =>
		{
			System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://store.steampowered.com/join/") { UseShellExecute = true });
		};

		LostPasswordButton.OnClick += () =>
		{
			System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://help.steampowered.com/en/wizard/HelpWithLogin") { UseShellExecute = true });
		};

		LoginButton.OnClick += () =>
		{
			AttemptSteamLogin();
		};

		panel.AddControl(UsernameEdit);
		panel.AddControl(PasswordEdit);
		panel.AddControl(RememberPasswordButton);
		panel.AddControl(LoginButton);
		panel.AddControl(CancelButton);
		panel.AddControl(CreateNewAccountButton);
		panel.AddControl(LostPasswordButton);

		Steam.Instance.manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
		Steam.Instance.manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
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

		UsernameEdit.Draw();
		PasswordEdit.Draw();
		RememberPasswordButton.Draw();
		LoginButton.Draw();
		CancelButton.Draw();
		divider.Draw();
		CreateNewAccountButton.Draw();
		LostPasswordButton.Draw();

		panel.DrawText("Account name", 112, 74, new Color(255, 255, 255, 255), false, false, 8, FontAlignment.Right);
		panel.DrawText("Password", 112, 108, new Color(255, 255, 255, 255), false, false, 8, FontAlignment.Right);

		panel.DrawText("Don't have a Steam account?", 220, 228, new Color(255, 255, 255, 255), false, false, 8, FontAlignment.Right);
		panel.DrawText("Forgot your login info?", 220, 260, new Color(255, 255, 255, 255), false, false, 8, FontAlignment.Right);

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