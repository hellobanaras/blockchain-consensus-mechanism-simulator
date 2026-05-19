# Claude Code — Repo Guide

> **For Claude (and any AI assistant) operating in this repository.** This file is auto-loaded into context when you start work here. Read it first. Anything you learn that future sessions should know goes into `.claude/memory/` (project-level memory) or back into this file.

---

## 1. What this repo is

An **integrated simulation framework** for the design and empirical evaluation of five blockchain consensus protocols — **PoW, PoS, DPoS, PBFT, PoET** — implemented as the M.Tech thesis of Umesh Kumar (Executive M.Tech AIDSE, IIT Patna, roll `25s03res52`, `umesh_25s03res52@iitp.ac.in`).

The first formal demo (Project I, Sem III) is **Saturday 23 May 2026, Group 7, 4:00–5:00 PM IST**. The final thesis defence follows in Semester IV.

The approved problem statement is in [`mtech/ANNEXURE_I_ABSTRACT_OF_PROBLEM_STATEMENT.pdf`](mtech/ANNEXURE_I_ABSTRACT_OF_PROBLEM_STATEMENT.pdf). The full thesis scope (single authoritative source) is [`docs/THESIS_SCOPE_SPECIFICATION.md`](docs/THESIS_SCOPE_SPECIFICATION.md).

## 2. Tech stack

- **.NET 9** · C# 13 · Blazor Server · ASP.NET Core · SignalR · Entity Framework Core 9 · PostgreSQL 16
- UI: Bootstrap 5 + Chart.js
- Auth: ASP.NET Identity (RBAC: `Admin`, `Operator`, `Viewer`)
- Container: Docker / Docker Compose
- Presentation: Marp Markdown → `.pptx` + `.pdf`

## 3. Solution layout

Two ASP.NET hosts share a single Postgres. **Api is read-only**; **Web owns the
simulation runtime and writes**. See `Consensus.Core/Services/DbBackedSimulationService.cs`
for why this split is encoded as two different `ISimulationService` implementations.

```
src/
├── Consensus.Core    Domain entities, 5 protocol implementations, SimulationService
│                     (in-memory runtime), DbBackedSimulationService (read-only),
│                     AnalyticsService, FairnessMetrics, repository interfaces
├── Consensus.Data    EF Core DbContext, Postgres migrations, repository implementations,
│                     IdentitySeeder
├── Consensus.Api     READ-only REST host. Registers DbBackedSimulationService so its
│                     controllers (SimulationController, AnalyticsController,
│                     BlocksController, SimulationResultsController, SimulationsController,
│                     DashboardAnalyticsController) serve DB-backed data without holding
│                     a simulation runtime. Write endpoints return NotSupported by design.
└── Consensus.Web     Blazor Server UI + SignalR hubs (SimulationHub, AnalyticsHub) +
                      hosted background runner (SimulationHostedService). Owns the
                      in-memory SimulationService runtime + persists rounds/blocks/events.
                      Fetches DISPLAY data (lists, dashboards) from Api via
                      ConsensusApiClient (Services/ConsensusApiClient.cs).

docs/
├── THESIS_SCOPE_SPECIFICATION.md    Single source of truth for thesis scope
├── EXPERIMENT_PROTOCOL.md           Scenario S1–S7 procedure
├── METRICS_REFERENCE.md             Every metric (Gini, entropy, p95/p99, ...)
├── experiments/S{1..7}*.json        Scenario configs
└── presentation/                     Marp source + built .pptx/.pdf

mtech/                                Approved Annexure I problem statement, IIT Patna
                                      thesis format manual, supervisor consent letter
scripts/
├── startup.sh                       Container entrypoint: wait-for-postgres then dotnet run
├── build-deck.sh                    Marp → .pptx + .pdf
├── expand-experiment-suite.sh       JSON suite → per-run API payloads
└── init-db.sql                      Postgres init (uuid-ossp, pgcrypto)

.claude/                              REPO-LOCAL CLAUDE CODE INFRASTRUCTURE — see §10
```

Clean Architecture: **Core has no project reference to Data**. Persistence happens through `IRepository<T>` interfaces declared in Core (`Consensus.Core/Repositories/IRepositories.cs`) and implemented in Data. Never add a Core→Data ref.

## 4. Quick start (one command)

```bash
docker compose up --build
```

| Surface              | URL                          |
|----------------------|------------------------------|
| Blazor UI (Web)      | http://localhost:8080        |
| REST Api + Swagger   | http://localhost:5101        |
| Postgres (host-side) | localhost:5433               |
| pgAdmin (optional)   | http://localhost:5050 (with `--profile debug`) |

Ports picked to dodge common dev-box collisions (3000 → Next.js, 5432 → local Postgres, 5000/5001 → other .NET apps).

Seeded admin user: `admin@consensus-lab.dev` / `Admin@123!` (created on every fresh DB by `IdentitySeeder`).

For non-Docker local dev:

```bash
dotnet build BlockchainConsensusSimulator.sln
dotnet ef database update --project src/Consensus.Data --startup-project src/Consensus.Web
dotnet run --project src/Consensus.Web
```

See [`DOCKER_GUIDE.md`](DOCKER_GUIDE.md) for the full Docker walkthrough and troubleshooting.

## 5. Critical files (where the action lives)

| File | Purpose |
|------|---------|
| `src/Consensus.Core/Services/SimulationService.cs` | Core orchestrator. Round loop, persistence hooks, seeded RNG plumbing. |
| `src/Consensus.Core/Interfaces/IConsensusProtocol.cs` | Protocol contract — every protocol must implement, including `SetRandom(Random)`. |
| `src/Consensus.Core/Protocols/{Pow,Pos,Dpos,Pbft,Poet}Protocol.cs` | The five protocol implementations. |
| `src/Consensus.Core/Analytics/FairnessMetrics.cs` | Gini, Shannon entropy, percentile (NumPy-style linear interpolation). |
| `src/Consensus.Core/Services/AnalyticsService.cs` | `GetAlgorithmPerformanceAsync` — where Gini/entropy/p95/p99 are computed against DB-resident blocks. |
| `src/Consensus.Web/Components/Pages/Simulations.razor` | "New Simulation" modal — wires to `ISimulationService`. |
| `src/Consensus.Web/Components/Pages/SimulationDashboard.razor` | Live-update dashboard. SignalR group name is `JoinSimulation` (not `JoinSimulationGroup`). |
| `src/Consensus.Web/Hubs/SimulationHub.cs` | SignalR hub server side. |
| `src/Consensus.Web/Program.cs` | DI registration + startup-time migration + identity seeding. |
| `src/Consensus.Data/ConsensusDbContext.cs` | EF Core context. |
| `Dockerfile` / `docker-compose.yml` | Container stack (web + postgres + optional pgadmin). |

## 6. Common workflows

### Add a new consensus protocol

1. New file `src/Consensus.Core/Protocols/<Name>Protocol.cs` implementing `IConsensusProtocol`
2. Add enum value to `ConsensusAlgorithm` in `src/Consensus.Core/Enums/ConsensusEnums.cs`
3. Add factory case to `SimulationService.CreateConsensusProtocol`
4. Add an `<option>` to the `<select>` in `Simulations.razor`
5. (Optional) Add a specialized chart `<Name>AnalyticsChart.razor` and wire into the analytics page

### Add a new metric

1. Add a method to `Consensus.Core/Analytics/FairnessMetrics.cs` (or extend the existing methods)
2. Add property on `Consensus.Core/Models/AnalyticsModels.cs::AlgorithmPerformanceMetrics`
3. Populate it inside `AnalyticsService.GetAlgorithmPerformanceAsync`
4. (Optional) Surface in `AnalyticsService.ExportToCsv` / `ExportToJson`

### Add an EF migration

```bash
dotnet ef migrations add <DescriptiveName> \
  --project src/Consensus.Data --startup-project src/Consensus.Web
dotnet ef database update \
  --project src/Consensus.Data --startup-project src/Consensus.Web
```

Migrations auto-apply at container startup when `ConsensusSimulator:AutoMigrateDatabase=true` (default in `docker-compose.yml`).

### Build the thesis presentation deck

```bash
./scripts/build-deck.sh           # rebuilds docs/presentation/*.pptx and *.pdf via Marp CLI
```

Source: `docs/presentation/thesis-presentation.md` (Markdown + Marp directives).

### Run a benchmark experiment

```bash
./scripts/expand-experiment-suite.sh docs/experiments/S1-baseline.json \
  --output docs/experiments/payloads/S1
# then POST each *.api.json to /api/Simulation/start
```

## 7. Conventions and constraints

- **Don't break the Core→Data boundary.** Persistence flows through `Consensus.Core/Repositories/IRepositories.cs`. Resolve repos from `IServiceScopeFactory` inside `SimulationService` (the runtime task outlives a single Scoped lifetime).
- **Don't add `dotnet ef` to the container.** Migrations run inside `Program.cs` via `DatabaseInitializationService`.
- **Default to writing no comments.** Only explain *why* something non-obvious is the way it is.
- **Best-effort persistence.** Every DB write inside `SimulationService` is wrapped in try/catch that only logs — a DB failure must never crash a live simulation demo. The kill-switch is the env var `Simulation__PersistToDb=false`.
- **Seeded RNG.** Reproducibility is a thesis claim. When you touch a protocol, route every `Random` use through the `_random` field set by `IConsensusProtocol.SetRandom(Random)`.
- **No new abstractions unless required.** The repository pattern, the analytics export schema, and `FairnessMetrics` are intentionally simple; resist generalising.

## 8. Things deliberately deferred (post Saturday 23 May)

Listed with full effort estimates in `.claude/plans/may-23-mvp-plan.md` §"Deferred — Gap Analysis":

1. xUnit test rebuild (intentionally removed by the user, to rewrite from scratch in Phase 4)
2. Full S1–S7 benchmark execution with ≥30 reps
3. Federated-learning payload integration (service exists, hook into round loop is deferred)
4. Runtime fault-injection API (S5 partition)
5. `ISimulationService.PauseSimulationAsync` / `ResumeSimulationAsync`
6. Repository-pattern refactor of `SimulationService`'s direct DbContext usage
7. Consolidation of `Analytics.razor` / `AnalyticsV3.razor` / `AnalyticsDashboard.razor`
8. Batch experiment runner CLI on top of `expand-experiment-suite.sh`
9. Protocol-suitability scoring model for RQ5
10. PoET cryptographic-RNG seeding (or thesis-doc the limitation)
11. DI lifetime correction for `SimulationService`
12. README sync after all the above lands

## 9. Recovery & resume — surviving mid-task failure

If a session is interrupted or a multi-step task partially completes, the next session can resume cleanly by reading these three files in order:

1. [`CLAUDE.md`](CLAUDE.md) — *you are here* — repo overview
2. [`.claude/PROGRESS.md`](.claude/PROGRESS.md) — running log of completed vs in-flight work, with cursor pointing at the next concrete step
3. [`.claude/plans/may-23-mvp-plan.md`](.claude/plans/may-23-mvp-plan.md) — the canonical 5-day MVP plan with file-path + line-range edits

**Rule:** any time you complete a meaningful unit of work (one of the Day-N items in the plan, or any task that touched the build), append a line to `.claude/PROGRESS.md` under the appropriate section. That file is your durable cursor.

Recovery script (manual):

```bash
git status                                       # see uncommitted work
dotnet build BlockchainConsensusSimulator.sln    # does it still compile?
cat .claude/PROGRESS.md                          # where did the previous session stop?
```

## 10. The `.claude/` directory — repo-local agent infrastructure

```
.claude/
├── plans/                  Long-form implementation plans (the canonical one is may-23-mvp-plan.md)
├── PROGRESS.md             Running cursor: what's done, what's next
├── commands/               Repo-local slash commands
│   ├── verify-mvp.md       Run the full 12-step MVP smoke checklist
│   ├── build-deck.md       Rebuild thesis presentation (.pptx + .pdf)
│   ├── start-stack.md      docker compose up --build with health-wait
│   └── resume.md           Resume from .claude/PROGRESS.md cursor
├── agents/                 Repo-local subagent definitions
│   ├── consensus-protocol-expert.md   Specialist for IConsensusProtocol implementations
│   └── thesis-research.md             Specialist for thesis-scope alignment
├── memory/                 Project-level memory (write surprising facts here)
└── settings.json           Pre-approved bash commands (committed, safe defaults)
```

`settings.local.json` (if you create one) is git-ignored — that's your personal overrides.

## 11. User profile — Umesh

- **Senior engineer**, prefers terse responses and concrete file-and-line citations over high-level prose.
- **Highly time-constrained** during exam prep — when given a multi-day plan, push through end-to-end and report when ready to test rather than checking in between phases.
- **Wants reproducibility everywhere** — seeded RNG, persisted exports, dockerised stack.
- **Won't be in this conversation later.** Everything that future sessions need to know about *how this repo works* must live in this file or in `.claude/`.

Save new memories (preferences, decisions, project context) into `.claude/memory/` using the same conventions as the global memory system. Index them in `.claude/memory/MEMORY.md` (create on demand).

## 12. Slash commands (repo-local)

| Command          | What it does                                                            |
|------------------|-------------------------------------------------------------------------|
| `/verify-mvp`    | Runs the 12-step Saturday-morning smoke checklist                       |
| `/build-deck`    | Marp rebuild of `.pptx` + `.pdf` from the slide Markdown                |
| `/start-stack`   | `docker compose up --build` and wait for healthy                        |
| `/resume`        | Read `.claude/PROGRESS.md` and propose the next concrete step           |

Full definitions in `.claude/commands/`.

## 13. Pointers

- Plan: `.claude/plans/may-23-mvp-plan.md`
- Progress cursor: `.claude/PROGRESS.md`
- Thesis scope: `docs/THESIS_SCOPE_SPECIFICATION.md`
- Experiment protocol: `docs/EXPERIMENT_PROTOCOL.md`
- Metrics: `docs/METRICS_REFERENCE.md`
- Docker guide: `DOCKER_GUIDE.md`
- Annexure I (approved problem statement): `mtech/ANNEXURE_I_ABSTRACT_OF_PROBLEM_STATEMENT.pdf`
