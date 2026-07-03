public class User
{
	public ulong SteamID;
	public string AccountName;
	public string PersonaName;
	public string RefreshToken;
	public byte[] AvatarSHA;
	public User(ulong steamID, string accountName, string personaName, string refreshToken, byte[] avatarSHA)
	{
		SteamID = steamID;
		AccountName = accountName;
		PersonaName = personaName;
		RefreshToken = refreshToken;
		AvatarSHA = avatarSHA;
	}
}