# Request-scoped Game Context Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the bug where every game URL (`/games/<id>/base`) shows the same data because all singleton repositories read from the *default* game's `WorldState`. Make HTTP API calls game-aware via an `X-Game-Id` request header.

**Architecture:** Introduce a small `HttpContextWorldStateAccessor` singleton that, on each `WorldState` access, looks at `HttpContext.Items["BGE.GameId"]` (set by a new `CurrentGameMiddleware` from the `X-Game-Id` header) and returns the matching `GameInstance.WorldState` from `GameRegistry`. When no header is present (background tick threads, anonymous endpoints) it falls back to the default instance — preserving every current behavior. The React axios client adds the `X-Game-Id` header automatically by parsing `/games/:gameId/...` from the URL. No repositories or controllers need to change; they all see correct data once the accessor resolves dynamically.

**Tech Stack:** ASP.NET Core 9, xUnit, React 18 + Vite + TypeScript, axios, Vitest, React Router v6.

**Out of scope (separate issues, do not touch in this PR):**
- Per-instance `GameTickEngine` (currently one shared engine wired to all instances — separate latent issue noted in `Program.cs:168` `Phase 1: single game`).
- `BearerTokenMiddleware` API-key lookup (also reads default game's players — same fix pattern but different surface area).
- SignalR hub game scoping.
- Spectator / replay endpoints (already construct accessors per-instance manually).

---

## File Structure

**Create:**
- `src/BrowserGameEngine.FrontendServer/Middleware/CurrentGameMiddleware.cs` — reads `X-Game-Id` header, validates the game exists in `GameRegistry`, stores `GameId` in `HttpContext.Items["BGE.GameId"]`. Skips silently when header absent.
- `src/BrowserGameEngine.FrontendServer/HttpContextWorldStateAccessor.cs` — replaces the static singleton accessor. Resolves `WorldState` per call from `HttpContext.Items["BGE.GameId"]` via `GameRegistry`, falls back to default instance.
- `src/BrowserGameEngine.StatefulGameServer.Test/Integration/MultiGameIsolationIntegrationTest.cs` — proves resources for game A and game B are isolated when called with different `X-Game-Id` headers.
- `src/ReactClient/test/lib/gameIdHeader.test.ts` — unit test for the URL → header extraction.
- `src/ReactClient/src/lib/gameIdFromPath.ts` — small helper extracting `gameId` from `/games/:gameId/...` paths.

**Modify:**
- `src/BrowserGameEngine.StatefulGameServer/GameServerExtensions.cs` — drop the singleton `IWorldStateAccessor` registration that binds to the default instance. The accessor will be registered in `FrontendServer` instead so that it can use `IHttpContextAccessor` (which lives only in the web app).
- `src/BrowserGameEngine.FrontendServer/Program.cs` — register `IHttpContextAccessor`, register `IWorldStateAccessor → HttpContextWorldStateAccessor`, plug `CurrentGameMiddleware` into the pipeline before `CurrentUserMiddleware`.
- `src/BrowserGameEngine.StatefulGameServer/Repositories/User/UserRepository.cs` — switch from raw `WorldState` to `IWorldStateAccessor` so cookie/oauth login resolves the right game's player.
- `src/ReactClient/src/api/client.ts` — add request interceptor injecting `X-Game-Id` from URL path.

---

## Task 1: Add `gameIdFromPath` helper + tests (frontend)

**Files:**
- Create: `src/ReactClient/src/lib/gameIdFromPath.ts`
- Create: `src/ReactClient/test/lib/gameIdHeader.test.ts`

- [ ] **Step 1.1: Write the failing test**

```typescript
// src/ReactClient/test/lib/gameIdHeader.test.ts
import { describe, it, expect } from 'vitest'
import { gameIdFromPath } from '@/lib/gameIdFromPath'

describe('gameIdFromPath', () => {
  it('extracts gameId from a game-scoped path', () => {
    expect(gameIdFromPath('/games/abc123/base')).toBe('abc123')
    expect(gameIdFromPath('/games/abc123/units')).toBe('abc123')
    expect(gameIdFromPath('/games/97ad41885175/enemybase/p1')).toBe('97ad41885175')
  })

  it('returns null when path is not game-scoped', () => {
    expect(gameIdFromPath('/')).toBeNull()
    expect(gameIdFromPath('/games')).toBeNull()
    expect(gameIdFromPath('/games/')).toBeNull()
    expect(gameIdFromPath('/profile')).toBeNull()
    expect(gameIdFromPath('/admin/games')).toBeNull()
  })
})
```

- [ ] **Step 1.2: Run test — verify it fails**

Run from `src/ReactClient`: `npm test -- lib/gameIdHeader`

Expected: FAIL with import error (`gameIdFromPath` not found).

- [ ] **Step 1.3: Write minimal implementation**

```typescript
// src/ReactClient/src/lib/gameIdFromPath.ts
const GAME_PATH_PATTERN = /^\/games\/([^/]+)(?:\/|$)/

export function gameIdFromPath(pathname: string): string | null {
  const m = GAME_PATH_PATTERN.exec(pathname)
  if (!m) return null
  const id = m[1]
  if (!id) return null
  return id
}
```

- [ ] **Step 1.4: Run test — verify pass**

Run from `src/ReactClient`: `npm test -- lib/gameIdHeader`

Expected: PASS (2 passing).

- [ ] **Step 1.5: Commit**

```
git add src/ReactClient/src/lib/gameIdFromPath.ts src/ReactClient/test/lib/gameIdHeader.test.ts
git commit -m "feat(client): add gameIdFromPath helper for URL parsing"
```

---

## Task 2: axios request interceptor injects `X-Game-Id` header

**Files:**
- Modify: `src/ReactClient/src/api/client.ts`
- Modify: `src/ReactClient/test/lib/gameIdHeader.test.ts` (extend with interceptor behavior)

- [ ] **Step 2.1: Add failing tests for the interceptor**

Append to `src/ReactClient/test/lib/gameIdHeader.test.ts`:

```typescript
import { afterEach } from 'vitest'
import apiClient from '@/api/client'
import type { InternalAxiosRequestConfig } from 'axios'

describe('apiClient X-Game-Id interceptor', () => {
  const originalLocation = window.location

  function setPath(pathname: string) {
    Object.defineProperty(window, 'location', {
      configurable: true,
      writable: true,
      value: { ...originalLocation, pathname },
    })
  }

  afterEach(() => {
    Object.defineProperty(window, 'location', {
      configurable: true,
      writable: true,
      value: originalLocation,
    })
  })

  function makeConfig(): InternalAxiosRequestConfig {
    return {
      headers: new (apiClient.defaults.headers.constructor as any)(),
    } as unknown as InternalAxiosRequestConfig
  }

  it('adds X-Game-Id header when on a game-scoped page', async () => {
    setPath('/games/abc123/base')
    const handler = apiClient.interceptors.request.handlers[0]
    const config = await handler.fulfilled(makeConfig())
    expect(config.headers['X-Game-Id']).toBe('abc123')
  })

  it('does not add X-Game-Id header on non-game pages', async () => {
    setPath('/profile')
    const handler = apiClient.interceptors.request.handlers[0]
    const config = await handler.fulfilled(makeConfig())
    expect(config.headers['X-Game-Id']).toBeUndefined()
  })
})
```

Also update the top-of-file import to include `afterEach`:

```typescript
import { describe, it, expect, afterEach } from 'vitest'
```

- [ ] **Step 2.2: Run test — verify failures (interceptor not yet present)**

From `src/ReactClient`: `npm test -- lib/gameIdHeader`

Expected: 2 new tests fail because the interceptor doesn't exist yet.

- [ ] **Step 2.3: Implement the request interceptor**

```typescript
// src/ReactClient/src/api/client.ts
import axios from 'axios'
import { gameIdFromPath } from '@/lib/gameIdFromPath'

const apiClient = axios.create({
  headers: {
    'X-Requested-With': 'XMLHttpRequest',
  },
})

apiClient.interceptors.request.use((config) => {
  const gameId = gameIdFromPath(window.location.pathname)
  if (gameId) {
    config.headers.set('X-Game-Id', gameId)
  }
  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      const returnUrl = encodeURIComponent(window.location.pathname + window.location.search)
      window.location.href = `/signin?returnUrl=${returnUrl}`
    }
    return Promise.reject(error)
  }
)

export default apiClient
```

- [ ] **Step 2.4: Run test — verify all pass**

From `src/ReactClient`: `npm test -- lib/gameIdHeader`

Expected: 4 passing.

- [ ] **Step 2.5: Commit**

```
git add src/ReactClient/src/api/client.ts src/ReactClient/test/lib/gameIdHeader.test.ts
git commit -m "feat(client): inject X-Game-Id header from current URL"
```

---

## Task 3: Add `CurrentGameMiddleware` (backend)

**Files:**
- Create: `src/BrowserGameEngine.FrontendServer/Middleware/CurrentGameMiddleware.cs`

- [ ] **Step 3.1: Write the middleware**

```csharp
// src/BrowserGameEngine.FrontendServer/Middleware/CurrentGameMiddleware.cs
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameRegistry;

namespace BrowserGameEngine.FrontendServer.Middleware {
	/// <summary>
	/// Reads the X-Game-Id request header and stores the corresponding GameId in
	/// HttpContext.Items so HttpContextWorldStateAccessor can resolve the right
	/// WorldState. Silently ignores missing headers (callers that don't need a
	/// game context — e.g. /api/games or unauthenticated pages — work as before).
	/// Returns 400 when the header is set but malformed/unknown.
	/// </summary>
	public class CurrentGameMiddleware {
		public const string GameIdHeader = "X-Game-Id";
		public const string GameIdItemKey = "BGE.GameId";

		private readonly RequestDelegate next;

		public CurrentGameMiddleware(RequestDelegate next) {
			this.next = next;
		}

		public async Task InvokeAsync(HttpContext context, GameRegistry gameRegistry) {
			var headerValue = context.Request.Headers[GameIdHeader].FirstOrDefault();
			if (!string.IsNullOrEmpty(headerValue)) {
				var gameId = new GameId(headerValue);
				var instance = gameRegistry.TryGetInstance(gameId);
				if (instance == null) {
					context.Response.StatusCode = StatusCodes.Status400BadRequest;
					await context.Response.WriteAsync($"Unknown game id: {headerValue}");
					return;
				}
				context.Items[GameIdItemKey] = gameId;
			}
			await next(context);
		}
	}
}
```

- [ ] **Step 3.2: Build to confirm it compiles**

From `src/`: `dotnet build BrowserGameEngine.FrontendServer/BrowserGameEngine.FrontendServer.csproj`

Expected: Build succeeded.

- [ ] **Step 3.3: Commit**

```
git add src/BrowserGameEngine.FrontendServer/Middleware/CurrentGameMiddleware.cs
git commit -m "feat(server): add CurrentGameMiddleware reading X-Game-Id header"
```

---

## Task 4: Add `HttpContextWorldStateAccessor` (backend)

**Files:**
- Create: `src/BrowserGameEngine.FrontendServer/HttpContextWorldStateAccessor.cs`

- [ ] **Step 4.1: Write the accessor**

```csharp
// src/BrowserGameEngine.FrontendServer/HttpContextWorldStateAccessor.cs
using BrowserGameEngine.FrontendServer.Middleware;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;

namespace BrowserGameEngine.FrontendServer {
	/// <summary>
	/// Resolves the current request's WorldState dynamically. When the current
	/// HttpContext has a GameId set by CurrentGameMiddleware, returns that
	/// game's WorldState. Otherwise falls back to the default GameInstance —
	/// preserving the previous singleton behaviour for code paths that have no
	/// HttpContext (background hosted services, tick engine).
	/// </summary>
	public class HttpContextWorldStateAccessor : IWorldStateAccessor {
		private readonly IHttpContextAccessor httpContextAccessor;
		private readonly GameRegistry gameRegistry;

		public HttpContextWorldStateAccessor(IHttpContextAccessor httpContextAccessor, GameRegistry gameRegistry) {
			this.httpContextAccessor = httpContextAccessor;
			this.gameRegistry = gameRegistry;
		}

		public WorldState WorldState {
			get {
				var ctx = httpContextAccessor.HttpContext;
				if (ctx != null && ctx.Items.TryGetValue(CurrentGameMiddleware.GameIdItemKey, out var raw) && raw is GameId gameId) {
					var instance = gameRegistry.TryGetInstance(gameId);
					if (instance != null) return instance.WorldState;
				}
				return gameRegistry.GetDefaultInstance().WorldState;
			}
		}
	}
}
```

- [ ] **Step 4.2: Build**

From `src/`: `dotnet build BrowserGameEngine.FrontendServer/BrowserGameEngine.FrontendServer.csproj`

Expected: Build succeeded.

- [ ] **Step 4.3: Commit**

```
git add src/BrowserGameEngine.FrontendServer/HttpContextWorldStateAccessor.cs
git commit -m "feat(server): add HttpContextWorldStateAccessor"
```

---

## Task 5: Wire the new accessor + middleware into Program.cs

**Files:**
- Modify: `src/BrowserGameEngine.StatefulGameServer/GameServerExtensions.cs`
- Modify: `src/BrowserGameEngine.FrontendServer/Program.cs`

- [ ] **Step 5.1: Remove the default-bound IWorldStateAccessor registration**

In `src/BrowserGameEngine.StatefulGameServer/GameServerExtensions.cs`, **delete** the line:

```csharp
services.AddSingleton<IWorldStateAccessor>(defaultInstance.WorldStateAccessor);
```

(currently line 25). Leave the rest of the file untouched.

- [ ] **Step 5.2: Register `IHttpContextAccessor` and the new accessor in Program.cs**

In `src/BrowserGameEngine.FrontendServer/Program.cs`, immediately after the `await ConfigureGameServices(builder.Services);` call (around line 161), add:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<BrowserGameEngine.StatefulGameServer.IWorldStateAccessor,
	BrowserGameEngine.FrontendServer.HttpContextWorldStateAccessor>();
```

- [ ] **Step 5.3: Plug `CurrentGameMiddleware` into the pipeline**

In `src/BrowserGameEngine.FrontendServer/Program.cs`, find this block:

```csharp
app.UseMiddleware<BearerTokenMiddleware>();
app.UseRateLimiter();
app.UseMiddleware<CurrentUserMiddleware>();
```

Insert `CurrentGameMiddleware` immediately before `CurrentUserMiddleware` so the player lookup runs in the right game context:

```csharp
app.UseMiddleware<BearerTokenMiddleware>();
app.UseRateLimiter();
app.UseMiddleware<BrowserGameEngine.FrontendServer.Middleware.CurrentGameMiddleware>();
app.UseMiddleware<CurrentUserMiddleware>();
```

- [ ] **Step 5.4: Build & run existing tests to make sure nothing breaks**

From `src/`: `dotnet build && dotnet test`

Expected: full build succeeded, all existing tests pass. (We removed the singleton DI binding but added a replacement that resolves the same default game when no header is present, so behavior is identical for existing tests.)

- [ ] **Step 5.5: Commit**

```
git add src/BrowserGameEngine.StatefulGameServer/GameServerExtensions.cs src/BrowserGameEngine.FrontendServer/Program.cs
git commit -m "feat(server): wire HttpContextWorldStateAccessor + CurrentGameMiddleware"
```

---

## Task 6: Migrate `UserRepository` to use `IWorldStateAccessor`

**Files:**
- Modify: `src/BrowserGameEngine.StatefulGameServer/Repositories/User/UserRepository.cs`
- Modify: `src/BrowserGameEngine.StatefulGameServer.Test/TestGame/TestGame.cs` (and any other build-error sites)

`UserRepository` currently takes a raw `WorldState` (the singleton-registered default game) which makes the cookie/oauth login path always pick a player from the default game even after the accessor change. We migrate it to `IWorldStateAccessor` so it resolves dynamically too.

- [ ] **Step 6.1: Switch the constructor**

```csharp
// src/BrowserGameEngine.StatefulGameServer/Repositories/User/UserRepository.cs
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class UserRepository {
		private readonly GlobalState globalState;
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public UserRepository(GlobalState globalState, IWorldStateAccessor worldStateAccessor) {
			this.globalState = globalState;
			this.worldStateAccessor = worldStateAccessor;
		}

		public UserImmutable? GetByGithubId(string githubId) {
			if (globalState.Users.TryGetValue(githubId, out var user)) {
				return user.ToImmutable();
			}
			return null;
		}

		public bool ExistsByGithubId(string githubId) {
			return globalState.Users.ContainsKey(githubId);
		}

		public IEnumerable<PlayerImmutable> GetPlayersForUser(string userId) {
			return world.Players.Values
				.Where(p => p.UserId == userId)
				.Select(p => p.ToImmutable());
		}

		public PlayerImmutable? GetPlayerByApiKeyHash(string apiKeyHash) {
			var player = world.Players.Values.FirstOrDefault(p => p.ApiKeyHash == apiKeyHash);
			return player?.ToImmutable();
		}

		public UserImmutable? GetByUserId(string userId) {
			var user = globalState.Users.Values.FirstOrDefault(u => u.UserId == userId);
			return user?.ToImmutable();
		}

		public string? GetDisplayNameByUserId(string userId) {
			var user = globalState.Users.Values.FirstOrDefault(u => u.UserId == userId);
			return user?.DisplayName;
		}
	}
}
```

- [ ] **Step 6.2: Locate failing call sites**

From `src/`: `dotnet build`

The build will fail at sites that pass `WorldState` directly to `new UserRepository(...)`. The DI registration is constructor-resolved so it picks up the change automatically.

- [ ] **Step 6.3: Update test call sites**

In `src/BrowserGameEngine.StatefulGameServer.Test/TestGame/TestGame.cs`, replace `new UserRepository(GlobalState, WorldState)` with `new UserRepository(GlobalState, Accessor)` (the `Accessor` field already exists). Apply the same change in any other caller flagged by the build error.

- [ ] **Step 6.4: Build & test**

From `src/`: `dotnet build && dotnet test`

Expected: all green.

- [ ] **Step 6.5: Commit**

```
git add src/BrowserGameEngine.StatefulGameServer/Repositories/User/UserRepository.cs src/BrowserGameEngine.StatefulGameServer.Test/TestGame/TestGame.cs
git commit -m "refactor(server): UserRepository takes IWorldStateAccessor for game scoping"
```

---

## Task 7: Integration test — multi-game isolation

**Files:**
- Create: `src/BrowserGameEngine.StatefulGameServer.Test/Integration/MultiGameIsolationIntegrationTest.cs`

- [ ] **Step 7.1: Write the failing test**

```csharp
// src/BrowserGameEngine.StatefulGameServer.Test/Integration/MultiGameIsolationIntegrationTest.cs
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test.Integration {
	/// <summary>
	/// Verifies that GET /api/resources scoped via the X-Game-Id header returns
	/// each game's own resources. Regression test for the bug where every game
	/// URL showed identical data because all repositories read the singleton-bound
	/// default WorldState.
	/// </summary>
	public class MultiGameIsolationIntegrationTest : IntegrationTestBase {
		public MultiGameIsolationIntegrationTest(BgeWebApplicationFactory factory) : base(factory) { }

		[Fact]
		public async Task Resources_AreScopedByGameIdHeader() {
			var userId = $"isolation-user-{Guid.NewGuid():N}";

			var creatorClient = CreateClient(userId);
			var gameAId = await CreateGameAsync(creatorClient, "iso-A");
			var gameBId = await CreateGameAsync(creatorClient, "iso-B");

			var registry = Factory.Services.GetRequiredService<GameRegistry>();
			var instanceA = registry.TryGetInstance(new GameId(gameAId))!;
			var instanceB = registry.TryGetInstance(new GameId(gameBId))!;

			var playerIdA = AddPlayerToGame(instanceA, userId, "Alice");
			var playerIdB = AddPlayerToGame(instanceB, userId, "Alice");

			SetMineralsForPlayer(instanceA, playerIdA, 1234);
			SetMineralsForPlayer(instanceB, playerIdB, 5678);

			var clientA = CreateClient(userId);
			clientA.DefaultRequestHeaders.Add("X-Game-Id", gameAId);
			var resA = await clientA.GetFromJsonAsync<PlayerResourcesViewModel>("/api/resources", JsonOptions);

			var clientB = CreateClient(userId);
			clientB.DefaultRequestHeaders.Add("X-Game-Id", gameBId);
			var resB = await clientB.GetFromJsonAsync<PlayerResourcesViewModel>("/api/resources", JsonOptions);

			Assert.Equal(1234, GetMinerals(resA!));
			Assert.Equal(5678, GetMinerals(resB!));
		}

		[Fact]
		public async Task UnknownGameIdHeader_Returns400() {
			var userId = $"isolation-user-{Guid.NewGuid():N}";
			var client = CreateClient(userId);
			client.DefaultRequestHeaders.Add("X-Game-Id", "this-game-does-not-exist");

			var response = await client.GetAsync("/api/resources");
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task CookieLogin_SelectsPlayerInCurrentGame() {
			var userId = $"cookie-user-{Guid.NewGuid():N}";

			var creatorClient = CreateClient(userId);
			var gameAId = await CreateGameAsync(creatorClient, "cookie-A");
			var gameBId = await CreateGameAsync(creatorClient, "cookie-B");

			var registry = Factory.Services.GetRequiredService<GameRegistry>();
			var instanceA = registry.TryGetInstance(new GameId(gameAId))!;
			var instanceB = registry.TryGetInstance(new GameId(gameBId))!;

			var playerA = AddPlayerToGame(instanceA, userId, "Alice-A");
			var playerB = AddPlayerToGame(instanceB, userId, "Alice-B");
			SetMineralsForPlayer(instanceA, playerA, 11);
			SetMineralsForPlayer(instanceB, playerB, 22);

			var clientA = CreateClient(userId);
			clientA.DefaultRequestHeaders.Add("X-Game-Id", gameAId);
			var resA = await clientA.GetFromJsonAsync<PlayerResourcesViewModel>("/api/resources", JsonOptions);
			Assert.Equal(11, GetMinerals(resA!));

			var clientB = CreateClient(userId);
			clientB.DefaultRequestHeaders.Add("X-Game-Id", gameBId);
			var resB = await clientB.GetFromJsonAsync<PlayerResourcesViewModel>("/api/resources", JsonOptions);
			Assert.Equal(22, GetMinerals(resB!));
		}

		private async Task<string> CreateGameAsync(System.Net.Http.HttpClient client, string name) {
			var body = new {
				Name = $"{name}-{Guid.NewGuid():N}",
				GameDefType = "sco",
				StartTime = DateTime.UtcNow.AddMinutes(-5),
				EndTime = DateTime.UtcNow.AddDays(1),
				TickDuration = "00:00:10",
				MaxPlayers = 8,
				DiscordWebhookUrl = (string?)null,
				Settings = (object?)null,
			};
			var resp = await client.PostAsJsonAsync("/api/games", body, JsonOptions);
			resp.EnsureSuccessStatusCode();
			var summary = await DeserializeAsync<GameSummaryViewModel>(resp);
			return summary!.GameId;
		}

		private static PlayerId AddPlayerToGame(GameInstance instance, string userId, string playerName) {
			var pid = PlayerIdFactory.Create(Guid.NewGuid().ToString());
			var raceId = instance.GameDef.PlayerTypes[0].Id.Id;
			var write = new PlayerRepositoryWrite(instance.WorldStateAccessor, TimeProvider.System);
			write.TryCreatePlayer(pid, userId, raceId, instance.Record.MaxPlayers);
			write.ChangePlayerName(new BrowserGameEngine.StatefulGameServer.Commands.ChangePlayerNameCommand(pid, playerName));
			return pid;
		}

		private static void SetMineralsForPlayer(GameInstance instance, PlayerId playerId, int amount) {
			var player = instance.WorldState.Players[playerId];
			player.State.Resources[Id.ResDef("minerals")] = amount;
		}

		private static int GetMinerals(PlayerResourcesViewModel vm) {
			vm.SecondaryResources.Cost.TryGetValue("minerals", out var v);
			return v;
		}
	}
}
```

- [ ] **Step 7.2: Run the new tests**

From `src/`: `dotnet test --filter "FullyQualifiedName~MultiGameIsolationIntegrationTest"`

Expected: 3 passing.

If `Resources_AreScopedByGameIdHeader` returns identical numbers, the wiring from Tasks 3–5 is broken — debug there before continuing. If `UnknownGameIdHeader_Returns400` fails with 200/500, `CurrentGameMiddleware` isn't reached or doesn't validate.

- [ ] **Step 7.3: Run full test suite**

From `src/`: `dotnet test`

Expected: all green.

- [ ] **Step 7.4: Commit**

```
git add src/BrowserGameEngine.StatefulGameServer.Test/Integration/MultiGameIsolationIntegrationTest.cs
git commit -m "test(server): integration tests verifying X-Game-Id isolates resources"
```

---

## Task 8: Manual smoke test — full stack

**Files:** none

- [ ] **Step 8.1: Boot the dev environment**

From repo root: `docker-compose up -d`

Then in `src/ReactClient`: `npm run dev`

- [ ] **Step 8.2: Verify in browser**

Open two games in two tabs:
- Tab 1: `http://localhost:5173/games/<gameA>/base`
- Tab 2: `http://localhost:5173/games/<gameB>/base`

Resources should differ between tabs. Network panel: each `/api/resources` request should carry the matching `X-Game-Id` header.

If you don't have two games locally, create them via the UI's Games page first.

- [ ] **Step 8.3: Tear down**

From repo root: `docker-compose down`

No commit for this task — manual verification only.

---

## Task 9: Open the PR

**Files:** none

- [ ] **Step 9.1: Push branch**

```
git push -u origin game-context
```

- [ ] **Step 9.2: Create PR**

Use `gh pr create` with title "fix: scope HTTP API by X-Game-Id header" and a body that summarizes:

- Adds `X-Game-Id` request header pipeline so per-game endpoints return correct data.
- Fixes the bug where multiple game URLs displayed identical resources.
- Approach: `CurrentGameMiddleware` + `HttpContextWorldStateAccessor` + axios interceptor + `UserRepository` migration.
- Out of scope: `BearerTokenMiddleware` API-key, per-instance `GameTickEngine`, SignalR scoping.
- Test plan: `dotnet test`, `npm test`, manual two-tab verification.

- [ ] **Step 9.3: Capture PR URL**

The command prints the PR URL. Report it back.

- [ ] **Step 9.4: Clean up worktree (after PR is open)**

From `/home/chris/repos/my/bge`: `git worktree remove /tmp/bge-work/game-context`

---

## Self-Review Notes

**Spec coverage:** Bug is "every game URL shows the same resources." Tasks 3–5 fix the HTTP repo path; Task 6 fixes the `CurrentUserMiddleware` cookie/oauth path; Tasks 1–2 fix the frontend so it actually sends the header; Task 7 proves both ends meet via integration tests; Task 8 smoke-tests the deployed pipeline.

**Type consistency:** `CurrentGameMiddleware.GameIdItemKey` is referenced from `HttpContextWorldStateAccessor` — same constant. `gameIdFromPath` returns `string | null`, used by interceptor with truthiness check. `IWorldStateAccessor` injection signature is `(GlobalState, IWorldStateAccessor)` consistently in `UserRepository`.

**Placeholder scan:** All steps have concrete code or concrete commands. No "TBD," "appropriate error handling," or unspecified test bodies.
