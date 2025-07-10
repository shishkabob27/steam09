using System.Runtime.InteropServices;

public class TaskIcon
{
    const int WM_USER = 0x0400;
    const int WM_TRAYMESSAGE = WM_USER + 1;
    const int WM_DESTROY = 0x0002;
    const uint NIM_ADD = 0x00;
    const uint NIM_DELETE = 0x02;
    const uint NIF_MESSAGE = 0x01;
    const uint NIF_ICON = 0x02;
    const uint NIF_TIP = 0x04;
    const int WM_LBUTTONUP = 0x0202;
    const int WM_RBUTTONUP = 0x0205;
    const int WM_COMMAND = 0x0111;

    // menu item ids
    const int MENU_SHOW_LIBRARY = 1001;
    const int MENU_QUIT_STEAM = 1002;

    delegate IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WNDCLASS
    {
        public uint style;
        public WindowProc lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int x, y; }

    [DllImport("user32.dll", SetLastError = true)]
    static extern short RegisterClass(ref WNDCLASS lpWndClass);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA pnid);

    [DllImport("user32.dll")]
    static extern bool PeekMessage(out MSG msg, IntPtr hWnd, uint min, uint max, uint wRemoveMsg);

    [DllImport("user32.dll")]
    static extern bool TranslateMessage(ref MSG msg);

    [DllImport("user32.dll")]
    static extern IntPtr DispatchMessage(ref MSG msg);

    [DllImport("user32.dll")]
    static extern void PostQuitMessage(int code);

    [DllImport("user32.dll")]
    static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    const uint PM_REMOVE = 0x0001;
    const uint MF_STRING = 0x00000000;
    const uint TPM_RIGHTBUTTON = 0x0002;
    const uint TPM_RETURNCMD = 0x0100;

    // loadimage constants
    const uint IMAGE_ICON = 1;
    const uint LR_LOADFROMFILE = 0x00000010;
    const uint LR_DEFAULTSIZE = 0x00000040;

    static IntPtr hwnd;
    static NOTIFYICONDATA nid;
    static bool isInitialized = false;
    static IntPtr currentIcon = IntPtr.Zero;

    public void Initialize()
    {
        if (isInitialized) return;

        string className = "TrayWndClass";

        WNDCLASS wc = new WNDCLASS
        {
            lpfnWndProc = MyWndProc,
            lpszClassName = className,
        };

        RegisterClass(ref wc);
        hwnd = CreateWindowEx(0, className, "HiddenTrayWnd", 0, 0, 0, 0, 0,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        LoadIconFromFile("resources/taskicon/steam_tray.ico");

        nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = hwnd,
            uID = 1,
            uFlags = NIF_ICON | NIF_MESSAGE | NIF_TIP,
            uCallbackMessage = WM_TRAYMESSAGE,
            hIcon = currentIcon,
            szTip = "Steam09"
        };

        Shell_NotifyIcon(NIM_ADD, ref nid);
        isInitialized = true;
    }

    public bool LoadIconFromFile(string iconPath)
    {
        if (!File.Exists(iconPath))
        {
            Console.WriteLine($"Icon file not found: {iconPath}");
            currentIcon = LoadIcon(IntPtr.Zero, new IntPtr(32512)); // IDI_APPLICATION
            return false;
        }

        IntPtr newIcon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_DEFAULTSIZE);
        
        if (newIcon == IntPtr.Zero)
        {
            Console.WriteLine($"Failed to load icon: {iconPath}");
            currentIcon = LoadIcon(IntPtr.Zero, new IntPtr(32512)); // IDI_APPLICATION
            return false;
        }

        if (currentIcon != IntPtr.Zero && currentIcon != LoadIcon(IntPtr.Zero, new IntPtr(32512)))
        {
            DestroyIcon(currentIcon);
        }

        currentIcon = newIcon;
        return true;
    }

    public bool ChangeIcon(string iconPath)
    {
        if (!isInitialized) return false;

        if (!LoadIconFromFile(iconPath)) return false;

        nid.hIcon = currentIcon;
        return Shell_NotifyIcon(NIM_ADD, ref nid);
    }

    public void ProcessMessages()
    {
        if (!isInitialized) return;

        MSG msg;
        while (PeekMessage(out msg, IntPtr.Zero, 0, 0, PM_REMOVE))
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }

    public void Cleanup()
    {
        if (isInitialized)
        {
            Shell_NotifyIcon(NIM_DELETE, ref nid);
            
            if (currentIcon != IntPtr.Zero && currentIcon != LoadIcon(IntPtr.Zero, new IntPtr(32512)))
            {
                DestroyIcon(currentIcon);
                currentIcon = IntPtr.Zero;
            }
            
            isInitialized = false;
        }
    }

    static void ShowPopupMenu()
    {
        IntPtr hMenu = CreatePopupMenu();
        
        AppendMenu(hMenu, MF_STRING, MENU_SHOW_LIBRARY, "My Games");
        AppendMenu(hMenu, MF_STRING, MENU_QUIT_STEAM, "Quit");

        GetCursorPos(out POINT pt);
        SetForegroundWindow(hwnd);

        int cmd = TrackPopupMenu(hMenu, TPM_RIGHTBUTTON | TPM_RETURNCMD, pt.x, pt.y, 0, hwnd, IntPtr.Zero);
        
        DestroyMenu(hMenu);

        if (cmd == MENU_SHOW_LIBRARY)
        {
            Steam.Instance?.ShowMainWindow();
        }
        else if (cmd == MENU_QUIT_STEAM)
        {
            Steam.Instance?.QuitApplication();
        }
    }

    static IntPtr MyWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_TRAYMESSAGE)
        {
            if ((int)lParam == WM_LBUTTONUP || (int)lParam == WM_RBUTTONUP)
            {
                ShowPopupMenu();
            }
        }
        else if (msg == WM_DESTROY)
        {
            PostQuitMessage(0);
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }
}
