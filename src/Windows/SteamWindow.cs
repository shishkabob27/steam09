using KGUI;

public class SteamWindow : BaseWindow
{
	public Steam client;
	public SteamWindow(Steam client, string uuid) : base(uuid)
	{
		this.client = client;
	}
}