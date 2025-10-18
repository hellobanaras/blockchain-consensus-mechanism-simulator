# Quickstart Guide: Consensus Lab

## Overview
Consensus Lab is a Blazor Server application that simulates blockchain consensus protocols in real-time. This guide will get you up and running with the development environment in under 10 minutes.

## Prerequisites
- .NET 8.0 SDK or later
- PostgreSQL 14+ (or Docker for containerized database)
- Node.js 18+ (for client-side dependencies)
- Visual Studio 2022 or VS Code with C# extension

## Quick Setup (Docker)

### 1. Clone and Start
```bash
git clone https://github.com/hellobanaras/blockchain-consensus-mechanism-simulator.git
cd blockchain-consensus-mechanism-simulator
docker-compose up -d
```

### 2. Access Application
- Web UI: http://localhost:5000
- API Docs: http://localhost:5000/swagger
- Database: localhost:5432 (user: consensus, password: devpassword)

### 3. Run Demo Simulation
1. Navigate to the Dashboard
2. Select "PoET" protocol
3. Set 6 nodes, 20 rounds
4. Click "Start Simulation"
5. Watch real-time updates in the log panel

## Development Setup

### 1. Database Setup
```bash
# Option A: Docker PostgreSQL
docker run --name consensus-db -e POSTGRES_USER=consensus -e POSTGRES_PASSWORD=devpassword -e POSTGRES_DB=consensuslab -p 5432:5432 -d postgres:14

# Option B: Local PostgreSQL (adjust connection string in appsettings.json)
createdb consensuslab
```

### 2. Project Setup
```bash
# Restore dependencies
dotnet restore

# Create database and run migrations
dotnet ef database update --project src/Consensus.Data --startup-project src/Consensus.Web

# Seed sample data
dotnet run --project src/Consensus.Web -- --seed
```

### 3. Run Development Server
```bash
# Start web application with hot reload
dotnet watch run --project src/Consensus.Web
```

### 4. Verify Setup
- Navigate to https://localhost:5001
- Check /health endpoint returns "Healthy"
- Verify database connection in logs

## Project Structure

```
src/
├── Consensus.Web/              # Blazor Server UI
│   ├── Pages/                  # Razor pages
│   ├── Components/             # Reusable components  
│   ├── Hubs/SimulationHub.cs   # SignalR hub
│   └── Program.cs              # Application entry point
├── Consensus.Core/             # Business logic
│   ├── Entities/               # Domain models
│   ├── Protocols/              # Consensus implementations
│   ├── Services/               # Application services
│   └── Interfaces/             # Abstractions
├── Consensus.Api/              # REST API
│   ├── Controllers/            # API controllers
│   └── Models/                 # DTOs
└── Consensus.Data/             # Data access
    ├── Context/                # EF Core context
    ├── Migrations/             # Database migrations
    └── Repositories/           # Data repositories

tests/
├── Consensus.Core.Tests/       # Unit tests
├── Consensus.Api.Tests/        # Integration tests
└── Consensus.E2E.Tests/        # Playwright tests
```

## Common Commands

### Database Operations
```bash
# Add new migration
dotnet ef migrations add MigrationName --project src/Consensus.Data --startup-project src/Consensus.Web

# Update database
dotnet ef database update --project src/Consensus.Data --startup-project src/Consensus.Web

# Reset database (development only)
dotnet ef database drop --project src/Consensus.Data --startup-project src/Consensus.Web --force
```

### Testing
```bash
# Run unit tests
dotnet test tests/Consensus.Core.Tests/

# Run integration tests  
dotnet test tests/Consensus.Api.Tests/

# Run all tests
dotnet test

# Run E2E tests (requires app running)
dotnet test tests/Consensus.E2E.Tests/
```

### Building
```bash
# Build solution
dotnet build

# Build for production
dotnet publish src/Consensus.Web -c Release -o ./publish

# Build Docker image
docker build -t consensus-lab .
```

## Configuration

### Environment Variables
```bash
# Database connection
export ConnectionStrings__DefaultConnection="Host=localhost;Database=consensuslab;Username=consensus;Password=devpassword"

# SignalR settings
export SignalR__RedisConnectionString="localhost:6379"  # Production only

# Authentication
export Authentication__JwtSecret="your-256-bit-secret-key-here"

# Feature flags
export Features__Payload__SupplyChain=true
export Features__Payload__FederatedLearning=false
```

### Configuration Files
- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production settings

## First Simulation Tutorial

### 1. Basic PoET Simulation
1. Go to Dashboard → "Start New Simulation"
2. Protocol: Proof of Elapsed Time (PoET)
3. Nodes: 6, Rounds: 10
4. Parameters: Wait Low: 50ms, Wait High: 300ms
5. Click "Run Simulation"
6. Observe real-time log updates showing:
   - Round start events
   - Wait time calculations  
   - Winner selection
   - Block creation

### 2. Explore Block Explorer
1. During simulation, click "View Blockchain"
2. See growing chain with block details:
   - Height, Hash, Previous Hash
   - Proposer node
   - Timestamp and metadata
3. Click individual blocks for full details

### 3. View Analytics
1. After simulation completes, go to "Analytics"
2. Examine winner distribution chart
3. Review wait time histogram (PoET specific)
4. Export data as CSV for further analysis

## Protocol-Specific Guides

### Proof of Work (PoW)
```json
{
  "protocol": "pow",
  "nodeCount": 8,
  "rounds": 15,
  "params": {
    "difficulty": 4,
    "hashPowerDistribution": "uniform"
  }
}
```
- Simulates mining with exponential wait times
- Higher difficulty = longer average block times
- Power distribution affects winning probability

### PBFT (Practical Byzantine Fault Tolerance)
```json
{
  "protocol": "pbft",
  "nodeCount": 7,
  "rounds": 12,
  "params": {
    "faultyNodes": 2,
    "threshold": 5
  }
}
```
- Requires 2f+1 honest nodes (f = faulty count)
- Three-phase protocol: pre-prepare, prepare, commit
- Visualizes Byzantine fault scenarios

### Proof of Burn (PoB)
```json
{
  "protocol": "pob",
  "nodeCount": 6,
  "rounds": 20,
  "params": {
    "burnStrategy": "percentage",
    "burnRate": 0.05,
    "reward": 5.0
  }
}
```
- Nodes burn tokens to earn block rewards
- Economic incentives drive participation
- Tracks cumulative burns and rewards

## Troubleshooting

### Common Issues

**Database Connection Failed**
```bash
# Check PostgreSQL is running
pg_isready -h localhost -p 5432

# Verify connection string in appsettings.json
# Check firewall rules for port 5432
```

**SignalR Connection Lost**  
```bash
# Check browser console for errors
# Verify network connectivity
# Check server logs for connection issues
```

**Simulation Not Starting**
```bash
# Check application logs
dotnet run --project src/Consensus.Web --verbosity detailed

# Verify hosted service is registered
# Check for validation errors in parameters
```

### Debugging Tips
1. Enable detailed logging: `"Logging": {"LogLevel": {"Default": "Debug"}}`
2. Use browser dev tools to monitor SignalR messages
3. Check EF Core query logging: `"Microsoft.EntityFrameworkCore": "Information"`
4. Monitor performance counters in /health endpoint

## Next Steps
1. Try different consensus protocols and compare results
2. Enable Supply-Chain payload mode for domain-specific simulations  
3. Explore the protocol playground for parameter tuning
4. Review API documentation at /swagger for integration scenarios
5. Run load tests with multiple concurrent simulations

## Support
- GitHub Issues: [Repository Issues](https://github.com/hellobanaras/blockchain-consensus-mechanism-simulator/issues)
- Documentation: [Full Docs](./README.md)
- API Reference: https://localhost:5001/swagger (when running locally)