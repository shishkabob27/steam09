using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using SteamKit2.Authentication;

public class SteamGuardAuthenticator : IAuthenticator
{
	private TaskCompletionSource<string>? currentCodeTask;
	private SteamGuardWindow? currentWindow;

	// Thread-safe queue for window creation requests
	private static readonly ConcurrentQueue<Action> windowCreationQueue = new();

	/// <inheritdoc />
	public Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect)
	{
		currentCodeTask = new TaskCompletionSource<string>();

		// Queue window creation to be executed on main thread
		windowCreationQueue.Enqueue(() =>
		{
			currentWindow = new SteamGuardWindow(Steam.Instance, "Steam Guard", 300, 150);
			currentWindow.SetAuthenticator(this);
			currentWindow.SetDeviceCodeMode();
			Steam.Instance.PendingWindows.Add(currentWindow);
		});

		if (previousCodeWasIncorrect)
		{
			Console.Error.WriteLine("The previous 2-factor auth code you have provided is incorrect.");
		}

		return currentCodeTask.Task;
	}

	/// <inheritdoc />
	public Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
	{
		currentCodeTask = new TaskCompletionSource<string>();

		// Queue window creation to be executed on main thread
		windowCreationQueue.Enqueue(() =>
		{
			currentWindow = new SteamGuardWindow(Steam.Instance, "Steam Guard", 300, 150);
			currentWindow.SetAuthenticator(this);
			currentWindow.SetEmailMode(email);
			Steam.Instance.PendingWindows.Add(currentWindow);
		});

		if (previousCodeWasIncorrect)
		{
			Console.Error.WriteLine("The previous 2-factor auth code you have provided is incorrect.");
		}

		return currentCodeTask.Task;
	}

	/// <inheritdoc />
	public Task<bool> AcceptDeviceConfirmationAsync()
	{
		// Queue window creation to show user they need to approve on mobile
		windowCreationQueue.Enqueue(() =>
		{
			currentWindow = new SteamGuardWindow(Steam.Instance, "Steam Guard", 300, 150);
			currentWindow.SetAuthenticator(this);
			currentWindow.SetConfirmationMode();
			Steam.Instance.PendingWindows.Add(currentWindow);
		});

		return Task.FromResult(true);
	}

	public void OnCodeEntered(string code)
	{
		currentCodeTask?.SetResult(code);
		currentCodeTask = null;
		currentWindow = null;
	}

	public void OnCodeCancelled()
	{
		currentCodeTask?.SetException(new OperationCanceledException("Steam Guard authentication was cancelled"));
		currentCodeTask = null;
		currentWindow = null;
	}

	public void OnConfirmationCancelled()
	{
		currentWindow = null;
	}

	public static void ProcessWindowCreationQueue()
	{
		while (windowCreationQueue.TryDequeue(out Action? windowCreation))
		{
			windowCreation?.Invoke();
		}
	}
}