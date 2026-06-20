using System.Drawing;
using KGUI;
using SDL;

public class GameCategoryToggleControl : TreeViewItem
{
	public ListViewControl gameList;

	public int categoryIndex;

	unsafe SDL_Texture* _categoryIconTexture;

	protected override int GetInitialHeight() => 30;

	public GameCategoryToggleControl(UIControl parent) : base(parent)
	{
		SetIsCategory(true);
		this.height = 30;

		unsafe
		{
			_categoryIconTexture = LoadTexture(Assets.GetAssetPath("graphics/category_icon.png"));
		}
	}

	public unsafe override void Draw()
	{

		if (mouseOver)
		{
			if (_expanded) DrawTextureSheet(_categoryIconTexture, 12, 15, 1, 0, 14, 9);
			else DrawTextureSheet(_categoryIconTexture, 12, 15, 1, 1, 14, 9);
		}
		else
		{
			if (_expanded) DrawTextureSheet(_categoryIconTexture, 12, 15, 0, 0, 14, 9);
			else DrawTextureSheet(_categoryIconTexture, 12, 15, 0, 1, 14, 9);
		}

		DrawText(text, 28, GetInitialHeight() - 15, Color.FromArgb(196, 181, 80), fontSize: 7);

		DrawBox(29, GetInitialHeight() - 2, width - 58, 1, Color.FromArgb(121, 126, 121));
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
				else if ((gameItemControl.game.Status == GameStatus.Installed || gameItemControl.game.Status == GameStatus.UpdatePending) && !gameItemControl.game.IsFavorite && categoryIndex == 1)
				{
					gamesBelongingToCategory.Add(gameItemControl);
				}
				else if ((gameItemControl.game.Status == GameStatus.NotInstalled || gameItemControl.game.Status == GameStatus.Queued || gameItemControl.game.Status == GameStatus.Downloading) && !gameItemControl.game.IsFavorite && categoryIndex == 2)
				{
					gamesBelongingToCategory.Add(gameItemControl);
				}
			}
		}

		return gamesBelongingToCategory;
	}

	public override void TestClickExpandButton(UIControl control)
	{
		_expanded = !_expanded;
	}
}