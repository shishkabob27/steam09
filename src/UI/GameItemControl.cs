using System.Drawing;
using KGUI;
using SDL;

public class GameItemControl : UIControl, IDisposable
{
	public Game game;
	RemoteImageControl gameIconControl;
	public bool highlighted = false;

	unsafe SDL_Texture* FavoriteIcon;

	public GameItemControl(UIControl parent) : base(parent)
	{
		height = 20;

		unsafe
		{
			FavoriteIcon = LoadTexture(Assets.GetAssetPath("graphics/favorite_button.png"));
		}

		gameIconControl = new RemoteImageControl(this);
		gameIconControl.SetSize(16, 16);
		gameIconControl.Reposition(29, 2);
		gameIconControl.AcceptMouseEvents = false;
		gameIconControl.ManualDraw = true;
		AddChild(gameIconControl);
	}

	public void SetGame(Game game)
	{
		this.game = game;
		string icon = game.AppInfo?["common"]?["icon"]?.ToString() ?? "";
		gameIconControl.SetImageUrl($"https://cdn.cloudflare.steamstatic.com/steamcommunity/public/images/apps/{game.AppID}/{icon}.jpg");
	}
	
	public void Dispose()
	{
		// SDL.DestroyTexture(gameIcon);
		// gameIcon = default;

		// SDL.DestroyTexture(FavoriteIcon);
		// FavoriteIcon = default;

		GC.SuppressFinalize(this);
	}

	public override void Draw()
	{
		base.Draw();

		if (focused)
		{
			DrawBox(27, 0, width - 54, height, Color.FromArgb(47, 49, 45));
		}

		gameIconControl.Draw();

		Color textColor = Color.FromArgb(230, 236, 224);
		if (game.Status == GameStatus.NotInstalled) textColor =  Color.FromArgb(121, 126, 121);
		DrawText(game.Name, 49, 5, textColor);
		DrawText(game.GetStatusString(), (width / 2) - 24, 5, textColor);
		DrawText(game.GetDeveloper(), (width - 248), 5, game.Status == GameStatus.NotInstalled ? textColor : Color.FromArgb(255, 255, 255), true, true);

		unsafe
		{
			DrawTextureSheet(FavoriteIcon, (width / 2) - 44, 2, game.IsFavorite ? 1 : 0, 0, 16, 16);
		}
	}

	public SDL_FRect GetFavoriteIconRect()
	{
		return new SDL_FRect{ x = (width / 2) - 44, y = 2, w = 16, h = 16 };
	}
}