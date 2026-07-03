using System.Drawing;
using KGUI;
using KGUI.Controls;
using SDL;
using SteamKit2;

public class FriendItemControl : UIControl
{
	public RemoteImageControl AvatarImage;
	public unsafe SDL_Texture* AvatarBorderTexture;

	public Friend Friend;

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

	public void SetSelf(User user)
	{
		Self = true;
		Friend = new Friend
		{
			SteamID = user.SteamID,
			PersonaName = user.PersonaName,
			PersonaState = EPersonaState.Online,
		};

		UpdateAvatarImage(user.AvatarSHA);
	}

	public void SetFriend(Friend friend)
	{
		Friend = friend;

		UpdateAvatarImage(friend.AvatarSHA);
	}

	void UpdateAvatarImage(byte[] avatarSHA)
	{
		if (avatarSHA == null) return;
		if (avatarSHA.Length == 0) return;
		string avatarSHAString = Convert.ToHexStringLower(avatarSHA);
		if (avatarSHAString.All(c => c == '0')) avatarSHAString = "fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb";

		if (AvatarImage == null)
		{
			AvatarImage = new RemoteImageControl(this);
			AddChild(AvatarImage);
			AvatarImage.SetSize(32, 32);
			AvatarImage.Reposition(XOffset + 8, 8);
		}

		AvatarImage.SetImageUrl("https://avatars.fastly.steamstatic.com/" + avatarSHAString + "_medium.jpg");
	}

	public override void Draw()
	{
		base.Draw();

		// background
		if (!Self || DrawBackground) DrawBox(XOffset, 0, width - XOffset, height, Color.FromArgb(58, 58, 60));

		// //border
		unsafe
		{
			if (Friend.PersonaState == EPersonaState.Offline) DrawTextureSheet(AvatarBorderTexture, XOffset + 4, 4, 0, 0, 40, 40);
			else if (Friend.GamePlayed != 0) DrawTextureSheet(AvatarBorderTexture, XOffset + 4, 4, 2, 0, 40, 40);
			else DrawTextureSheet(AvatarBorderTexture, XOffset + 4, 4, 1, 0, 40, 40);
		}

		Color nameColor;
		if (Friend.PersonaState == EPersonaState.Offline) nameColor = Color.FromArgb(156, 158, 157);
		else if (Friend.GamePlayed != 0) nameColor = Color.FromArgb(191, 224, 142);
		else nameColor = Color.FromArgb(128, 163, 185);

		//name
		DrawText(Friend.PersonaName, XOffset + 52, 9, nameColor, bold: true);

		//status
		Color statusColor;
		if (Friend.PersonaState == EPersonaState.Offline) statusColor = Color.FromArgb(220, 156, 158, 157);
		else if (Friend.GamePlayed != 0) statusColor = Color.FromArgb(220, 191, 224, 142);
		else statusColor = Color.FromArgb(220, 128, 163, 185);

		string statusText;
		if (Friend.PersonaState == EPersonaState.Offline) statusText = "Last Online: " + Friend.LastOnline.AsTimeAgo();
		else if (Friend.GamePlayed != 0) statusText = "In-Game";
		else statusText = "Online";

		//game played

		DrawText(statusText, XOffset + 52, 21, statusColor);
		if (Friend.GamePlayed != 0) DrawText(Friend.GamePlayedName, XOffset + 52, 33, statusColor);
	}
}