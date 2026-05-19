#!/bin/bash
# Entrypoint for the Api container. Mirrors scripts/startup.sh (used by Web) —
# wait for Postgres to accept connections, then start the .NET host. Migrations
# auto-apply inside Program.cs (ConsensusSimulator:AutoMigrateDatabase=true).
set -e

DB_HOST="${POSTGRES_HOST:-postgres}"
DB_PORT="${POSTGRES_PORT:-5432}"
DB_USER="${POSTGRES_USER:-consensus_user}"
DB_NAME="${POSTGRES_DB:-consensusdb}"

echo "[api] Waiting for Postgres at ${DB_HOST}:${DB_PORT}..."
until pg_isready -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t 2 >/dev/null 2>&1; do
  sleep 2
  echo "[api] Postgres not ready yet, retrying..."
done

echo "[api] Postgres is ready. Launching Consensus.Api on ${ASPNETCORE_URLS:-http://+:8080}"
exec dotnet Consensus.Api.dll
