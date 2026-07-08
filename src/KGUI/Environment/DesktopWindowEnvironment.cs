using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL;

namespace KGUI
{
	public class DesktopWindowEnvironment : WindowEnvironment
	{
		Dictionary<uint,GCHandle> _eventWatchGCHandles = [];
		unsafe delegate* unmanaged[Cdecl]<IntPtr, SDL_Event*, SDLBool> _eventWatchCallbackDelegate;

		public DesktopWindowEnvironment()
		{
			SDLEventsUtils.SubscribeToEvents(HandleEvent);
		}

		public override BackendWindow GetBackendWindow()
		{
			return new DesktopBackendWindow();
		}

		public override void AddWindow(BaseWindow window)
		{
			base.AddWindow(window);

			unsafe
			{
				_eventWatchCallbackDelegate = &EventWatchCallback;
				_eventWatchGCHandles[window.backendWindow.GetInternalWindowID()] = GCHandle.Alloc(window.backendWindow);
				SDL3.SDL_AddEventWatch(_eventWatchCallbackDelegate, GCHandle.ToIntPtr(_eventWatchGCHandles[window.backendWindow.GetInternalWindowID()]));
			}
		}

		public override void RemoveWindow(BaseWindow window)
		{
			base.RemoveWindow(window);

			if (_eventWatchGCHandles.ContainsKey(window.backendWindow.GetInternalWindowID()))
			{
				_eventWatchGCHandles[window.backendWindow.GetInternalWindowID()].Free();
				_eventWatchGCHandles.Remove(window.backendWindow.GetInternalWindowID());
			}
		}

		public override void Update(float deltaTime)
		{
			SDLEventsUtils.ProcessEvents();

			base.Update(deltaTime);
		}

		private void HandleEvent(SDL_Event e)
		{
			foreach (BaseWindow window in GetWindows())
			{
				if ((uint)e.window.windowID != window.backendWindow.GetInternalWindowID()) continue;
				if (e.type >= (uint)SDL_EventType.SDL_EVENT_WINDOW_FIRST && e.type <= (uint)SDL_EventType.SDL_EVENT_WINDOW_LAST)
				{
					switch (e.window.type)
					{
						case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
							if (e.window.data1 <= 0 || e.window.data2 <= 0) break; //ignore minimize events that set size to 0,0
							window.backendWindow.SetWindowSize(e.window.data1, e.window.data2, false);
							window.backendWindow.EndDraw();
							break;

						case SDL_EventType.SDL_EVENT_WINDOW_DISPLAY_CHANGED:
							if (e.window.data1 <= 0 || e.window.data2 <= 0) break; //ignore minimize events that set size to 0,0
							window.backendWindow.SetWindowSize(e.window.data1, e.window.data2, false);
							window.backendWindow.EndDraw();
							break;

						//Repaint on expose
						case SDL_EventType.SDL_EVENT_WINDOW_EXPOSED:
							window.backendWindow.EndDraw();
							break;

						//Mouse enter
						case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER:
							window.MouseFocus = true;
							break;

						//Mouse exit
						case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
							window.MouseFocus = false;
							break;

						//Keyboard focus gained
						case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
							window.KeyboardFocus = true;
							break;

						//Keyboard focus lost
						case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
							window.KeyboardFocus = false;
							break;

						case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
							RemoveWindow(window);
							break;
					}
				}
				else
				{
					switch (e.Type)
					{
						case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
							window.OnMouseMotion((int)e.motion.x, (int)e.motion.y);
							break;
						case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:	
							window.OnMouseUp((int)e.motion.x, (int)e.motion.y, (int)e.button.button);
							break;
						case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
							window.OnMouseScroll((int)e.wheel.x, (int)e.wheel.y);
							break;
						case SDL_EventType.SDL_EVENT_KEY_DOWN:
							window.OnKeyDown(e.key.key, e.key.mod);
							break;
						case SDL_EventType.SDL_EVENT_KEY_UP:
							window.OnKeyUp(e.key.key, e.key.mod);
							break;
					}
				}
			}
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
		unsafe static SDLBool EventWatchCallback(IntPtr userdata, SDL_Event* sdlEvent)
		{
			BackendWindow? backendWindow = GCHandle.FromIntPtr(userdata).Target as BackendWindow;
			if (backendWindow == null) return true;
			if (backendWindow.GetInternalWindowID() != (uint)sdlEvent->window.windowID) return true;

			switch ((SDL_EventType)sdlEvent->type)
			{
				case SDL_EventType.SDL_EVENT_WINDOW_SAFE_AREA_CHANGED:
				case SDL_EventType.SDL_EVENT_WINDOW_EXPOSED:
					backendWindow.Window.ForceUpdateDrawLoop();
					break;				
				case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
				case SDL_EventType.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
					backendWindow.SetWindowSize(sdlEvent->window.data1, sdlEvent->window.data2, false);
					break;
			}
			return true;
		}
	}
}