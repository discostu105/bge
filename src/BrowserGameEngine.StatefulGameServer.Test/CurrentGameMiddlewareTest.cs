using System;
using System.IO;
using System.Threading.Tasks;
using BrowserGameEngine.FrontendServer.Middleware;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class CurrentGameMiddlewareTest {
		private static GameRecordImmutable MakeRecord(string gameId, GameStatus status) {
			return new GameRecordImmutable(
				new GameId(gameId),
				"Test Game",
				"sco",
				status,
				DateTime.UtcNow,
				DateTime.UtcNow.AddDays(1),
				TimeSpan.FromSeconds(30)
			);
		}

		private static (CurrentGameMiddleware mw, HttpContext ctx, bool[] nextCalled) MakeMiddleware(string? headerValue) {
			var nextCalled = new[] { false };
			var mw = new CurrentGameMiddleware(_ => {
				nextCalled[0] = true;
				return Task.CompletedTask;
			});
			var ctx = new DefaultHttpContext();
			ctx.Response.Body = new MemoryStream();
			if (headerValue != null) ctx.Request.Headers[CurrentGameMiddleware.GameIdHeader] = headerValue;
			return (mw, ctx, nextCalled);
		}

		[Fact]
		public async Task InvokeAsync_NoHeader_PassesThrough() {
			var globalState = new GlobalState();
			var registry = new GameRegistry.GameRegistry(globalState);
			var (mw, ctx, nextCalled) = MakeMiddleware(headerValue: null);

			await mw.InvokeAsync(ctx, registry);

			Assert.True(nextCalled[0]);
			Assert.Equal(StatusCodes.Status200OK, ctx.Response.StatusCode);
			Assert.False(ctx.Items.ContainsKey(CurrentGameMiddleware.GameIdItemKey));
		}

		[Fact]
		public async Task InvokeAsync_UnknownGameId_Returns400() {
			var globalState = new GlobalState();
			var registry = new GameRegistry.GameRegistry(globalState);
			var (mw, ctx, nextCalled) = MakeMiddleware(headerValue: "does-not-exist");

			await mw.InvokeAsync(ctx, registry);

			Assert.False(nextCalled[0]);
			Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
		}

		[Fact]
		public async Task InvokeAsync_ActiveGameInGlobalState_PassesThrough() {
			var globalState = new GlobalState();
			globalState.AddGame(MakeRecord("active-game", GameStatus.Active));
			var registry = new GameRegistry.GameRegistry(globalState);
			var (mw, ctx, nextCalled) = MakeMiddleware(headerValue: "active-game");

			await mw.InvokeAsync(ctx, registry);

			Assert.True(nextCalled[0]);
			Assert.Equal(StatusCodes.Status200OK, ctx.Response.StatusCode);
			var stored = Assert.IsType<GameId>(ctx.Items[CurrentGameMiddleware.GameIdItemKey]!);
			Assert.Equal("active-game", stored.Id);
		}

		[Fact]
		public async Task InvokeAsync_FinishedGameNotInRegistry_PassesThrough() {
			// Finished games are evicted from the in-memory registry to free memory,
			// but their lobby/results endpoints (and any client navigation that sets the
			// X-Game-Id header) must still succeed. The middleware must validate against
			// the persisted game catalog (GlobalState), not the registry.
			var globalState = new GlobalState();
			globalState.AddGame(MakeRecord("finished-game", GameStatus.Finished));
			var registry = new GameRegistry.GameRegistry(globalState);
			// Intentionally do NOT register the instance — mirrors post-finalization state
			var (mw, ctx, nextCalled) = MakeMiddleware(headerValue: "finished-game");

			await mw.InvokeAsync(ctx, registry);

			Assert.True(nextCalled[0]);
			Assert.Equal(StatusCodes.Status200OK, ctx.Response.StatusCode);
			var stored = Assert.IsType<GameId>(ctx.Items[CurrentGameMiddleware.GameIdItemKey]!);
			Assert.Equal("finished-game", stored.Id);
		}
	}
}
