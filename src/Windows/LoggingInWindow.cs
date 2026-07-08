using KGUI;
using KGUI.Controls;

public class LoggingInWindow : SteamWindow
{
	public LoggingInWindow(Steam steam, string uuid) : base(steam, uuid) {}

	public override void PostInitialize()
	{
		panel.GetControlByID<LabelControl>("loginText").text = Localization.GetString("Steam_LaunchingSteam").Replace("%s1", client.CurrentUser.AccountName);
	}
}