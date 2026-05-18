---
name: architecture-invariants
description: Hard rules about the codebase layering and runtime that future sessions must not break.
metadata:
  type: feedback
---

Three invariants discovered during the May-23 MVP build that future sessions must respect.

**1. Clean Architecture: Core does not reference Data.**
`Consensus.Core.csproj` has no `<ProjectReference>` to `Consensus.Data`. Persistence flows through `IRepository<T>` interfaces declared in `Consensus.Core/Repositories/IRepositories.cs` and implemented in `Consensus.Data/Repositories/`. Inside `SimulationService` we resolve repositories from `IServiceScopeFactory` because the round-execution `Task.Run` outlives a Scoped lifetime.
- **Why:** Preserves layering; avoids circular concerns; reflects what a thesis-grade architecture should look like.
- **How to apply:** Reject any PR that adds `using Consensus.Data;` inside Core, or that injects `ConsensusDbContext` into a Core service.

**2. Seeded RNG must flow through `IConsensusProtocol.SetRandom(Random)`.**
Each protocol class holds a mutable (not `readonly`) `Random _random` field. `SimulationService` constructs one `Random(seed)` per simulation and calls `protocol.SetRandom(rng)` before `InitializeAsync`. Any new `new Random()` inside a protocol's selection logic breaks reproducibility — H1, H3, H4 in the thesis depend on it.
- **Why:** Reproducibility is a primary thesis claim. The `randomSeed: 42` in every `docs/experiments/S*.json` must produce identical leader sequences.
- **How to apply:** When reviewing protocol code, grep for `new Random` and `Random.Shared`; both are red flags. The single exception is PoET's `RandomNumberGenerator.Create()` for cryptographic-token bytes, deliberately not seeded and documented as a thesis limitation.

**3. Persistence inside `SimulationService` is best-effort.**
Every `PersistXxxAsync` helper is wrapped in a try/catch that only logs. A DB exception must never bubble up and kill a live demo. The kill-switch is `Simulation__PersistToDb=false` (env var), set in `docker-compose.yml`.
- **Why:** A live IIT Patna viva can't survive a stack trace. The simulator's in-memory state is independently sufficient for the UI / SignalR demo path.
- **How to apply:** Never replace the try/catch with `throw`. Never add persistence calls that aren't best-effort.
