# Tasks: Consensus Lab

**Input**: Design documents from `/specs/001-consensus-lab-implementation/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests are included based on feature specification requirements for acceptance tests.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## User Story Mapping

Based on functional requirements from spec.md:

- **US1** (P1): Protocol Selection & Basic Simulation - Core simulation engine with real-time updates
- **US2** (P2): Block Explorer - Blockchain visualization and navigation
- **US3** (P2): Analytics Dashboard - Winner distributions, metrics, and data export
- **US4** (P2): Protocol Playground - Parameter configuration and testing
- **US5** (P3): Payload Modes - Supply-Chain and Federated Learning scenarios
- **US6** (P3): User Management & Roles - Authentication and authorization

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create .NET solution structure with four projects: src/Consensus.Web/, src/Consensus.Core/, src/Consensus.Api/, src/Consensus.Data/
- [x] T002 [P] Initialize Consensus.Web project with Blazor Server template and dependencies
- [x] T003 [P] Initialize Consensus.Core project as class library with consensus protocol dependencies
- [x] T004 [P] Initialize Consensus.Api project with ASP.NET Core minimal API template
- [x] T005 [P] Initialize Consensus.Data project with Entity Framework Core and PostgreSQL provider
- [x] T006 [P] Create test projects structure: tests/Consensus.Core.Tests/, tests/Consensus.Api.Tests/, tests/Consensus.Web.Tests/, tests/Consensus.E2E.Tests/
- [x] T007 [P] Configure solution file to include all projects with proper dependencies
- [x] T008 [P] Setup NuGet package references for SignalR, EF Core, Swashbuckle, and testing frameworks
- [x] T009 [P] Create Docker configuration files: Dockerfile, docker-compose.yml, .dockerignore
- [x] T010 [P] Setup CI/CD configuration files: .github/workflows/build.yml, .github/workflows/test.yml

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T011 Create Entity Framework DbContext in src/Consensus.Data/ConsensusDbContext.cs
- [x] T012 [P] Define core entities: Node in src/Consensus.Core/Entities/Node.cs (matches data-model.md Node entity)  
- [x] T013 [P] Define core entities: Round in src/Consensus.Core/Entities/ConsensusRound.cs (matches data-model.md Round entity)
- [x] T014 [P] Define core entities: Block in src/Consensus.Core/Entities/Block.cs (matches data-model.md Block entity)
- [x] T015 [P] Define core entities: Experiment in src/Consensus.Core/Entities/SimulationRun.cs (matches data-model.md Experiment entity)
- [x] T016 [P] Define core entities: EventLog in src/Consensus.Core/Entities/EventLog.cs (for audit trail and simulation events)
- [x] T017 Configure EF Core model configuration and relationships in src/Consensus.Data/Context/EntityConfigurations/
- [x] T018 [P] Create initial database migration with all entities
- [x] T019 [P] Define core interfaces: IConsensusProtocol in src/Consensus.Core/Interfaces/IConsensusProtocol.cs
- [x] T020 [P] Define core interfaces: ISimulationService in src/Consensus.Core/Interfaces/ISimulationService.cs
- [x] T021 [P] Define core interfaces: IBlockValidator in src/Consensus.Core/Interfaces/IBlockValidator.cs
- [x] T022 [P] Create base SimContext class in src/Consensus.Core/Services/SimContext.cs
- [x] T023 Configure dependency injection in src/Consensus.Web/Program.cs
- [x] T024 [P] Setup SignalR hub base structure in src/Consensus.Web/Hubs/SimulationHub.cs
- [x] T025 [P] Configure PostgreSQL connection and environment settings in appsettings.json
- [x] T026 [P] Setup Serilog logging configuration in src/Consensus.Web/Program.cs
- [x] T027 [P] Create base repository pattern in src/Consensus.Core/Repositories/IRepositories.cs and src/Consensus.Data/Repositories/Repositories.cs
- [x] T028 [P] Setup API middleware pipeline: authentication, authorization, rate limiting in src/Consensus.Web/Middleware/
- [x] T029 [P] Configure Swagger/OpenAPI documentation in src/Consensus.Web/Program.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Protocol Selection & Basic Simulation (Priority: P1) 🎯 MVP

**Goal**: Users can select a consensus protocol, configure basic parameters, start simulation, and watch real-time block creation

**Independent Test**: Start PoET simulation with 6 nodes, 10 rounds, verify real-time updates via SignalR and final blockchain height

### Tests for User Story 1

**NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T030 [P] [US1] Acceptance test for protocol selection and simulation start in tests/Consensus.E2E.Tests/SimulationFlowTests.cs
- [ ] T031 [P] [US1] Integration test for simulation API endpoints in tests/Consensus.Api.Tests/SimulationControllerTests.cs
- [ ] T032 [P] [US1] Unit tests for PoET protocol implementation in tests/Consensus.Core.Tests/Protocols/PoetProtocolTests.cs

### Implementation for User Story 1

- [ ] T033 [P] [US1] Implement PoET consensus protocol in src/Consensus.Core/Protocols/PoetProtocol.cs
- [ ] T034 [P] [US1] Create StartSimulationRequest DTO in src/Consensus.Api/Models/StartSimulationRequest.cs
- [ ] T035 [P] [US1] Create RoundUpdate DTO for SignalR in src/Consensus.Web/Models/RoundUpdate.cs
- [ ] T036 [US1] Implement SimulationService core logic in src/Consensus.Core/Services/SimulationService.cs
- [ ] T037 [US1] Implement SimulationHostedService for background processing in src/Consensus.Web/Services/SimulationHostedService.cs
- [ ] T038 [US1] Create simulation start API endpoint in src/Consensus.Api/Controllers/SimulationController.cs
- [ ] T039 [US1] Implement SignalR hub methods for real-time updates in src/Consensus.Web/Hubs/SimulationHub.cs
- [ ] T040 [US1] Create Dashboard page with protocol selection in src/Consensus.Web/Pages/Dashboard.razor
- [ ] T041 [US1] Create protocol selector component in src/Consensus.Web/Components/ProtocolSelector.razor
- [ ] T042 [US1] Create simulation log component for real-time updates in src/Consensus.Web/Components/SimulationLog.razor
- [ ] T043 [US1] Add validation for simulation parameters using FluentValidation
- [ ] T044 [US1] Implement error handling and logging for simulation failures

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Block Explorer (Priority: P2)

**Goal**: Users can browse the blockchain, view block details, and navigate the chain structure

**Independent Test**: After simulation completes, navigate to block explorer, browse blocks, click individual blocks for details

### Tests for User Story 2

- [ ] T045 [P] [US2] Acceptance test for block explorer navigation in tests/Consensus.E2E.Tests/BlockExplorerTests.cs
- [ ] T046 [P] [US2] Integration test for block API endpoints in tests/Consensus.Api.Tests/BlockControllerTests.cs

### Implementation for User Story 2

- [ ] T047 [P] [US2] Create BlockSummary and BlockDetail DTOs in src/Consensus.Api/Models/BlockModels.cs
- [ ] T048 [P] [US2] Implement block repository in src/Consensus.Data/Repositories/BlockRepository.cs
- [ ] T049 [US2] Create block API endpoints for listing and details in src/Consensus.Api/Controllers/BlockController.cs
- [ ] T050 [US2] Create Block Explorer page in src/Consensus.Web/Pages/Explorer.razor
- [ ] T051 [US2] Create block list component with pagination in src/Consensus.Web/Components/BlockList.razor
- [ ] T052 [US2] Create block detail component in src/Consensus.Web/Components/BlockDetail.razor
- [ ] T053 [US2] Create block card component for summary display in src/Consensus.Web/Components/BlockCard.razor
- [ ] T054 [US2] Add navigation between blocks (previous/next) in block detail view
- [ ] T055 [US2] Implement block filtering by protocol and experiment

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Analytics Dashboard (Priority: P2)

**Goal**: Users can view simulation analytics, winner distributions, protocol metrics, and export data

**Independent Test**: After simulation, view analytics page with charts for winner distribution, wait times, export CSV data

### Tests for User Story 3

- [ ] T056 [P] [US3] Acceptance test for analytics visualization in tests/Consensus.E2E.Tests/AnalyticsTests.cs
- [ ] T057 [P] [US3] Integration test for analytics API endpoints in tests/Consensus.Api.Tests/AnalyticsControllerTests.cs

### Implementation for User Story 3

- [ ] T058 [P] [US3] Create AnalyticsSummary DTO in src/Consensus.Api/Models/AnalyticsSummary.cs
- [ ] T059 [P] [US3] Implement analytics service for data aggregation in src/Consensus.Core/Services/AnalyticsService.cs
- [ ] T060 [US3] Create analytics API endpoints in src/Consensus.Api/Controllers/AnalyticsController.cs
- [ ] T061 [US3] Create Analytics page in src/Consensus.Web/Pages/Analytics.razor
- [ ] T062 [US3] Create winner distribution chart component in src/Consensus.Web/Components/WinnersBarChart.razor
- [ ] T063 [US3] Create wait time histogram component (PoET) in src/Consensus.Web/Components/WaitHistogram.razor
- [ ] T064 [US3] Create stake/burn trends chart component in src/Consensus.Web/Components/StakeBurnChart.razor
- [ ] T065 [US3] Implement analytics data export service in src/Consensus.Core/Services/ExportService.cs
- [ ] T066 [US3] Add CSV/JSON export API endpoints in src/Consensus.Api/Controllers/ExportController.cs
- [ ] T067 [US3] Create export download component in src/Consensus.Web/Components/ExportDownload.razor
- [ ] T068 [US3] Add Chart.js integration via JavaScript interop

**Checkpoint**: User Stories 1, 2, AND 3 should now be independently functional

---

## Phase 6: User Story 4 - Protocol Playground (Priority: P2)

**Goal**: Users can edit protocol-specific parameters, save configurations, and run custom experiments

**Independent Test**: Configure PoET parameters (wait bounds), save configuration, start simulation with custom parameters

### Tests for User Story 4

- [ ] T069 [P] [US4] Acceptance test for protocol configuration in tests/Consensus.E2E.Tests/PlaygroundTests.cs
- [ ] T070 [P] [US4] Integration test for settings API endpoints in tests/Consensus.Api.Tests/SettingsControllerTests.cs

### Implementation for User Story 4

- [ ] T071 [P] [US4] Create protocol settings DTOs in src/Consensus.Api/Models/ProtocolSettings.cs
- [ ] T072 [P] [US4] Implement settings repository in src/Consensus.Data/Repositories/SettingsRepository.cs
- [ ] T073 [US4] Create settings API endpoints in src/Consensus.Api/Controllers/SettingsController.cs
- [ ] T074 [US4] Create Playground page in src/Consensus.Web/Pages/Playground.razor
- [ ] T075 [US4] Create protocol parameter editor component in src/Consensus.Web/Components/ProtocolEditor.razor
- [ ] T076 [US4] Create configuration save/load functionality
- [ ] T077 [US4] Add parameter validation for each protocol type
- [ ] T078 [US4] Integrate playground with simulation start workflow

**Checkpoint**: Core simulation platform with analytics and configuration is complete

---

## Phase 7: User Story 5 - Payload Modes (Priority: P3)

**Goal**: Users can enable Supply-Chain or Federated Learning payload modes to see domain-specific block content

**Independent Test**: Enable Supply-Chain mode, run simulation, verify blocks contain product event payloads

### Tests for User Story 5

- [ ] T079 [P] [US5] Acceptance test for payload mode activation in tests/Consensus.E2E.Tests/PayloadTests.cs
- [ ] T080 [P] [US5] Unit tests for payload generators in tests/Consensus.Core.Tests/Payloads/PayloadGeneratorTests.cs

### Implementation for User Story 5

- [ ] T081 [P] [US5] Create payload interfaces: IPayloadGenerator in src/Consensus.Core/Interfaces/IPayloadGenerator.cs
- [ ] T082 [P] [US5] Implement Supply-Chain payload generator in src/Consensus.Core/Payloads/SupplyChainPayload.cs
- [ ] T083 [P] [US5] Implement Federated Learning payload generator in src/Consensus.Core/Payloads/FederatedLearningPayload.cs
- [ ] T084 [US5] Add payload mode configuration to experiment settings
- [ ] T085 [US5] Create payload-specific block card components in src/Consensus.Web/Components/SupplyChainBlockCard.razor
- [ ] T086 [US5] Create FL accuracy chart component in src/Consensus.Web/Components/FLAccuracyChart.razor
- [ ] T087 [US5] Integrate payload generators with consensus protocols
- [ ] T088 [US5] Add payload mode toggles to dashboard and playground

**Checkpoint**: Domain-specific simulation modes are functional

---

## Phase 8: User Story 6 - User Management & Roles (Priority: P3)

**Goal**: Implement role-based access control with Viewer, Operator, and Admin roles

**Independent Test**: Login with different role accounts, verify permission restrictions work correctly

### Tests for User Story 6

- [ ] T089 [P] [US6] Acceptance test for role-based access in tests/Consensus.E2E.Tests/AuthorizationTests.cs
- [ ] T090 [P] [US6] Integration test for authentication in tests/Consensus.Api.Tests/AuthenticationTests.cs

### Implementation for User Story 6

- [ ] T091 [P] [US6] Configure ASP.NET Core Identity in src/Consensus.Web/Program.cs
- [ ] T092 [P] [US6] Create User entity and roles in src/Consensus.Core/Entities/User.cs
- [ ] T093 [P] [US6] Create authentication middleware in src/Consensus.Api/Middleware/AuthenticationMiddleware.cs
- [ ] T094 [US6] Implement role-based authorization policies
- [ ] T095 [US6] Create login/logout pages in src/Consensus.Web/Pages/Authentication/
- [ ] T096 [US6] Create user management interface for admins in src/Consensus.Web/Pages/Admin/
- [ ] T097 [US6] Add authorization attributes to API controllers
- [ ] T098 [US6] Implement audit logging for admin actions
- [ ] T099 [US6] Create role-based navigation in layout components

**Checkpoint**: All user stories are complete with proper access control

---

## Phase 9: Additional Consensus Protocols

**Goal**: Implement remaining consensus protocols beyond PoET

**Independent Test**: Run simulations with each protocol type and verify protocol-specific behavior

- [ ] T100 [P] Implement PoW protocol in src/Consensus.Core/Protocols/PowProtocol.cs
- [ ] T101 [P] Implement PoS protocol in src/Consensus.Core/Protocols/PosProtocol.cs
- [ ] T102 [P] Implement DPoS protocol in src/Consensus.Core/Protocols/DposProtocol.cs
- [ ] T103 [P] Implement PoA protocol in src/Consensus.Core/Protocols/PoaProtocol.cs
- [ ] T104 [P] Implement PBFT protocol in src/Consensus.Core/Protocols/PbftProtocol.cs
- [ ] T105 [P] Implement PoB protocol in src/Consensus.Core/Protocols/PobProtocol.cs
- [ ] T106 Add protocol-specific unit tests in tests/Consensus.Core.Tests/Protocols/
- [ ] T107 Create protocol-specific parameter editors for each type
- [ ] T108 Add protocol-specific analytics metrics and charts

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T109 [P] Setup database seeding with sample experiments in src/Consensus.Data/Seed/SampleData.cs
- [ ] T110 [P] Add comprehensive error handling and user-friendly error messages
- [ ] T111 [P] Implement health checks endpoint at /health
- [ ] T112 [P] Add rate limiting middleware for API endpoints
- [ ] T113 [P] Performance optimization for large simulations (100 nodes, 100 rounds)
- [ ] T114 [P] Add comprehensive logging throughout the application
- [ ] T115 [P] Create deployment documentation in docs/deployment.md
- [ ] T116 [P] Validate quickstart.md against running application
- [ ] T117 Code cleanup and refactoring across all projects
- [ ] T118 Security audit and input validation hardening
- [ ] T119 Load testing with multiple concurrent simulations
- [ ] T120 [P] Add monitoring and metrics collection
- [ ] T121 [P] Create user documentation and help system

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-8)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Additional Protocols (Phase 9)**: Depends on US1 (basic simulation) completion
- **Polish (Phase 10)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Independent but integrates with US1 data
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Independent but uses US1 simulation data
- **User Story 4 (P2)**: Can start after Foundational (Phase 2) - Independent but enhances US1 functionality
- **User Story 5 (P3)**: Requires US1 completion for payload integration
- **User Story 6 (P3)**: Can start after Foundational (Phase 2) - Independent authentication system

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Models/DTOs before services
- Services before controllers
- Core implementation before UI components
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel (T002-T010)
- All Foundational tasks marked [P] can run in parallel within Phase 2 (T012-T029)
- Once Foundational phase completes, User Stories 1-4 and 6 can start in parallel (if team capacity allows)
- User Story 5 requires US1 completion before starting
- All tests for a user story marked [P] can run in parallel
- Entity/DTO creation tasks within a story marked [P] can run in parallel
- Protocol implementations (Phase 9) can all run in parallel
- Most Polish tasks marked [P] can run in parallel

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together:
Task: "Acceptance test for protocol selection and simulation start in tests/Consensus.E2E.Tests/SimulationFlowTests.cs"
Task: "Integration test for simulation API endpoints in tests/Consensus.Api.Tests/SimulationControllerTests.cs"
Task: "Unit tests for PoET protocol implementation in tests/Consensus.Core.Tests/Protocols/PoetProtocolTests.cs"

# Launch all DTOs for User Story 1 together:
Task: "Create StartSimulationRequest DTO in src/Consensus.Api/Models/StartSimulationRequest.cs"
Task: "Create RoundUpdate DTO for SignalR in src/Consensus.Web/Models/RoundUpdate.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Protocol Selection & Basic Simulation)
4. **STOP and VALIDATE**: Test User Story 1 independently with PoET protocol
5. Deploy/demo basic consensus simulation capability

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo (with block explorer)
4. Add User Story 3 → Test independently → Deploy/Demo (with analytics)
5. Add User Story 4 → Test independently → Deploy/Demo (with playground)
6. Add additional protocols → Full protocol support
7. Each story adds value without breaking previous functionality

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Core simulation)
   - Developer B: User Story 2 (Block explorer)
   - Developer C: User Story 4 (Playground)
   - Developer D: User Story 6 (Authentication)
3. User Story 3 starts after US1 completes (needs simulation data)
4. User Story 5 starts after US1 completes (needs simulation integration)
5. Additional protocols can be distributed across team members

---

## Notes

- [P] tasks = different files, no dependencies within the phase
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing (TDD approach)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Focus on MVP (User Story 1) first for early validation
- All file paths follow the established .NET solution structure from plan.md