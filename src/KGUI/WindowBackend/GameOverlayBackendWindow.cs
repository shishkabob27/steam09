using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL;

namespace KGUI
{
    public class GameOverlayBackendWindow : BackendWindow
    {
        uint _windowID = 0;
        string _mTitle = "";

        int _mWidth = 0;
        int _mHeight = 0;

        int _x = 0;
        int _y = 0;

        float _windowOpacity = 1.0f;

        private bool _resizable = false;
        private int _minimumWidth = 0;
        private int _minimumHeight = 0;
        public bool Resizable { get { return _resizable; } }
        
        public unsafe SDL_Renderer* _sdl3Renderer;
        public unsafe SDL_Texture* RenderTexture;
        

        public override uint GetInternalWindowID()
        {
            return _windowID;
        }

        public override void CreateWindow(BaseWindow window, string title, int width, int height, bool resizable, int minimumWidth, int minimumHeight, bool useCustomWindowDecorations, bool isPopupWindow)
        {
            this.Window = window;

            _mTitle = title;
            _mWidth = width;
            _mHeight = height;
        }

        public override void DestroyWindow()
        {
            // unsafe
            // {
			// 	SDL3.SDL_SetWindowHitTest(_window, null, nint.Zero);
			// 	if (_hitTestGCHandle.IsAllocated)
			// 		_hitTestGCHandle.Free();

            //     SDL3.SDL_DestroyRenderer(_sdl3Renderer);
            //     SDL3.SDL_DestroyWindow(_window);
            // }
        }

        public override void MinimizeWindow()
        {
            unsafe
            {
                //SDL3.SDL_MinimizeWindow(_window);
            }
        }

        public override void FocusWindow()
        {
            // unsafe
            // {
            //     SDL3.SDL_ShowWindow(_window);
            //     SDL3.SDL_RaiseWindow(_window);
            // }
        }

        public override void HideWindow()
        {
            unsafe
            {
              //  SDL3.SDL_HideWindow(_window);
            }
        }

        public override void SetWindowResizable(bool resizable, int minimumWidth = 0, int minimumHeight = 0)
        {
            unsafe
            {
                _resizable = resizable;
                // SDL3.SDL_SetWindowResizable(_window, resizable);
                // if (!resizable) return;
                // if (minimumWidth > 0 && minimumHeight > 0)
                // {
                //     SDL3.SDL_SetWindowMinimumSize(_window, minimumWidth, minimumHeight);
                // }
            }
        }

        public override void SetWindowPosition(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public override void SetWindowPositionCentered()
        {
            _x = (int)SDL3.SDL_WINDOWPOS_CENTERED;
            _y = (int)SDL3.SDL_WINDOWPOS_CENTERED;
        }

        public override void GetWindowPosition(out int x, out int y)
        {
            x = _x;
            y = _y;
        }

        public override void SetWindowSize(int width, int height, bool UpdateWindow = true)
        {
            unsafe
            {
                _mWidth = width;
                _mHeight = height;
                SDL3.SDL_DestroyTexture(RenderTexture);
                RenderTexture = SDL3.SDL_CreateTexture(_sdl3Renderer, SDL_PixelFormat.SDL_PIXELFORMAT_ARGB8888, SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, width, height);
            }
        }

        public override string GetTitle()
        {
            return _mTitle;
        }

        public override void SetTitle(string title)
		{
			_mTitle = title;
		}

        public override void SetWindowOpacity(float opacity)
        {
            _windowOpacity = opacity;
        }

        public override void BeginDraw()
        {
            unsafe
            {
                //ensure renderer targets the window
                SDL3.SDL_SetRenderTarget(_sdl3Renderer, RenderTexture);

                //set clear color and clear renderer
                SDL3.SDL_SetRenderDrawColor(_sdl3Renderer, 0, 0, 0, 0);
                SDL3.SDL_RenderClear(_sdl3Renderer);
            }
        }

        public override void EndDraw()
        {
            unsafe
            {
                SDL3.SDL_SetRenderTarget(_sdl3Renderer, null);
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