using KGUI;
using Newtonsoft.Json;
using SteamKit2;

public partial class Steam
{
	public List<Friend> Friends = new List<Friend>();
	public List<ChatHistory> ChatHistories = new List<ChatHistory>();

	public string GetPersonaName(ulong steamID)
	{
		//check if self, if so, return persona name
		if (steamID == CurrentUser.SteamID) return CurrentUser.PersonaName;

		Friend friend = Friends.Find(f => f.SteamID == steamID);
		if (friend != null) return friend.PersonaName;
		return "";
	}

	async void OnFriendsList(SteamFriends.FriendsListCallback callback)
	{
		foreach (var friend in callback.FriendList)
		{
			Friends.Add(new Friend
			{
				SteamID = friend.SteamID.ConvertToUInt64(),
				PersonaName = "",
				PersonaState = EPersonaState.Offline,
				Relationship = friend.Relationship,
				AvatarSHA = steamFriends.GetFriendAvatar(friend.SteamID),
			});

			steamFriends.RequestFriendInfo(friend.SteamID);
		}

		//get chat histories for all friends
		foreach (var friend in Friends)
		{
			ChatHistories.Add(new ChatHistory(friend.SteamID));
			steamFriends.RequestMessageHistory(friend.SteamID);
		}
	}

	void OnFriendProfileInfo(SteamFriends.ProfileInfoCallback callback)
	{
		Console.WriteLine("Friend " + callback.SteamID + " profile info.");
	}

	void OnFriendPersonaState(SteamFriends.PersonaStateCallback callback)
	{
		if (callback.FriendID == CurrentUser.SteamID)
		{
			CurrentUser.AvatarSHA = callback.AvatarHash;
			ModifyLoginUser(CurrentUser);

			return;
		}
		
		Friend friend = Friends.Find(f => f.SteamID == callback.FriendID.ConvertToUInt64());
		if (friend != null)
		{
			friend.PersonaName = steamFriends.GetFriendPersonaName(callback.FriendID);
			friend.GamePlayedName = steamFriends.GetFriendGamePlayedName(callback.FriendID);
			friend.GamePlayed = steamFriends.GetFriendGamePlayed(callback.FriendID).AppID;
			friend.PersonaState = steamFriends.GetFriendPersonaState(callback.FriendID);
			friend.LastOnline = callback.LastLogOn.ToLocalTime();

			//request game info
			if (friend.GamePlayed != 0)
			{
				SteamApps.PICSRequest pICSRequest = new SteamApps.PICSRequest(friend.GamePlayed);
				steamApps.PICSGetProductInfo(pICSRequest, null, false);
			}

			// FriendsWindow friendsWindow = (FriendsWindow)Windows.Find(x => x is FriendsWindow);
			// if (friendsWindow != null) friendsWindow.UpdateFriend(friend);

			// //find chat window with friend
			// ChatWindow chatWindow = (ChatWindow)Windows.Find(x => x is ChatWindow && ((ChatWindow)x).FriendSteamID == callback.FriendID.ConvertToUInt64());
			// if (chatWindow != null)
			// {
			// 	chatWindow.UpdateFriendItemControl(friend);
			// }
		}
	}

	void OnPICSProductInfo(SteamApps.PICSProductInfoCallback callback)
	{
		KeyValuePair<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> app = callback.Apps.FirstOrDefault();
		if (app.Value == null) return;

		//find friend
		Friend friend = Friends.Find(f => f.GamePlayed == app.Key);
		if (friend != null)
		{
			friend.GamePlayedName = app.Value.KeyValues["common"]["name"].Value ?? "";

			FriendsWindow? friendsWindow = WindowManager.Instance.GetWindowEnvironment("desktop")?.GetWindows()?.OfType<FriendsWindow>().FirstOrDefault();
			if (friendsWindow != null) friendsWindow.UpdateFriend(friend);

			// ChatWindow chatWindow = (ChatWindow)Windows.Find(x => x is ChatWindow && ((ChatWindow)x).FriendSteamID == friend.SteamID);
			// if (chatWindow != null) chatWindow.UpdateFriendItemControl(friend);
		}
	}

	void OnFriendPersonaChange(SteamFriends.PersonaChangeCallback callback)
	{
		Console.WriteLine("Friend " + callback.Name + " changed their persona");
	}

	void OnFriendMsg(SteamFriends.FriendMsgCallback callback)
	{
		if (callback.Message == null || callback.Message == "") return; // Typing indicator

		ChatHistory chatHistory = ChatHistories.Find(x => x.SteamID == callback.Sender.ConvertToUInt64());
		if (chatHistory == null)
		{
			chatHistory = new ChatHistory(callback.Sender.ConvertToUInt64());
			ChatHistories.Add(chatHistory);
		}

		chatHistory.Messages.Add(new ChatMessage
		{
			SenderSteamID = callback.Sender.ConvertToUInt64(),
			Message = callback.Message ?? "",
			Timestamp = DateTime.Now,
			Unread = true,
			PersonaState = steamFriends.GetFriendPersonaState(callback.Sender),
			GamePlayedID = (int)steamFriends.GetFriendGamePlayed(callback.Sender).AppID,
		});

		//find chat window with friend
		// ChatWindow chatWindow = (ChatWindow)Windows.Find(x => x is ChatWindow && ((ChatWindow)x).FriendSteamID == callback.Sender.ConvertToUInt64());
		// if (chatWindow != null)
		// {
		// 	chatWindow.CreateMessageControl(chatHistory.Messages.Last());
		// }
	}

	void OnFriendMsgHistory(SteamFriends.FriendMsgHistoryCallback callback)
	{
		ChatHistory chatHistory = ChatHistories.Find(x => x.SteamID == callback.SteamID.ConvertToUInt64());
		if (chatHistory == null)
		{
			chatHistory = new ChatHistory(callback.SteamID.ConvertToUInt64());
			ChatHistories.Add(chatHistory);
		}

		foreach (var message in callback.Messages)
		{
			chatHistory.Messages.Add(new ChatMessage
			{
				SenderSteamID = message.SteamID.ConvertToUInt64(),
				Message = message.Message,
				Timestamp = message.Timestamp,
				Unread = message.Unread,
				PersonaState = steamFriends.GetFriendPersonaState(message.SteamID),
				GamePlayedID = (int)steamFriends.GetFriendGamePlayed(message.SteamID).AppID,
			});
		}
	}
}