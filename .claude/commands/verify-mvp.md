---
description: Run the 12-step Saturday-morning MVP smoke checklist for the M.Tech demo.
---

You are running the full end-to-end verification for the May-23 thesis demo. Execute each step in order. **Stop at the first failure** and report which step failed and why; do not proceed past a red step.

Use the `Bash` tool for shell commands. Use `Read` to inspect files. Report status after each step.

### Steps

1. `git status` is clean (or only documented in-progress changes). `git log --oneline -10` matches the expected day-by-day commits.
2. Pull base images so the demo doesn't depend on network speed mid-talk:
   `docker pull mcr.microsoft.com/dotnet/sdk:9.0 mcr.microsoft.com/dotnet/aspnet:9.0 postgres:16-alpine`
3. `docker compose down -v && docker compose up -d --build`. Wait until `docker compose ps` shows `postgres` healthy and `web` running.
4. `docker compose logs web | head -80` — should contain `Auto-migrating database`, `Database migration completed`, an `IdentitySeeder` success line, and `Now listening on: http://[::]:8080`. No stack traces.
5. Open http://localhost:3000/login → sign in as `admin@consensus-lab.dev` / `Admin@123!`.
6. Navigate to `/simulations` → click **New Simulation** → fill: name=`Demo-PoW`, algorithm=`ProofOfWork`, nodes=`10`, faulty=`2`, rounds=`60`, topology=`FullMesh`, **randomSeed=`42`** → Submit. Page navigates to `/simulation/{id}`.
7. Dashboard shows live round-completed updates within ~5 s. Node count = 10, round counter increments.
8. Open a second browser tab → start `Demo-PBFT` (`PracticalByzantineFaultTolerance`, same params, seed=42). Both run concurrently.
9. After ~60 s `Demo-PoW` flips to `Completed`. Confirm DB writes:
   `docker compose exec postgres psql -U consensus_user -d consensusdb -c "SELECT name, status, random_seed FROM simulation_runs ORDER BY created_at DESC LIMIT 5;"`
   Then:
   `docker compose exec postgres psql -U consensus_user -d consensusdb -c "SELECT COUNT(*) FROM blocks; SELECT COUNT(*) FROM consensus_rounds; SELECT COUNT(*) FROM event_logs;"`
   All three counts > 0.
10. From `/simulations`, click Export on the PoW row → download JSON → verify it contains `randomSeed: 42`, non-empty `rounds[]`, `metrics.giniCoefficient ∈ (0,1)`, `metrics.blockDistributionEntropy > 0`, `metrics.p95BlockTimeMs > 0`.
11. Reproducibility check: re-run `Demo-PoW` with seed=42. Export. Diff the two leader sequences — must be identical.
12. Visit the analytics page that uses `DposAnalyticsChart`. Refresh twice. Gini stays stable (proves the `random.NextDouble()` placeholder was removed).
13. Open `docs/presentation/thesis-presentation.pdf` — 18 slides, no missing image placeholders.

### Fallbacks (apply only if the corresponding step fails)

- **Step 3 / 4 fail (DB issue):** `docker compose down -v` and retry. If still failing, set `Simulation__PersistToDb=false` in compose env and continue — the demo will run in memory.
- **Step 6 fails (modal error):** check `docker compose logs web` for stack trace; most likely cause is a stale DB schema. Reset with `down -v`.
- **Step 9 returns 0 counts:** persistence is failing; the simulator runtime still works. Note in PROGRESS.md and demo with the in-memory path.

Append a short summary line to `.claude/PROGRESS.md` under a new `## Verification — <date>` section regardless of outcome.
