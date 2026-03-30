using System.Text.Json;
using Microsoft.JSInterop;

namespace BrowserGameEngine.BlazorClient.Code;

public class TutorialService
{
	private readonly IJSRuntime _js;
	private bool _loaded;
	private bool _dismissed;
	private bool[] _steps = new bool[5];

	public event Action? OnChanged;

	public bool IsLoaded => _loaded;
	public bool IsDismissed => _dismissed;
	public IReadOnlyList<bool> Steps => _steps;
	public int CompletedCount => _steps.Count(s => s);

	public TutorialService(IJSRuntime js)
	{
		_js = js;
	}

	public async Task LoadAsync()
	{
		if (_loaded) return;
		var dismissed = await GetLocalStorage("bge_tutorial_completed");
		_dismissed = dismissed == "true";
		var stepsJson = await GetLocalStorage("bge_tutorial_steps");
		if (!string.IsNullOrEmpty(stepsJson))
		{
			try
			{
				var arr = JsonSerializer.Deserialize<bool[]>(stepsJson);
				if (arr != null && arr.Length == 5)
					_steps = arr;
			}
			catch { }
		}
		_loaded = true;
	}

	public async Task MarkStepAsync(int stepIndex)
	{
		if (stepIndex < 0 || stepIndex >= 5) return;
		await LoadAsync();
		if (_steps[stepIndex]) return;
		_steps[stepIndex] = true;
		await SetLocalStorage("bge_tutorial_steps", JsonSerializer.Serialize(_steps));
		OnChanged?.Invoke();
	}

	public async Task DismissAsync()
	{
		await LoadAsync();
		_dismissed = true;
		await SetLocalStorage("bge_tutorial_completed", "true");
		OnChanged?.Invoke();
	}

	public async Task ShowAsync()
	{
		await LoadAsync();
		_dismissed = false;
		await SetLocalStorage("bge_tutorial_completed", "false");
		OnChanged?.Invoke();
	}

	private ValueTask<string?> GetLocalStorage(string key)
		=> _js.InvokeAsync<string?>("localStorage.getItem", key);

	private ValueTask SetLocalStorage(string key, string value)
		=> _js.InvokeVoidAsync("localStorage.setItem", key, value);
}
