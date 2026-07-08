namespace KGUI
{
    public class WindowEnvironment
    {
        public string UUID = "";

        protected List<BaseWindow> _windows = [];
        protected List<BaseWindow> _pendingWindows = [];
        protected List<BaseWindow> _pendingWindowsToRemove = [];

		//backend type
		public enum BackendType
		{
			Desktop,
			GameOverlay,
		}

		public BackendType backendType = BackendType.Desktop;

        public virtual void Update(float deltaTime)
        {
			foreach (BaseWindow window in _pendingWindows)
			{
				_windows.Add(window);
			}
			_pendingWindows.Clear();

            List<BaseWindow> actualWindowsToRemove = [];
			foreach (BaseWindow window in _pendingWindowsToRemove)
			{
				//check if window is already faded out
				if (window.windowOpacity <= 0.0f)
				{
					window.backendWindow.DestroyWindow();
					_windows.Remove(window);
					actualWindowsToRemove.Add(window);
					continue;
				}

				window.isFadingOut = true;
			}

			foreach (BaseWindow window in actualWindowsToRemove)
			{
				_pendingWindowsToRemove.Remove(window);
			}

			foreach (BaseWindow window in _windows)
			{
				window?.Update(deltaTime);
			}
        }

        public virtual void Draw()
        {
            foreach (BaseWindow window in _windows)
            {
                window?.PreDraw();
                window?.Draw();
                window?.PostDraw();
            }
        }

		public virtual BackendWindow GetBackendWindow()
		{
			throw new NotImplementedException();
		}
        
        public virtual void AddWindow(BaseWindow window)
        {
			_pendingWindows.Add(window);
			window.Initialize(this, GetBackendWindow());
        }

        public virtual void RemoveWindow(BaseWindow window)
        {
            if (window == null)
			{
				return;
			}
			
			_pendingWindowsToRemove.Add(window);
        }

        public void RemoveWindow<T>(string uuid = "") where T : BaseWindow
		{
			foreach (BaseWindow window in _windows)
			{
				if (window is T && (window.UUID == uuid || string.IsNullOrEmpty(uuid)))
				{
					RemoveWindow(window);
				}
			}
		}

		public void HighlightWindow<T>(string uuid = "") where T : BaseWindow
		{
			foreach (BaseWindow window in _windows)
			{
				if (window is T && (window.UUID == uuid || string.IsNullOrEmpty(uuid)))
				{
					window.backendWindow.FocusWindow();
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

		public List<BaseWindow> GetWindows()
		{
			return [.. _windows];
		}
    }
}