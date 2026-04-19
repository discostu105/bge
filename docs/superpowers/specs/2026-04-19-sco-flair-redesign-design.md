# SCO Flair Redesign — Design Spec

**Status:** Draft · awaiting user review
**Date:** 2026-04-19
**Branch candidate:** `feat/sco-flair-redesign`
**Builds on:** `c98514a feat(ui): rethink Base & Units UX`, `de844da design-system foundation — shadcn + amber/tactical refresh`
**Reference bundle:** `~/repos/play/sco-revengineer/ux-bundle/` — UX docs, design tokens, 157 original GIF assets

---

## 1. Intent

BGE is the spiritual successor to StarCraft Online (SCO, 2003). The current UI is clean and functional but reads like a generic SaaS dashboard: amber-on-slate shadcn chrome, uniform card borders, Lucide icons everywhere, no sense of faction. It is also visually busier than SCO ever was — more cards, more nav items, more explanatory copy, more header controls.

This spec defines how to re-skin and simplify the React client so it feels like SCO: **dark-space atmosphere, race identity in the chrome, information-dense dataviz, and only four colors that mean something** (green = resources, blue = interactive, gold = prestige, race-color = identity).

The goal is **Full flair, modernized** (option B of 3 considered): keep the modern SPA, accessibility, responsive layout, and shadcn primitive library; change only what affects the *feel*. No functional changes to gameplay.

## 2. Problems being solved

Ranked, in order of user-stated priority:

1. **No faction identity.** A Terran, Zerg, and Protoss player all see the same UI. SCO's race-specific menu bar was the #1 nostalgia element — we have nothing equivalent.
2. **Too many visual containers.** Every section is a rounded-border card. The Base page stacks 6+ cards vertically. SCO used near-invisible `#272727` borders and transparent cells so the starfield was the only "container."
3. **Navigation surface is too wide.** 13 items across 4 sections. SCO had 8 primary icons plus a text-link footer.
4. **Header is too busy.** Six regions in 48px: title, resources, connection pill, notification bell, theme toggle, user menu. SCO had four: user, timer, resources, land.
5. **Too much explanatory copy.** Tooltips, empty-state paragraphs, "click Assign," build-cost explainers. SCO let numbers speak.

Not in scope: the point user explicitly excluded — "everything has the same visual weight" as a complexity problem. We will still *add* visual hierarchy for flair (e.g., tiered ranking typography), but we are not trying to re-rank the Base page's information hierarchy.

## 3. Decisions locked during brainstorming

| Decision | Choice | Rejected alternatives |
|---|---|---|
| Scope | **B. Full flair, modernized** | A. Light flair (reskin only); C. Classic 2003 mode as opt-in theme |
| Nav structure | **C. Primary 6-item sidebar + utility-link footer** — mirrors SCO's right-column text nav | A. Flat 8 with "…" menu; B. Two groups (Command/Intel) |
| Unit/building art | **C. Commissioned stylized SVG silhouettes per race** — crisp, IP-clean, harmonizes with Lucide | A. Original GIFs as-is; B. 2× pixel-scaled GIFs; D. SVG + GIF hybrid |
| Race chrome | Pure CSS (gradients + subtle overlays); per-race emblem SVG; no raster menu-bar textures | Raster race textures from the bundle |
| IP stance | Inferred from C: **IP-clean.** No StarCraft sprites shipped in the live bundle. Race names (Terran/Zerg/Protoss) stay — they're already in the codebase. |

## 4. Design foundation

### 4.1 Color tokens

Extend `index.css` `@theme` with SCO-semantic roles. All other tokens stay.

| Role | Value | Usage |
|---|---|---|
| `--color-bg` | `#050810` | Page bg (near-black with blue tint) |
| `--color-bg-surface` | transparent | Sections bleed into starfield; no solid cards |
| `--color-border` | `hsl(222 40% 15%)` | Hair-thin section rules, hex-grid dividers |
| `--color-resource` | `#22C55E` | All resource numerics — minerals, gas, income |
| `--color-interactive` | `#3B82F6` | Links, action buttons, player usernames that are clickable |
| `--color-prestige` | `#F59E0B` | Research upgrades, rank-1 badge, timer ring |
| `--color-foreground` | `#F3F6FB` | Body text |
| `--color-muted` | `#94a3b8` | Secondary text, labels, hints |
| `--color-enemy` | `#fca5a5` | Enemy names in combat views |
| `--color-danger` | `#EF4444` | Errors, losses in battle reports |

**Race accent palette** (applied via `html[data-race="terran|zerg|protoss"]` on `<html>`):

| Race | Primary | Secondary | Emblem fill |
|---|---|---|---|
| Terran | `#CBD5E1` (slate) | `#F59E0B` (industrial amber) | `#1f2937` |
| Zerg | `#B45309` (rust) | `#ea580c` (burnt orange) | `#3a1a08` |
| Protoss | `#FBBF24` (gold) | `#0e7490` (teal) | `#0c1e2e` |

A race is set on the root HTML element when the user is in a game they've joined. Meta pages (sign-in, game list) use a neutral slate palette — no race until the player picks one.

**Glow effects** (for resource numbers, rank 1 name, active nav accent) — subtle text-shadow / box-shadow at 0.3–0.4 alpha, keyed to the color.

### 4.2 Typography

- **Body:** `Inter` via webfont (fallback system-ui). 13px base (down from current 14px).
- **Mono:** `JetBrains Mono` — already in tokens; use for all resource numerics, counts, timers, table columns.
- **Headings:** same Inter, weighted. No second heading font.
- **Label class:** 10–11px, uppercase, letter-spacing 0.18em, semibold — replaces much of the current `<h2>` usage.
- **Ranking tier typography** (the signature flair):

| Rank | Size | Weight |
|---|---|---|
| 1 | 32px | 800 |
| 2–5 | 18px | 700 |
| 6–10 | 15px | 600 |
| 11–20 | 13px | 500 |
| 21+ | 11.5px | 400 (muted) |

### 4.3 Starfield background

One reusable CSS utility replaces the current `.bg-grid`:

```css
.bg-starfield {
  background-color: #050810;
  background-image:
    radial-gradient(1px 1px at 20% 30%, rgba(255,255,255,0.8), transparent),
    radial-gradient(1px 1px at 70% 60%, rgba(255,255,255,0.6), transparent),
    radial-gradient(1.5px 1.5px at 40% 80%, rgba(220,220,255,0.7), transparent),
    /* ~10 layers, varied sizes and opacities */;
  background-size: 300px 300px, 200px 200px, 250px 250px /* , ... */;
}
```

Applied to `<body>` or `<main>`. No animation in phase 1 (optional later: slow parallax via `@keyframes` and `prefers-reduced-motion` gate). Zero JS cost, zero runtime.

### 4.4 Container strategy

Drop default card borders site-wide. Replace with:

- **`Section`** primitive: uppercase label + thin `border-bottom: 1px solid var(--color-border)` + content. No box.
- **`DataGrid`** primitive: items in `display:grid` with `gap: 1px` on a `var(--color-border)` background — produces a pixel-thin grid (see Base buildings, Units home list).
- **`Panel`** primitive (retained for modals/overlays only): `bg-card` with border and radius, as today.
- Ad-hoc `<div className="rounded-lg border bg-card p-4">` throughout the code is refactored to these primitives.

This is the single biggest "complexity reduction" — it removes dozens of nested card borders.

## 5. Shell redesign

### 5.1 Sidebar (`GameLayout`)

From current 13-item 4-section nav to:

```
┌──────────────────────┐
│ ══════════════════   │ ← race stripe (6px gradient)
├──────────────────────┤
│ ◆ TERRAN             │ ← race emblem + race name + round
│   Round 14 · Season  │
├──────────────────────┤
│   ⬡ Base             │ ← 6 primary nav items
│   ⚔ Units            │    icon = race-tinted SVG
│   ◉ Research         │    active = race accent left-border
│   ⇄ Market           │
│   ◈ Live View        │
│   ◆ Alliance         │
│                      │
├──────────────────────┤
│ ★ Ranking  ✉ Msgs    │ ← utility footer
│ 💬 Chat    📖 Codex  │    text links, smaller
│ 👤 Profile ❓ Help   │    blue (interactive)
└──────────────────────┘
```

Width stays at `w-56` (unchanged). The utility footer is pinned to the bottom (`mt-auto`).

**Demoted routes** (kept accessible via utility footer or account menu — not primary):

- Alliance Ranking → grouped with Ranking (tabs inside Ranking page, not separate nav item).
- Unit Types → "Codex" in utility footer.
- History, Stats, Profile → user-menu dropdown in header, as today.
- Trade → merged into Market (tabs), since the functionality is adjacent.

### 5.2 Header

From 6 regions to 4:

| Region | Content |
|---|---|
| Commander name | Race-colored, with race tag (e.g. `Commander Chris · Terran`). Tap/click → profile. |
| Timer ring | 34px circular SVG, race-accent stroke, mono digits in center. The ritual element. |
| Resource strip | `Min / Gas / Land / Units` — mono numerics, green for minerals+gas (with glow), blue for land, muted for units count. |
| Utility row | Messages unread badge, notification bell, user avatar menu. |

**Dropped controls:**

- **Theme toggle.** Commit to dark-only. SCO is space. Light theme was never in character.
- **Connection pill.** Move to sidebar footer as a tiny dot + tooltip. Doesn't need header real estate.

### 5.3 Mobile

Sidebar still collapses to drawer below `md`. Utility footer becomes a horizontal scroll bar at the bottom of the drawer. Header resource strip drops `Units` count below `sm`.

## 6. Page patterns

### 6.1 Base

Current structure: Title → 3-card Economy strip → 4 Asset groups → Build Queue card → Resource History card → Activity accordion → dialogs.

Proposed:

```
Title + sub (rank, round)
──────────────────────
Overview row — 3 stats, no cards (Workers · Land · Queue)
──────────────────────
§ BUILDINGS          5 / 11 · + train units
  [ DataGrid : 3-col grid-gap-1px hex-pattern ]
──────────────────────
§ BUILD QUEUE        3 items
  monospace list — index, name, ticks, × to cancel
──────────────────────
§ RESOURCE HISTORY
  inline chart (Recharts retained, restyled to match palette)
──────────────────────
§ RECENT             [show 8 more]
  terminal-style log lines, mono 11px
```

Four `<Section>` blocks. Zero cards. Zero accordions (the Recent section is always visible but capped at 3 lines by default, with "show more" → expanded inline).

The train-units / worker-assignment / colonize / split-unit dialogs all stay exactly as they are — they already match the "dense + mono" aesthetic after the Apr 19 redesign.

### 6.2 Units

- Home units as a 2-column `DataGrid`, each cell 56–72px tall, showing sprite + name + stats + count (mono 22px) + inline actions.
- Deployed units in a warning-tinted block (light red wash, thin red border) with target username (enemy-colored) and arrival/return timer (gold).
- Merge/Split/Attack actions as text links next to each stack, not separate buttons. Same as SCO.

### 6.3 Research

Keep the current 2-track layout (Attack / Defense). Restyle:

- Track line becomes a horizontal timeline connecting node dots.
- Upgrade levels in gold (`--color-prestige`), unresearched in muted, in-progress with timer pulse.
- "Research now" CTA → blue interactive link, not a filled button.

### 6.4 Ranking

The signature page. See section 4.2 for tier typography.

- Single table, no outer border.
- Current player highlighted with green text + green-gradient left border wash (replaces the current amber-primary highlight — amber is now reserved for prestige only).
- Allied players get a subtle blue wash (`rgba(59,130,246,0.04)`).
- Race-colored player names (slate / amber / gold).
- Online dot with glow, offline dot muted.
- Filter tabs (All / Human / AI) collapse to text links `meta` row at the top.
- Alliance Ranking becomes a tab on this page rather than a separate route.

### 6.5 Market / Trade (merged)

Two tabs: **Market** (post/accept open orders) and **Direct** (player-to-player — the current Trade page). Consolidates two nav items into one — a direct consequence of the "6 primary nav" choice. Both pages keep their data, contracts, and interactions; the merger is purely chrome. If it turns out one tab gets overwhelmingly more use, we split them back out.

### 6.6 Chat / Messages

No visual redesign. The current views already match the "dense + mono" direction well enough. Fold race-colored names in the chat stream as the only touch-up.

## 7. Art direction

### 7.1 SVG icon kit — new work

Approximately **70–90 icons** drawn fresh — exact count depends on whether we redraw every unit per race or share a base silhouette tinted by race (see Risks §11).

| Category | Count (full / shared) | Sizes |
|---|---|---|
| Nav icons (6 primary + 6 utility) | 12 | 16×16 |
| Unit silhouettes | ~30 full / ~10 shared | 28×28 and 40×40 |
| Building silhouettes | ~18 full / ~6 shared | 28×28 and 40×40 |
| Action icons (attack, defend, recall, split, merge) | 5 | 20×20 |
| Resource icons (minerals, gas, land) | 3 | 16×16 with larger 96×96 variant |
| Race emblems | 3 | 32×32 |
| Status (online dot glow, offline dot) | 2 | CSS-only via ::before |

Default: **full per-race variants** for units and buildings — it's where the flair pays off most. If the Phase 2 pace lags, fall back to shared silhouettes.

Style rules:

- Stroke 1.2–1.5px, flat fill at low opacity, no gradients inside icons.
- Race color applied via `stroke="currentColor"` + `color: var(--race-primary)`.
- Angular-geometric for Terran (equilateral triangles, rectangles). Curved-organic for Zerg (béziers, asymmetric blobs). Regular-geometric for Protoss (hexagons, concentric circles).
- Each has a "locked/unavailable" state achieved via `opacity: 0.4 + filter: grayscale(0.8)` — not a separate file (unlike SCO's `.gif` + `2.gif` convention).

### 7.2 Originals as a reference ONLY

The 157 bundled GIFs stay in the reference repo. They are NOT shipped in BGE. The art direction uses them for silhouette reference only; all icons are redrawn.

### 7.3 Splash art

Login page currently has no splash. Add a single full-bleed CSS art composition (gradient + starfield + silhouetted planets) matching the dark-space vibe. No raster asset needed; pure CSS. Race-selection on `JoinGame` gets three large (180×180) race emblem tiles — same SVGs used in the sidebar, scaled up.

## 8. Removed / out of scope

- **Light theme** — dropped; dark-only.
- **CRT / scanline effect** — not in phase 1. Could be a later toggle.
- **Animated starfield parallax** — out; CSS-static is enough for now.
- **Sound effects** — out. The game has always been silent.
- **Mini-ranking footer iframe** (SCO convention) — no, we have a real ranking page and live updates.
- **Battle report animation** — battle reports get color-coded (green gains, red losses, unit icons inline) but no step-by-step animation in phase 1.
- **Re-flowing the Base page information hierarchy** — we keep the reading order the user approved in `c98514a`; we only change the chrome around it.

## 9. Phasing

Recommended build order (each phase is independently reviewable):

### Phase 1 — Foundation (tokens + shell)

1. Token additions in `index.css` (`--color-resource`, `-interactive`, `-prestige`, race palettes, starfield utility).
2. `<html data-race="...">` wiring in `GameLayout` based on player race.
3. Three new primitives: `Section`, `DataGrid`, `Panel`. Deprecate direct `<Card>` usage in page code (keep `Card` in the primitives library for modals).
4. `GameLayout` rewrite: new sidebar structure (6+utility), new header (4 regions, circular timer, drop theme toggle).
5. Starfield background on `<main>`; drop `.bg-grid`.
6. Race emblem SVGs (3 files).

**Exit criteria:** shell looks right on Base page with existing content. Race chrome visibly shifts between `data-race` values. Typography pass done. No new pages touched yet.

### Phase 2 — SVG icon kit

1. Nav icon set (12).
2. Action icons (5).
3. Resource icons (3).
4. Unit silhouettes (~30, all three races).
5. Building silhouettes (~18).

Drawn in a single pass so they share visual language. Commit as SVG-in-JSX components under `src/ReactClient/src/components/icons/`.

### Phase 3 — Page patterns

1. Base page — section refactor, DataGrid buildings, terminal-style recent log.
2. Units page — DataGrid + deployed-block variant.
3. Ranking page — tiered typography + race colors + alliance-tab merge.
4. Research page — track restyle.
5. Market page — Trade merged as tab.
6. Chat/Messages — race-colored names only.

Each page is one PR, reviewable in isolation. All still consume the same data/contexts/queries.

### Phase 4 — Login / JoinGame polish

1. Splash CSS composition on login.
2. Large race-emblem tiles on JoinGame.

### Phase 5 — Cleanup

1. Remove `ThemeContext`, `.light` classes, theme toggle.
2. Remove `.bg-grid` utility.
3. Remove unused Lucide icons (some of the 90 SVGs replace them; audit and drop unused imports).
4. Remove `BuildQueue` standalone card wrapper; keep logic, wrap in `<Section>`.

## 10. Success criteria

A user who played SCO should:

- See the dark-space starfield and know immediately "this is SCO."
- See their race chrome and feel identity without reading any labels.
- Spot their own rank on the Ranking page within 1 second.
- Notice that resource numbers are green, links are blue, prestige is gold — with no legend needed.

A user who never played SCO should:

- See no more visual noise than a modern admin dashboard.
- Find every feature in ≤ 2 clicks from anywhere in the game.
- Recognize faction colors consistently across the UI.

Quantitatively:

- Nav items reduced from 13 to 6 primary + 6 utility.
- Card borders removed from ≥ 90% of page code (`grep -c "rounded-lg border bg-card"` → near-zero).
- Header control count reduced from 6 to 4.
- Asset weight of the new SVG kit ≤ 30KB gzipped total.

## 11. Risks

- **SVG icon kit workload.** 90 icons is a lot. Mitigation: share silhouettes across races where the *shape* is the same unit (e.g., a "worker" icon whose color and accent change per race) — reduces the true count to ~60.
- **Breaking existing E2E tests.** The Playwright suite has selectors on card borders and icon labels. Need to update selectors in parallel with Phase 1.
- **Disagreement on race aggressiveness.** If the stripe + emblem + accent feels too subtle, we have room to intensify (e.g., add a race-specific subtle texture overlay on the sidebar background). Budgeted as a Phase-1-late adjustment, not a blocker.
- **Color contrast.** Race palettes must still pass WCAG AA on their usual backgrounds. Zerg rust especially needs a contrast check against the sidebar gradient.

## 12. Appendix — reference material

- `~/repos/play/sco-revengineer/ux-bundle/CURRENT-UX-DOCUMENTATION.md` — exhaustive SCO UX reference
- `~/repos/play/sco-revengineer/ux-bundle/DESIGN-MODERNIZATION-GUIDE.md` — modernization bridge
- `~/repos/play/sco-revengineer/ux-bundle/design-tokens.json` — extracted token values
- `~/repos/play/sco-revengineer/ux-bundle/original-assets/` — 157 reference GIFs + original CSS
- `docs/UI-UX-DESIGN.md` — current BGE UI documentation
- Mockups produced during brainstorming: `.superpowers/brainstorm/633869-1776616317/content/`
  - `base-terran-mockup.html` — approved Terran reference
  - `zerg-protoss-variants.html` — approved race variants
  - `ranking-units.html` — approved Ranking + Units patterns
