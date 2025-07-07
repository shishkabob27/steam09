using SDL_Sharp;
using SDL_Sharp.Ttf;

public class MainWindow : SteamWindow
{
	Texture ResizeTexture;

	Texture BottomButtonsTexture;
	RootBottomButtonControl NewsButton;
	RootBottomButtonControl FriendsButton;
	RootBottomButtonControl ServersButton;
	RootBottomButtonControl SettingsButton;
	RootBottomButtonControl SupportButton;

	TabList TabList;
	int tabIndex = 0;

	bool inBrowserWindow = false;
	bool browserInitialized = false;
	Browser browser;
	Texture browserControlTexture;
	BrowserButtonControl BackButton;
	BrowserButtonControl ForwardButton;
	BrowserButtonControl ReloadButton;
	BrowserButtonControl StopButton;
	BrowserButtonControl HomeButton;

	ButtonControl GameActionButton; // Install, Launch
	ButtonControl PropertiesButton; // game properties

	int selectedGameID = 0;

	ListControl gameList;

	public bool ReloadGameList = true; // reload on startup
	Dictionary<int, bool> catagoryOpenState; // Catagory index, open state

	public MainWindow(Steam steam, string title, int width, int height, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0) : base(steam, title, width, height, resizable, minimumWidth, minimumHeight)
	{
		unsafe
		{
			Surface* resizeSurface = SDL_Sharp.Image.IMG.Load("resources/graphics/resizer.png");
			ResizeTexture = SDL.CreateTextureFromSurface(renderer, resizeSurface);
			SDL.FreeSurface(resizeSurface);
		}

		unsafe
		{
			Surface* bottomButtonsSurface = SDL_Sharp.Image.IMG.Load("resources/graphics/bottom_buttons.png");
			BottomButtonsTexture = SDL.CreateTextureFromSurface(renderer, bottomButtonsSurface);
			SDL.FreeSurface(bottomButtonsSurface);
		}

		unsafe
		{
			Surface* browserControlSurface = SDL_Sharp.Image.IMG.Load("resources/graphics/browser_controls.png");
			browserControlTexture = SDL.CreateTextureFromSurface(renderer, browserControlSurface);
			SDL.FreeSurface(browserControlSurface);
		}

		NewsButton = new RootBottomButtonControl(panel, renderer, "newsbutton", 32, mHeight - 68, 100, 52, index: 0);
		FriendsButton = new RootBottomButtonControl(panel, renderer, "friendsbutton", 145, mHeight - 68, 100, 52, index: 1);
		ServersButton = new RootBottomButtonControl(panel, renderer, "serversbutton", 266, mHeight - 68, 100, 52, index: 2);
		SettingsButton = new RootBottomButtonControl(panel, renderer, "settingsbutton", 385, mHeight - 68, 100, 52, index: 3);
		SupportButton = new RootBottomButtonControl(panel, renderer, "supportbutton", 505, mHeight - 68, 100, 52, index: 4);

		NewsButton.texture = BottomButtonsTexture;
		FriendsButton.texture = BottomButtonsTexture;
		ServersButton.texture = BottomButtonsTexture;
		SettingsButton.texture = BottomButtonsTexture;
		SupportButton.texture = BottomButtonsTexture;

		panel.AddControl(NewsButton);
		panel.AddControl(FriendsButton);
		panel.AddControl(ServersButton);
		panel.AddControl(SettingsButton);
		panel.AddControl(SupportButton);

		TabList = new TabList(panel, renderer, "tablist", 1, 68);
		panel.AddControl(TabList);

		TabList.Children.Add(new TabItem(panel, renderer, "tabitem_store", 0, 0, 66, 22, "Store", TabList));
		//TabList.Children.Add(new TabItem(panel, renderer, "tabitem_community", 0, 0, 71, 22, "Community", TabList));
		TabList.Children.Add(new TabItem(panel, renderer, "tabitem_mygames", 0, 0, 66, 22, "My games", TabList));

		TabList.SetTabSelected("tabitem_mygames", true);

		TabList.OnTabSelected = OnTabSelected;

		foreach (var tabItem in TabList.Children)
		{
			panel.AddControl(tabItem);
		}

		//browser controls
		{
			BackButton = new BrowserButtonControl(panel, renderer, "backbutton", 13, 82, 16, 16, 0);
			ForwardButton = new BrowserButtonControl(panel, renderer, "forwardbutton", 43, 82, 16, 16, 1);
			ReloadButton = new BrowserButtonControl(panel, renderer, "reloadbutton", 73, 82, 16, 16, 2);
			StopButton = new BrowserButtonControl(panel, renderer, "stopbutton", 103, 82, 16, 16, 3);
			HomeButton = new BrowserButtonControl(panel, renderer, "homebutton", 133, 82, 16, 16, 4);

			BackButton.texture = browserControlTexture;
			ForwardButton.texture = browserControlTexture;
			ReloadButton.texture = browserControlTexture;
			StopButton.texture = browserControlTexture;
			HomeButton.texture = browserControlTexture;

			panel.AddControl(BackButton);
			panel.AddControl(ForwardButton);
			panel.AddControl(ReloadButton);
			panel.AddControl(StopButton);
			panel.AddControl(HomeButton);

			BackButton.OnClick = () =>
			{
				browser?.Back();
			};
			ForwardButton.OnClick = () =>
			{
				browser?.Forward();
			};
			ReloadButton.OnClick = () =>
			{
				browser?.Reload();
			};
			StopButton.OnClick = () =>
			{
				browser?.Stop();
			};
			HomeButton.OnClick = () =>
			{
				browser?.LoadURL("https://store.steampowered.com/");
			};
		}

		//game list
		{
			gameList = new ListControl(panel, renderer, "gamelist", 1, 90, mWidth - 2, mHeight - 117 - 90);
			panel.AddControl(gameList);
		}

		FriendsButton.OnClick = () =>
		{
			//if the friends window is already open, just focus it
			if (steam.PendingWindows.Any(x => x is FriendsWindow) || steam.Windows.Any(x => x is FriendsWindow))
			{
				steam.PendingWindows.Find(x => x is FriendsWindow)?.FocusWindow();
				steam.Windows.Find(x => x is FriendsWindow)?.FocusWindow();
				return;
			}

			FriendsWindow friendsWindow = new FriendsWindow(steam, $"Friends - {steam.CurrentUser.AccountName}", 300, 500, true);
			steam.PendingWindows.Add(friendsWindow);
		};

		CreateControls();
	}

	void OnTabSelected(string tabName)
	{
		tabIndex = TabList.Children.FindIndex(x => x.ControlName == tabName);

		if (tabName == "tabitem_store" || tabName == "tabitem_community")
		{
			inBrowserWindow = true;

			if (!browserInitialized)
			{
				InitializeBrowser();
				browser.LoadURL("https://store.steampowered.com/");
			}

		}
		else
		{
			inBrowserWindow = false;
		}
	}

	~MainWindow()
	{
		SDL.DestroyTexture(ResizeTexture);
		SDL.DestroyTexture(BottomButtonsTexture);
		SDL.DestroyTexture(NewsButton.texture);
		SDL.DestroyTexture(FriendsButton.texture);
		SDL.DestroyTexture(ServersButton.texture);
		SDL.DestroyTexture(SettingsButton.texture);
		SDL.DestroyTexture(SupportButton.texture);
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		if (ReloadGameList)
		{
			LoadGameList();
			ReloadGameList = false;
		}


		if (inBrowserWindow)
		{
			browser.Resize(mWidth - 2, mHeight - 111 - 79);

			browser.OnMouseMove(panel.MouseX - 1, panel.MouseY - 111);
			browser.Update();

			//enable browser controls
			ReloadButton.enabled = true;
			StopButton.enabled = true;
			HomeButton.enabled = true;

			BackButton.enabled = browser.CanGoBack();
			ForwardButton.enabled = browser.CanGoForward();
		}
		else
		{
			//disable browser controls
			BackButton.enabled = false;
			ForwardButton.enabled = false;
			ReloadButton.enabled = false;
			StopButton.enabled = false;
			HomeButton.enabled = false;

			//resize game list
			gameList.width = mWidth - 2;
			gameList.height = mHeight - 117 - 90;
		}

		//move bottom buttons to bottom
		NewsButton.y = mHeight - 68;
		FriendsButton.y = mHeight - 68;
		ServersButton.y = mHeight - 68;
		SettingsButton.y = mHeight - 68;
		SupportButton.y = mHeight - 68;
	}

	public override void Draw()
	{
		base.Draw();

		TabList.Draw();

		if (!inBrowserWindow)
		{
			GameListDraw();
		}
		else
		{
			BrowserDraw();
		}

		//draw resize button
		panel.DrawTexture(ResizeTexture, mWidth - 23, mHeight - 23);

		//Draw bottom buttons
		NewsButton.Draw();
		panel.DrawText("News", 71, mHeight - 27, new Color(143, 146, 141, 255));

		FriendsButton.Draw();
		panel.DrawText("Friends", 179, mHeight - 27, new Color(143, 146, 141, 255));

		ServersButton.Draw();
		panel.DrawText("Servers", 299, mHeight - 27, new Color(143, 146, 141, 255));

		SettingsButton.Draw();
		panel.DrawText("Settings", 417, mHeight - 27, new Color(143, 146, 141, 255));

		SupportButton.Draw();
		panel.DrawText("Support", 538, mHeight - 27, new Color(143, 146, 141, 255));

		SDL.RenderPresent(renderer);
	}

	public void GameListDraw()
	{

		//draw catagory list
		//background
		panel.DrawBox(1, 90, mWidth - 2, mHeight - 117 - 90, new Color(73, 78, 73, 255));
		gameList.Draw();

		//bars
		{
			panel.DrawBox(0, 68, mWidth, 22, new Color(104, 106, 101, 255));
			panel.DrawBox(0, mHeight - 117, mWidth, 39, new Color(104, 106, 101, 255));
		}

		//game list table names
		{
			panel.DrawText("Games", 53, 73, new Color(216, 222, 211, 255), fontSize: 7);
			panel.DrawText("Status", (mWidth / 2) - 24, 73, new Color(216, 222, 211, 255), fontSize: 7);
			panel.DrawText("Developer", mWidth - 250, 73, new Color(216, 222, 211, 255), fontSize: 7);
		}

		//fit buttons to width of window
		GameActionButton.x = mWidth - 110;
		PropertiesButton.x = mWidth - 216;

		GameActionButton.y = mHeight - 110;
		PropertiesButton.y = mHeight - 110;

		{
			Game game = steam.Games.Find(x => x.AppID == selectedGameID);

			if (game == null)
			{
				GameActionButton.enabled = false;
				PropertiesButton.enabled = false;
			}
			else
			{
				GameActionButton.enabled = true;
				PropertiesButton.enabled = true;
				switch (game.Status)
				{
					case GameStatus.Installed:
						GameActionButton.text = "Launch";
						break;
					case GameStatus.UpdatePending:
						GameActionButton.text = "Update";
						break;
					case GameStatus.NotInstalled:
						GameActionButton.text = "Install";
						break;
					default:
						GameActionButton.text = "Launch";
						break;
				}
			}

			GameActionButton.Draw();
			PropertiesButton.Draw();
		}
	}

	public void BrowserDraw()
	{
		//top bar
		panel.DrawBox(0, 68, mWidth, 43, new Color(104, 106, 101, 255));

		BackButton.Draw();
		ForwardButton.Draw();
		ReloadButton.Draw();
		StopButton.Draw();
		HomeButton.Draw();

		Rect browserRect = new(1, 111, mWidth - 2, mHeight - 111 - 79);
		browser.Draw(renderer, browserRect);
	}

	public void InitializeBrowser()
	{
		browser = new Browser();
		browser.Initialize();
		browserInitialized = true;
	}

	public override void OnMouseScroll(int scrollX, int scrollY)
	{
		if (!inBrowserWindow) return;

		if (panel.MouseY > 111 && panel.MouseY < mHeight - 79)
		{
			browser.OnMouseScroll(scrollX, scrollY);
		}
	}

	public override void OnMouseDown(int x, int y, int button)
	{
		if (inBrowserWindow && panel.MouseY > 111 && panel.MouseY < mHeight - 79)
		{
			browser.OnMouseDown(button);
		}
	}

	public override void OnMouseUp(int x, int y, int button)
	{
		if (inBrowserWindow && panel.MouseY > 111 && panel.MouseY < mHeight - 79)
		{
			browser.OnMouseUp(button);
		}
	}

	public override void OnKeyDown(Keycode key, KeyModifier mod)
	{
		if (inBrowserWindow)
		{
			browser.OnKeyDown(key, mod);
		}
	}

	public override void OnKeyUp(Keycode key, KeyModifier mod)
	{
		if (inBrowserWindow)
		{
			browser.OnKeyUp(key, mod);
		}
	}

	void CreateGameCategory(string categoryName, int index, bool open = true)
	{
		GameCategoryToggleControl gameCategoryToggleControl = new GameCategoryToggleControl(panel, renderer, $"gamecategorytoggle_{categoryName}", categoryName, gameList, index, open);
		gameList.Children.Add(gameCategoryToggleControl);
		panel.AddControl(gameCategoryToggleControl);
	}

	void LoadGameList()
	{
		//keep track of catagory open state
		catagoryOpenState = new Dictionary<int, bool>();
		foreach (var gameCategoryToggleControl in gameList.Children.OfType<GameCategoryToggleControl>())
		{
			catagoryOpenState[gameCategoryToggleControl.categoryIndex] = gameCategoryToggleControl.Open;
		}

		gameList.Clear(false);

		//create favorites category
		CreateGameCategory("MY FAVORITES", 0, catagoryOpenState.ContainsKey(0) ? catagoryOpenState[0] : true);
		foreach (var game in steam.Games.Where(g => g.IsFavorite))
		{
			CreateGameItemControl(game);
		}

		// create categories first
		CreateGameCategory("INSTALLED", 1, catagoryOpenState.ContainsKey(1) ? catagoryOpenState[1] : true);
		foreach (var game in steam.Games.Where(g => g.Status == GameStatus.Installed || g.Status == GameStatus.UpdatePending))
		{
			CreateGameItemControl(game);
		}

		// create not installed category
		CreateGameCategory("NOT INSTALLED", 2, catagoryOpenState.ContainsKey(2) ? catagoryOpenState[2] : true);
		foreach (var game in steam.Games.Where(g => g.Status == GameStatus.NotInstalled))
		{
			CreateGameItemControl(game);
		}

		//if there was a game selected, select it again
		if (selectedGameID != 0)
		{
			GameItemControl gameItemControl = gameList.Children
				.OfType<GameItemControl>()
				.FirstOrDefault(x => x.game != null && x.game.AppID == selectedGameID);

			if (gameItemControl != null)
			{
				gameItemControl.highlighted = true;
			}
		}

		//if a catagory has no games, hide it
		foreach (var child in gameList.Children)
		{
			if (child is GameCategoryToggleControl gameCategoryToggleControl)
			{
				gameCategoryToggleControl.visible = gameCategoryToggleControl.GetGamesBelongingToCategory().Count > 0;
			}
		}

		//update game visibility
		foreach (var child in gameList.Children)
		{
			if (child is GameCategoryToggleControl gameCategoryToggleControl)
			{
				gameCategoryToggleControl.UpdateGameVisibility();
			}
		}
	}

	void CreateControls()
	{
		//these button's positions are relative to the window
		GameActionButton = new ButtonControl(panel, renderer, "gameactionbutton", 0, 0, 98, 24, "Launch");
		panel.AddControl(GameActionButton);
		GameActionButton.OnClick = () => OnGameAction(selectedGameID);

		PropertiesButton = new ButtonControl(panel, renderer, "propertiesbutton", 0, 0, 98, 24, "Properties");
		panel.AddControl(PropertiesButton);
		PropertiesButton.OnClick = () =>
		{
			Game game = steam.Games.Find(x => x.AppID == selectedGameID);
			if (game == null) return;

			GamePropertiesWindow gamePropertiesWindow = new GamePropertiesWindow(steam, "", 516, 400, game);
			steam.PendingWindows.Add(gamePropertiesWindow);
		};
	}

	void CreateGameItemControl(Game game)
	{
		GameItemControl gameItemControl = new GameItemControl(panel, renderer, $"gamecontrol_{game.AppID}", 0, 0, game, height: 20);
		gameList.Children.Add(gameItemControl);
		panel.AddControl(gameItemControl);
		gameItemControl.OnClick = () =>
		{
			if (inBrowserWindow) return;

			//dehighlight all other controls
			foreach (var child in gameList.Children)
			{
				if (child is GameItemControl otherGameItemControl)
				{
					otherGameItemControl.highlighted = false;
				}
			}

			gameItemControl.highlighted = true;
			selectedGameID = game.AppID;
		};
		gameItemControl.OnDoubleClick = () => OnGameAction(game.AppID);
		gameItemControl.OnRightClick = () =>
		{
			//check if mouse cursor is withen bounds of the list
			if (panel.MouseY < gameList.y || panel.MouseY > gameList.y + gameList.height) return;

			gameItemControl.OnClick();

			PopupMenuWindow popupMenuWindow = new PopupMenuWindow(steam, $"Game Actions - {game.Name}", 120, 0);
			popupMenuWindow.AddItem(game.Status == GameStatus.Installed ? "Launch game..." : "Install game...", () =>
			{
				OnGameAction(game.AppID);
			});
			popupMenuWindow.AddSeparator();
			popupMenuWindow.AddItem("Properties", () =>
			{
				Game game = steam.Games.Find(x => x.AppID == gameItemControl.game.AppID);
				if (game == null) return;

				GamePropertiesWindow gamePropertiesWindow = new GamePropertiesWindow(steam, "", 516, 400, game);
				steam.PendingWindows.Add(gamePropertiesWindow);
			});
			steam.PendingWindows.Add(popupMenuWindow);
		};
	}

	//called when the game is double clicked in the game list or the game action button is clicked while a game is selected
	async void OnGameAction(int gameID)
	{
		if (inBrowserWindow) return;

		Game game = steam.Games.Find(x => x.AppID == gameID);
		if (game == null) return;

		if (game.Status == GameStatus.NotInstalled || game.Status == GameStatus.UpdatePending)
		{
			InstallGameWindow installGameWindow = new InstallGameWindow(steam, $"Install {game.Name}", 450, 460);
			installGameWindow.SetGame(game);
			steam.PendingWindows.Add(installGameWindow);
		}
		else if (game.Status == GameStatus.Installed)
		{
			steam.StartGame(game);
		}
	}
}