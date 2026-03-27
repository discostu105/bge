# Technology Stack Review

## Current Stack Overview

| Layer | Technology | Version |
|-------|-----------|---------|
| **Language** | C# | Latest (LangVersion: preview) |
| **Runtime** | .NET | 9.0 |
| **Backend** | ASP.NET Core | 9.0 |
| **Frontend** | Blazor WebAssembly (WASM) | 9.0 |
| **Auth** | Discord OAuth2 (`AspNet.Security.OAuth.Discord`) | 9.0 |
| **Persistence** | File-based (dev) / AWS S3 (prod) | - |
| **Observability** | OpenTelemetry, Serilog, Prometheus, Rookout | Mixed |
| **Deployment** | Docker, AWS ECS Fargate, Terraform | - |
| **CI/CD** | GitHub Actions | - |
| **Testing** | xUnit, BenchmarkDotNet | 2.9 / 0.14 |

## Architecture Assessment

### Strengths

- **Single-language stack (C#):** Frontend, backend, and shared models all in C#. Eliminates context switching and allows sharing types (ViewModels) between client and server.
- **Clean project separation:** Well-organized into Client, FrontendServer, StatefulGameServer, GameModel, GameDefinition, and Persistence layers.
- **Good abstraction over storage:** `IBlobStorage` with file and S3 implementations makes local dev easy and production deployment flexible.
- **Modern observability:** OpenTelemetry + Serilog + Prometheus is a solid monitoring stack.
- **Infrastructure as Code:** Terraform for AWS provisioning, Docker for reproducible builds.
- **CI/CD pipeline:** Automated build/test/deploy via GitHub Actions to AWS ECS.

### Concerns

- **No relational database:** State is serialized as blobs. This works for a small game but limits querying, concurrency, and data integrity as the project scales.
- **Stateful in-process game server:** The `StatefulGameServer` holds state in memory. This means no horizontal scaling — a single ECS task handles everything (512 CPU / 1024 MB).
- **`OpenTelemetry.Exporter.Jaeger` is deprecated:** Jaeger exporter was deprecated in favor of OTLP exporter. Should migrate to `OpenTelemetry.Exporter.OpenTelemetryProtocol`.
- **Rookout dependency:** Niche live-debugging tool. Adds a dependency that may not justify its cost for a hobby/small project.

---

## Blazor Assessment

### Is Blazor Still Actively Developed?

**Yes, strongly.** At Microsoft Build 2025, Microsoft explicitly designated Blazor as its **primary future investment** in Web UI for .NET. While MVC and Razor Pages remain supported, Blazor is where the innovation is happening.

Key evidence:
- .NET 10 (November 2025, LTS) shipped major Blazor improvements:
  - 76% reduction in `blazor.web.js` bundle size (183 KB → 43 KB)
  - `[PersistentState]` attribute for component state preservation
  - WebAuthn/passkeys support
  - Hot Reload enabled by default for WASM debug builds
- 43% of .NET developers use Blazor in production (JetBrains 2026 .NET Ecosystem Report)
- Active sites grew from ~12,500 (late 2023) to ~149,000 (mid-2025)
- Used in production by Ferrari, Frankfurt Airport, Tyler Technologies

**Verdict: Blazor is not going anywhere. It's Microsoft's strategic bet for .NET web UI.**

### Is Blazor a Good Choice for This Project?

**Yes, it is a good fit.** Here's why:

1. **C# end-to-end:** This project shares ViewModels between client and server. With React, you'd need to maintain TypeScript interfaces separately or use code generation.
2. **Small team / solo developer:** Blazor eliminates the need to maintain two separate ecosystems (npm + NuGet). One toolchain, one language.
3. **Game-like UI complexity is moderate:** The UI has ~18 Razor components — forms, tables, resource displays. This is not a complex interactive canvas that would benefit from React's ecosystem.
4. **No JavaScript/npm dependency:** The project has zero npm dependencies. Switching to React would introduce node_modules, bundlers (Vite/webpack), and a parallel dependency tree.

### When Would React Be Better?

React would have advantages in specific scenarios that **don't strongly apply here**:

| Factor | React Advantage | Relevance to This Project |
|--------|----------------|--------------------------|
| **Talent pool** | ~40% of devs know React vs. Blazor's smaller pool | Low — appears to be a small team / hobby project |
| **Component ecosystem** | Massive library of UI components (shadcn, MUI, etc.) | Low — UI is custom game UI, not standard CRUD forms |
| **Real-time rendering** | Canvas/WebGL libraries (PixiJS, Three.js) are mature | Medium — could matter if the game adds graphical elements |
| **Bundle size** | React is ~45 KB vs. Blazor WASM's ~2+ MB initial download | Medium — Blazor WASM has a larger initial load, but caches well via service worker |
| **SEO** | React SSR/SSG ecosystem is mature | None — this is an authenticated game, not a content site |
| **Rapid UI iteration** | Hot module replacement is faster | Low — .NET 10 improved Blazor hot reload significantly |

### Blazor WASM vs. Blazor Server vs. Blazor United

The project currently uses **Blazor WebAssembly (hosted)**. This is a reasonable choice for a game where:
- Client-side execution avoids latency on every UI interaction
- Offline capability via service worker is a nice-to-have
- The server doesn't need to maintain SignalR connections per user

If upgrading to .NET 10, consider evaluating **Blazor's unified rendering model** (Auto mode), which starts with server-side rendering and transitions to WASM — giving faster initial load while keeping the WASM benefits.

---

## Recommendations

### Keep As-Is (Good Choices)
- Blazor WebAssembly as the frontend framework
- ASP.NET Core backend
- C# end-to-end with shared ViewModels
- Docker + ECS Fargate deployment
- Terraform for infrastructure
- xUnit + BenchmarkDotNet for testing

### Consider Upgrading
- **Upgrade to .NET 10 (LTS):** Current .NET 9 is STS (support ends May 2026). .NET 10 is LTS with 3-year support and brings meaningful Blazor improvements.
- **Replace Jaeger exporter with OTLP exporter:** `OpenTelemetry.Exporter.Jaeger` is deprecated. Use `OpenTelemetry.Exporter.OpenTelemetryProtocol` instead.
- **Evaluate removing Rookout:** Unless actively used, it adds unnecessary dependency surface.

### Consider If Scaling
- **Add a lightweight database** (e.g., PostgreSQL or SQLite) if querying game state becomes a bottleneck.
- **Extract game state to Redis or similar** if horizontal scaling becomes necessary.
- **Explore Blazor Auto render mode** (.NET 10+) for faster initial page loads.

---

## Conclusion

The technology choices are **coherent and well-suited** for a small-to-medium browser game project. Blazor WASM is a solid choice — it's actively developed, strategically backed by Microsoft, and provides real productivity benefits for a C#-centric team. Switching to React would introduce significant complexity (dual ecosystem, type sync, npm toolchain) without proportional benefits for this project's scope and team size. The most impactful near-term improvement would be upgrading to .NET 10 LTS for long-term support and the latest Blazor enhancements.
