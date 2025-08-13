using SDL_Sharp;

public class GamePropertiesWindow : SteamWindow
{
	Game game;

	ButtonControl closeButton;

	TabList tabList;

	int tabIndex = 0;


	public GamePropertiesWindow(Steam steam, string title, int width, int height, Game game, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0) : base(steam, title, width, height, resizable, minimumWidth, minimumHeight)
	{
		this.game = game;
		SetWindowTitle(Localization.GetString("Steam_GameProperties_Title").Replace("%game%", game.Name));

		closeButton = new ButtonControl(panel, renderer, "closebutton", 0, 0, 72, 24, Localization.GetString("Steam_GameProperties_Close"), 1);
		panel.AddControl(closeButton);
		closeButton.OnClick = () =>
		{
			steam.PendingWindowsToRemove.Add(this);
		};

		tabList = new TabList(panel, renderer, "tablist", 8, 54, 516, 320);
		panel.AddControl(tabList);

		tabList.Children.Add(new TabItem(panel, renderer, "tabitem_general", 0, 0, 98, 22, Localization.GetString("Steam_GameProperties_GeneralTab"), tabList));
		tabList.Children.Add(new TabItem(panel, renderer, "tabitem_updates", 0, 0, 98, 22, Localization.GetString("Steam_GameProperties_UpdatesTab"), tabList));
		tabList.Children.Add(new TabItem(panel, renderer, "tabitem_localfiles", 0, 0, 98, 22, Localization.GetString("Steam_GameProperties_LocalFilesTab"), tabList));
		tabList.Children.Add(new TabItem(panel, renderer, "tabitem_language", 0, 0, 98, 22, Localization.GetString("Steam_GameProperties_LanguageTab"), tabList));

		tabList.SetTabSelected("tabitem_general", true);

		tabList.OnTabSelected = OnTabSelected;

		foreach (var tabItem in tabList.Children)
		{
			panel.AddControl(tabItem);
		}
	}

	public void OnTabSelected(string tabName)
	{
		tabIndex = tabList.Children.IndexOf(tabList.Children.Find(x => x.ControlName == tabName));
	}


	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		//align close button to the bottom right of the window
		closeButton.x = mWidth - closeButton.width - 12;
		closeButton.y = mHeight - closeButton.height - 12;
	}

	public override void Draw()
	{
		base.Draw();

		panel.DrawBox(8, 54, mWidth - 16, mHeight - 102, new Color(103, 106, 101, 255));

		closeButton.Draw();

		tabList.Draw();

		if (tabIndex == 0) // General
		{
			panel.DrawText(Localization.GetString("Steam_Game_Homepage"), 32, 79, new Color(218, 222, 214, 255));
			panel.DrawText(game.GetHomepage(), 156, 79, new Color(255, 255, 255, 255), true, true);

			panel.DrawText(Localization.GetString("Steam_Game_Developer"), 32, 109, new Color(218, 222, 214, 255));
			panel.DrawText(game.GetDeveloper(), 156, 109, new Color(255, 255, 255, 255), true, true);

			panel.DrawText(Localization.GetString("Steam_Game_Manual"), 32, 137, new Color(218, 222, 214, 255));
			panel.DrawText(game.GetManual().Item1, 156, 137, new Color(255, 255, 255, 255), true, true);
		}

		SDL.RenderPresent(renderer);
	}
}