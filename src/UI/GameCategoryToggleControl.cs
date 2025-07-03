using SDL_Sharp;

public class GameCategoryToggleControl : UIControl
{
	public bool Open = true;

	ListControl gameList;

	public int categoryIndex;

	public GameCategoryToggleControl(UIPanel parent, Renderer renderer, string controlName, string catagoryText, ListControl gameList, int categoryIndex, bool open = true) : base(parent, renderer, controlName, 0, 0, 0, 0)
	{
		this.text = catagoryText;
		this.height = 30;
		this.gameList = gameList;
		this.categoryIndex = categoryIndex;
		this.Open = open;

		OnClick = () =>
		{
			Open = !Open;
			UpdateGameVisibility();
		};

		OnDoubleClick = () =>
		{
			Open = !Open;
			UpdateGameVisibility();
		};
	}

	public override void Draw()
	{
		base.Draw();

		if (mouseOver)
		{
			if (Open) parent.DrawTexture(parent.categoryCollapsedMouseOverTexture, x + 12, y + 15);
			else parent.DrawTexture(parent.categoryExpandedMouseOverTexture, x + 12, y + 15);
		}
		else
		{
			if (Open) parent.DrawTexture(parent.categoryCollapsedTexture, x + 12, y + 15);
			else parent.DrawTexture(parent.categoryExpandedTexture, x + 12, y + 15);
		}

		parent.DrawText(text, x + 28, y + height - 15, new Color(196, 181, 80, 255), fontSize: 7);

		//draw line
		parent.DrawBox(x + 29, y + height - 2, width - 58, 1, new Color(121, 126, 121, 255));
	}

	public void UpdateGameVisibility()
	{
		List<GameItemControl> gamesBelongingToCategory = GetGamesBelongingToCategory();

		//update visibility of games
		foreach (var gameControl in gamesBelongingToCategory)
		{
			gameControl.visible = Open;
		}
	}

	public List<GameItemControl> GetGamesBelongingToCategory()
	{
		List<GameItemControl> gamesBelongingToCategory = new List<GameItemControl>();

		foreach (var gameControl in gameList.Children.Where(x => x is GameItemControl))
		{
			if (gameControl is GameItemControl gameItemControl)
			{
				if (gameItemControl.game.IsFavorite && categoryIndex == 0)
				{
					gamesBelongingToCategory.Add(gameItemControl);
				}
				else if ((gameItemControl.game.Status == GameStatus.Installed || gameItemControl.game.Status == GameStatus.UpdatePending) && categoryIndex == 1)
				{
					gamesBelongingToCategory.Add(gameItemControl);
				}
				else if (gameItemControl.game.Status == GameStatus.NotInstalled && categoryIndex == 2)
				{
					gamesBelongingToCategory.Add(gameItemControl);
				}
			}
		}

		return gamesBelongingToCategory;
	}
}