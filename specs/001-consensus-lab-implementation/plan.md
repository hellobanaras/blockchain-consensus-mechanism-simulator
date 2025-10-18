# Implementation Plan: Consensus Lab

**Branch**: `001-consensus-lab-implementation` | **Date**: 2025-10-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-consensus-lab-implementation/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Consensus Lab is a Blazor Server web application that simulates blockchain consensus protocols (PoW, PoS, DPoS, PoA, PBFT, PoET, PoB) allowing users to configure virtual nodes, run consensus rounds, and visualize blockchain growth in real-time. The system includes analytics dashboards, block explorer, protocol playground, and optional payload modes for Supply-Chain and Federated Learning scenarios. Built with Blazor Server UI, ASP.NET Core API, EF Core data layer, PostgreSQL database, and SignalR for real-time updates.

## Technical Context

**Language/Version**: C# .NET 8.0 (Blazor Server)
**Primary Dependencies**: ASP.NET Core, Blazor Server, Entity Framework Core, SignalR, PostgreSQL, Swashbuckle (Swagger)  
**Storage**: PostgreSQL (production), SQLite (development), EF Core migrations  
**Testing**: NUnit/xUnit for unit tests, WebApplicationFactory for integration tests, Playwright for UI tests  
**Target Platform**: Web browsers (modern), Docker containers, Azure App Service  
**Project Type**: Web application with real-time features  
**Performance Goals**: Start to first RoundUpdate <1.0s, 100 rounds/10 nodes <8s, SignalR broadcast <50ms to 50 clients  
**Constraints**: Real-time updates every second, simulation setup <2 minutes, analytics render <3 seconds  
**Scale/Scope**: 10+ concurrent users, 7 consensus protocols, 5-100 nodes per simulation, 1-100 rounds per experiment

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Status**: PASS - Constitution template not yet configured for this project. No specific organizational constraints defined at this time. Project follows standard .NET best practices and web application architecture patterns.

**Post-Phase 1 Re-evaluation**: PASS - The designed system maintains:
- Clean separation of concerns (Web, Core, API, Data layers)
- Standard .NET project structure and conventions
- Comprehensive testing strategy (Unit, Integration, E2E)
- Proper abstraction through interfaces
- Industry-standard technology choices
- Well-defined API contracts and documentation
- No violations of common software engineering principles

## Project Structure

### Documentation (this feature)

```
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```
src/
├── Consensus.Web/              # Blazor Server UI
│   ├── Pages/                  # Razor pages (Dashboard, Explorer, etc.)
│   ├── Components/             # Reusable Blazor components
│   ├── Hubs/                   # SignalR hubs for real-time updates
│   └── wwwroot/                # Static assets (CSS, JS, images)
├── Consensus.Core/             # Business logic class library
│   ├── Entities/               # Domain entities (Node, Block, Round, etc.)
│   ├── Protocols/              # Consensus protocol implementations
│   ├── Services/               # Business services
│   └── Interfaces/             # Abstractions and contracts
├── Consensus.Api/              # REST API (minimal API or controllers)
│   ├── Controllers/            # API controllers
│   ├── Models/                 # DTOs and request/response models
│   └── Middleware/             # Custom middleware
└── Consensus.Data/             # Data access layer
    ├── Context/                # EF Core DbContext
    ├── Migrations/             # Database migrations
    ├── Repositories/           # Data access repositories
    └── Seed/                   # Database seed data

tests/
├── Consensus.Core.Tests/       # Unit tests for business logic
├── Consensus.Api.Tests/        # API integration tests
├── Consensus.Web.Tests/        # Blazor UI tests
└── Consensus.E2E.Tests/        # End-to-end Playwright tests
```

**Structure Decision**: Selected multi-project web application structure to separate concerns between UI (Blazor), business logic (Core), API (REST), and data access (EF Core). This follows .NET solution conventions and enables independent testing and deployment of components.

## Complexity Tracking

*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |

