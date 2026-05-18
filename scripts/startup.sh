#!/bin/bash
# Container entrypoint: wait for Postgres, then hand off to the .NET host.
# Migrations + identity seeding now run inside Program.cs (see DatabaseInitializationService
# and IdentitySeeder), so we no longer call `dotnet ef` here.
set -e

DB_HOST="${POSTGRES_HOST:-postgres}"
DB_PORT="${POSTGRES_PORT:-5432}"
DB_USER="${POSTGRES_USER:-consensus_user}"
DB_NAME="${POSTGRES_DB:-consensusdb}"

echo "[startup] Waiting for Postgres at ${DB_HOST}:${DB_PORT} (user=${DB_USER}, db=${DB_NAME})..."
until pg_isready -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t 2 >/dev/null 2>&1; do
  sleep 2
  echo "[startup] Postgres not ready yet, retrying..."
done

echo "[startup] Postgres is ready. Launching Consensus.Web on ${ASPNETCORE_URLS:-http://+:8080}"
exec dotnet Consensus.Web.dll
