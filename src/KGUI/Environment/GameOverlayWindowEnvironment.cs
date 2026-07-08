using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL;

namespace KGUI
{
	public class GameOverlayWindowEnvironment : WindowEnvironment
	{
		unsafe SDL_Window* _gameOverlayWindow;
		unsafe SDL_Renderer* _gameOverlayRenderer;

		SDL_WindowID _windowID = 0;

		uint windowAccumulate = 0;

		int envWidth = 1280;
		int envHeight = 720;

		int _mouseX = 0;
		int _mouseY = 0;

		BaseWindow? _focusedWindow = null;

		bool grabbedTitlebar = false;
		bool grabbedResizer = false;
		int grabbedTitlebarOffsetX = 0;
		int grabbedTitlebarOffsetY = 0;
		int grabbedResizerOffsetX = 0;
		int grabbedResizerOffsetY = 0;

		public GameOverlayWindowEnvironment()
		{
			unsafe
			{
				_gameOverlayWindow = SDL3.SDL_CreateWindow("Game Overlay", envWidth, envHeight, SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS | SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
				_gameOverlayRenderer = SDL3.SDL_CreateRenderer(_gameOverlayWindow, (byte*)null);
				_windowID = SDL3.SDL_GetWindowID(_gameOverlayWindow);
			}

			SDLEventsUtils.SubscribeToEvents(HandleEvent);
		}

		public override BackendWindow GetBackendWindow()
		{
			return new GameOverlayBackendWindow();
		}

		public override void AddWindow(BaseWindow window)
		{
			base.AddWindow(window);
			_focusedWindow = window;
			windowAccumulate++;
			unsafe
			{
				GameOverlayBackendWindow gameOverlayBackendWindow = window.backendWindow as GameOverlayBackendWindow;
				gameOverlayBackendWindow._sdl3Renderer = _gameOverlayRenderer;
				gameOverlayBackendWindow.Renderer = new SDL3WindowRenderer(_gameOverlayRenderer);
				gameOverlayBackendWindow.RenderTexture = SDL3.SDL_CreateTexture(_gameOverlayRenderer, SDL_PixelFormat.SDL_PIXELFORMAT_ARGB8888, SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, window.backendWindow.GetWindowWidth(), window.backendWindow.GetWindowHeight());
			}
			
		}

		public override void Update(float deltaTime)
		{
			base.Update(deltaTime);

			SDLEventsUtils.ProcessEvents();

			foreach (BaseWindow window in _windows)
			{
				if (window == null) continue;
				window.backendWindow.GetWindowPosition(out int windowX, out int windowY);

				int relX = _mouseX - windowX;
				int relY = _mouseY - windowY;
				window.OnMouseMotion(relX, relY);

				if (windowX == (int)SDL3.SDL_WINDOWPOS_CENTERED && windowY == (int)SDL3.SDL_WINDOWPOS_CENTERED)
				{
					int newX = envWidth/2 - (window.backendWindow.GetWindowWidth() / 2);
					int newY = envHeight/2 - (window.backendWindow.GetWindowHeight() / 2);
					window.backendWindow.SetWindowPosition(newX, newY);
				}
			}	
		}

		private void HandleEvent(SDL_Event e)
		{
			unsafe
			{
				BaseWindow? mMouseOverWindow = null;
				for (int i = _windows.Count - 1; i >= 0; i--)
				{
					if (_windows[i] == null) continue;
					BaseWindow window = _windows[i];
					window.backendWindow.GetWindowPosition(out int x, out int y);
					if (_mouseX >= x && _mouseX <= x + window.backendWindow.GetWindowWidth() && _mouseY >= y && _mouseY <= y + window.backendWindow.GetWindowHeight())
					{
						mMouseOverWindow = window;
						break;
					}
				}
		
			
				switch (e.Type)
				{
					case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
						envWidth = (int)e.window.data1;
						envHeight = (int)e.window.data2;
						break;

					case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
						_mouseX = (int)e.motion.x;
						_mouseY = (int)e.motion.y;

						if (_focusedWindow != null)
						{
							if (grabbedTitlebar)
							{
								_focusedWindow.backendWindow.SetWindowPosition(_mouseX - grabbedTitlebarOffsetX, _mouseY - grabbedTitlebarOffsetY);
							}
							if (grabbedResizer)
							{
								_focusedWindow.backendWindow.GetWindowPosition(out int windowX, out int windowY);
								int relX = _mouseX + grabbedResizerOffsetX - windowX;
								int relY = _mouseY + grabbedResizerOffsetY - windowY;
								_focusedWindow.backendWindow.SetWindowSize(relX, relY, false);
							}
						}

						break;
					case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
						if (mMouseOverWindow != _focusedWindow && mMouseOverWindow != null)
						{
							_focusedWindow = mMouseOverWindow;

							_windows.Remove(_focusedWindow);
							_windows.Add(_focusedWindow);
						}

						if (_focusedWindow != null)
						{
							_focusedWindow.backendWindow.GetWindowPosition(out int windowX, out int windowY);
							int relX = _mouseX - windowX;
							int relY = _mouseY - windowY;

							//inbounds of window
							if (relX > 0 && relX < _focusedWindow.backendWindow.GetWindowWidth() && relY > 0 && relY < _focusedWindow.backendWindow.GetWindowHeight())
							{

								if (relY < 20)
								{
									grabbedTitlebarOffsetX = relX;
									grabbedTitlebarOffsetY = relY;
									grabbedTitlebar = true;
								}
								else if (relX > _focusedWindow.backendWindow.GetWindowWidth() - 21 && relY > _focusedWindow.backendWindow.GetWindowHeight() - 21)
								{
									grabbedResizerOffsetX = _focusedWindow.backendWindow.GetWindowWidth() - relX;
									grabbedResizerOffsetY = _focusedWindow.backendWindow.GetWindowHeight() - relY;
									grabbedResizer = true;
								}
							}

							_focusedWindow.OnMouseDown(relX, relY, (int)e.button.button);
						}
						break;
					case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:	
						if (_focusedWindow != null)
						{
							grabbedTitlebar = false;
							grabbedResizer = false;
							
							_focusedWindow.backendWindow.GetWindowPosition(out int windowX, out int windowY);
							int relX = _mouseX - windowX;
							int relY = _mouseY - windowY;
							_focusedWindow.OnMouseUp(relX, relY, (int)e.button.button);
						}
						break;
					case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
						_focusedWindow?.OnMouseScroll((int)e.wheel.x, (int)e.wheel.y);
						break;
					case SDL_EventType.SDL_EVENT_KEY_DOWN:
						_focusedWindow?.OnKeyDown(e.key.key, e.key.mod);
						break;
					case SDL_EventType.SDL_EVENT_KEY_UP:
						_focusedWindow?.OnKeyUp(e.key.key, e.key.mod);
						break;
				}
			}
		}

		public override void Draw()
		{
			base.Draw();

			unsafe
			{
				SDL3.SDL_SetRenderDrawColor(_gameOverlayRenderer, 0, 0, 0, 255);
				SDL3.SDL_RenderClear(_gameOverlayRenderer);

				foreach (BaseWindow window in _windows)
				{
					if (window == null) continue;

					window.backendWindow.GetWindowPosition(out int x, out int y);
					SDL_FRect rect = new SDL_FRect() { x = x, y = y, w = window.backendWindow.GetWindowWidth(), h = window.backendWindow.GetWindowHeight() };

					SDL3.SDL_SetTextureBlendMode((window.backendWindow as GameOverlayBackendWindow).RenderTexture, SDL_BlendMode.SDL_BLENDMODE_BLEND);
					SDL3.SDL_SetTextureAlphaMod((window.backendWindow as GameOverlayBackendWindow).RenderTexture, (byte)(window.windowOpacity * 255));
					SDL3.SDL_RenderTexture(_gameOverlayRenderer, (window.backendWindow as GameOverlayBackendWindow).RenderTexture, null, &rect);
				}
				SDL3.SDL_RenderPresent(_gameOverlayRenderer);
			}
		}
	}
}