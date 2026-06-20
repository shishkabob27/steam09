using System.Drawing;
using KGUI;

public class InstallGameWindow : SteamWindow
{
	Game game;

	ButtonControl backButton;
	ButtonControl nextButton;
	ButtonControl cancelButton;

	int screen = 0; //0 - info, 1 - shortcuts

	string gameSize; //In MB
	int diskSpaceAvailable; //In MB

	public InstallGameWindow(Steam steam, string uuid) : base(steam, uuid)
	{
		backButton = panel.GetControlByID<ButtonControl>("BackButton");
		nextButton = panel.GetControlByID<ButtonControl>("NextButton");
		cancelButton = panel.GetControlByID<ButtonControl>("CancelButton");

		backButton.text = Localization.GetString("WizardPanel_Back");
		cancelButton.text = Localization.GetString("vgui_Cancel");

		backButton.enabled = false;

		backButton.OnClick += (backButton) =>
		{
			screen = 0;
			backButton.enabled = false;
		};

		nextButton.OnClick += async (nextButton) =>
		{
			if (screen == 0)
			{
				backButton.enabled = true;
				screen = 1;
			}
			else if (screen == 1)
			{
				client.DownloadManager.DownloadGame(game.AppID);
				WindowManager.Instance.CloseWindow(this);
			}
		};

		cancelButton.OnClick += (cancelButton) =>
		{
			WindowManager.Instance.CloseWindow(this);
		};

		GetDiskSpaceAvailable();
	}

	public void SetGame(Game game)
	{
		this.game = game;
		gameSize = (game.EstimatedSize / 1024 / 1024).ToString("F0");
	}

	public override void Draw()
	{
		base.Draw();

		nextButton.text = screen == 0 ? Localization.GetString("WizardPanel_Next") : Localization.GetString("Steam_Install");

		if (screen == 0)
		{
			//draw info screen
			panel.DrawText(Localization.GetString("Steam_InstallGameInfo").Replace("%game%", game.Name), 30, 56, Color.FromArgb(255, 255, 255, 255));

			panel.DrawText(Localization.GetString("Steam_ScanCDKey_SpaceRequired"), 30, 120, Color.FromArgb(255, 255, 255, 255));
			panel.DrawText(Localization.GetString("Steam_ScanCDKey_SpaceAvailable"), 30, 145, Color.FromArgb(255, 255, 255, 255));

			panel.DrawText($"{gameSize} MB", (mWidth / 2) + 10, 120, Color.FromArgb(255, 255, 255, 255));
			panel.DrawText($"{diskSpaceAvailable} MB", (mWidth / 2) + 10, 145, Color.FromArgb(255, 255, 255, 255));
		}
		else if (screen == 1)
		{
			panel.DrawText($"TODO: Shortcuts screen", 30, 56, Color.FromArgb(255, 255, 255, 255));
		}
	}

	void GetDiskSpaceAvailable()
	{
		string currentDrive = Environment.CurrentDirectory.Substring(0, 3);

		//get drive info
		DriveInfo drive = new DriveInfo(currentDrive);
		diskSpaceAvailable = (int)(drive.TotalFreeSpace / 1024 / 1024);
	}
}