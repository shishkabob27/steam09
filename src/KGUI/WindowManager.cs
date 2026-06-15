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

		private List<BaseWindow> _pendingWindows = [];
		private List<BaseWindow> _pendingWindowsToRemove = [];
		private List<BaseWindow> _windows = [];

		public List<BaseWindow> GetWindows()
		{
			return _windows;
		}

		public void Update(float deltaTime)
		{
			foreach (BaseWindow window in _pendingWindows)
			{
				_windows.Add(window);
			}
			_pendingWindows.Clear();

			List<BaseWindow> ActualWindowsToRemove = new List<BaseWindow>();
			foreach (BaseWindow window in _pendingWindowsToRemove)
			{
				//check if window is already faded out
				if (window.windowOpacity <= 0.0f)
				{
					window.CloseWindow();
					_windows.Remove(window);
					ActualWindowsToRemove.Add(window);
					continue;
				}

				window.isFadingOut = true;
			}

			foreach (BaseWindow window in ActualWindowsToRemove)
			{
				_pendingWindowsToRemove.Remove(window);
			}

			foreach (BaseWindow window in _windows)
			{
				window?.Update(deltaTime);
			}
		}

		public void Draw()
		{
			foreach (BaseWindow window in _windows)
			{
				window?.PreDraw();
				window?.Draw();
				window?.PostDraw();
			}
		}

		public void CreateWindow(BaseWindow window)
		{
			_pendingWindows.Add(window);
		}

		public void CloseWindow(BaseWindow? window)
		{
			if (window == null)
			{
				return;
			}
			
			_pendingWindowsToRemove.Add(window);
		}

		public void CloseWindow<T>(string uuid = "") where T : BaseWindow
		{
			foreach (BaseWindow window in _windows)
			{
				if (window is T && (window.UUID == uuid || string.IsNullOrEmpty(uuid)))
				{
					CloseWindow(window);
				}
			}
		}

		public void HighlightWindow<T>(string uuid = "") where T : BaseWindow
		{
			foreach (BaseWindow window in _windows)
			{
				if (window is T && (window.UUID == uuid || string.IsNullOrEmpty(uuid)))
				{
					window.FocusWindow();
				}
			}
		}

		public bool IsWindowOpen<T>(string uuid) where T : BaseWindow
		{
			foreach (BaseWindow window in _windows)
			{
				if (window is T && (window.UUID == uuid || string.IsNullOrEmpty(uuid)))
				{
					return true;
				}
			}
			return false;
		}
	}
}