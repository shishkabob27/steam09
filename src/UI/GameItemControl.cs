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
		gameIconControl.SetImageUrl(game.IconUrl);
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
		if (game.Status == GameStatus.NotInstalled && !game.HasPartialDownload) textColor = Color.FromArgb(121, 126, 121);

		Color downloadStatusColor = textColor;
		if (game.Status == GameStatus.Queued || game.Status == GameStatus.Downloading || game.DownloadStatus != DownloadStatus.None || game.HasPartialDownload) downloadStatusColor = Color.FromArgb(196, 181, 80);
		DrawText(game.Name, 49, 5, textColor);
		DrawText(game.GetStatusString(), (width / 2) - 24, 5, downloadStatusColor);
		DrawText(game.Developer, (width - 248), 5, (game.Status == GameStatus.NotInstalled && !game.HasPartialDownload) ? textColor : Color.FromArgb(255, 255, 255), true, true);

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