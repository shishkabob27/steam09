using SDL_Sharp;
using SteamKit2;

public class ChatMessageControl : UIControl
{

	string senderPersonaName;
	string message;

	public EPersonaState PersonaState;
	public int GamePlayedID;

	public ChatMessageControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, ulong senderSteamID, string message, EPersonaState personaState, int gamePlayedID, int width = 0, int height = 0) : base(parent, renderer, controlName, x, y, width, height)
	{
		this.senderPersonaName = Steam.Instance.GetPersonaName(senderSteamID);
		this.message = message;
		this.PersonaState = personaState;
		this.GamePlayedID = gamePlayedID;
	}

	public override void Draw()
	{
		base.Draw();

		Color nameColor;
		if (PersonaState == EPersonaState.Offline) nameColor = new Color(156, 158, 157, 255);
		else if (GamePlayedID != 0) nameColor = new Color(191, 224, 142, 255);
		else nameColor = new Color(128, 163, 185, 255);

		int nameWidth = parent.steamFont8.MeasureText(senderPersonaName);

		parent.DrawText(senderPersonaName, x + 9, y + 5, nameColor);
		parent.DrawText($": {message}", x + 9 + nameWidth, y + 5, new Color(255, 255, 255, 255));
	}
}