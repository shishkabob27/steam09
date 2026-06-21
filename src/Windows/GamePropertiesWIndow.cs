using System.Drawing;
using KGUI;
using KGUI.Controls;

public class GamePropertiesWindow : SteamWindow
{
	Game _game;

	ButtonControl closeButton;

	int tabIndex = 0;


	public GamePropertiesWindow(Steam steam, string uuid) : base(steam, uuid)
	{

		closeButton = panel.GetControlByID<ButtonControl>("CloseButton");
		closeButton.text = Localization.GetString("Steam_GameProperties_Close");


		// tabList = new TabList(panel, renderer, "tablist", 8, 54, 516, 320);
		// panel.AddControl(tabList);

		// tabList.Children.Add(new TabItem(panel, renderer, "tabitem_general", 0, 0, 98, 22, Localization.GetString("Steam_GameProperties_GeneralTab"), tabList));
		// tabList.Children.Add(new TabItem(panel, renderer, "tabitem_updates", 0, 0, 98, 22, Localization.GetString("Steam_GameProperties_UpdatesTab"), tabList));
		// tabList.Children.Add(new TabItem(panel, renderer, "tabitem_localfiles", 0, 0, 98, 22, Localization.GetString("Steam_GameProperties_LocalFilesTab"), tabList));
		// tabList.Children.Add(new TabItem(panel, renderer, "tabitem_language", 0, 0, 98, 22, Localization.GetString("Steam_GameProperties_LanguageTab"), tabList));

		// tabList.SetTabSelected("tabitem_general", true);

		// tabList.OnTabSelected = OnTabSelected;

		// foreach (var tabItem in tabList.Children)
		// {
		// 	panel.AddControl(tabItem);
		// }
	}

	public void SetGame(Game game)
	{
		this._game = game;
		SetTitle(Localization.GetString("Steam_GameProperties_Title").Replace("%game%", game.Name));
	}

	public void OnTabSelected(string tabName)
	{
		//tabIndex = tabList.Children.IndexOf(tabList.Children.Find(x => x.ControlName == tabName));
	}

	public override void Draw()
	{
		base.Draw();

		if (tabIndex == 0) // General
		{
			panel.DrawText(Localization.GetString("Steam_Game_Homepage"), 32, 79, Color.FromArgb(218, 222, 214));
			panel.DrawText(_game.Homepage, 156, 79, Color.FromArgb(255, 255, 255, 255), true, true);

			panel.DrawText(Localization.GetString("Steam_Game_Developer"), 32, 109, Color.FromArgb(218, 222, 214));
			panel.DrawText(_game.Developer, 156, 109, Color.FromArgb(255, 255, 255, 255), true, true);

			panel.DrawText(Localization.GetString("Steam_Game_Manual"), 32, 137, Color.FromArgb(218, 222, 214));
			panel.DrawText(_game.ManualUrl.Item1, 156, 137, Color.FromArgb(255, 255, 255, 255), true, true);
		}
	}

	void OnCloseButtonClicked()
	{
		WindowManager.Instance.CloseWindow(this);
	}
}