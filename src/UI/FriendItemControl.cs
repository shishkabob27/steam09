using System.Drawing;
using KGUI;
using KGUI.Controls;
using SDL;
using SteamKit2;

public class FriendItemControl : UIControl
{
	public RemoteImageControl AvatarImage;
	public unsafe SDL_Texture* AvatarBorderTexture;

	public ulong SteamID;
	public string PersonaName = "";
	public EPersonaState PersonaState;
	public int GamePlayedID;
	public string GamePlayedName;
	public DateTime LastOnline;

	public bool Self = false;
	public bool DrawBackground = true;

	public int XOffset = 20;

	public FriendItemControl(UIControl parent) : base(parent)
	{
		unsafe
		{
			AvatarBorderTexture = LoadTexture(Assets.GetAssetPath("graphics/avatar_border.png"));
		}
	}

	public void SetFriend(ulong steamID, string personaName, EPersonaState personaState, int gamePlayedID, string gamePlayedName, DateTime lastOnline)
	{
		SteamID = steamID;
		PersonaName = personaName;
		PersonaState = personaState;
		GamePlayedID = gamePlayedID;
		GamePlayedName = gamePlayedName;
		LastOnline = lastOnline;

		RemoteImageControl avatarImage = new RemoteImageControl(this);
		avatarImage.SetSize(32, 32);
		avatarImage.Reposition(XOffset + 8, 8);
	}

	public override void Draw()
	{
		base.Draw();

		// background
		if (!Self || DrawBackground) DrawBox(XOffset, 0, width - XOffset, height, Color.FromArgb(58, 58, 60));

		// //border
		unsafe
		{
			if (PersonaState == EPersonaState.Offline) DrawTextureSheet(AvatarBorderTexture, XOffset + 4, 4, 0, 0, 40, 40);
			else if (GamePlayedID != 0) DrawTextureSheet(AvatarBorderTexture, XOffset + 4, 4, 2, 0, 40, 40);
			else DrawTextureSheet(AvatarBorderTexture, XOffset + 4, 4, 1, 0, 40, 40);
		}

		Color nameColor;
		if (PersonaState == EPersonaState.Offline) nameColor = Color.FromArgb(156, 158, 157);
		else if (GamePlayedID != 0) nameColor = Color.FromArgb(191, 224, 142);
		else nameColor = Color.FromArgb(128, 163, 185);

		//name
		DrawText(PersonaName, XOffset + 52, 9, nameColor, bold: true);

		//status
		Color statusColor;
		if (PersonaState == EPersonaState.Offline) statusColor = Color.FromArgb(220, 156, 158, 157);
		else if (GamePlayedID != 0) statusColor = Color.FromArgb(220, 191, 224, 142);
		else statusColor = Color.FromArgb(220, 128, 163, 185);

		string statusText;
		if (PersonaState == EPersonaState.Offline) statusText = "Last Online: " + LastOnline.AsTimeAgo();
		else if (GamePlayedID != 0) statusText = "In-Game";
		else statusText = "Online";

		//game played

		DrawText(statusText, XOffset + 52, 21, statusColor);
		if (GamePlayedID != 0) DrawText(GamePlayedName, XOffset + 52, 33, statusColor);
	}
}