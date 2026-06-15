using KGUI;
using SteamKit2;

public class FriendsWindow : SteamWindow
{
	FriendItemControl SelfFriendItemControl;
	public List<FriendItemControl> FriendItemControls = new List<FriendItemControl>();

	ListViewControl FriendListControl;

	public FriendsWindow(Steam steam, string uuid) : base(steam, uuid)
	{
		FriendListControl = panel.GetControlByID<ListViewControl>("friendListControl");

		FriendListControl.AddChild(new SolidBackgroundControl(FriendListControl) { height = 12 });

		//self
		FriendItemControl selfFriendItemControl = new FriendItemControl(FriendListControl);
		selfFriendItemControl.SetSize(200, 48);
		selfFriendItemControl.SetFriend(steam.CurrentUser.SteamID, steam.CurrentUser.PersonaName, EPersonaState.Online, 0, "", DateTime.Now);
		selfFriendItemControl.Self = true;
		selfFriendItemControl.DrawBackground = false;
		SelfFriendItemControl = selfFriendItemControl;
		FriendListControl.AddChild(selfFriendItemControl);
		FriendItemControls.Add(selfFriendItemControl);

		FriendListControl.AddChild(new SolidBackgroundControl(FriendListControl) { height = 8 });

		LoadFriendList();

		FriendListControl.AddChild(new SolidBackgroundControl(FriendListControl) { height = 8 });

		panel.SetFocus(FriendListControl);
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);
	}

	public override void Draw()
	{
		base.Draw();
	}

	public void LoadFriendList()
	{
		foreach (var friend in client.Friends)
		{
			FriendItemControl friendItemControl = new FriendItemControl(FriendListControl);
			friendItemControl.SetSize(200, 48);
			friendItemControl.SetFriend(friend.SteamID, friend.PersonaName, friend.PersonaState, (int)friend.GamePlayed, friend.GamePlayedName, friend.LastOnline);

			FriendListControl.AddChild(friendItemControl);
			FriendItemControls.Add(friendItemControl);

		// 	friendItemControl.OnDoubleClick = () =>
		// 	{
		// 		steam.PendingWindows.Add(new ChatWindow(steam, $"{friend.PersonaName} - Chat", 440, 310, true, 440, 310, friend));
		// 	};
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