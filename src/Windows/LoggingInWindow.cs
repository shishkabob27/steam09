using KGUI;

public class LoggingInWindow : SteamWindow
{
	public LoggingInWindow(Steam steam, string uuid) : base(steam, uuid)
	{
		panel.GetControlByID<LabelControl>("loginText").text = Localization.GetString("Steam_LaunchingSteam").Replace("%s1", steam.CurrentUser.AccountName);
	}
}