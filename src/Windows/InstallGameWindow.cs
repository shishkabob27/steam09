using QRCoder;
using SDL_Sharp;

public class InstallGameWindow : SteamWindow
{
	Game game;

	ButtonControl backButton;
	ButtonControl installButton;
	ButtonControl cancelButton;

	int screen = 0; //0 - info, 1 - shortcuts

	string gameSize; //In MB
	int diskSpaceAvailable; //In MB

	public InstallGameWindow(Steam steam, string title, int width, int height, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0) : base(steam, title, width, height, resizable, minimumWidth, minimumHeight)
	{
		backButton = new ButtonControl(panel, renderer, "backbutton", 0, 0, 72, 24, "< Back", 1);
		installButton = new ButtonControl(panel, renderer, "installbutton", 0, 0, 72, 24, "Install", 1);
		cancelButton = new ButtonControl(panel, renderer, "cancelbutton", 0, 0, 72, 24, "Cancel", 1);
		panel.AddControl(backButton);
		panel.AddControl(installButton);
		panel.AddControl(cancelButton);

		backButton.enabled = false;

		backButton.OnClick += () =>
		{
			screen = 0;
			backButton.enabled = false;
		};

		installButton.OnClick += async () =>
		{
			if (screen == 0)
			{
				backButton.enabled = true;
				screen = 1;
			}
			else if (screen == 1)
			{
				steam.DownloadGame(game.AppID);
				steam.PendingWindowsToRemove.Add(this);
			}
		};

		cancelButton.OnClick += () =>
		{
			steam.PendingWindowsToRemove.Add(this);
		};

		GetDiskSpaceAvailable();
	}

	public void SetGame(Game game)
	{
		this.game = game;
		gameSize = (game.GetEstimatedSize() / 1024 / 1024).ToString("F0");
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		//Align buttons to the bottom right of the window
		cancelButton.x = mWidth - cancelButton.width - 10;
		cancelButton.y = mHeight - cancelButton.height - 10;

		installButton.x = cancelButton.x - installButton.width - 10;
		installButton.y = mHeight - installButton.height - 10;

		backButton.x = installButton.x - backButton.width - 10;
		backButton.y = mHeight - backButton.height - 10;
	}

	public override void Draw()
	{
		base.Draw();

		//draw background
		panel.DrawBox(10, 30, mWidth - 20, mHeight - 72, new Color(101, 106, 98, 255));

		installButton.Draw();
		installButton.text = screen == 0 ? "Next >" : "Install";

		cancelButton.Draw();

		backButton.Draw();

		if (screen == 0)
		{
			//draw info screen
			panel.DrawText($"You are about to install {game.Name}.", 30, 56, new Color(255, 255, 255, 255));

			panel.DrawText($"Disk space required:", 30, 120, new Color(255, 255, 255, 255));
			panel.DrawText($"Disk space available:", 30, 145, new Color(255, 255, 255, 255));

			panel.DrawText($"{gameSize} MB", (mWidth / 2) + 10, 120, new Color(255, 255, 255, 255));
			panel.DrawText($"{diskSpaceAvailable} MB", (mWidth / 2) + 10, 145, new Color(255, 255, 255, 255));
		}
		else if (screen == 1)
		{
			panel.DrawText($"TODO: Shortcuts screen", 30, 56, new Color(255, 255, 255, 255));
		}

		SDL.RenderPresent(renderer);
	}

	void GetDiskSpaceAvailable()
	{
		string currentDrive = Environment.CurrentDirectory.Substring(0, 3);

		//get drive info
		DriveInfo drive = new DriveInfo(currentDrive);
		diskSpaceAvailable = (int)(drive.TotalFreeSpace / 1024 / 1024);
	}
}