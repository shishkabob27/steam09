using System.Drawing;
using KGUI;
using KGUI.Controls;
using Newtonsoft.Json;
using SDL;

public class MainWindow : SteamWindow
{
	unsafe SDL_Texture* ResizeTexture;

	bool inBrowserWindow = false;
	bool browserInitialized = false;
	BrowserControl browserControl;
	//Texture browserControlTexture;
	// BrowserButtonControl BackButton;
	// BrowserButtonControl ForwardButton;
	// BrowserButtonControl ReloadButton;
	// BrowserButtonControl StopButton;
	// BrowserButtonControl HomeButton;

	SolidBackgroundControl topBar;

	ButtonControl GameActionButton; // Install, Launch
	ButtonControl PropertiesButton; // game properties

	int selectedGameID = -1;
	int selectedToolID = -1;

	ListViewControl gameList;
	ListViewControl toolsList;

	string currentlistView = "game";

	public bool initializedGameList = true; // reload on startup
	public bool initializedToolList = true;

	Queue<Game> gameUpdates = new Queue<Game>();

	public MainWindow(Steam steam, string uuid) : base(steam, uuid)
	{
		SetTitle(Localization.GetString("Steam_Root_Title").Replace("%account%", steam.CurrentUser.AccountName));

		unsafe
		{
			ResizeTexture = panel.RootControl.LoadTexture(Assets.GetAssetPath("graphics/resizer.png"));
		}

	// 	unsafe
	// 	{
	// 		Surface* browserControlSurface = SDL_Sharp.Image.IMG.Load("resources/graphics/browser_controls.png");
	// 		browserControlTexture = SDL.CreateTextureFromSurface(renderer, browserControlSurface);
	// 		SDL.FreeSurface(browserControlSurface);
	// 	}

		//browser controls

		topBar = panel.GetControlByID<SolidBackgroundControl>("topBar");
		browserControl = panel.GetControlByID<BrowserControl>("browserControl");

	// 	{
	// 		BackButton = new BrowserButtonControl(panel, renderer, "backbutton", 13, 82, 16, 16, 0);
	// 		ForwardButton = new BrowserButtonControl(panel, renderer, "forwardbutton", 43, 82, 16, 16, 1);
	// 		ReloadButton = new BrowserButtonControl(panel, renderer, "reloadbutton", 73, 82, 16, 16, 2);
	// 		StopButton = new BrowserButtonControl(panel, renderer, "stopbutton", 103, 82, 16, 16, 3);
	// 		HomeButton = new BrowserButtonControl(panel, renderer, "homebutton", 133, 82, 16, 16, 4);

	// 		BackButton.OnClick = () =>
	// 		{
	// 			browser?.Back();
	// 		};
	// 		ForwardButton.OnClick = () =>
	// 		{
	// 			browser?.Forward();
	// 		};
	// 		ReloadButton.OnClick = () =>
	// 		{
	// 			browser?.Reload();
	// 		};
	// 		StopButton.OnClick = () =>
	// 		{
	// 			browser?.Stop();
	// 		};
	// 		HomeButton.OnClick = () =>
	// 		{
	// 			browser?.LoadURL("https://store.steampowered.com/");
	// 		};
	// 	}
	
		gameList = panel.GetControlByID<ListViewControl>("LibraryList");
		toolsList = panel.GetControlByID<ListViewControl>("ToolsList");
		toolsList.visible = false;
		toolsList.enabled = false;

		GameActionButton = panel.GetControlByID<ButtonControl>("GameActionButton");
		PropertiesButton = panel.GetControlByID<ButtonControl>("PropertiesButton");

	 	CreateControls();
	 	LoadFavorites();

		OnMyGamesTabSelected();
	}

	void OnStoreTabSelected()
	{
		inBrowserWindow = true;
		browserControl.visible = true;
		browserControl.enabled = true;

		if (!browserInitialized)
		{
			browserControl.LoadURL("https://store.steampowered.com/");
		}

		//enable browser controls
		// BackButton.visible = true;
		// ForwardButton.visible = true;
		// ReloadButton.visible = true;
		// StopButton.visible = true;
		// HomeButton.visible = true;

		// ReloadButton.enabled = true;
		// StopButton.enabled = true;
		// HomeButton.enabled = true;

		topBar.height = 43;

		gameList.enabled = false;
		gameList.visible = false;

		toolsList.enabled = false;
		toolsList.visible = false;
	}

	void OnMyGamesTabSelected()
	{
		inBrowserWindow = false;
		browserControl.visible = false;
		browserControl.enabled = false;

		//disable browser controls
		// BackButton.visible = false;
		// ForwardButton.visible = false;
		// ReloadButton.visible = false;
		// StopButton.visible = false;
		// HomeButton.visible = false;

		// BackButton.enabled = false;
		// ForwardButton.enabled = false;
		// ReloadButton.enabled = false;
		// StopButton.enabled = false;
		// HomeButton.enabled = false;
		topBar.height = 22;

		gameList.enabled = true;
		gameList.visible = true;

		toolsList.enabled = false;
		toolsList.visible = false;

		currentlistView = "game";

		//find and select the previously selected game
		GameItemControl? gameItemControl = gameList.Children
			.OfType<GameItemControl>()
			.FirstOrDefault(x => x.game != null && x.game.AppID == selectedGameID);
		if (gameItemControl != null)
		{
			panel.SetFocus(gameItemControl);
		}
	}

	void OnToolsTabSelected()
	{
		inBrowserWindow = false;
		browserControl.visible = false;
		browserControl.enabled = false;

		topBar.height = 22;

		gameList.enabled = false;
		gameList.visible = false;

		toolsList.enabled = true;
		toolsList.visible = true;

		GameItemControl? gameItemControl = toolsList.Children
		.OfType<GameItemControl>()
		.FirstOrDefault(x => x.game != null && x.game.AppID == selectedToolID);
		if (gameItemControl != null)
		{
			panel.SetFocus(gameItemControl);
		}

		currentlistView = "tool";
	}

	void OnFriendsButtonClicked()
	{
		if (WindowManager.Instance.IsWindowOpen<FriendsWindow>($"friends"))
		{
			WindowManager.Instance.HighlightWindow<FriendsWindow>($"friends");
			return;
		}

		FriendsWindow friendsWindow = new FriendsWindow(client, "friends");
		friendsWindow.SetTitle($"Friends - {client.CurrentUser.AccountName}");
		WindowManager.Instance.CreateWindow(friendsWindow);
	}

	~MainWindow()
	{
		unsafe
		{
	 		SDL3.SDL_DestroyTexture(ResizeTexture);
		}
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		if (initializedGameList)
		{
			LoadGameList(gameList, "game");
			initializedGameList = false;
		}

		if (initializedToolList)
		{
			LoadGameList(toolsList, "tool");
			initializedToolList = false;
		}

		while (gameUpdates.Count > 0)
		{
			Game game = gameUpdates.Dequeue();
			UpdateGameItem(game);
		}

		{
			Game? CurrentlySelectedGame = null;
			if (currentlistView == "game")
				CurrentlySelectedGame = client.Games.Find(x => x.AppID == selectedGameID);
			else if (currentlistView == "tool")
				CurrentlySelectedGame = client.Games.Find(x => x.AppID == selectedToolID);

			if (CurrentlySelectedGame == null)
			{
				GameActionButton.enabled = false;
				PropertiesButton.enabled = false;
			}
			else
			{
				GameActionButton.enabled = true;
				PropertiesButton.enabled = true;
				GameActionButton.text = CurrentlySelectedGame.Status switch
				{
					GameStatus.Installed => Localization.GetString("Steam_Launch"),
					GameStatus.UpdatePending => Localization.GetString("Steam_UpdateColumn"),
					GameStatus.NotInstalled => Localization.GetString("Steam_Install"),
					GameStatus.Queued => "Queued",
					GameStatus.Downloading => "Downloading",
					_ => Localization.GetString("Steam_Launch"),
				};
			}
		}


		if (inBrowserWindow)
		{
			// BackButton.enabled = browser.CanGoBack();
			// ForwardButton.enabled = browser.CanGoForward();
		}
	}

	public override void Draw()
	{
		base.Draw();

		if (!inBrowserWindow)
			GameListDraw();
		
		unsafe
		{
			panel.RootControl.DrawTexture(ResizeTexture, mWidth - 23, mHeight - GetInternalY() - 23);
		}
	}

	public void GameListDraw()
	{
		panel.DrawText(Localization.GetString("Steam_GamesColumn"), 53, 73, Color.FromArgb(216, 222, 211), fontSize: 7);
		panel.DrawText(Localization.GetString("Steam_StatusColumn"), (mWidth / 2) - 24, 73, Color.FromArgb(216, 222, 211), fontSize: 7);
		panel.DrawText(Localization.GetString("Steam_DeveloperColumn"), mWidth - 250, 73, Color.FromArgb(216, 222, 211), fontSize: 7);
	}

	GameCategoryToggleControl CreateGameCategory(string categoryName, int index, ListViewControl list)
	{
		GameCategoryToggleControl gameCategoryToggleControl = new GameCategoryToggleControl(list);
		gameCategoryToggleControl.ID = $"category_{index}_{list.ID}";
		gameCategoryToggleControl.categoryIndex = index;
		gameCategoryToggleControl.gameList = gameList;
		gameCategoryToggleControl.text = categoryName;

		list.AddChild(gameCategoryToggleControl);
		return gameCategoryToggleControl;
	}

	void LoadGameList(ListViewControl list, string filter)
	{
		list.Clear(false);

		Func<Game, bool> gameFilter = (game) => game.Type.Equals(filter, StringComparison.OrdinalIgnoreCase);

		//create favorites category
		GameCategoryToggleControl favoritesCategory = CreateGameCategory(Localization.GetString("Steam_GamesSection_Favorites"), 0, list);
		foreach (var game in client.Games.Where(g => g.IsFavorite && gameFilter(g)).OrderBy(g => g.Name))
		{
			CreateGameItemControl(game, favoritesCategory);
		}

		// create categories first
		GameCategoryToggleControl installedCategory = CreateGameCategory(Localization.GetString("Steam_GamesSection_Installed"), 1, list);
		foreach (var game in client.Games.Where(g => (g.Status == GameStatus.Installed || g.Status == GameStatus.UpdatePending) && !g.IsFavorite && gameFilter(g)).OrderBy(g => g.Name))
		{
			CreateGameItemControl(game, installedCategory);
		}

		// create not installed category
		GameCategoryToggleControl notInstalledCategory = CreateGameCategory(Localization.GetString("Steam_GamesSection_NotInstalled"), 2, list);
		foreach (var game in client.Games.Where(g => (g.Status == GameStatus.NotInstalled || g.Status == GameStatus.Queued || g.Status == GameStatus.Downloading) && !g.IsFavorite && gameFilter(g)).OrderBy(g => g.Name))
		{
			CreateGameItemControl(game, notInstalledCategory);
		}


		//if a catagory has no games, hide it
		foreach (var child in list.Children)
		{
			if (child is GameCategoryToggleControl gameCategoryToggleControl)
			{
				gameCategoryToggleControl.visible = gameCategoryToggleControl.Children.Count > 0;
			}
		}
	}

	void CreateControls()
	{
		GameActionButton.OnClick = (GameActionButton) => OnGameAction();
		GameActionButton.text = Localization.GetString("Steam_Launch");

		PropertiesButton.OnClick = (PropertiesButton) => OnGameProperties();
		PropertiesButton.text = Localization.GetString("Steam_Properties");
	}

	void CreateGameItemControl(Game game, GameCategoryToggleControl parentCategory)
	{
		GameItemControl gameItemControl = new GameItemControl(parentCategory);
		gameItemControl.SetGame(game);
		parentCategory.AddChild(gameItemControl);
		gameItemControl.OnClick = (gameitemcontrol) =>
		{
			if (inBrowserWindow) return;

			//check if mouse is over the favorite icon
			SDL_FRect favoriteIconRect = gameItemControl.GetFavoriteIconRect();
			if (favoriteIconRect.x <= gameItemControl.GetRelativeMouseX() && favoriteIconRect.x + favoriteIconRect.w >= gameItemControl.GetRelativeMouseX() && favoriteIconRect.y <= gameItemControl.GetRelativeMouseY() && favoriteIconRect.y + favoriteIconRect.h >= gameItemControl.GetRelativeMouseY())
			{
				game.IsFavorite = !game.IsFavorite;
				SaveFavorites();
				UpdateGameItem(game);
				return;
			}

		 	if (currentlistView == "game")
				selectedGameID = game.AppID;
			else if (currentlistView == "tool")
				selectedToolID = game.AppID;
		};
		gameItemControl.OnDoubleClick = (gameItemControl) => OnGameAction(game.AppID);
		gameItemControl.OnRightClick = (control) =>
		{
			if (inBrowserWindow) return;

			//check if mouse cursor is withen bounds of the list
			//if (panel.MouseY < gameList.y || panel.MouseY > gameList.y + gameList.height) return;

			gameItemControl.OnClick(control);
			panel.SetFocus(gameItemControl);

			PopupMenuWindow popupMenuWindow = new PopupMenuWindow(Steam.Instance, $"GameAction_{game.AppID}");
			popupMenuWindow.AddItem(game.Status == GameStatus.Installed ? Localization.GetString("SteamUI_GamesDialog_RightClick_LaunchGames") : Localization.GetString("SteamUI_GamesDialog_RightClick_InstallGame"), (c) =>
			{
				OnGameAction(game.AppID);
			});
			popupMenuWindow.AddSeparator();
			popupMenuWindow.AddItem(game.IsFavorite ? Localization.GetString("Steam_GamesDialog_RightClick_RemoveFromFavorites") : Localization.GetString("Steam_GamesDialog_RightClick_AddToFavorites"), (c) =>
			{
				game.IsFavorite = !game.IsFavorite;
				SaveFavorites();
				UpdateGameItem(game);
			});
			popupMenuWindow.AddItem(Localization.GetString("Steam_RightClick_DeleteLocalContent"), null);
			popupMenuWindow.AddSeparator();
			popupMenuWindow.AddItem(Localization.GetString("Steam_Properties"), (c) =>
			{
				Game? game = Steam.Instance.Games.Find(x => x.AppID == gameItemControl.game.AppID);
				if (game == null) return;

				GamePropertiesWindow gamePropertiesWindow = new GamePropertiesWindow(Steam.Instance, "properties_" + game.AppID);
				gamePropertiesWindow.SetGame(game);
				WindowManager.Instance.CreateWindow(gamePropertiesWindow);
			});
			WindowManager.Instance.CreateWindow(popupMenuWindow);
		};
	}

	public void QueueGameUpdate(Game game)
	{
		if (!gameUpdates.Contains(game))
		{
			gameUpdates.Enqueue(game);
		}
	}

	void UpdateGameItem(Game game)
	{
		GameItemControl? gameItemControl = null;
		void walk(List<UIControl> children)
		{
			foreach (var child in children)
			{
				if (child is GameItemControl gameItem && gameItem.game.AppID == game.AppID)
				{
					gameItemControl = gameItem;
					return;
				}
				walk(child.Children);
			}
		}

		ListViewControl currentList = gameList;
		if (game.Type.Equals("tool", StringComparison.OrdinalIgnoreCase)) currentList = toolsList;

		walk(currentList.Children);

		bool newItem = gameItemControl == null;
		
		int previousCategoryIndex = -1;
		if (!newItem)
			previousCategoryIndex = gameItemControl!.parent is GameCategoryToggleControl category ? category.categoryIndex : -1;

		int currentCategoryIndex = -1;
		if (game.IsFavorite)
			currentCategoryIndex = 0;
		else if (game.Status == GameStatus.Installed || game.Status == GameStatus.UpdatePending)
			currentCategoryIndex = 1;
		else if (game.Status == GameStatus.NotInstalled || game.Status == GameStatus.Queued || game.Status == GameStatus.Downloading)
			currentCategoryIndex = 2;

		GameCategoryToggleControl? newCategory = null;
		if (currentCategoryIndex != -1)
		{
			newCategory = currentList.Children.OfType<GameCategoryToggleControl>().FirstOrDefault(c => c.categoryIndex == currentCategoryIndex);
		}
		
		if (newCategory == null)
		{
			throw new Exception($"Game {game.Name} does not fit in any category!");
		}

		if (!newItem && previousCategoryIndex != currentCategoryIndex)
		{
			//remove from old category
			if (gameItemControl!.parent is GameCategoryToggleControl oldCategory)
			{
				oldCategory.RemoveChild(gameItemControl);
				newCategory.AddChild(gameItemControl);
				newCategory.OrderChildren(c => (c is GameItemControl gameItem) ? string.Compare(gameItem.game.Name, game.Name) : 0);
				oldCategory.visible = oldCategory.Children.Count > 0;
				newCategory.visible = true;
			}
		}
		else if (gameItemControl == null)
		{
			CreateGameItemControl(game, newCategory);
		}
		else
		{
			Console.WriteLine($"Updating game item for {game.AppID} was called but nothing changed.");
		}
	}
	
	// //called when the game is double clicked in the game list or the game action button is clicked while a game is selected
	async void OnGameAction(int gameID = -1)
	{
		if (inBrowserWindow) return;

		if (gameID == -1)
		{
			if (currentlistView == "game")
				gameID = selectedGameID;
			else if (currentlistView == "tool")
				gameID = selectedToolID;
		}

		Game? game = client.Games.Find(x => x.AppID == gameID);
		if (game == null) return;

		if (game.Status == GameStatus.NotInstalled || game.Status == GameStatus.UpdatePending || game.Status == GameStatus.Queued || game.Status == GameStatus.Downloading)
		{
			if (WindowManager.Instance.IsWindowOpen<InstallGameWindow>($"install_wizard_{game.AppID}"))
			{
				WindowManager.Instance.HighlightWindow<InstallGameWindow>($"install_wizard_{game.AppID}");
				return;
			}

			InstallGameWindow installGameWindow = new InstallGameWindow(client, $"install_wizard_{game.AppID}");
			installGameWindow.SetTitle(Localization.GetString("Steam_InstallAppWizard_Title").Replace("%game%", game.Name));
			installGameWindow.SetGame(game);
			WindowManager.Instance.CreateWindow(installGameWindow);
		}
		else if (game.Status == GameStatus.Installed)
		{
			client.StartGame(game);
		}
	}

	void OnGameProperties(int gameID = -1)
	{
		if (inBrowserWindow) return;

		if (gameID == -1)
		{
			if (currentlistView == "game")
				gameID = selectedGameID;
			else if (currentlistView == "tool")
				gameID = selectedToolID;
		}

		Game? game = client.Games.Find(x => x.AppID == gameID);
		if (game == null) return;

		if (WindowManager.Instance.IsWindowOpen<GamePropertiesWindow>($"properties_{game.AppID}"))
		{
			WindowManager.Instance.HighlightWindow<GamePropertiesWindow>($"properties_{game.AppID}");
			return;
		}

		GamePropertiesWindow gamePropertiesWindow = new GamePropertiesWindow(client, "properties_" + game.AppID);
		gamePropertiesWindow.SetGame(game);
		WindowManager.Instance.CreateWindow(gamePropertiesWindow);
	}

	void SaveFavorites()
	{
		List<int> favoriteAppIds = new List<int>();
		foreach (var game in client.Games)
		{
			if (game.IsFavorite)
			{
				favoriteAppIds.Add(game.AppID);
			}
		}

		File.WriteAllText(Utils.GetAbsolutePath($"userdata/{client.CurrentUser.SteamID}/config/favorites.json"), JsonConvert.SerializeObject(favoriteAppIds));
	}

	void LoadFavorites()
	{
		if (File.Exists(Utils.GetAbsolutePath($"userdata/{client.CurrentUser.SteamID}/config/favorites.json")))
		{
			string favoritesJson = File.ReadAllText(Utils.GetAbsolutePath($"userdata/{client.CurrentUser.SteamID}/config/favorites.json"));
			List<int> favoriteAppIds = JsonConvert.DeserializeObject<List<int>>(favoritesJson);
			foreach (var appId in favoriteAppIds)
			{
				Game game = client.Games.Find(x => x.AppID == appId);
				if (game != null)
				{
					game.IsFavorite = true;
				}
			}
		}
	}
}