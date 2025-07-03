using SDL_Sharp;
using SDL_Sharp.Image;

public class GameItemControl : UIControl
{
	public Game game;
	public Texture gameIcon;
	public bool highlighted = false;

	public GameItemControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, Game game, int width = 0, int height = 0) : base(parent, renderer, controlName, x, y, width, height)
	{
		this.game = game;

		//load game icon
		string logoPath = $"appcache/librarycache/{game.AppID}/icon.jpg";
		if (File.Exists(logoPath))
		{
			unsafe
			{
				Surface* surface = IMG.Load(logoPath);
				gameIcon = SDL.CreateTextureFromSurface(renderer, surface);
				SDL.FreeSurface(surface);
			}
		}
	}

	~GameItemControl()
	{
		SDL.DestroyTexture(gameIcon);
	}

	public override void Draw()
	{
		base.Draw();

		if (highlighted)
		{
			parent.DrawBox(27, y, width - 54, height, new Color(47, 49, 45, 255));
		}

		if (gameIcon != null)
		{
			Rect rect = new Rect(x + 28, y + 2, 16, 16);
			unsafe
			{
				SDL.RenderCopy(renderer, gameIcon, null, &rect);
			}
		}

		Color textColor = new Color(230, 236, 224, 255);
		if (game.Status == GameStatus.NotInstalled) textColor = new Color(121, 126, 121, 255);
		parent.DrawText(game.Name, 49, y + 5, textColor);
		parent.DrawText(game.GetStatusString(), (width / 2) - 24, y + 5, textColor);
		parent.DrawText(game.GetDeveloper(), (width - 248), y + 5, textColor, true, true);

	}
}