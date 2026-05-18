# May 23 Presentation MVP — Blockchain Consensus Simulator

**Student:** Umesh Kumar — Executive M.Tech. AIDSE, IIT Patna (Group 7, Sat 23 May 2026, 4–5 PM)
**Repo:** `/Users/ukumar/source/repos/blockchain-consensus-mechanism-simulator` (branch `release`)
**Days available:** 5 (Mon 18 → Fri 22 implementation, Sat 23 demo)

---

## Context

The repo audit showed a mature platform shell (5 protocols, EF schema, SignalR, Blazor UI, RBAC, 7 experiment JSONs) but with research-facing gaps blocking thesis claims:

1. `SimulationService` runs purely in-memory — no rows are saved during a run, so exports return empty.
2. No deterministic random seed — the `randomSeed: 42` in every experiment JSON is currently ignored.
3. Gini coefficient and Shannon entropy are required for hypothesis H3 but exist only as DTO fields (`AnalyticsModels.cs:106,112`) and a `random.NextDouble()` placeholder in `DposAnalyticsChart.razor:634`.
4. `Simulations.razor:399` "New Simulation" button is an `alert("Feature coming soon!")` stub.
5. `SimulationDashboard.razor:375` loads a hardcoded mock simulation instead of the real one, and invokes the wrong SignalR method name (`JoinSimulationGroup` vs `JoinSimulation` at hub).
6. No `Dockerfile` or `docker-compose.yml` has ever existed in git — they need to be authored from scratch.
7. No presentation deck exists.

User decisions (asked & answered):
- **MVP scope:** demo path repairs + DB persistence + deterministic seed + Gini/entropy/p95/p99 (most aggressive option).
- **Deck format:** Marp Markdown compiling to .pptx and PDF, stored in repo.
- **DB:** PostgreSQL 16 in docker-compose (matches existing migrations + `scripts/startup.sh`).
- **Tests:** intentionally deferred to post-thesis; not in MVP.

Outcome we are aiming at on Sat 23 morning: `git clone` → `docker compose up` → browser to `localhost:3000` → log in → start a seeded PoW + PBFT simulation from the UI → watch live rounds → export JSON containing real rounds, Gini, entropy, p95/p99 → re-run with same seed and get identical leader sequence.

---

## Day-by-Day Implementation

### Day 1 (Mon 18) — DB persistence wiring

Make `SimulationService` write every entity it produces. Critical foundation; everything else depends on it.

- **`src/Consensus.Core/Services/SimulationService.cs:14-24`** — change constructor signature from `(ILogger<SimulationService>)` to `(ILogger<SimulationService>, IServiceScopeFactory)`. Store the factory as `_scopeFactory`. (Do not inject `ConsensusDbContext` directly — `SimulationService` is registered Scoped but its background task at L120 outlives a single scope; resolve `ConsensusDbContext` from a fresh scope per write.)
- **`src/Consensus.Core/Services/SimulationService.cs:44-79`** (CreateSimulationAsync) — after node generation (L79), open a scope: `using var scope = _scopeFactory.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ConsensusDbContext>(); db.SimulationRuns.Add(simulation); foreach (var n in nodes) db.Nodes.Add(n); await db.SaveChangesAsync();`. Wrap in `try/catch` that logs but does not throw.
- **`src/Consensus.Core/Services/SimulationService.cs:282-325`** (round-loop body) — after `OnRoundCompleted` at L325, open a scope and insert `round` (ConsensusRound), `newBlock` if non-null, and a single `EventLog` row (`EventType="RoundCompleted"`, payload = JSON of `result.Metrics`). One `SaveChangesAsync` per round.
- **`src/Consensus.Core/Services/SimulationService.cs:369-386`** (completion block) — open scope, reload simulation, set `Status=Completed`, `CompletedAt`, `Progress=100`, `TotalTransactions`, save.
- **Feature-flag everything**: read `_config.GetValue<bool>("Simulation:PersistToDb", true)` and skip the writes if false. Adds an instant rollback if persistence misbehaves during the live demo.
- **`src/Consensus.Web/Program.cs`** — `ISimulationService` registration: leave as `Scoped` for now (existing SignalR plumbing works); only change if smoke test surfaces a captured-scope bug.

**Risk gate at end of Day 1:** start one sim via `POST /api/Simulation/start`, then `psql -c "SELECT count(*) FROM blocks WHERE simulation_run_id = '<id>'"` returns > 0. If yes, persistence works; commit and move on.

### Day 2 (Tue 19) — Deterministic seed end-to-end

- **`src/Consensus.Core/Models/SimulationModels.cs`** (`CreateSimulationRequest`) — add `public int? RandomSeed { get; init; }`.
- **`src/Consensus.Core/Entities/SimulationRun.cs`** — add `public int? RandomSeed { get; set; }`. Generate migration:
  ```bash
  dotnet ef migrations add AddSimulationSeed \
    --project src/Consensus.Data --startup-project src/Consensus.Web
  dotnet ef database update --project src/Consensus.Data --startup-project src/Consensus.Web
  ```
- **`src/Consensus.Core/Services/SimulationService.cs:44-66`** — create one `Random` per simulation: `var rng = request.RandomSeed.HasValue ? new Random(request.RandomSeed.Value) : Random.Shared;`. Set `simulation.RandomSeed = request.RandomSeed;`.
- **`src/Consensus.Core/Services/SimulationService.cs:511-534`** (`CreateNodesAsync`) — accept `Random rng` argument; replace `Random.Shared.Next(80, 120)` at L525 with `rng.Next(80, 120)`.
- **`src/Consensus.Core/Interfaces/IConsensusProtocol.cs:31`** — add default-implemented `void SetRandom(Random rng) { }`. Default no-op keeps every other protocol/test compiling.
- **`src/Consensus.Core/Protocols/PowProtocol.cs`**, **`PosProtocol.cs:48`**, **`DposProtocol.cs:56`**, **`PbftProtocol.cs:64`**, **`PoetProtocol.cs:39`** — change `private Random _random = new Random();` to `private Random _random = new Random();` plus `public void SetRandom(Random rng) => _random = rng;` override. No call-site changes inside the protocols.
- **`src/Consensus.Core/Services/SimulationService.cs:65-66`** — after `CreateConsensusProtocol(...)` call `protocol.SetRandom(rng);` before `InitializeAsync`.
- Leave `PoetProtocol.cs:465` (`RandomNumberGenerator.Create()`) alone — document in thesis Ch. 5 limitations that cryptographic randomness intentionally is not seeded.

**Verification:** start two sims with `RandomSeed=42`, dump the `leader_node_id` sequence from `consensus_rounds` for each, must be identical.

### Day 3 (Wed 20) — Gini, Shannon entropy, p95/p99 in `AnalyticsService`

- **NEW `src/Consensus.Core/Analytics/FairnessMetrics.cs`** (~60 LOC, no deps):
  - `static double ComputeGini(IEnumerable<int> counts)` — sort ascending, `G = (2·Σ(i·xᵢ) − (n+1)·Σxᵢ) / (n·Σxᵢ)`. Returns 0 if total is 0.
  - `static double ComputeShannonEntropy(IEnumerable<int> counts)` — `H = −Σ pᵢ·log₂(pᵢ)` over non-zero proportions.
  - `static double Percentile(IEnumerable<double> values, double p)` — linear interpolation on sorted list.
- **`src/Consensus.Core/Services/AnalyticsService.cs`** — in `GetAlgorithmPerformanceAsync` (locate by method name), build `leaderDistribution = blocks.GroupBy(b => b.ProposerId).ToDictionary(g => g.Key, g => g.Count())` from `ConsensusDbContext`, then populate `GiniCoefficient`, `BlockDistributionEntropy`, `P95BlockTimeMs`, `P99BlockTimeMs` from consecutive `Block.Timestamp` diffs.
- **`src/Consensus.Core/Models/AnalyticsModels.cs`** — add `double P95BlockTimeMs`, `double P99BlockTimeMs`. Mirror in `src/Consensus.Api/Models/AnalyticsModels.cs` and `src/Consensus.Web/Models/Api/AnalyticsModels.cs`.
- **`src/Consensus.Web/Components/Charts/DposAnalyticsChart.razor:634`** — replace `GiniCoefficient = 0.3 + random.NextDouble() * 0.4` with the value pulled via the existing analytics fetch.

**Verification:** export a finished sim, check `metrics.giniCoefficient ∈ (0,1)`, `metrics.blockDistributionEntropy > 0`, `metrics.p95BlockTimeMs > 0`.

### Day 4 (Thu 21) — Demo path UI repairs

- **`src/Consensus.Web/Components/Pages/Simulations.razor:399-410`** — replace the `alert` stub with: build `CreateSimulationRequest` from the modal form bindings (markup already at L217-276), inject `ISimulationService` at top of file, call `CreateSimulationAsync(req)` then `StartSimulationAsync(id)`, then `Navigation.NavigateTo($"/simulations/dashboard/{id}")`. Add one `<input type="number" optional>` for `RandomSeed`.
- **`src/Consensus.Web/Components/Pages/Simulations.razor:503-518`** — replace stubs with `simulation.Nodes?.Count ?? 0`, `simulation.ConsensusRounds?.Count ?? 0`, `simulation.Blocks?.Count ?? 0`.
- **`src/Consensus.Web/Components/Pages/SimulationDashboard.razor:371-407`** (`LoadSimulationAsync`) — delete the hardcoded mock. Inject `ISimulationService` and `ConsensusDbContext`. Try in-memory first: `await SimulationService.GetSimulationAsync(SimulationId)`. If null, query DB with `.Include(s => s.Nodes).Include(s => s.Blocks).Include(s => s.ConsensusRounds)`. If still null, render a "Not found" state.
- **`src/Consensus.Web/Components/Pages/SimulationDashboard.razor:427`** — `"JoinSimulationGroup"` → `"JoinSimulation"` to match `SimulationHub.cs:27`.

**Verification:** clean DB → "New Simulation" PoW + PBFT side-by-side with seed=42, dashboard ticks live, refresh mid-run reloads from DB.

### Day 5 (Fri 22) — Containerize, write deck, dry-run

#### 5.1 Containerization (one-click `docker compose up`)

Author three new files at repo root:

**`Dockerfile`** (multi-stage, .NET 9 SDK → ASP.NET runtime, targets `Consensus.Web`):
- Stage 1 `build`: `mcr.microsoft.com/dotnet/sdk:9.0`, copy `.sln` + four `.csproj`, `dotnet restore`, copy source, `dotnet publish src/Consensus.Web/Consensus.Web.csproj -c Release -o /app/publish` with `--no-restore`.
- Stage 2 `runtime`: `mcr.microsoft.com/dotnet/aspnet:9.0`, install `postgresql-client` for `pg_isready` (called by `scripts/startup.sh`), copy publish output, copy `scripts/startup.sh`, install `dotnet-ef` tool (`dotnet tool install --global dotnet-ef --version 9.*`), `EXPOSE 8080`, `ENTRYPOINT ["/app/startup.sh"]`.

**`docker-compose.yml`** (3 services):
- `postgres` (image `postgres:16-alpine`, env: `POSTGRES_USER=consensus_user`, `POSTGRES_PASSWORD=consensus_pass`, `POSTGRES_DB=consensusdb`, volume `postgres-data`, mount `./scripts/init-db.sql:/docker-entrypoint-initdb.d/init-db.sql`, port `5432:5432`, healthcheck `pg_isready -U consensus_user`).
- `web` (build `.`, depends_on `postgres` healthy, env: `ASPNETCORE_ENVIRONMENT=Development`, `ConnectionStrings__DefaultConnection=Host=postgres;Database=consensusdb;Username=consensus_user;Password=consensus_pass`, `Simulation__PersistToDb=true`, port `3000:8080`).
- `pgadmin` (image `dpage/pgadmin4`, port `5050:80`, env: `PGADMIN_DEFAULT_EMAIL=admin@consensus-lab.dev`, `PGADMIN_DEFAULT_PASSWORD=Admin@123!`, profiles: `["debug"]` so it stays off by default).

**`.dockerignore`** (exclude `bin/`, `obj/`, `.git/`, `.vscode/`, `tests/`, `mtech/`, `docs/experiments/results/`).

**`DOCKER_GUIDE.md`** (re-author from scratch): one page of `docker compose up`, `docker compose logs web`, `docker compose down -v` (reset DB), seeded admin credentials, troubleshooting (port in use, migration failure).

Pre-pull base images (`docker pull mcr.microsoft.com/dotnet/sdk:9.0 mcr.microsoft.com/dotnet/aspnet:9.0 postgres:16-alpine`) the night before so the live demo isn't gated on network speed.

#### 5.2 Presentation deck (Marp Markdown → .pptx + PDF)

Create **`docs/presentation/thesis-presentation.md`** with Marp front-matter (`marp: true`, IIT Patna-styled theme — default Marp gaia theme is acceptable, customize header/footer). Target 18 slides:

1. **Title slide** — thesis title, name, AIDSE, IIT Patna, date, advisor (placeholder), QR to GitHub.
2. **Agenda** — problem, scope, system, results, demo, future work.
3. **Problem statement** — verbatim Annexure I one-liner, four-quadrant security/scalability diagram (copy from Annexure I PDF page 1).
4. **Why this matters (AIDSE)** — distributed AI, FL coordination, India context.
5. **Research questions & hypotheses** — RQ1–RQ5 + H1–H5 table (from `THESIS_SCOPE_SPECIFICATION.md §5`).
6. **System architecture** — copy the ASCII layered diagram from scope doc §3.1; cite stack (.NET 9, Blazor, SignalR, EF Core, Postgres).
7. **Five protocols at a glance** — PoW / PoS / DPoS / PBFT / PoET, one bullet each, code-line counts.
8. **Simulation engine** — round loop, fault model, seeded RNG (newly delivered), persistence.
9. **Metrics framework** — block time, Gini, entropy, p95/p99, success rate, energy proxy.
10. **Experiment design** — S1–S7 table with research-question mapping.
11. **Live demo (placeholder)** — single full-bleed screenshot of dashboard mid-run; the actual demo plays here.
12. **Reproducibility** — same seed → same leader sequence; show diff of two exports side-by-side.
13. **Sample results — baseline (S1)** — placeholder bar chart of mean block time across 5 protocols (real numbers if Day 5 dry-run produces data; otherwise label "Preliminary, n=1").
14. **Sample results — fairness (Gini)** — Gini values across protocols from a Day-5 baseline run.
15. **Sample results — Byzantine (S4 sketch)** — degradation curve; honest "preliminary" label if needed.
16. **Engineering scorecard** — what's done, what's deferred (cite the gap list below).
17. **Future work** — full benchmark execution (S2–S7 × 30 reps), test suite rebuild, FL payload integration, fault injection API, protocol scoring model (RQ5), thesis write-up timeline.
18. **Q&A / Thank you** — contact, GitHub, advisor.

Build script (one-liner for `scripts/build-deck.sh`):
```bash
npx @marp-team/marp-cli@latest docs/presentation/thesis-presentation.md \
  -o docs/presentation/thesis-presentation.pptx --pptx --allow-local-files
npx @marp-team/marp-cli@latest docs/presentation/thesis-presentation.md \
  -o docs/presentation/thesis-presentation.pdf  --pdf  --allow-local-files
```

Embed three screenshots captured during the Day 5 dry-run: dashboard mid-run, export JSON with non-zero Gini, two-run leader-sequence diff. Store under `docs/presentation/assets/`.

#### 5.3 Dry-run (Friday evening)

Full end-to-end: `docker compose down -v && docker compose up -d`, wait healthy, run the 12-step verification checklist below, capture all three screenshots, regenerate the deck, archive `.pptx` + `.pdf`.

---

## Critical Files Modified

```
src/Consensus.Core/Services/SimulationService.cs          (Day 1, Day 2)
src/Consensus.Core/Models/SimulationModels.cs             (Day 2)
src/Consensus.Core/Entities/SimulationRun.cs              (Day 2 — triggers migration)
src/Consensus.Core/Interfaces/IConsensusProtocol.cs       (Day 2)
src/Consensus.Core/Protocols/{Pow,Pos,Dpos,Pbft,Poet}Protocol.cs   (Day 2 — add SetRandom only)
src/Consensus.Core/Analytics/FairnessMetrics.cs           (Day 3 NEW)
src/Consensus.Core/Services/AnalyticsService.cs           (Day 3)
src/Consensus.Core/Models/AnalyticsModels.cs              (Day 3 — add P95/P99)
src/Consensus.Api/Models/AnalyticsModels.cs               (Day 3 — mirror)
src/Consensus.Web/Models/Api/AnalyticsModels.cs           (Day 3 — mirror)
src/Consensus.Web/Components/Charts/DposAnalyticsChart.razor   (Day 3 — kill placeholder)
src/Consensus.Web/Components/Pages/Simulations.razor      (Day 4)
src/Consensus.Web/Components/Pages/SimulationDashboard.razor   (Day 4)
src/Consensus.Web/Program.cs                              (Day 1 — if scope correction needed)
src/Consensus.Data/Migrations/<new>_AddSimulationSeed.cs  (Day 2 — generated)
Dockerfile                                                (Day 5 NEW)
docker-compose.yml                                        (Day 5 NEW)
.dockerignore                                             (Day 5 NEW)
DOCKER_GUIDE.md                                           (Day 5 NEW)
docs/presentation/thesis-presentation.md                  (Day 5 NEW)
scripts/build-deck.sh                                     (Day 5 NEW)
```

**Existing assets reused (do NOT rewrite):** `scripts/startup.sh` (already postgres-aware), `scripts/init-db.sql` (uuid-ossp + pgcrypto extensions), `Consensus.Data/Seed/IdentitySeeder.cs` (creates `admin@consensus-lab.dev`/`Admin@123!`), `Consensus.Data/Repositories/*` (available but bypassed in MVP — direct DbContext is fewer moving parts), all five `*Protocol.cs` files (only `SetRandom` added).

---

## Deferred — Gap Analysis for Post-23-May Thesis Work

Ordered by thesis-importance, with rough effort estimate. None of these block May 23.

| # | Gap | Effort | Why it matters |
|---|-----|--------|----------------|
| 1 | **Rebuild test projects from scratch** (xUnit, ≥100 unit tests across the 5 protocols + integration tests for SimulationService persistence) | 2–3 weeks | NFR-03 in scope doc; gives the viva committee something to ask about; protects future refactors. |
| 2 | **Execute full S1–S7 benchmark batches** (≥30 reps S1, ≥10 reps S2–S7), produce `docs/experiments/results/` CSVs + statistical analysis notebooks | 3–4 weeks | This is the actual *thesis evidence* — RQ1–RQ5 answered with numbers. |
| 3 | **Federated Learning payload integration** — wire `FederatedLearningService` into `SimulationService.ExecuteRoundAsync` so S7 can run; emit `flRoundMeanMs` / `flParticipantCount` metrics | 1 week | RQ4 + H5 + the AIDSE relevance pillar of Annexure I. |
| 4 | **Runtime fault-injection API** — `POST /api/Simulation/{id}/inject-fault` body `{ type: "NetworkPartition", durationSeconds, affectedFraction }`, calls protocol's existing `HandleNodeFaultAsync` | 1 week | S5 partition scenario; current `byzantineNodeCount` is creation-time-only. |
| 5 | **Pause / Resume on `ISimulationService`** + API endpoints | 2 days | Currently claimed by scope doc but missing; trivial fix. |
| 6 | **Repository pattern adoption** — refactor Day-1 direct `ConsensusDbContext` writes inside `SimulationService` to use the `IBlockRepository` / `IRoundRepository` / etc. that already exist in `Consensus.Core/Repositories/IRepositories.cs` | 3–5 days | Architectural cleanliness; the Day-1 shortcut earns a TODO. |
| 7 | **Analytics page consolidation** — pick one of `Analytics.razor` / `AnalyticsV3.razor` / `AnalyticsDashboard.razor`, retire the other two | 2 days | Scope doc §3.3 explicitly flags this. |
| 8 | **Batch experiment runner** — promote `scripts/expand-experiment-suite.sh` to a CLI that POSTs each generated payload, polls status, downloads exports, builds the cross-run CSV described in EXPERIMENT_PROTOCOL §8.3 | 3 days | Without this, item #2 above is manual drudgery. |
| 9 | **Protocol suitability scoring model (RQ5)** — weighted scoring across normalized TPS / latency / success / energy / Gini, surfaced as a radar chart for thesis Ch. 5 Figure 5.4 | 4–5 days | Direct AIDSE contribution; required to answer RQ5. |
| 10 | **Seed cryptographic RNG in PoET** OR document as a deliberate limitation in thesis Ch. 5 | 1 day | Honesty in the reproducibility narrative. |
| 11 | **DI lifetime correction** for `SimulationService` (currently Scoped, captures across background task) | 1 day | Latent bug; current behavior works only because the captured scope happens to outlive the background task. |
| 12 | **README sync** — current README claims items that are now done and omits items added in this MVP. Final pass after every other change lands | 1 day | Reviewer first impression. |

Suggested post-MVP order: (5) and (12) immediately after submission for low-hanging fruit; then (1) and (8) in parallel; then (2) drives chapters 4 and 5; (3), (4), (9) feed the AIDSE storyline; (6), (7), (10), (11) are technical polish for the final thesis tag `v1.0-thesis`.

---

## Risk Register

| # | Change | What might break | Rollback |
|---|--------|------------------|----------|
| 1 | Day-1 DB writes in the round loop | DB exception mid-round halts the live demo | `try/catch` per persistence block + feature flag `Simulation:PersistToDb=false` flips off writes instantly |
| 2 | EF migration `AddSimulationSeed` on Day 2 | Migration fails on the demo box minutes before the talk | Run migration on Day 2 against the dev compose stack; `RandomSeed` column is nullable so prior schema reads fine; `dotnet ef migrations remove` command rehearsed |
| 3 | `SetRandom` plumbing into the 5 protocols | A protocol silently keeps using `new Random()` and seeded runs look reproducible but aren't | Default-interface no-op = silent fallback to current behavior; verify by diffing leader sequence of two seeded runs |
| 4 | `SimulationDashboard` mock → real service call | Race between in-memory state and DB flush could show "not found" | Two-tier lookup (memory then DB); else last-resort "loading…" spinner |
| 5 | Dockerfile multi-stage build size / startup latency | Cold `docker compose up` takes too long for live demo | Pre-pull base images Friday night; warm up the stack 30 min before the talk; keep `docker compose down -v` for the reset story |
| 6 | Marp .pptx export quirks (fonts, image sizing) | Slide looks fine in Marp preview but ugly in PowerPoint on a projector | Generate `.pptx` Friday, open in Keynote/PowerPoint, eyeball every slide; export PDF as the projector-safe fallback |

---

## Verification — Saturday-morning end-to-end checklist

To be run at 8:00 AM Sat 23 May on the demo machine.

1. `git status` clean; `git log --oneline -10` matches expected Day-1..Day-5 commits.
2. `docker pull mcr.microsoft.com/dotnet/sdk:9.0 mcr.microsoft.com/dotnet/aspnet:9.0 postgres:16-alpine` (warm cache).
3. `docker compose down -v && docker compose up -d`; wait until `docker compose ps` shows `postgres` healthy and `web` running.
4. `docker compose logs web | head -50` — no startup exception; migrations applied; admin user seeded.
5. Browser → `http://localhost:3000/login` → sign in with `admin@consensus-lab.dev` / `Admin@123!`.
6. `/simulations` → "New Simulation" → fill: name=`Demo-PoW`, algorithm=`ProofOfWork`, nodes=`10`, byzantine=`2`, blockTime=`2000`, duration=`60`, **randomSeed=`42`** → Submit → page navigates to `/simulations/dashboard/{id}`.
7. Dashboard shows live round-completed updates within 5 seconds; node list shows 10 nodes; round counter increments.
8. Open second tab → start `Demo-PBFT` (PracticalByzantineFaultTolerance, same params, seed=42); both run concurrently.
9. Wait ~60 s for `Demo-PoW` to flip to `Completed`. In a third tab: `docker compose exec postgres psql -U consensus_user -d consensusdb -c "SELECT count(*) FROM blocks WHERE simulation_run_id = '<demo-pow-id>';"` — expect > 0; same for `consensus_rounds` and `event_logs`.
10. Click Export on the PoW row → download JSON → open in editor; verify `randomSeed=42`, non-empty `rounds[]`, `metrics.giniCoefficient ∈ (0,1)`, `metrics.blockDistributionEntropy > 0`, `metrics.p95BlockTimeMs > 0`.
11. Re-run `Demo-PoW` with the same seed=42, export again, `diff` the leader sequences — must be identical (proves determinism).
12. `/analytics-v3` (or chosen primary analytics page) → DPoS chart Gini value is stable across two refreshes (proves the `random.NextDouble()` line is gone).
13. Open `docs/presentation/thesis-presentation.pdf` — 18 slides, no missing image placeholders, three screenshots captured Friday are embedded.

If any step fails, fall back: (a) disable persistence via `Simulation__PersistToDb=false` env var and demo the in-memory live flow; (b) present the PDF deck regardless; (c) cite gap-analysis items 1–4 as next steps under "Future Work."
