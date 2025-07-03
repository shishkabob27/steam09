using Newtonsoft.Json;
using SteamKit2;

public partial class Steam
{
	List<User> GetPreviousLoginUsers()
	{
		List<User> users = new List<User>();

		//check if config/loginusers.vdf exists
		if (File.Exists("config/loginusers.json"))
		{
			users = JsonConvert.DeserializeObject<List<User>>(File.ReadAllText("config/loginusers.json"));
		}
		else
		{
			File.WriteAllText("config/loginusers.json", "[]");
		}

		return users;
	}

	void AddLoginUser(User user)
	{
		List<User> users = GetPreviousLoginUsers();

		//check if user already exists
		if (users.Find(u => u.SteamID == user.SteamID) != null)
		{
			Console.WriteLine("User already exists");
			return;
		}

		users.Add(user);
		File.WriteAllText("config/loginusers.json", JsonConvert.SerializeObject(users, Formatting.Indented));
	}

	void RemoveLoginUser(User user)
	{
		List<User> users = GetPreviousLoginUsers();
		users.Remove(user);
		File.WriteAllText("config/loginusers.json", JsonConvert.SerializeObject(users, Formatting.Indented));
	}

	public void ModifyLoginUser(User user)
	{
		//read file
		string usersJson = File.ReadAllText("config/loginusers.json");
		dynamic users = JsonConvert.DeserializeObject(usersJson);

		//find user
		bool found = false;
		foreach (dynamic u in users)
		{
			if (u.SteamID == user.SteamID)
			{
				u.PersonaName = user.PersonaName;
				u.RefreshToken = user.RefreshToken;
				u.WebAPIKey = user.WebAPIKey;
				found = true;
				break;
			}
		}

		if (!found)
		{
			AddLoginUser(user);
			return;
		}

		//write file
		File.WriteAllText("config/loginusers.json", JsonConvert.SerializeObject(users, Formatting.Indented));
	}

	bool AttemptCachedLogin()
	{
		List<User> users = GetPreviousLoginUsers();
		if (users.Count == 0)
		{
			return false;
		}

		User user = users[0];
		CurrentUser = user;

		manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
		steamClient.Connect();

		return true;
	}

	public void OnConnected(SteamClient.ConnectedCallback callback)
	{
		Console.WriteLine("Connected to Steam, logging in");
		steamUser.LogOn(new SteamUser.LogOnDetails
		{
			Username = CurrentUser.AccountName,
			AccessToken = CurrentUser.RefreshToken,
		});
	}

	void OnDisconnected(SteamClient.DisconnectedCallback callback)
	{
		Console.WriteLine("Disconnected from Steam!");
	}

	void OnLoggedOn(SteamUser.LoggedOnCallback callback)
	{
		if (callback.Result != EResult.OK)
		{
			//TODO: handle gracefully
			Console.WriteLine("Failed to log on to Steam: " + callback.Result);
			Environment.Exit(0);
		}

		Console.WriteLine("Logged on to Steam! SteamID64: " + steamUser.SteamID.ConvertToUInt64());

		CurrentUser.SteamID = steamUser.SteamID.ConvertToUInt64();
		ModifyLoginUser(CurrentUser);

		if (!CheckWebApiKey())
		{
			PendingWindows.Add(new RequestWebAPIKeyWindow(this, $"STEAM - {CurrentUser.AccountName}", 300, 150, false));
			return;
		}

		ContinueLogin();
	}

	bool CheckWebApiKey()
	{
		if (CurrentUser.WebAPIKey == null || CurrentUser.WebAPIKey == "")
		{
			Console.WriteLine("No web api key found, asking user to enter it");
			return false;
		}
		return true;
	}

	public void ContinueLogin()
	{
		//create logging in window
		PendingWindows.Add(new LoggingInWindow(this, "STEAM", 400, 100, false));

		//get games
		GetGames();
	}

	void OnLoggedOff(SteamUser.LoggedOffCallback callback)
	{
		Console.WriteLine("Logged off from Steam!");
	}

	void OnAccountInfo(SteamUser.AccountInfoCallback callback)
	{
		CurrentUser.PersonaName = callback.PersonaName;
		ModifyLoginUser(CurrentUser);

		steamFriends.SetPersonaState(EPersonaState.Online);
	}
}