using Newtonsoft.Json;
using SteamKit2;

public partial class Steam
{
	public List<Friend> Friends = new List<Friend>();
	public List<ChatHistory> ChatHistories = new List<ChatHistory>();

	async void GetAvatars(List<ulong> steamIDs)
	{
		try
		{
			//see if an avatar already exists for each steamid
			List<ulong> steamIDsToRemove = new List<ulong>();
			foreach (ulong steamID in steamIDs)
			{
				if (File.Exists("config/avatarcache/" + steamID + "_32.jpg")) steamIDsToRemove.Add(steamID);
			}

			//remove the steamids that already have an avatar
			steamIDs.RemoveAll(steamID => steamIDsToRemove.Contains(steamID));

			if (steamIDs.Count == 0) return;

			string response;
			HttpClient client = new HttpClient();
			client.DefaultRequestHeaders.Add("User-Agent", "steam09");

			response = await client.GetStringAsync($"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={CurrentUser.WebAPIKey}&steamids={string.Join(",", steamIDs)}");

			//parse json
			dynamic avatars = JsonConvert.DeserializeObject(response);

			foreach (dynamic avatar in avatars.response.players)
			{
				string[] avatarURLs = new string[] { avatar.avatarfull, avatar.avatarmedium, avatar.avatar };
				foreach (string avatarURL in avatarURLs)
				{
					string size = "32";
					if (avatarURL.ToString() == avatar.avatarfull.ToString()) size = "184";
					if (avatarURL.ToString() == avatar.avatarmedium.ToString()) size = "64";

					//check if avatar is a valid url
					if (avatarURL == null || avatarURL == "") continue;

					string avatarUrl = avatarURL.ToString();
					if (!Uri.IsWellFormedUriString(avatarUrl, UriKind.Absolute)) continue;

					//send request to get avatar
					HttpClient avatarClient = new HttpClient();
					avatarClient.DefaultRequestHeaders.Add("User-Agent", "steam09");
					HttpResponseMessage ImageResponse = await avatarClient.GetAsync(avatarUrl);
					if (ImageResponse.IsSuccessStatusCode)
					{
						byte[] data = await ImageResponse.Content.ReadAsByteArrayAsync();
						//save to file
						File.WriteAllBytes($"config/avatarcache/{avatar.steamid}_{size}.jpg", data);
					}
				}
			}
		}
		catch (Exception e)
		{
			Console.WriteLine("Failed to get avatars: " + e.Message);
			return;
		}
	}

	public string GetAvatarPath(ulong steamID, AvatarSize size)
	{
		if (File.Exists($"config/avatarcache/{steamID}_{(int)size}.jpg")) return $"config/avatarcache/{steamID}_{(int)size}.jpg";

		//if not, return default avatar
		if (size == AvatarSize.Small) return "resources/graphics/avatar_32blank.png";
		else return "resources/graphics/avatar_64blank.png";
	}

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
			});

			steamFriends.RequestFriendInfo(friend.SteamID);
		}

		//Get avatars for all friends and self
		List<ulong> steamIDs = Friends.Select(f => f.SteamID).ToList();
		steamIDs.Add(CurrentUser.SteamID);
		GetAvatars(steamIDs);

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

			FriendsWindow friendsWindow = (FriendsWindow)Windows.Find(x => x is FriendsWindow);
			if (friendsWindow != null) friendsWindow.UpdateFriend(friend);

			//find chat window with friend
			ChatWindow chatWindow = (ChatWindow)Windows.Find(x => x is ChatWindow && ((ChatWindow)x).FriendSteamID == callback.FriendID.ConvertToUInt64());
			if (chatWindow != null)
			{
				chatWindow.UpdateFriendItemControl(friend);
			}
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

			FriendsWindow friendsWindow = (FriendsWindow)Windows.Find(x => x is FriendsWindow);
			if (friendsWindow != null) friendsWindow.UpdateFriend(friend);

			ChatWindow chatWindow = (ChatWindow)Windows.Find(x => x is ChatWindow && ((ChatWindow)x).FriendSteamID == friend.SteamID);
			if (chatWindow != null) chatWindow.UpdateFriendItemControl(friend);
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
		ChatWindow chatWindow = (ChatWindow)Windows.Find(x => x is ChatWindow && ((ChatWindow)x).FriendSteamID == callback.Sender.ConvertToUInt64());
		if (chatWindow != null)
		{
			chatWindow.CreateMessageControl(chatHistory.Messages.Last());
		}
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