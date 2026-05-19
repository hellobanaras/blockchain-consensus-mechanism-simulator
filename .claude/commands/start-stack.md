---
description: docker compose up --build and wait for healthy. One-click launch for the demo.
---

Bring the full stack up with health-gated readiness:

```bash
docker compose down -v 2>/dev/null
docker compose up -d --build
```

After the command returns, wait for both services to be healthy:

```bash
docker compose ps
```

Look for `postgres` with `(healthy)` and `web` running. If `web` is restarting, immediately run:

```bash
docker compose logs --tail=80 web
```

and report what's wrong rather than letting the container loop. Likely causes:
- `relation does not exist` → migrations didn't run; check `ConsensusSimulator__AutoMigrateDatabase` env (should be `true` in compose).
- Connection refused → postgres healthcheck hasn't passed yet; wait another 10s and retry.

Once healthy, report the four URLs from `DOCKER_GUIDE.md`:
- UI: http://localhost:8080
- Swagger: http://localhost:8080/swagger
- Postgres (host): `localhost:5433` (`consensus_user` / `consensus_password` / `consensusdb`)
- pgAdmin (only if started with `--profile debug`): http://localhost:5050

Host ports 8080 / 5433 are chosen to avoid common collisions: 3000 (Next.js dev servers) and 5432 (a local Postgres install). Override via `docker-compose.override.yml` if needed; that file is git-ignored.

If Docker Desktop is not running, ask the user to start it; do NOT try `colima` or other alternatives without explicit permission.
