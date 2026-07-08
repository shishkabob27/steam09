namespace KGUI
{
	public class WindowManager
	{   
		public static WindowManager Instance 
		{ 
			get 
			{
				if (_instance == null)
				{
					_instance = new WindowManager();
				}
				return _instance;
			} 
		}
		private static WindowManager? _instance;

		private List<WindowEnvironment> _windowEnvironments = [];

		public void AddWindowEnvironment(WindowEnvironment windowEnvironment)
		{
			_windowEnvironments.Add(windowEnvironment);
		}

		public void RemoveWindowEnvironment(WindowEnvironment windowEnvironment)
		{
			_windowEnvironments.Remove(windowEnvironment);
		}

		public WindowEnvironment? GetWindowEnvironment(string uuid)
		{
			foreach (WindowEnvironment windowEnvironment in _windowEnvironments)
			{
				if (windowEnvironment.UUID == uuid)
				{
					return windowEnvironment;
				}
			}
			return null;
		}

		public void Update(float deltaTime)
		{
			foreach (WindowEnvironment windowEnvironment in _windowEnvironments)
			{
				windowEnvironment.Update(deltaTime);
			}
		}

		public void Draw()
		{
			foreach (WindowEnvironment windowEnvironment in _windowEnvironments)
			{
				windowEnvironment.Draw();
			}
		}

		public void CloseWindow<T>(string uuid = "") where T : BaseWindow
		{
			foreach (WindowEnvironment windowEnvironment in _windowEnvironments)
			{
				windowEnvironment.RemoveWindow<T>(uuid);
			}
		}

		public void HighlightWindow<T>(string uuid = "") where T : BaseWindow
		{
			foreach (WindowEnvironment windowEnvironment in _windowEnvironments)
			{
				windowEnvironment.HighlightWindow<T>(uuid);
			}
		}

		public bool IsWindowOpen<T>(string uuid) where T : BaseWindow
		{
			foreach (WindowEnvironment windowEnvironment in _windowEnvironments)
			{
				if (windowEnvironment.IsWindowOpen<T>(uuid))
				{
					return true;
				}
			}
			return false;
		}
	}
}