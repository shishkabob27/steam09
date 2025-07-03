using SteamKit2;

public class ChatHistory
{
	public ulong SteamID;

	public List<ChatMessage> Messages = new List<ChatMessage>();

	public ChatHistory(ulong steamID)
	{
		SteamID = steamID;
	}
}

public class ChatMessage
{
	public ulong SenderSteamID;
	public string Message = "";
	public DateTime Timestamp = DateTime.MinValue;
	public bool Unread = false;
	public EPersonaState PersonaState;
	public int GamePlayedID;
}