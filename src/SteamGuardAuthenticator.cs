using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using KGUI;
using SteamKit2.Authentication;

public class SteamGuardAuthenticator : IAuthenticator
{
	private TaskCompletionSource<string>? currentCodeTask;
	private SteamGuardWindow? currentWindow;

	private static readonly ConcurrentQueue<Action> windowCreationQueue = new();

	/// <inheritdoc />
	public Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect)
	{
		Console.WriteLine("Received device code request...");
		currentCodeTask = new TaskCompletionSource<string>();

		windowCreationQueue.Enqueue(() =>
		{
			currentWindow = new SteamGuardWindow(Steam.Instance, "steam_guard_window");
			currentWindow.SetAuthenticator(this);
			currentWindow.SetDeviceCodeMode();
			WindowManager.Instance.CreateWindow(currentWindow);
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
		Console.WriteLine("Received email code request...");
		currentCodeTask = new TaskCompletionSource<string>();

		windowCreationQueue.Enqueue(() =>
		{
			currentWindow = new SteamGuardWindow(Steam.Instance, "steam_guard_window");
			currentWindow.SetAuthenticator(this);
			currentWindow.SetEmailMode(email);
			WindowManager.Instance.CreateWindow(currentWindow);
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
		Console.WriteLine("Received device confirmation request...");

		windowCreationQueue.Enqueue(() =>
		{
			currentWindow = new SteamGuardWindow(Steam.Instance, "steam_guard_window");
			currentWindow.SetAuthenticator(this);
			currentWindow.SetConfirmationMode();
			WindowManager.Instance.CreateWindow(currentWindow);
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