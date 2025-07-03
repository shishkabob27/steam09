using System.Runtime.CompilerServices;
using SDL_Sharp;
using SDL_Sharp.Ttf;
using SteamKit2;

public class FriendsWindow : SteamWindow
{
	Texture TopTexture;
	Texture AvatarBorderTexture;

	FriendItemControl SelfFriendItemControl;
	public List<FriendItemControl> FriendItemControls = new List<FriendItemControl>();

	ListControl FriendListControl;

	public FriendsWindow(Steam steam, string title, int width, int height, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0) : base(steam, title, width, height, resizable, minimumWidth, minimumHeight)
	{
		unsafe
		{
			Surface* TopSurface = SDL_Sharp.Image.IMG.Load("resources/graphics/FriendsListSlantBG.png");
			TopTexture = SDL.CreateTextureFromSurface(renderer, TopSurface);
			SDL.FreeSurface(TopSurface);

			Surface* AvatarBorderSurface = SDL_Sharp.Image.IMG.Load("resources/graphics/avatar_border.png");
			AvatarBorderTexture = SDL.CreateTextureFromSurface(renderer, AvatarBorderSurface);
			SDL.FreeSurface(AvatarBorderSurface);
		}

		FriendListControl = new ListControl(panel, renderer, "friendListControl", 2, 21, 1, 1);
		FriendListControl.Gap = 2;
		panel.AddControl(FriendListControl);

		FriendListControl.Children.Add(new SpacerControl(panel, renderer, "spacerControl", 0, 0, height: 12));

		//self
		FriendItemControl selfFriendItemControl = new FriendItemControl(panel, renderer, "selfFriendItemControl", 20, 0, steam.CurrentUser.SteamID, 200, 48);
		selfFriendItemControl.AvatarBorderTexture = AvatarBorderTexture;
		selfFriendItemControl.SteamID = steam.CurrentUser.SteamID;
		selfFriendItemControl.PersonaName = steam.CurrentUser.PersonaName;
		selfFriendItemControl.PersonaState = EPersonaState.Online;
		selfFriendItemControl.Self = true;
		selfFriendItemControl.DrawBackground = false;
		SelfFriendItemControl = selfFriendItemControl;
		panel.AddControl(selfFriendItemControl);
		FriendItemControls.Add(selfFriendItemControl);
		FriendListControl.Children.Add(selfFriendItemControl);

		FriendListControl.Children.Add(new SpacerControl(panel, renderer, "spacerControl", 0, 0, height: 8));

		LoadFriendList();
		panel.SetFocus(FriendListControl);
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		//resize 
		FriendListControl.width = mWidth - 4;
		FriendListControl.height = mHeight - 63;

		FriendListControl.Update();
	}

	public override void Draw()
	{
		base.Draw();

		//draw background
		panel.DrawBox(FriendListControl.x, FriendListControl.y, FriendListControl.width, FriendListControl.height, new Color(36, 38, 35, 255));

		Rect clipRect = new Rect(FriendListControl.x, FriendListControl.y, FriendListControl.width, FriendListControl.height);
		SDL.RenderSetClipRect(renderer, ref clipRect);

		for (int i = 0; i < FriendListControl.width; i += 16)
		{
			panel.DrawTexture(TopTexture, FriendListControl.x + i, FriendListControl.y);
		}

		unsafe
		{
			SDL.RenderSetClipRect(renderer, null);
		}

		FriendListControl.Draw();

		SDL.RenderPresent(renderer);
	}

	public void LoadFriendList()
	{
		foreach (var friend in steam.Friends)
		{
			FriendItemControl friendItemControl = new FriendItemControl(panel, renderer, "friendItemControl", 20, 0, friend.SteamID, 200, 48);
			friendItemControl.AvatarBorderTexture = AvatarBorderTexture;
			friendItemControl.SteamID = friend.SteamID;
			friendItemControl.PersonaName = friend.PersonaName;
			friendItemControl.PersonaState = friend.PersonaState;
			friendItemControl.GamePlayedID = (int)friend.GamePlayed;
			friendItemControl.GamePlayedName = friend.GamePlayedName;
			friendItemControl.LastOnline = friend.LastOnline;
			panel.AddControl(friendItemControl);
			FriendItemControls.Add(friendItemControl);
			FriendListControl.Children.Add(friendItemControl);

			friendItemControl.OnDoubleClick = () =>
			{
				steam.PendingWindows.Add(new ChatWindow(steam, $"{friend.PersonaName} - Chat", 440, 310, true, 440, 310, friend));
			};
		}
	}

	public void UpdateFriend(Friend friend)
	{
		FriendItemControl friendItemControl = FriendItemControls.Find(f => f.SteamID == friend.SteamID);
		if (friendItemControl != null)
		{
			friendItemControl.PersonaName = friend.PersonaName;
			friendItemControl.PersonaState = friend.PersonaState;
			friendItemControl.GamePlayedID = (int)friend.GamePlayed;
			friendItemControl.GamePlayedName = friend.GamePlayedName;
			friendItemControl.LastOnline = friend.LastOnline;
		}
	}
}