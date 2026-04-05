# BGE Implementation Status

Last updated: 2026-04-05. See [PLAN.md](PLAN.md) for the full feature registry.

---

## Recently Merged (PRs #150–#210)

| BGE | Feature | PR |
|---|---|---|
| — | ci: add test coverage reporting and optimize Docker build | #210 |
| BGE-651 | Security hardening Steps 1-6 | #209 |
| BGE-613 | Real-time notification toasts + SignalR client | #208 |
| BGE-650 | E2E integration tests for critical game flows | #207 |
| BGE-640 | Complete Protoss race data with shields mechanic | #206 |
| BGE-639 | Complete Zerg race data | #205 |
| BGE-649 | Fix MarketController route ambiguity (405 on integration tests) | #204 |
| BGE-629 | Admin dashboard with player moderation and game management | #203 |
| BGE-614 | API pagination for list endpoints | #202 |
| BGE-612 | SignalR GameHub and real-time event publishing | #201 |
| BGE-626 | Mobile responsive UI pass | #200 |
| BGE-628 | Performance benchmarks for game tick engine | #199 |
| BGE-627 | Production monitoring and alerting | #198 |
| — | Fix Diplomacy, Spy, and Players page crashes | #196 |
| BGE-597 | Replace hardcoded colors with theme variables | #195 |
| BGE-610 | Structured logging, request timing, ECS container health check | #194 |
| BGE-598 | Route-based code splitting for React client | #193 |
| BGE-599 | Full accessibility pass | #192 |
| BGE-586 | Balance simulation CLI (`tools/BalanceSim`) | #191 |
| BGE-584 | E2E Playwright tests — resources, attack flow, alliances | #190 |
| BGE-578 | Show winner name and condition badge in victory banner | #189 |
| BGE-588 | UX polish — theme colors, mobile sidebar, accessibility | #188 |
| — | Fix MarketController route template (integration tests) | #187 |
| BGE-580 | Fix message thread view causing browser freeze | #186 |
| BGE-581 | Alliance Chat as third Messages tab | #185 |
| BGE-577 | Trade page for player-to-player trading | #184 |
| BGE-579 | EconomicThreshold victory progress bar on game dashboard | #183 |
| — | Add integration tests for API controllers | #181 |
| BGE-571 | Sort ranking by score + highlight current player | #180 |
| BGE-570 | Rename 'spies' route to 'operations' | #179 |
| — | Add error boundaries and consistent loading/error states | #178 |
| BGE-562 | Playwright E2E test framework for React client | #177 |
| BGE-560 | React client build + lint in CI pipeline | #176 |
| BGE-549 | Player Profile pages (my profile + public) | #175 |
| BGE-550 | All-time players list page | #174 |
| BGE-548 | React Alliances pages (list + detail) | #173 |
| — | Fix Messages: recipient name in Sent tab + reply pre-fill | #172 |
| BGE-526 | React pages — Achievements, History, AdminGames; delete BlazorClient | #171 |
| BGE-522 | React social pages — Chat, Messages, Rankings, Profiles | #170 |
| BGE-520 | React app shell — layout, nav, auth, CurrentGame context | #168 |
| BGE-534–537 | Fix spy missions, alliance name/route, ranking highlight | #169 |
| BGE-525 | React pages — Games, Lobby, JoinGame, Summary, Results, CreatePlayer, UnitDefinitions | #167 |
| BGE-524 | React pages — Alliances, AllianceDetail, AllianceRanking | #166 |
| BGE-523 | React pages — Spies, SpyReports, Diplomacy, EnemyBase, SelectEnemy | #164/165 |
| BGE-512 | xUnit tests for victory conditions | #163 |
| BGE-521 | React client scaffold + core game pages (Base, Units, Research, Market) | #162 |
| — | Merge Tech Tree and Upgrades into single Research section | #161 |
| BGE-513 | Victory conditions UI — banner, progress bar, contextual badges | #160 |
| BGE-511 | Victory conditions — EconomicThreshold module + VictoryConditionType tracking | #159 |
| BGE-471 | Alliance UI — war status indicators, invite, declare war, peace | #158 |
| BGE-489 | Player trading system + in-game messaging overhaul (Sprint 7) | #157 |
| BGE-466 | In-game leaderboard page + API endpoint | #156 |
| — | Address BGE-465 code review nits | #155 |
| BGE-479 | Add Incoming Intel tab to Spy page | #154 |
| BGE-482 | Controller-level tests for spy, alliance, market, leaderboard, research APIs | #153 |
| — | Remove WriteIndented and cache JsonSerializerOptions in blob serializers | #152 |
| BGE-465 | Spy offensive actions backend | #151 |
| BGE-467 | Alliance invite & war system | #150 |

---

## Open PRs Pending Merge

| PR | BGE | Description | Status |
|---|---|---|---|
| #211 | BGE-667 | Cross-race integration tests for Zerg and Protoss | In review |
| #212 | BGE-641 | Real-time game chat with alliance channel and rate limiting | In review |

---

## Active Work

| BGE | Title | Status |
|---|---|---|
| BGE-668 | Update IMPL-STATUS.md and PLAN.md with current project state | in_progress (this PR) |
| BGE-667 | Cross-race integration tests for Zerg and Protoss | PR #211 open |
| BGE-641 | Real-time game chat with alliance channel and rate limiting | PR #212 open |

---

## What Remains from the Original Spec

1. **Alliance leader election** — Currently leader = creator. Voting mechanism not yet built.

---

## Infrastructure & Ops

- **Production**: AWS ECS Fargate at [ageofagents.net](https://ageofagents.net)
- **State storage**: S3 with versioning (`games/{gameId}/state.json`, `global/state.json`)
- **CI**: GitHub Actions — `dotnet build --configuration Release` + `dotnet test` + React lint/build on push/PR to master. Test coverage reporting added.
- **Secrets**: AWS SSM Parameter Store (GitHub OAuth, Discord webhook URL)
- **Observability**: Serilog structured logging, Prometheus metrics, OpenTelemetry tracing, CloudWatch alarms, ECS health check at `/health`
- **SignalR**: Real-time event publishing via `GameHub`; real-time notification toasts on React client
