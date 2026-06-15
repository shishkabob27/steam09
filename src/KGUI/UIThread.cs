using System.Collections.Concurrent;
using System.Threading;

namespace KGUI
{
	public static class UIThread
	{
		static readonly ConcurrentQueue<Action> pendingActions = new();
		static int mainThreadId;

		public static void Initialize()
		{
			mainThreadId = Thread.CurrentThread.ManagedThreadId;
		}

		public static void Invoke(Action action)
		{
			if (Thread.CurrentThread.ManagedThreadId == mainThreadId)
			{
				action();
				return;
			}

			pendingActions.Enqueue(action);
		}

		public static void ProcessPending()
		{
			while (pendingActions.TryDequeue(out var action))
			{
				action();
			}
		}
	}
}