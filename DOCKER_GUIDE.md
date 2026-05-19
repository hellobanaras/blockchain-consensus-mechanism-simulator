# Docker Guide — Consensus Simulator (M.Tech Thesis Demo)

## TL;DR — one-click launch

```bash
docker compose up --build
```

Wait ~30–60 s for the image build and first-time migration, then open:

| Surface              | URL                         | Notes                                                            |
|----------------------|-----------------------------|------------------------------------------------------------------|
| Blazor UI (Web)      | http://localhost:8080       | Login → start a simulation → live dashboard                      |
| REST Api + Swagger   | http://localhost:5101       | Read-only HTTP surface; `Consensus.Api` host. Web fetches reads from here. |
| Postgres (host)      | `localhost:5433`            | `consensus_user` / `consensus_password` / db `consensusdb` (containers use 5432 internally) |
| pgAdmin (optional)   | http://localhost:5050       | Enable with `docker compose --profile debug up` (admin@consensus-lab.dev / Admin@123!) |

**Architecture inside the compose network:** Web on `http://web:8080`, Api on `http://api:8080`, Postgres on `postgres:5432`. The Web host calls the Api over the internal network for all read data (lists, dashboards, exports — see `Consensus.Web/Services/ConsensusApiClient.cs`); write/orchestration paths (start, stop, round loop, SignalR) stay in the Web host. Both Web and Api connect to the shared Postgres.

Seeded admin user (created by `IdentitySeeder` at startup):

```
email:    admin@consensus-lab.dev
password: Admin@123!
roles:    Admin, Operator, Viewer
```

## What the stack contains

`docker-compose.yml` brings up three services:

1. **`postgres`** — Postgres 16 Alpine, with `scripts/init-db.sql` applied on first boot (enables `uuid-ossp` and `pgcrypto` extensions).
2. **`web`** — multi-stage build of the Blazor Server app (`Consensus.Web`). The `DatabaseInitializationService` auto-applies EF migrations on startup when `ConsensusSimulator__AutoMigrateDatabase=true`.
3. **`pgadmin`** — optional Postgres web UI, only started when you pass `--profile debug`.

## Common operations

```bash
# Rebuild after code changes
docker compose up --build

# Stream logs from the web container
docker compose logs -f web

# Reset the database (drops the postgres-data volume)
docker compose down -v
docker compose up --build

# Open a psql shell to inspect tables
docker compose exec postgres psql -U consensus_user -d consensusdb

# Stop everything
docker compose down
```

## Health-check sequence (what you should see in `docker compose logs`)

1. `postgres` reports `database system is ready to accept connections`.
2. `web` reports `[startup] Waiting for Postgres ...` then `[startup] Postgres is ready. Launching Consensus.Web ...`.
3. `web` runs migrations (`Auto-migrating database...`) and `IdentitySeeder` seeds the admin user.
4. `web` logs `Now listening on: http://[::]:8080`.

## Troubleshooting

| Symptom                                                | Likely cause                                                  | Fix                                                                                              |
|--------------------------------------------------------|---------------------------------------------------------------|--------------------------------------------------------------------------------------------------|
| `address already in use` on 3000 / 5432                | Another local process is bound to the port                    | `lsof -i :3000` to find it, or change the host-port mapping in `docker-compose.yml`              |
| Web container restarts in a loop                       | Postgres isn't healthy yet, or connection string is wrong     | `docker compose logs postgres` should show `accepting connections`; verify the `ConnectionStrings__DefaultConnection` env var |
| `relation "AspNetUsers" does not exist`                | Migrations didn't run                                         | Confirm env `ConsensusSimulator__AutoMigrateDatabase=true` (compose default); restart `web`      |
| Login page shows but credentials fail                  | `IdentitySeeder` errored before seeding                       | `docker compose logs web | grep -i seed`; reset the DB with `docker compose down -v && up`     |
| Live dashboard shows "Not found" after starting a sim  | DB write didn't flush before the dashboard read               | The page re-tries; refresh once. Persistence is best-effort: a DB error doesn't crash the run.   |
| Simulation runs but export is empty                    | `Simulation__PersistToDb=false` (toggled off as a safety net) | Set `Simulation__PersistToDb=true` (compose default) and re-run                                  |

## Switching the persistence feature flag

If a live demo run starts misbehaving and you suspect a DB write is the cause, you can disable persistence without rebuilding:

```bash
docker compose stop web
ConsensusSimulator__AutoMigrateDatabase=true Simulation__PersistToDb=false \
  docker compose up web
```

The simulation still runs in memory and live dashboard / charts still tick — only the database writes are skipped.

## Container layout

```
/app
├── Consensus.Web.dll        # entrypoint assembly
├── Consensus.Web.deps.json
├── appsettings.json
├── startup.sh               # waits for Postgres, then `exec dotnet Consensus.Web.dll`
└── wwwroot/                 # static assets shipped with the publish
```

Base images:
- Build: `mcr.microsoft.com/dotnet/sdk:9.0`
- Runtime: `mcr.microsoft.com/dotnet/aspnet:9.0`

Image size is roughly 250–280 MB after `--build`. First build pulls ~600 MB of base layers; subsequent builds reuse them.
