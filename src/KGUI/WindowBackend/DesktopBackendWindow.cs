using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL;

namespace KGUI
{
    public class DesktopBackendWindow : BackendWindow
    {
        private unsafe SDL_Window* _window;
        private SDL_WindowID _windowID;
        private unsafe SDL_Renderer* _sdl3Renderer;

        string _mTitle = "";

        int _mWidth = 0;
        int _mHeight = 0;

        unsafe delegate* unmanaged[Cdecl]<SDL_Window*, SDL_Point*, nint, SDL_HitTestResult> _hitTestCallbackDelegate;
		GCHandle _hitTestGCHandle;

		unsafe delegate* unmanaged[Cdecl]<IntPtr, SDL_Event*, SDLBool> _eventWatchCallbackDelegate;
		GCHandle _eventWatchGCHandle;

        public override uint GetInternalWindowID()
        {
            unsafe
            {
                return (uint)_windowID;
            }
        }

        public override void CreateWindow(BaseWindow window, string title, int width, int height, bool resizable, int minimumWidth, int minimumHeight, bool useCustomWindowDecorations, bool isPopupWindow)
        {
            this.Window = window;

            unsafe
            {
                SDL_WindowFlags windowFlags = SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS | SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS;
                if (useCustomWindowDecorations)
                {
                    windowFlags |= SDL_WindowFlags.SDL_WINDOW_BORDERLESS | SDL_WindowFlags.SDL_WINDOW_TRANSPARENT;
                }

                if (isPopupWindow)
                {
                    windowFlags |= SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP;
                }

                _mTitle = title;
                _mWidth = width;
                _mHeight = height;
                
                _window = SDL3.SDL_CreateWindow((Utf8String)title, width, height, windowFlags);

                if (window == null)
                {
                    Console.WriteLine("Failed to create window: " + SDL3.SDL_GetError());
                    return;
                }

                this._windowID = SDL3.SDL_GetWindowID(_window);
                    
                _sdl3Renderer = SDL3.SDL_CreateRenderer(_window, (byte*)null);
                if (_sdl3Renderer == null)
                {
                    Console.WriteLine("Failed to create renderer: " + SDL3.SDL_GetError());
                    return;
                }

                this.Renderer = new SDL3WindowRenderer(_sdl3Renderer);

                if (useCustomWindowDecorations && !isPopupWindow)
                {
                	_hitTestCallbackDelegate = &HitTestCallback;
                	_hitTestGCHandle = GCHandle.Alloc(this);
                	SDL3.SDL_SetWindowHitTest(_window, _hitTestCallbackDelegate, (nint)_hitTestGCHandle);
                }
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
		unsafe static SDL_HitTestResult HitTestCallback(SDL_Window* window, SDL_Point* point, nint data)
		{
			if (data == nint.Zero)
				return SDL_HitTestResult.SDL_HITTEST_NORMAL;

			GCHandle handle = GCHandle.FromIntPtr(data);
			DesktopBackendWindow instance = (DesktopBackendWindow)handle.Target!;

			int x = point->x;
			int y = point->y;

			// check minimize and close buttons (they should be normal so clicks work)
			SDL_FRect minimizeRect = new SDL_FRect { x = instance._mWidth - 33, y = 0, w = 15, h = 21 };
			SDL_FRect closeRect = new SDL_FRect { x = instance._mWidth - 18, y = 0, w = 15, h = 21 };
			
			if (x >= minimizeRect.x && y >= minimizeRect.y && x <= minimizeRect.x + minimizeRect.w && y <= minimizeRect.y + minimizeRect.h)
			{
				return SDL_HitTestResult.SDL_HITTEST_NORMAL;
			}
			
			if (x >= closeRect.x && y >= closeRect.y && x <= closeRect.x + closeRect.w && y <= closeRect.y + closeRect.h)
			{
				return SDL_HitTestResult.SDL_HITTEST_NORMAL;
			}

			// titlebar is draggable
			if (y < instance.Window.TitleBarHeight)
			{
				return SDL_HitTestResult.SDL_HITTEST_DRAGGABLE;
			}

			// check resize corners and edges if window is resizable
			bool isResizable = SDL3.SDL_GetWindowFlags(window).HasFlag(SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
			if (isResizable)
			{
				// corners
				if (x < instance.Window.WindowBorderSize && y < instance.Window.WindowBorderSize)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_TOPLEFT;
				if (x >= instance.GetWindowHeight() - instance.Window.WindowBorderSize && y < instance.Window.WindowBorderSize)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_TOPRIGHT;
				if (x < instance.Window.WindowBorderSize && y >= instance.GetWindowHeight() - instance.Window.WindowBorderSize)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_BOTTOMLEFT;
				if (x >= instance.GetWindowWidth() - instance.Window.WindowBorderSize && y >= instance.GetWindowHeight() - instance.Window.WindowBorderSize)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_BOTTOMRIGHT;

				if (x >= instance.GetWindowWidth() - 21 && y >= instance.GetWindowHeight() - 21)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_BOTTOMRIGHT;

				// edges
				if (x < instance.Window.WindowBorderSize)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_LEFT;
				if (x >= instance.GetWindowWidth() - instance.Window.WindowBorderSize)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_RIGHT;
				if (y < instance.Window.WindowBorderSize)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_TOP;
				if (y >= instance.GetWindowHeight() - instance.Window.WindowBorderSize)
					return SDL_HitTestResult.SDL_HITTEST_RESIZE_BOTTOM;
			}

			// rest of window is normal
			return SDL_HitTestResult.SDL_HITTEST_NORMAL;
		}

        public override void DestroyWindow()
        {
            unsafe
            {
				SDL3.SDL_SetWindowHitTest(_window, null, nint.Zero);
				if (_hitTestGCHandle.IsAllocated)
					_hitTestGCHandle.Free();

                SDL3.SDL_DestroyRenderer(_sdl3Renderer);
                SDL3.SDL_DestroyWindow(_window);
            }
        }

        public override void MinimizeWindow()
        {
            unsafe
            {
                SDL3.SDL_MinimizeWindow(_window);
            }
        }

        public override void FocusWindow()
        {
            unsafe
            {
                SDL3.SDL_ShowWindow(_window);
                SDL3.SDL_RaiseWindow(_window);
            }
        }

        public override void HideWindow()
        {
            unsafe
            {
                SDL3.SDL_HideWindow(_window);
            }
        }

        public override void SetWindowResizable(bool resizable, int minimumWidth = 0, int minimumHeight = 0)
        {
            unsafe
            {
                SDL3.SDL_SetWindowResizable(_window, resizable);
                if (!resizable) return;
                if (minimumWidth > 0 && minimumHeight > 0)
                {
                    SDL3.SDL_SetWindowMinimumSize(_window, minimumWidth, minimumHeight);
                }
            }
        }

        public override void SetWindowPosition(int x, int y)
        {
            unsafe
            {
                SDL3.SDL_SetWindowPosition(_window, x, y);
            }
        }

        public override void SetWindowPositionCentered()
        {
            unsafe
            {
                SDL3.SDL_SetWindowPosition(_window, (int)SDL3.SDL_WINDOWPOS_CENTERED, (int)SDL3.SDL_WINDOWPOS_CENTERED);
            }
        }

        public override void GetWindowPosition(out int x, out int y)
        {
            int _x, _y;
            unsafe
            {
                SDL3.SDL_GetWindowPosition(_window, &_x, &_y);
            }
            x = _x;
            y = _y;
        }
        

        public override void SetWindowSize(int width, int height, bool UpdateWindow = true)
        {
            unsafe
            {
                if (UpdateWindow) SDL3.SDL_SetWindowSize(_window, width, height);
                _mWidth = width;
                _mHeight = height;
            }
        }

        public override string GetTitle()
        {
            return _mTitle;
        }

        public override void SetTitle(string title)
		{
			_mTitle = title;
			unsafe
			{
				SDL3.SDL_SetWindowTitle(_window, (Utf8String)title);
			}
		}

        public override void SetWindowOpacity(float opacity)
        {
            unsafe
            {
                SDL3.SDL_SetWindowOpacity(_window, opacity);
            }
        }

        public override void BeginDraw()
        {
            unsafe
            {
                //ensure renderer targets the window
                SDL3.SDL_SetRenderTarget(_sdl3Renderer, null);

                //set clear color and clear renderer
                SDL3.SDL_SetRenderDrawColor(_sdl3Renderer, 0, 0, 0, 0);
                SDL3.SDL_RenderClear(_sdl3Renderer);
            }
        }

        public override void EndDraw()
        {
            unsafe
            {
                SDL3.SDL_RenderPresent(_sdl3Renderer);
            }
        }

        public override int GetWindowWidth()
        {
            return _mWidth;
        }

        public override int GetWindowHeight()
        {
            return _mHeight;
        }
    }
}