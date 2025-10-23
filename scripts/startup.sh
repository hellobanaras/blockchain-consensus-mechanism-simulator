#!/bin/bash
set -e

echo "Starting application..."

# Install dotnet ef tool if not already installed
export PATH="$PATH:/home/appuser/.dotnet/tools"

# Check if the database is ready
echo "Waiting for database to be ready..."
until pg_isready -h postgres -p 5432 -U consensus_user -d consensusdb; do
  echo "Database is unavailable - sleeping..."
  sleep 2
done

echo "Database is ready!"

# Try to run migrations
echo "Running database migrations..."
if command -v dotnet-ef &> /dev/null; then
    dotnet ef database update --no-build || echo "Migration failed or no migrations to apply"
else
    echo "EF tools not available, skipping migrations"
fi

# Start the application
echo "Starting Blazor application..."
exec dotnet Consensus.Web.dll