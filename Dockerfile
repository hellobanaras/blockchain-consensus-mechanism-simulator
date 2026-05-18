# syntax=docker/dockerfile:1.6
# Multi-stage build for Consensus.Web (Blazor Server + REST API + SignalR hub).
# Stage 1 builds with .NET 9 SDK; Stage 2 runs on the ASP.NET 9 runtime image.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution + project files first to maximise layer-caching of `dotnet restore`.
COPY BlockchainConsensusSimulator.sln ./
COPY src/Consensus.Core/Consensus.Core.csproj    src/Consensus.Core/
COPY src/Consensus.Data/Consensus.Data.csproj    src/Consensus.Data/
COPY src/Consensus.Api/Consensus.Api.csproj      src/Consensus.Api/
COPY src/Consensus.Web/Consensus.Web.csproj      src/Consensus.Web/

RUN dotnet restore src/Consensus.Web/Consensus.Web.csproj

# Copy the rest of the source and publish a release build.
COPY src/ src/
RUN dotnet publish src/Consensus.Web/Consensus.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# postgresql-client provides pg_isready for the wait-for-DB loop in startup.sh.
RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./
COPY scripts/startup.sh /app/startup.sh
RUN chmod +x /app/startup.sh

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true

EXPOSE 8080

# Migrations + admin-user seeding now happen inside Program.cs at startup.
# startup.sh waits for the postgres container, then `exec dotnet Consensus.Web.dll`.
ENTRYPOINT ["/app/startup.sh"]
