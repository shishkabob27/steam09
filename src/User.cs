public class User
{
	public ulong SteamID;
	public string AccountName;
	public string PersonaName;
	public string WebAPIKey;
	public string RefreshToken;

	public User(ulong steamID, string accountName, string personaName, string webAPIKey, string refreshToken)
	{
		SteamID = steamID;
		AccountName = accountName;
		PersonaName = personaName;
		WebAPIKey = webAPIKey;
		RefreshToken = refreshToken;
	}
}