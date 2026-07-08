using KGUI;
using KGUI.Controls;
using SteamKit2;

public class FriendsWindow : SteamWindow
{
	FriendItemControl SelfFriendItemControl;
	public List<FriendItemControl> FriendItemControls = new List<FriendItemControl>();

	ListViewControl FriendListControl;

	public FriendsWindow(Steam steam, string uuid) : base(steam, uuid) {}

	public override void PostInitialize()
	{		
		backendWindow.SetTitle($"Friends - {client.CurrentUser.AccountName}");
		
		FriendListControl = panel.GetControlByID<ListViewControl>("friendListControl");

		FriendListControl.AddChild(new SolidBackgroundControl(FriendListControl) { height = 12 });

		//self
		FriendItemControl selfFriendItemControl = new FriendItemControl(FriendListControl);
		selfFriendItemControl.SetSize(200, 48);
		selfFriendItemControl.SetSelf(client.CurrentUser);
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

	public void LoadFriendList()
	{
		foreach (var friend in client.Friends)
		{
			FriendItemControl friendItemControl = new FriendItemControl(FriendListControl);
			friendItemControl.SetSize(200, 48);
			friendItemControl.SetFriend(friend);

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
		FriendItemControl friendItemControl = FriendItemControls.Find(f => f.Friend.SteamID == friend.SteamID);
		if (friendItemControl != null)
		{
			friendItemControl.SetFriend(friend);
		}
	}
}