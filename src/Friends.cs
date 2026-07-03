using SteamKit2;

public class Friend
{
	public ulong SteamID;
	public string PersonaName;
	public EPersonaState PersonaState;
	public EFriendRelationship Relationship;
	public byte[] AvatarSHA;
	public string GamePlayedName;
	public uint GamePlayed;
	public DateTime LastOnline;
}