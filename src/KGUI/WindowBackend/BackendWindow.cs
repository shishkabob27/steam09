namespace KGUI
{
    public abstract class BackendWindow
    {
        public WindowRenderer Renderer;
        public BaseWindow Window;

        public abstract uint GetInternalWindowID();
        public abstract void CreateWindow(BaseWindow window, string title, int width, int height, bool resizable, int minimumWidth, int minimumHeight, bool useCustomWindowDecorations, bool isPopupWindow);
        public abstract void DestroyWindow();
        public abstract void MinimizeWindow();
        public abstract void FocusWindow();
        public abstract void HideWindow();
        public abstract void SetWindowResizable(bool resizable, int minimumWidth = 0, int minimumHeight = 0);
        public abstract void SetWindowPosition(int x, int y);
        public abstract void SetWindowPositionCentered();
        public abstract void GetWindowPosition(out int x, out int y);
        public abstract void SetWindowSize(int width, int height, bool UpdateWindow = true);
        public abstract string GetTitle();
        public abstract void SetTitle(string title);
        public abstract void SetWindowOpacity(float opacity);
        public abstract void BeginDraw();
        public abstract void EndDraw();

        public abstract int GetWindowWidth();
        public abstract int GetWindowHeight();
    }
}