using System.Runtime.CompilerServices;
using SDL_Sharp;
using SDL_Sharp.Ttf;
using SteamKit2;

public class ChatWindow : SteamWindow
{
	public ulong FriendSteamID;
	FriendItemControl FriendItemControl;

	ListControl MessageListControl;

	TextEntryControl MessageInputControl;
	ButtonControl SendMessageButtonControl;

	public ChatWindow(Steam steam, string title, int width, int height, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0, Friend friend = null) : base(steam, title, width, height, resizable, minimumWidth, minimumHeight)
	{
		FriendSteamID = friend.SteamID;

		FriendItemControl = new FriendItemControl(panel, renderer, "friendItemControl", 9, 30, friend.SteamID, 200, 48);
		FriendItemControl.XOffset = 0;
		FriendItemControl.DrawBackground = false;
		FriendItemControl.SteamID = friend.SteamID;
		FriendItemControl.PersonaName = friend.PersonaName;
		FriendItemControl.PersonaState = friend.PersonaState;
		FriendItemControl.GamePlayedID = (int)friend.GamePlayed;
		FriendItemControl.GamePlayedName = friend.GamePlayedName;
		FriendItemControl.LastOnline = friend.LastOnline;

		panel.AddControl(FriendItemControl);

		unsafe
		{
			Surface* FriendAvatarSurface = SDL_Sharp.Image.IMG.Load(Steam.Instance.GetAvatarPath(friend.SteamID, AvatarSize.Small));
			FriendItemControl.AvatarTexture = SDL.CreateTextureFromSurface(renderer, FriendAvatarSurface);
			SDL.FreeSurface(FriendAvatarSurface);

			Surface* AvatarBorderSurface = SDL_Sharp.Image.IMG.Load("resources/graphics/avatar_border.png");
			FriendItemControl.AvatarBorderTexture = SDL.CreateTextureFromSurface(renderer, AvatarBorderSurface);
			SDL.FreeSurface(AvatarBorderSurface);
		}

		MessageListControl = new ListControl(panel, renderer, "messageListControl", 9, 132, 1, 1);
		panel.AddControl(MessageListControl);

		MessageInputControl = new TextEntryControl(panel, renderer, "messageInputControl", 9, 0, 1, 44);
		panel.AddControl(MessageInputControl);

		SendMessageButtonControl = new ButtonControl(panel, renderer, "sendMessageButtonControl", 9, 0, 56, 44, "Send", 1);
		panel.AddControl(SendMessageButtonControl);

		SendMessageButtonControl.OnClick = () =>
		{
			SendMessage(MessageInputControl.text);
		};

		MessageInputControl.OnEnterPressed = () =>
		{
			SendMessage(MessageInputControl.text);
		};

		LoadChatHistory();
	}

	public void UpdateFriendItemControl(Friend friend)
	{
		FriendItemControl.PersonaName = friend.PersonaName;
		FriendItemControl.PersonaState = friend.PersonaState;
		FriendItemControl.GamePlayedID = (int)friend.GamePlayed;
		FriendItemControl.GamePlayedName = friend.GamePlayedName;
		FriendItemControl.LastOnline = friend.LastOnline;
	}

	public void LoadChatHistory()
	{
		ChatHistory? chatHistory = Steam.Instance.ChatHistories.Find(x => x.SteamID == FriendSteamID);
		if (chatHistory != null)
		{
			foreach (var message in chatHistory.Messages)
			{
				CreateMessageControl(message);
			}
		}

		MessageListControl.ScrollToBottom();
	}

	public void SendMessage(string message)
	{
		if (string.IsNullOrWhiteSpace(message)) return;
		Steam.Instance.steamFriends.SendChatMessage(FriendSteamID, EChatEntryType.ChatMsg, message);
		MessageInputControl.text = "";

		ChatMessage chatMessage = new ChatMessage
		{
			SenderSteamID = Steam.Instance.CurrentUser.SteamID,
			Message = message,
			Timestamp = DateTime.Now,
			Unread = false,
			PersonaState = EPersonaState.Online, // TODO: Get persona state from current user
			GamePlayedID = 0,
		};

		CreateMessageControl(chatMessage);

		ChatHistory chatHistory = Steam.Instance.ChatHistories.Find(x => x.SteamID == FriendSteamID);
		if (chatHistory != null)
		{
			chatHistory.Messages.Add(chatMessage);
		}
	}

	public void CreateMessageControl(ChatMessage message)
	{
		bool isAtBottom = MessageListControl.IsAtBottom();

		MessageListControl.Children.Add(new ChatMessageControl(panel, renderer, "chatMessageControl", 0, 0, message.SenderSteamID, message.Message, message.PersonaState, message.GamePlayedID, height: 18));

		if (isAtBottom)
		{
			MessageListControl.ScrollToBottom();
		}
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);

		MessageListControl.width = mWidth - 18;
		MessageListControl.height = mHeight - 82 - MessageListControl.y;

		MessageInputControl.y = mHeight - 74;
		MessageInputControl.width = mWidth - 73 - 9;

		SendMessageButtonControl.x = mWidth - 65;
		SendMessageButtonControl.y = mHeight - 74;
	}

	public override void Draw()
	{
		base.Draw();

		//friend background
		panel.DrawBox(9, 30, mWidth - 18, 48, new Color(58, 58, 58, 255));

		FriendItemControl.Draw();

		//background
		panel.DrawBox(MessageListControl.x, MessageListControl.y, MessageListControl.width, MessageListControl.height, new Color(37, 37, 37, 255));
		MessageListControl.Draw();

		//message input
		MessageInputControl.Draw();
		SendMessageButtonControl.Draw();

		SDL.RenderPresent(renderer);
	}
}