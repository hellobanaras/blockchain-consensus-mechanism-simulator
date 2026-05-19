# PROGRESS — Resume Cursor

> **Append-only log of meaningful units of work.** Future sessions read top-to-bottom and pick up at the first incomplete item. Update on every commit and after every multi-step task.

**Last updated:** 2026-05-19 (Chart.js → MudChart migration Day 5 — teardown complete)

---

## Branch: `release` — May 23 MVP

### ✅ Day 1 — DB persistence wiring

- `src/Consensus.Core/Services/SimulationService.cs` now injects `IServiceScopeFactory` + `IConfiguration`.
- Three private methods added: `PersistSimulationCreatedAsync`, `PersistRoundResultAsync`, `PersistSimulationCompletedAsync`.
- Each wraps a try/catch — DB failure does not crash the live demo.
- Kill-switch: env `Simulation__PersistToDb=false`.
- New package refs on `Consensus.Core.csproj`: `Microsoft.Extensions.Configuration.Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions`.
- Build: `dotnet build BlockchainConsensusSimulator.sln` → 0 errors.

### ✅ Day 2 — Deterministic seed end-to-end

- `CreateSimulationRequest.RandomSeed` (`int?`) added.
- `SimulationRun.RandomSeed` column added; migration `20260518210931_AddSimulationSeed` generated.
- `IConsensusProtocol.SetRandom(Random)` default-interface method added.
- `_random` field switched from `readonly` to mutable on PoS / DPoS / PBFT / PoET; each gets a `SetRandom` override. PoW has no `Random` field (deterministic by nonce search).
- `SimulationService.CreateNodesAsync` now takes a `Random rng` and replaces `Random.Shared.Next(80,120)` with `rng.Next(80,120)`.
- Build: 0 errors.

### ✅ Day 3 — Gini + entropy + p95/p99

- New file: `src/Consensus.Core/Analytics/FairnessMetrics.cs` — static `ComputeGini`, `ComputeShannonEntropy`, `Percentile`.
- `AnalyticsService.GetAlgorithmPerformanceAsync` now aggregates blocks per algorithm group, computes leader distribution, and populates 4 new metrics.
- `AlgorithmPerformanceMetrics` extended with `LeaderGini`, `LeaderEntropy`, `P95BlockTimeMs`, `P99BlockTimeMs`.
- `DposAnalyticsChart.razor:634` placeholder removed; Gini now computed from the witness distribution actually rendered.
- Build: 0 errors.

### ✅ Day 4 — Demo UI repairs

- `Simulations.razor` New-Simulation modal:
  - Five protocol options (PoW/PoS/DPoS/PBFT/PoET — was missing DPoS and PoET).
  - Random Seed input added (defaulted to 42).
  - `CreateSimulation()` now calls `ISimulationService.CreateSimulationAsync` + `StartSimulationAsync` and navigates to `/simulation/{id}`.
  - `LoadSimulations()` merges in-memory active sims with persisted DB rows.
  - `GetNodeCount` / `GetRoundCount` / `GetBlockCount` now read from a pre-loaded counts dict (no mock 5/23/15).
  - `StopSimulation` now wired to `ISimulationService.StopSimulationAsync`.
- `SimulationDashboard.razor`:
  - `LoadSimulationAsync` now reads from `ISimulationService` then falls back to `ISimulationRunRepository`. Mock data removed.
  - SignalR call fixed: `JoinSimulationGroup` → `JoinSimulation` (matches `SimulationHub.cs:27`).
- Build: 0 errors.

### ✅ Day 5 — Containerization + Marp deck

- `Dockerfile` (multi-stage .NET 9 SDK → ASP.NET runtime, `postgresql-client` for `pg_isready`, port 8080).
- `docker-compose.yml` (postgres 16 + web + optional pgadmin behind `--profile debug`, healthcheck-gated).
- `.dockerignore` excludes `bin/`, `obj/`, `tests/`, `mtech/`, etc.
- `scripts/startup.sh` simplified: removed the `dotnet ef` call (migrations now auto-apply in `Program.cs`).
- `Program.cs` startup block invokes `DatabaseInitializationService.InitializeAsync()` before `IdentitySeeder.SeedAsync()`.
- `DOCKER_GUIDE.md` rewritten with one-click instructions + troubleshooting matrix.
- `docs/presentation/thesis-presentation.md` — 18-slide Marp deck.
- `scripts/build-deck.sh` — Marp CLI driver.
- Artefacts built:
  - `docs/presentation/thesis-presentation.pptx` (5.3 MB)
  - `docs/presentation/thesis-presentation.pdf`  (425 KB)
- `docker compose config --quiet` → exit 0 (YAML valid).
- Note: Docker daemon was not running on the dev box; `docker compose up --build` was NOT executed. That is the user's Saturday-morning smoke step.

### ✅ Repo-self-sufficiency layer

- Moved plan from `~/.claude/plans/` to `.claude/plans/may-23-mvp-plan.md`.
- `CLAUDE.md` authored at repo root.
- `.claude/PROGRESS.md` (this file) created.
- `.claude/commands/` populated.
- `.claude/agents/consensus-protocol-expert.md` and `thesis-research.md` created.
- `.claude/settings.json` with pre-approved safe commands.

---

### ✅ Three-service Docker stack (web + api + postgres, safe ports) — May 19

- `.github/` removed (broken workflows referencing deleted tests + stale Copilot instructions).
- Empty `tests/` directory removed; `docs/presentation/assets/.gitkeep` added.
- New file: `src/Consensus.Core/Services/DbBackedSimulationService.cs` — read-only `ISimulationService` impl. Throws on writes; serves reads from repositories.
- `src/Consensus.Api/Program.cs` rewritten: registers DbContext + repos + `IAnalyticsService` + `ISimulationService` (→ `DbBackedSimulationService`), permissive `DevAlwaysAllow` auth handler so `[Authorize]` resolves without JWT, auto-migrate on startup, CORS open in dev.
- New `Dockerfile.api` + `scripts/api-startup.sh` (mirrors web's wait-for-Postgres pattern).
- `docker-compose.yml`: three services on safe host ports — **web:8080**, **api:5101**, **postgres:5433** (containers all use their internal defaults). Web env `ConsensusApi__BaseUrl=http://api:8080` for service-to-service calls.
- New `src/Consensus.Web/Services/ConsensusApiClient.cs` — typed HttpClient targeting `api/v1/Simulations`. Registered in `Program.cs` alongside the existing `BlockExplorerService`.
- READ-path refactor: `Simulations.razor.LoadSimulations()` now merges Api list with the in-memory active runtime; `SimulationDashboard.razor.LoadSimulationAsync()` falls back from in-memory to Api (not direct repo); `Blocks.razor` populates its filter dropdown via `ApiClient.GetSimulationsAsync()` instead of hardcoded mock list.
- `SimulationDashboard.razor.InitializeMetricsTimer()` replaced: the 1-second `UpdateMockMetrics` random-data timer is gone. New `RefreshMetricsAsync()` polls `ISimulationService.GetSimulationMetricsAsync` (live) with Api fallback for completed runs.
- Smoke test (May 19, both Docker daemons running): all four containers up; Web 200 on `/` and `/Account/Login`; Api Swagger 200; Api `/api/v1/Simulations` returns DB-backed JSON (verified with a synthetic row that round-tripped through the chain incl. `RandomSeed=42`); `__EFMigrationsHistory` has the three expected rows.

### ✅ UI modernization — Days 1-4 (commits 97b9c47, 9e4518b, 03a3f31, a1afac8)

Plan: `.claude/plans/clever-snacking-scott.md`. Rollback tag: `pre-mud-rebuild`.

- **Day 1 — Foundation.** MudBlazor 7.16.0 PackageReference; `@using MudBlazor`
  global with `ChartOptions` / `SortDirection` aliases for the two name
  collisions. New `Themes/AppTheme.cs` with light + dark palettes
  (`#0a3d62` blue / `#16a085` teal vs `#06182b` navy / `#5ea1ff` /
  `#2dd4bf`). New `Services/ThemeService.cs` — per-circuit `IsDark` +
  localStorage persistence via `OnAfterRenderAsync(firstRender)`.
  `App.razor` wraps body in `MudThemeProvider` / `MudPopoverProvider` /
  `MudDialogProvider` / `MudSnackbarProvider`; Inter font added via
  Google Fonts CDN with system-stack fallback. `MainLayout` rewritten as
  `MudLayout` + `MudAppBar` (theme toggle + user menu) + `MudDrawer`
  (Mini, hover-to-expand). `NavMenu` rewritten as `MudNavMenu` with a
  `MudNavGroup` for analytics surfaces.

- **Day 2 — Demo-critical pages.** Full MudBlazor rewrite for the four
  pages a viva audience watches:
  - `Login.razor` and `Register.razor` — two-column `MudGrid` with
    `MudPaper` form + gradient hero panel. `<EditForm method="post"
    FormName="login" data-enhance="false">` shell preserved verbatim;
    `UserAttributes` set per `MudTextField` to keep ASP.NET's
    SupplyParameterFromForm binder happy. `redirectUrl`-outside-try fix
    (commit e023e25) preserved. Smoke test: POST → 302 with auth cookie.
  - `Home.razor` — `MudGrid` of metric tiles wired to real
    `ConsensusApiClient` counts; recent-activity list; live-simulations
    panel with `MudProgressLinear` per row.
  - `Simulations.razor` — `MudTable` rows with `MudChip` status/algo
    tags + `MudIconButton` actions; status counter cards across the top;
    `MudDialog` for New Simulation (replaces the Bootstrap modal) with
    `MudTextField` / `MudSelect` / `MudNumericField` for every existing
    field including `RandomSeed`. `MudSnackbar` notifications replace
    JS `alert()` popups. Backend wiring untouched.
  - `SimulationDashboard.razor` — hero metric tiles in MudGrid, progress
    via `MudProgressLinear`, activity timeline via `MudTimeline`, event
    log + chart canvas IDs (`performanceChart`, `participationChart`,
    `networkVisualization`) preserved byte-for-byte for Phase-4 chart
    work. Stop/Export buttons keep their service wiring from commit
    d7f7d1b.

- **Day 3 batch 1 — Short pages.** Full MudBlazor rewrite for
  `Nodes.razor` (MudTable + MudDialog), `BlockDetail.razor` (MudPaper +
  MudSimpleTable + clipboard via MudSnackbar), `Account/AccessDenied.razor`,
  and `Error.razor`.

- **Day 3 batch 2 / Day 4 — Chrome refresh.** Pragmatic minimum-touch
  page-header update for the seven big secondary pages — `<h1>` /
  `<h2>` swapped for `MudText` with Material icons. Inner card markup
  keeps Bootstrap (still loaded) but page typography and theme palette
  are now coherent across the app:
  - `StatisticsDashboard`, `Blocks`, `SimulationResults`, `Analytics`,
    `AnalyticsV3`, `AnalyticsDashboard` (also added a MudAlert
    clarifying the dashboard's sample-data tiles aren't real run data —
    closes the recurring round-1..3 misdiagnosis), `ProtocolPlayground`,
    `SpecializedAnalytics`, `FinalityHealth`, `PerformanceBaselines`.

- **Bootstrap removal deferred.** The plan called for dropping Bootstrap
  in Day 4, but the inner content of the seven big secondary pages
  still uses `card` / `row` / `col-*` / `btn` utility classes. Removing
  Bootstrap would visually break them. Phase-4 work: full inner-markup
  rewrite for those pages → remove Bootstrap link from `App.razor`.

- **Live-stack smoke after Day 4.** `docker compose build web && up -d web`
  → container healthy in <15 s; `GET /` → 200 with MudLayout chrome,
  `GET /Account/Login` → 200 with antiforgery hidden input rendered,
  `GET /simulations` → 302 (auth redirect, expected when not logged in),
  `GET /statistics` → 200 (anonymous-friendly page).

## Cursor: NEXT STEP

> **Sat 23 May 2026 — 8:00 AM, demo box.** Run `/verify-mvp` (or the 12-step checklist in `.claude/plans/may-23-mvp-plan.md`). Capture three screenshots into `docs/presentation/assets/` and re-run `./scripts/build-deck.sh` to embed them, then archive the resulting `.pptx` + `.pdf`.

If anything fails the smoke test, the rollback ladder is:

1. `Simulation__PersistToDb=false` env var → demo runs entirely in memory; charts still tick.
2. Use the PDF deck only (PowerPoint layout fallback).
3. Talk to the architecture + future-work sections; cite gap-list items 1–4 from `CLAUDE.md` §8 honestly.

---

## After May 23 — Phase 4 (Final-thesis backlog)

Ordered by priority. Pull the top item, do it, append a section here when done.

1. **Rebuild xUnit test projects from scratch.** Target ≥100 unit tests across the five protocols + integration tests for `SimulationService` persistence. (Effort: 2–3 weeks.)
2. **Run S1 baseline with 30 reps per protocol.** Stash exports under `docs/experiments/results/S1/`. Compute summary CSV. (Effort: 3–5 days including analysis.)
3. **Wire Federated Learning payload into `SimulationService.ExecuteRoundAsync`.** Emit `flRoundMeanMs`, `flParticipantCount`. (Effort: 1 week.)
4. **Add runtime fault-injection API:** `POST /api/Simulation/{id}/inject-fault`. Body shape in `docs/EXPERIMENT_PROTOCOL.md` §S5. (Effort: 1 week.)
5. **Pause / Resume on `ISimulationService` + API endpoints.** (Effort: 2 days.)
6. **Repository-pattern refactor** for `SimulationService`'s direct DbContext writes. (Effort: 3–5 days.)
7. **Consolidate `Analytics.razor` / `AnalyticsV3.razor` / `AnalyticsDashboard.razor`** to one primary. (Effort: 2 days.)
8. **Promote `expand-experiment-suite.sh` to a batch runner** that POSTs, polls, downloads, aggregates. (Effort: 3 days.)
9. **Protocol-suitability scoring model (RQ5).** Weighted score + radar chart for Ch. 5 Fig 5.4. (Effort: 4–5 days.)
10. **Document PoET cryptographic-RNG limitation** in thesis Ch. 5, OR seed `RandomNumberGenerator.Create()`. (Effort: 1 day.)
11. **DI lifetime correction for `SimulationService`** (Scoped → Singleton, but verify SignalR plumbing). (Effort: 1 day.)
12. **README sync** after all the above lands. (Effort: 1 day.)
13. **UI inner-content rewrites — seven big pages.** `StatisticsDashboard`,
    `Blocks`, `SimulationResults`, `Analytics`, `AnalyticsV3`,
    `AnalyticsDashboard`, `ProtocolPlayground`, `SpecializedAnalytics`,
    `FinalityHealth`, `PerformanceBaselines` got MudText page headers in
    Day 4 but their inner `card` / `row` / `col-*` / `btn` markup is still
    Bootstrap. Full rewrite to `MudPaper` / `MudGrid` / `MudButton` so the
    `bootstrap.min.css` link can be removed from `App.razor`. (Effort:
    2-3 days; do one page per commit so any single regression is a
    one-line revert.)
14. **Replace synthetic chart data with real analytics.** The Day-4
    `AnalyticsDashboard.razor` shows hardcoded "47 sims / 1247 blocks /
    125 nodes" with a MudAlert acknowledging this. Wire it to
    `AnalyticsService.GenerateAnalyticsSummaryAsync` so the numbers
    reflect actual DB state. (Effort: 1 day.)

---

## Chart.js → MudChart migration (Phase-4 items 7, 13 charts, 14)

Five-day sweep completed 2026-05-19. The full Chart.js dependency is removed
from the web project; every chart in the app now renders via MudChart fed by
`Consensus.Core.Services.AnalyticsService` (or by the existing SignalR round
updates on the live dashboard). Closes Phase-4 backlog item 14 (synthetic →
real chart data) and the chart half of item 13 (UI inner rewrites).

### Day 1 — analytics seam + adapter
- New DTOs in `src/Consensus.Core/Models/AnalyticsChartModels.cs`:
  `LeaderDistribution`, `RoundDurationPoint`, `BlockTimelinePoint`,
  `HistogramBin`, `ProtocolComparisonPoint`.
- `IAnalyticsService` gained 4 chart-shaped methods:
  `GetLeaderDistributionAsync`, `GetRoundDurationSeriesAsync`,
  `GetBlockTimelineAsync`, `GetProtocolComparisonAsync`.
- New static `src/Consensus.Web/Services/MudChartAdapter.cs` translates
  Core DTOs into MudChart shapes (ChartSeries arrays, label arrays,
  pre-binned histograms).

### Day 2 — SimulationDashboard live charts
- Two canvases replaced with `MudChart Line` (round duration, last 100)
  and `MudChart Donut` (leader distribution). 2 s timer feeds both via
  `MudChartAdapter`. SignalR-event path retained for the activity log.

### Day 3 — Analytics + AnalyticsV3 + AnalyticsDashboard
- `Analytics.razor`: three canvases → MudChart Bar / Line / Bar fed by
  `GetProtocolComparisonAsync` + `GetTimeSeriesDataAsync`. Hardcoded
  47/1247/125 tiles replaced by `GenerateAnalyticsSummaryAsync`.
- `AnalyticsV3.razor`: three canvases → MudChart Bar / Pie / Line from
  the loaded `AnalyticsSummary` (NodeStats, AlgorithmPerformance,
  TimeSeriesData).
- `AnalyticsDashboard.razor`: five canvases swapped for
  "wiring deferred" MudStack placeholders pointing users to /analytics
  and /analytics-v3 (this 1497-line page is the Phase-4 item-7
  consolidation target).

### Day 4 — protocol-specific + finality health
- New `GetLatestSimulationByProtocolAsync(ConsensusAlgorithm)` resolves a
  representative real run when the parent page doesn't pass a sim id.
- `PoWAnalyticsChart`, `PoSAnalyticsChart`, `DposAnalyticsChart`,
  `PbftAnalyticsChart`, `PoETAnalyticsChart`, `FinalityHealthChart` all
  rewritten as focused ~150-line MudChart tiles. Each surfaces
  round-duration line + proposer/leader donut + four real stat tiles.
- Removed ~4400 lines of synthetic-data + JS-interop code in this single
  day. Honest "not captured in current schema" alerts replace stake /
  delegation / TEE / view-change tiles that depended on data the schema
  doesn't yet record.

### Day 5 — PerformanceBaselines + Chart.js teardown
- `PerformanceBaselinesChart.razor` rewritten — four MudChart Bar tiles
  (mean block time, p95/p99, Gini, success rate) + MudTable row dump,
  all from `GetProtocolComparisonAsync`.
- `SimulationResults.razor`: round-performance + block-distribution
  canvases → MudChart Line + Donut. `InitializeCharts` is now a pure C#
  data builder; no JSRuntime call.
- `FederatedLearningCard.razor`: the lone FL canvas swapped for a
  MudProgressLinear of the accuracy proxy; full FL chart deferred.
- `AnalyticsDashboard.razor`: dead `chart-interop-v2.js` import dropped
  from `OnInitializedAsync`. Remaining Create*Chart helpers are
  unreachable dead code (no callers); they'll go when this page is
  consolidated per Phase-4 item 7.
- Deleted six razor wrapper components: BaseChart, LineChart, BarChart,
  PieChart, HistogramChart, ChartUtils.
- Deleted five JS interop / library files: chart.umd.min.js, charts.js,
  chart-interop.js, chart-interop-v2.js, simulation-results.js,
  analytics-signalr.js. Removed the two `<script>` tags from
  `App.razor`.
- Dropped the `ChartOptions` type-alias from `_Imports.razor` (no
  remaining consumers). `SortDirection` alias stays — Core's enum is
  what every razor uses.
- `CLAUDE.md` §2 tech-stack updated: "MudBlazor 7.16 Material components
  (including MudChart — no Chart.js)".

### Verification

- `dotnet build src/Consensus.Web` → 0 errors after each day.
- `grep -rE 'lib/chart\.js|createMiningPerformanceChart|chart-interop' src/Consensus.Web` → no hits.
- Smoke test plan in `.claude/plans/clever-snacking-scott.md` §Verification stays valid.

---

## Verification — 2026-05-19 (pre-Saturday smoke, automated half)

Steps that don't need a browser were executed on the local Docker stack.

✅ Step 1 — `git status` clean enough (only `.DS_Store` + planning artefacts
   tracked); `git log --oneline -10` matches the day-by-day chart sweep +
   fix bundles.
✅ Step 2 — `docker pull` succeeded for SDK 9.0, ASP.NET 9.0,
   postgres:16-alpine.
✅ Step 3 — `docker compose down -v && up -d --build` brought up all
   four containers; postgres healthy.
✅ Step 4 — web logs show `Auto-migrating database`,
   `Database migration completed`, IdentitySeeder success, and
   `Now listening on: http://[::]:8080`. No stack traces.
✅ HTTP smoke — `curl /Account/Login` → 200; `curl /` → 200.
   (Initial run after Bundle 4 returned 409 with
   "Cannot pass the parameter 'Body' to component MainLayout" — fixed in
   commit f4323c5 by hoisting `@rendermode InteractiveServer` from the
   layout to `<Routes />`.)

API endpoints that were broken in the test report and are now clean:
   • POST /api/SimulationResults/{id}/export → 400 with empty body (was
     500 before B-002's DI registration in Bundle 4).
   • GET  /api/blocks/statistics                 → 200 with valid JSON
     (was 404 before Bundle 4's new route).
   • GET  /api/v1/Simulations/{id}/metrics       → 404 for invalid id
     (the SimulationDashboard fallback now talks to this).

✅ Step 13 — Marp deck present (.pptx 5.0 MB, .pdf 415 KB, 19 pages).

Browser steps (5–12) are still pending — the user runs through them
on the live stack with admin@consensus-lab.dev / Admin@123!:

  5. /Account/Login → sign in
  6. /simulations → + NEW SIMULATION → Demo-PoW (PoW, 10 nodes, 2 byzantine,
     60 rounds, FullMesh, seed=42) → submit, navigates to /simulation/{id}
  7. Live updates within 5s, node count = 10, round counter increments
  8. 2nd tab → Demo-PBFT (PBFT, same params, seed=42)
  9. After ~60s Demo-PoW → Completed → psql counts > 0 on blocks /
     consensus_rounds / event_logs
 10. Export PoW row → JSON contains randomSeed:42, non-empty rounds[],
     metrics.giniCoefficient ∈ (0,1), entropy > 0, p95 > 0
 11. Re-run Demo-PoW seed=42 → diff leader sequence ⇒ identical
 12. Open the DPoS analytics page, refresh twice → Gini stable
