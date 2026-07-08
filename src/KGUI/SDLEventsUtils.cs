using SDL;

public static class SDLEventsUtils
{
    private unsafe static List<Action<SDL_Event>> eventHandlers = new List<Action<SDL_Event>>();

    public static void SubscribeToEvents(Action<SDL_Event> eventHandler)
    {
        eventHandlers.Add(eventHandler);
    }

    public static void UnsubscribeFromEvents(Action<SDL_Event> eventHandler)
    {
        eventHandlers.Remove(eventHandler);
    }

    public static void ProcessEvents()
    {
        unsafe
        {
            SDL_Event e;
            while (SDL3.SDL_PollEvent(&e))
            {
                foreach (var handler in eventHandlers)
                {
                    handler(e);
                }
            }
        }
    }
}