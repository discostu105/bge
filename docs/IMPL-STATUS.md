# BGE Implementation Status

Last updated: 2026-03-30. See [PLAN.md](PLAN.md) for the full feature registry.

---

## Recently Merged (last ~30 commits on master)

| BGE | Feature | PR |
|---|---|---|
| BGE-267 | In-app player notification center (bell icon, ring buffer, push on attack/game events) | #85 |
| BGE-276 | Fix: register AddAuthorizationCore in Blazor WASM client | — |
| BGE-241 | Discord webhook notifications for game start/end | #81 |
| BGE-189 | 10-second auto-refresh timer on all game-state pages | #58 |
| BGE-164 | Game Admin Page — Create & Manage Games | #50 |
| BGE-232 | End-to-end multi-game lifecycle integration test | #72 |
| BGE-218 | Replace async void timer callbacks with PeriodicTimer | #67 |
| BGE-217 | Redesign /games as Season Schedule with Bootstrap cards | #73 |
| BGE-224 | Post-game summary screen at /games/{id}/summary | #74 |
| BGE-213 | Player public profile page with cross-game stats | #76 |
| BGE-223 | Redirect to original page after OAuth sign-in | #75 |
| BGE-135 | Improve login UX — copy updates and persistent session | #77 |
| BGE-226 | Ending-soon alert fires only once per game window | #70 |
| BGE-220 | Add currentUser.IsValid guard in GamesController.Create | #71 |
| BGE-211 | Game auto-finalization engine | #68 |
| BGE-249 | Fix HistoryController to use GlobalState methods | #78 |
| BGE-212 | End-of-game countdown banner | — |
| BGE-192 | Player game history & stats page | — |
| BGE-165 | Game results screen with auto-redirect | — |
| BGE-163 | All-time cross-game leaderboard | — |
| BGE-162 | Game-scoped Blazor routing (Phase 3) | — |

---

## Open PRs Pending Merge

| PR | BGE | Description | Status |
|---|---|---|---|
| #64 | BGE-157 | Player registration UI | Blocked — needs rebase + review (BGE-284) |
| #60 | BGE-158 | JoinGame UI improvements | Blocked — needs rebase + review (BGE-284) |
| #85 | BGE-269 | Season subscription + auto-join | In progress — needs fix for 3 blocking issues (BGE-300) |

---

## Active Work

| BGE | Title | Status |
|---|---|---|
| BGE-300 | Fix PR #85 (BGE-269): rebase + fix 3 blocking issues | todo |
| BGE-284 | Merge PR #60 (BGE-158) and PR #64 (BGE-157) | blocked |
| BGE-182 | Keep /docs up to date | in_progress (this PR) |

---

## Next Up (todo, no PR open)

| BGE | Title | Priority |
|---|---|---|
| BGE-290 | Resource Trading Market | high |
| BGE-296 | Detailed Battle Reports | high |
| BGE-292 | Player Achievements Page | medium |
| BGE-291 | Spy / Scout Mission | medium |
| BGE-294 | Game-wide Chat Channel | medium |

---

## What Remains from the Original Spec

1. **Complete Zerg & Protoss definitions** — Terran is fully defined. Zerg and Protoss buildings/units need data work in `GameDefinition.SCO`. Requires balance simulation CLI first (see below).
2. **Alliance leader election** — Currently leader = creator. Voting mechanism not yet built.
3. **Balance simulation CLI** — Standalone `tools/BalanceSim` project to run combat simulations. Pre-requisite for safe Zerg/Protoss data work.

---

## Infrastructure & Ops

- **Production**: AWS ECS Fargate at [ageofagents.net](https://ageofagents.net)
- **State storage**: S3 with versioning (`games/{gameId}/state.json`, `global/state.json`)
- **CI**: GitHub Actions — `dotnet build --configuration Release` + `dotnet test` on push/PR to master
- **Secrets**: AWS SSM Parameter Store (GitHub OAuth, Discord webhook URL)
- **Observability**: Serilog console, Prometheus metrics, OpenTelemetry tracing, CloudWatch alarms
