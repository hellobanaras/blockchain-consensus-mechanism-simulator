# PROGRESS — Resume Cursor

> **Append-only log of meaningful units of work.** Future sessions read top-to-bottom and pick up at the first incomplete item. Update on every commit and after every multi-step task.

**Last updated:** 2026-05-19

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
