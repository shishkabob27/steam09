using Newtonsoft.Json;
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

	SolidBackgroundControl topBarBackground;
	SolidBackgroundControl bottomBarBackground;
	SolidBackgroundControl GameListBackground;

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


		//top bar background
		topBarBackground = new SolidBackgroundControl(panel, renderer, "topbarbackground", 0, 68, mWidth, 22, new Color(104, 106, 101, 255));
		topBarBackground.zIndex = 1;
		panel.AddControl(topBarBackground);

		//bottom bar background
		bottomBarBackground = new SolidBackgroundControl(panel, renderer, "bottombarbackground", 0, mHeight - 117, mWidth, 39, new Color(104, 106, 101, 255));
		bottomBarBackground.zIndex = 1;
		panel.AddControl(bottomBarBackground);

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
			//background
			GameListBackground = new SolidBackgroundControl(panel, renderer, "gamelistbackground", 1, 90, mWidth - 2, mHeight - 117 - 90, new Color(73, 78, 73, 255));
			GameListBackground.zIndex = -2;
			panel.AddControl(GameListBackground);

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

		TabList = new TabList(panel, renderer, "tablist", 1, 68);
		panel.AddControl(TabList);

		TabList.Children.Add(new TabItem(panel, renderer, "tabitem_store", 0, 0, 66, 22, Localization.GetString("Steam_Store_TabTitle"), TabList));
		//TabList.Children.Add(new TabItem(panel, renderer, "tabitem_community", 0, 0, 71, 22, "Community", TabList));
		TabList.Children.Add(new TabItem(panel, renderer, "tabitem_mygames", 0, 0, 66, 22, Localization.GetString("Steam_MyGames_TabTitle"), TabList));

		foreach (var tabItem in TabList.Children)
		{
			panel.AddControl(tabItem);
		}

		TabList.OnTabSelected = OnTabSelected;
		
		TabList.SetTabSelected("tabitem_mygames", true);

		CreateControls();

		LoadFavorites();
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

			//enable browser controls
			BackButton.visible = true;
			ForwardButton.visible = true;
			ReloadButton.visible = true;
			StopButton.visible = true;
			HomeButton.visible = true;

			ReloadButton.enabled = true;
			StopButton.enabled = true;
			HomeButton.enabled = true;

			topBarBackground.height = 43;

			gameList.enabled = false;
			foreach (var child in gameList.Children)
			{
				child.enabled = false;
			}
		}
		else
		{
			inBrowserWindow = false;

			//disable browser controls
			BackButton.visible = false;
			ForwardButton.visible = false;
			ReloadButton.visible = false;
			StopButton.visible = false;
			HomeButton.visible = false;

			BackButton.enabled = false;
			ForwardButton.enabled = false;
			ReloadButton.enabled = false;
			StopButton.enabled = false;
			HomeButton.enabled = false;

			gameList.enabled = true;
			foreach (var child in gameList.Children)
			{
				child.enabled = true;
			}

			topBarBackground.height = 22;
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

			BackButton.enabled = browser.CanGoBack();
			ForwardButton.enabled = browser.CanGoForward();
		}
		else
		{
			//resize game list
			gameList.width = mWidth - 2;
			gameList.height = mHeight - 117 - 90;

			GameListBackground.width = mWidth - 2;
			GameListBackground.height = mHeight - 117 - 90;

			//bars
			bottomBarBackground.y = mHeight - 117;
			bottomBarBackground.width = mWidth;
		}

		topBarBackground.width = mWidth;

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
		panel.DrawText(Localization.GetString("Steam_News"), 71, mHeight - 27, new Color(143, 146, 141, 255));
		panel.DrawText(Localization.GetString("Steam_Friends"), 179, mHeight - 27, new Color(143, 146, 141, 255));
		panel.DrawText(Localization.GetString("Steam_Servers"), 299, mHeight - 27, new Color(143, 146, 141, 255));
		panel.DrawText(Localization.GetString("Steam_Settings"), 417, mHeight - 27, new Color(143, 146, 141, 255));
		panel.DrawText(Localization.GetString("Steam_Support"), 538, mHeight - 27, new Color(143, 146, 141, 255));

		SDL.RenderPresent(renderer);
	}

	public void GameListDraw()
	{
		//game list table names
		{
			panel.DrawText(Localization.GetString("Steam_GamesColumn"), 53, 73, new Color(216, 222, 211, 255), fontSize: 7);
			panel.DrawText(Localization.GetString("Steam_StatusColumn"), (mWidth / 2) - 24, 73, new Color(216, 222, 211, 255), fontSize: 7);
			panel.DrawText(Localization.GetString("Steam_DeveloperColumn"), mWidth - 250, 73, new Color(216, 222, 211, 255), fontSize: 7);
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
						GameActionButton.text = Localization.GetString("Steam_Launch");
						break;
					case GameStatus.UpdatePending:
						GameActionButton.text = Localization.GetString("Steam_UpdateColumn");
						break;
					case GameStatus.NotInstalled:
						GameActionButton.text = Localization.GetString("Steam_Install");
						break;
					default:
						GameActionButton.text = Localization.GetString("Steam_Launch");
						break;
				}
			}
		}
	}

	public void BrowserDraw()
	{
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
		CreateGameCategory(Localization.GetString("Steam_GamesSection_Favorites"), 0, catagoryOpenState.ContainsKey(0) ? catagoryOpenState[0] : true);
		foreach (var game in steam.Games.Where(g => g.IsFavorite).OrderBy(g => g.Name))
		{
			CreateGameItemControl(game);
		}

		// create categories first
		CreateGameCategory(Localization.GetString("Steam_GamesSection_Installed"), 1, catagoryOpenState.ContainsKey(1) ? catagoryOpenState[1] : true);
		foreach (var game in steam.Games.Where(g => (g.Status == GameStatus.Installed || g.Status == GameStatus.UpdatePending) && !g.IsFavorite).OrderBy(g => g.Name))
		{
			CreateGameItemControl(game);
		}

		// create not installed category
		CreateGameCategory(Localization.GetString("Steam_GamesSection_NotInstalled"), 2, catagoryOpenState.ContainsKey(2) ? catagoryOpenState[2] : true);
		foreach (var game in steam.Games.Where(g => g.Status == GameStatus.NotInstalled && !g.IsFavorite).OrderBy(g => g.Name))
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
		GameActionButton = new ButtonControl(panel, renderer, "gameactionbutton", 0, 0, 98, 24, Localization.GetString("Steam_Launch"));
		GameActionButton.zIndex = 3;
		panel.AddControl(GameActionButton);
		GameActionButton.OnClick = () => OnGameAction(selectedGameID);

		PropertiesButton = new ButtonControl(panel, renderer, "propertiesbutton", 0, 0, 98, 24, Localization.GetString("Steam_Properties"));
		PropertiesButton.zIndex = 3;
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

			//check if mouse is over the favorite icon
			Rect favoriteIconRect = gameItemControl.GetFavoriteIconRect();
			if (favoriteIconRect.X <= panel.MouseX && favoriteIconRect.X + favoriteIconRect.Width >= panel.MouseX && favoriteIconRect.Y <= panel.MouseY && favoriteIconRect.Y + favoriteIconRect.Height >= panel.MouseY)
			{
				game.IsFavorite = !game.IsFavorite;
				SaveFavorites();
				ReloadGameList = true;
				return;
			}

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
			if (inBrowserWindow) return;

			//check if mouse cursor is withen bounds of the list
			if (panel.MouseY < gameList.y || panel.MouseY > gameList.y + gameList.height) return;

			gameItemControl.OnClick();

			PopupMenuWindow popupMenuWindow = new PopupMenuWindow(steam, $"Game Actions - {game.Name}", 120, 0);
			popupMenuWindow.AddItem(game.Status == GameStatus.Installed ? Localization.GetString("SteamUI_GamesDialog_RightClick_LaunchGames") : Localization.GetString("SteamUI_GamesDialog_RightClick_InstallGame"), () =>
			{
				OnGameAction(game.AppID);
			});
			popupMenuWindow.AddSeparator();
			popupMenuWindow.AddItem(Localization.GetString("Steam_Properties"), () =>
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
			InstallGameWindow installGameWindow = new InstallGameWindow(steam, $"{Localization.GetString("Steam_InstallAppWizard_Title").Replace("%game%", game.Name)}", 450, 460);
			installGameWindow.SetGame(game);
			steam.PendingWindows.Add(installGameWindow);
		}
		else if (game.Status == GameStatus.Installed)
		{
			steam.StartGame(game);
		}
	}

	void SaveFavorites()
	{
		List<int> favoriteAppIds = new List<int>();
		foreach (var game in steam.Games)
		{
			if (game.IsFavorite)
			{
				favoriteAppIds.Add(game.AppID);
			}
		}

		File.WriteAllText($"userdata/{steam.CurrentUser.SteamID}/config/favorites.json", JsonConvert.SerializeObject(favoriteAppIds));
	}

	void LoadFavorites()
	{
		if (File.Exists($"userdata/{steam.CurrentUser.SteamID}/config/favorites.json"))
		{
			string favoritesJson = File.ReadAllText($"userdata/{steam.CurrentUser.SteamID}/config/favorites.json");
			List<int> favoriteAppIds = JsonConvert.DeserializeObject<List<int>>(favoritesJson);
			foreach (var appId in favoriteAppIds)
			{
				Game game = steam.Games.Find(x => x.AppID == appId);
				if (game != null)
				{
					game.IsFavorite = true;
				}
			}
		}
	}
}