# Project Quality Analysis: Browser Game Engine (BGE)

**Date:** 2026-03-21
**Scope:** Full codebase review — architecture, code quality, testing, dependencies, security

---

## Overview

A .NET 8 / Blazor WebAssembly game engine — a revived hobby project ("StarCraft Online" from 2003). Stateful monolith with in-memory game state, Discord OAuth2 auth, and S3 persistence. 124 C# source files across 11 projects, 61 commits, primarily one contributor.

---

## Highlights

- **Clean architecture** — Repository pattern, command pattern, proper DI, clear separation of concerns across 11 well-scoped projects (GameModel, GameDefinition, Persistence, StatefulGameServer, BlazorClient, Shared, etc.)
- **Modern C# usage** — Nullable reference types enabled (`<Nullable>enable</Nullable>`), immutable records for domain models, minimal API style in Program.cs
- **Observability is impressive for a hobby project** — OpenTelemetry with Jaeger tracing, Prometheus metrics (`prometheus-net`), Serilog structured logging, Rookout live debugging
- **Benchmarks exist** — A dedicated `StatefulGameServer.Benchmarks` project for the game tick engine shows performance awareness
- **CI/CD in place** — GitHub Actions (`dotnet-core.yml`) for build + test on push/PR
- **Well-structured battle system** — `BattleBehaviorScoOriginal.cs` includes detailed pseudocode documentation explaining the algorithm before implementation

---

## Glaring Issues

### 1. Hardcoded Secret in Source Code
**Severity: HIGH**
`BrowserGameEngine.FrontendServer/Program.cs` contains a plaintext Rookout token:
```
token = "8ec5815b038ce52b430c0caa163689bdd61119e8d81a920786274f2cf3562c2a"
```
This should be moved to user secrets or environment variables immediately.

### 2. Preview/Pre-release Dependencies
**Severity: MEDIUM**
Multiple production dependencies are on preview versions:
- `Microsoft.AspNetCore.Components.WebAssembly.Server (7.0.0-preview)`
- `System.Configuration.ConfigurationManager (7.0.0-preview)`
Mixed .NET version targeting (5/6/7/8) across the history suggests incomplete migration.

### 3. No Concurrency Control
**Severity: MEDIUM**
Multiple `// TODO` comments in `UnitRepositoryWrite`, `AssetRepositoryWrite`, etc. flag missing synchronization. For an in-memory stateful server handling concurrent HTTP requests, this is a real race condition risk.

### 4. Significant Unfinished Game Logic
**Severity: MEDIUM**
- `ResourceGrowthSco` is self-described as "just a dummy logic"
- `ResourcesDestroyed` and `ResourcesStolen` in battle are not implemented
- Battle system is hardcoded to one implementation (TODO says it should be configurable via GameDef)

### 5. No Code Style Enforcement
**Severity: LOW**
No `.editorconfig`, no StyleCop analyzers, no Roslyn analyzer configuration. Some unused imports scattered around (e.g., `System.Security.Cryptography.X509Certificates`).

---

## Moderate Concerns

- **Test coverage is thin** — 6 test files covering game mechanics basics (battles, units, assets, resources, validation). No integration tests, no API endpoint tests, no Blazor component tests.
- **No developer documentation** — No CONTRIBUTING.md, setup guide, or architecture decision records. Onboarding would require reading code.
- **Serilog minimum level set to Error** in production config — this hides warnings and info-level telemetry, making debugging harder.
- **In-memory state only** — Expected for a hobby project, but one server crash loses all game state between persistence intervals.
- **Legacy Azure Pipelines file** — `azure-pipelines.yml` still in repo targeting .NET 5, while GitHub Actions is the active CI. Dead config should be removed.

---

## Quality Scorecard

| Aspect                | Rating     | Notes |
|-----------------------|------------|-------|
| Architecture          | Good | Clean separation, repository pattern, DI |
| Type Safety           | Excellent  | Nullable refs, records, strong typing |
| Testing               | Basic      | 6 unit test files, xUnit, no integration tests |
| Code Style            | Decent     | Consistent naming, but no automated enforcement |
| Documentation         | Minimal    | README exists, no dev docs |
| CI/CD                 | Good | GitHub Actions build + test |
| Observability         | Excellent  | Tracing, metrics, structured logging |
| Security              | Poor       | Hardcoded secret in source |
| Dependency Management | Needs Work | Preview packages, version mismatches |
| Completeness          | Partial    | Multiple TODOs, stubbed features |

---

## Overall Verdict

**Solid hobby project with good architectural instincts.** The code is clean, well-organized, and uses modern patterns appropriately. The author clearly has professional experience — DI, observability, benchmarking, and immutable domain models are not things a beginner reaches for.

The main gap is **completeness over polish**: many features are stubbed out, concurrency isn't addressed, and the dependency versions are messy. The hardcoded secret is the one thing that should be fixed regardless of the project's scope.

**Quality score: 6.5/10** — Good bones, needs finishing work.

---

## Recommended Next Steps (Priority Order)

1. Remove hardcoded Rookout token from source — use `dotnet user-secrets` or environment variables
2. Stabilize dependencies — upgrade all packages to stable .NET 8 releases
3. Add concurrency controls to repository write operations (at minimum, `lock` or `SemaphoreSlim`)
4. Remove legacy `azure-pipelines.yml`
5. Add `.editorconfig` with team conventions
6. Expand test coverage to include integration and API tests
