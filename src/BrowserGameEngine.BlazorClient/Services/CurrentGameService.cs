using BrowserGameEngine.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserGameEngine.BlazorClient.Services {
	public interface ICurrentGameService {
		GameDetailViewModel? CurrentGame { get; }
		string? CurrentGameId { get; }
		event Action? OnChanged;
		Task RefreshAsync();
	}

	public class CurrentGameService : ICurrentGameService, IDisposable {
		private readonly HttpClient http;
		private readonly NavigationManager nav;
		private static readonly Regex GameIdPattern = new(@"^games/([^/]+)/", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public GameDetailViewModel? CurrentGame { get; private set; }
		public string? CurrentGameId { get; private set; }
		public event Action? OnChanged;

		public CurrentGameService(HttpClient http, NavigationManager nav) {
			this.http = http;
			this.nav = nav;
			this.nav.LocationChanged += OnLocationChanged;
			_ = LoadFromUri(nav.Uri);
		}

		private async void OnLocationChanged(object? sender, LocationChangedEventArgs args) {
			await LoadFromUri(args.Location);
		}

		private async Task LoadFromUri(string uri) {
			var relative = nav.ToBaseRelativePath(uri);
			var match = GameIdPattern.Match(relative);
			var gameId = match.Success ? match.Groups[1].Value : null;

			if (gameId == CurrentGameId) return;

			CurrentGameId = gameId;
			if (gameId != null) {
				try {
					CurrentGame = await http.GetFromJsonAsync<GameDetailViewModel>($"api/games/{gameId}");
				} catch {
					CurrentGame = null;
				}
			} else {
				CurrentGame = null;
			}

			OnChanged?.Invoke();
		}

		public async Task RefreshAsync() {
			if (CurrentGameId == null) return;
			try {
				CurrentGame = await http.GetFromJsonAsync<GameDetailViewModel>($"api/games/{CurrentGameId}");
				OnChanged?.Invoke();
			} catch { }
		}

		public void Dispose() {
			nav.LocationChanged -= OnLocationChanged;
		}
	}
}
