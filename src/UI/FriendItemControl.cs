using SDL_Sharp;
using SDL_Sharp.Image;
using SteamKit2;

public class FriendItemControl : UIControl
{
	public Texture AvatarTexture;
	public Texture AvatarBorderTexture;

	public ulong SteamID;
	public string PersonaName = "";
	public EPersonaState PersonaState;
	public int GamePlayedID;
	public string GamePlayedName;
	public DateTime LastOnline;

	public bool Self = false;
	public bool DrawBackground = true;

	public int XOffset = 20;

	public FriendItemControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, ulong steamID, int width = 0, int height = 0) : base(parent, renderer, controlName, x, y, width, height)
	{
		SteamID = steamID;

		//attempt to load avatar
		unsafe
		{
			Surface* surface = IMG.Load(Steam.Instance.GetAvatarPath(SteamID, AvatarSize.Small));
			AvatarTexture = SDL.CreateTextureFromSurface(renderer, surface);
			SDL.FreeSurface(surface);
		}
	}

	public override void Draw()
	{
		base.Draw();

		//background
		if (!Self || DrawBackground) parent.DrawBox(x + XOffset, y, width - XOffset, height, new Color(58, 58, 60, 255));

		//avatar
		unsafe
		{
			Rect avatarRectDest = new Rect(x + XOffset + 8, y + 8, 32, 32);
			SDL.RenderCopy(renderer, AvatarTexture, null, &avatarRectDest);
		}

		//border
		if (PersonaState == EPersonaState.Offline) parent.DrawTextureSheet(AvatarBorderTexture, x + XOffset + 4, y + 4, 0, 0, 40, 40);
		else if (GamePlayedID != 0) parent.DrawTextureSheet(AvatarBorderTexture, x + XOffset + 4, y + 4, 2, 0, 40, 40);
		else parent.DrawTextureSheet(AvatarBorderTexture, x + XOffset + 4, y + 4, 1, 0, 40, 40);

		Color nameColor;
		if (PersonaState == EPersonaState.Offline) nameColor = new Color(156, 158, 157, 255);
		else if (GamePlayedID != 0) nameColor = new Color(191, 224, 142, 255);
		else nameColor = new Color(128, 163, 185, 255);

		//name
		parent.DrawText(PersonaName, x + XOffset + 52, y + 9, nameColor, bold: true);

		//status
		Color statusColor;
		if (PersonaState == EPersonaState.Offline) statusColor = new Color(156, 158, 157, 220);
		else if (GamePlayedID != 0) statusColor = new Color(191, 224, 142, 220);
		else statusColor = new Color(128, 163, 185, 220);

		string statusText;
		if (PersonaState == EPersonaState.Offline) statusText = "Last Online: " + LastOnline.AsTimeAgo();
		else if (GamePlayedID != 0) statusText = "In-Game";
		else statusText = "Online";

		//game played

		parent.DrawText(statusText, x + XOffset + 52, y + 21, statusColor);
		if (GamePlayedID != 0) parent.DrawText(GamePlayedName, x + XOffset + 52, y + 33, statusColor);
	}
}