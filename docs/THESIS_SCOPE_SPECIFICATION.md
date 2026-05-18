# M.Tech Thesis Scope Specification

**Program:** Executive M.Tech. in Artificial Intelligence & Data Science Engineering (AIDSE)  
**Institution:** Indian Institute of Technology Patna  
**Semester:** Final Semester (Thesis)  
**Student:** Umesh Kumar (`umesh_25s03res52@iitp.ac.in`)  
**Document Version:** 1.0  
**Date:** 18 May 2026  
**Repository:** [hellobanaras/blockchain-consensus-mechanism-simulator](https://github.com/hellobanaras/blockchain-consensus-mechanism-simulator)  
**Branch:** `release`

---

## 1. Document Purpose

This specification defines the **complete scope, current baseline, gaps, and final-semester deliverables** for the M.Tech thesis titled:

> **An Integrated Simulation Framework for the Design and Empirical Evaluation of Configurable Multi-Protocol Blockchain Consensus Mechanisms**

It supersedes earlier ad-hoc specification folders (`specs/`, `.specify/`) that have been removed from the repository. This document is the **single authoritative scope reference** for thesis work, implementation priorities, experimental design, and thesis chapter planning aligned with the IIT Patna M.Tech thesis manual (60–70 pages, excluding front matter).

---

## 2. Problem Statement Alignment

### 2.1 Approved Abstract (Annexure I)

The approved problem statement establishes the following research intent:

| Theme | Requirement |
|-------|-------------|
| **Core challenge** | Systematic, data-driven evaluation of prominent blockchain consensus mechanisms in a unified, configurable simulation environment |
| **Protocols of interest** | PoW, PoS, DPoS, PBFT, PoET (and implied comparative study across trade-offs) |
| **Evaluation dimensions** | Security, decentralization, energy efficiency, scalability, fault tolerance |
| **Platform capabilities** | Configurable protocol parameters, granular node settings, real-time analytical monitoring |
| **Metrics** | Block progression, proposer/leader distribution, latency, consensus health indicators |
| **Integration** | Protocol simulation + visual dashboards + exportable analytics in one platform |
| **Context** | AI-integrated, data-intensive distributed workflows requiring robust coordination |
| **Expected outcomes** | (1) validated comparative benchmark, (2) protocol behavior under load/fault, (3) reusable educational research tool |

### 2.2 Thesis Positioning for AIDSE

This work sits at the intersection of **distributed systems**, **empirical systems research**, and **data-driven decision support**:

- **AI/DSE relevance:** Structured simulation telemetry enables comparative analytics, trend detection, and protocol suitability scoring—foundational for AI-integrated decentralized workflows (e.g., federated learning coordination).
- **Research contribution:** Not merely implementing protocols, but producing **reproducible empirical benchmarks** and **observable comparative insights** under controlled fault and load scenarios.
- **Practical orientation:** Experiment-driven learning—complex consensus concepts validated through observation, not theory alone.

---

## 3. Current Project State Assessment

*Assessment date: May 2026, `release` branch.*

### 3.1 Solution Architecture (As-Built)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        Consensus.Web (Blazor Server)                    │
│  Dashboard │ Simulations │ Block Explorer │ Analytics │ Auth (Identity) │
│  SignalR Hubs (SimulationHub, AnalyticsHub) │ Chart.js visualizations     │
└───────────────────────────────┬─────────────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────────────┐
│                     Consensus.Api (REST / OpenAPI)                      │
│  SimulationController │ BlocksController │ AnalyticsController │ ...    │
└───────────────────────────────┬─────────────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────────────┐
│                     Consensus.Core (Domain & Engine)                    │
│  SimulationService │ Protocols (PoW, PoS, DPoS, PBFT, PoET)             │
│  AnalyticsService │ PayloadService (SupplyChain, FederatedLearning)     │
└───────────────────────────────┬─────────────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────────────┐
│              Consensus.Data (EF Core + PostgreSQL / SQLite)             │
│  SimulationRun │ Node │ Block │ ConsensusRound │ EventLog │ Identity    │
└─────────────────────────────────────────────────────────────────────────┘
```

**Technology stack:** .NET 9.0, Blazor Server, ASP.NET Core, Entity Framework Core, PostgreSQL, SignalR, Bootstrap 5, Chart.js, Docker, GitHub Actions CI/CD.

**Scale:** ~169 source/test files, ~33,000 lines of C#/Razor across 4 projects + 4 test projects.

### 3.2 Implemented Capabilities

#### Infrastructure & Platform ✅

| Component | Status | Evidence |
|-----------|--------|----------|
| Multi-project clean architecture | Complete | `BlockchainConsensusSimulator.sln` |
| Database schema & migrations | Complete | EF Core migrations (Oct 2025) |
| Docker containerization | Complete | `Dockerfile`, `docker-compose.yml`, `DOCKER_GUIDE.md` |
| CI/CD pipelines | Complete | `.github/workflows/build-and-test.yml`, `code-quality.yml`, `deploy.yml` |
| Authentication & RBAC | Complete | ASP.NET Identity; Admin / Operator / Viewer roles |

#### Consensus Protocol Engine ✅ (Core Set)

| Protocol | Implementation | Unit Tests |
|----------|----------------|------------|
| Proof of Work (PoW) | `PowProtocol.cs` | 20 tests |
| Proof of Stake (PoS) | `PosProtocol.cs` | 24 tests |
| Delegated PoS (DPoS) | `DposProtocol.cs` | 20 tests |
| PBFT | `PbftProtocol.cs` | 22 tests |
| Proof of Elapsed Time (PoET) | `PoetProtocol.cs` | 13 tests |

**Test health:** 112 core unit tests passing (Consensus.Core.Tests); protocol layer is the most mature subsystem.

#### Simulation Orchestration ✅ (Substantial)

- `SimulationService` — create, start, pause, resume, stop simulations; protocol factory; round execution loop
- `SimulationHostedService` — background execution in web host
- Configurable parameters: node count, Byzantine nodes, network topology, block time, latency, transactions per block
- Fault types modeled: network partition, Byzantine, crash, slow response, invalid message

#### Web UI ✅ (Broad Coverage)

| Page / Module | Route / Purpose |
|---------------|-----------------|
| Dashboard | `/dashboard` — simulation control center, live stats, SignalR connectivity |
| Simulations | Simulation lifecycle management |
| Simulation Dashboard / Results | Per-run monitoring and results |
| Block Explorer | `/blocks`, `/block/{id}` |
| Node Management | `/nodes` |
| Analytics (v1, v3) | Performance KPIs, algorithm comparison |
| Specialized Analytics | Protocol-specific charts (PoW, PoS, DPoS, PBFT, PoET) |
| Performance Baselines | Comparative baseline metrics |
| Finality Health | Consensus finality monitoring |
| Protocol Playground | Parameter experimentation |
| Statistics Dashboard | Aggregate network statistics |

#### Analytics & Export ✅ (Partial)

- `AnalyticsService` — winner distribution, algorithm performance, time-series aggregation
- `AnalyticsExportService` — CSV and JSON export
- `SimulationResultsExportService` — run-level export
- Protocol-specific chart components for all five implemented protocols

#### Domain Extensions ✅ (Partial)

- **Payload modes:** `SupplyChain`, `FederatedLearning` (enum + service stubs for AI-relevant workloads)
- **Network topologies:** Full mesh, ring, star, tree, random, small-world, scale-free, grid, custom

### 3.3 Maturity Summary

| Layer | Maturity | Notes |
|-------|----------|-------|
| Protocol implementations | **High** | Five protocols with comprehensive unit tests |
| Simulation engine | **Medium–High** | Core loop works; persistence integration needs hardening |
| UI / UX | **Medium–High** | Many pages; some duplication (Analytics vs AnalyticsV3) |
| Comparative benchmarking | **Medium** | UI exists; standardized experiment matrix not yet defined |
| E2E / integration tests | **Low** | E2E project exists; minimal coverage |
| API test coverage | **Low** | Placeholder tests in Api/Web test projects |
| Documentation | **Medium** | README comprehensive but partially stale vs code |
| Thesis-grade experiments | **Not started** | Requires formal experimental design (Section 8) |

---

## 4. Gap Analysis (Problem Statement vs Current State)

### 4.1 Protocol Coverage

| Protocol (Problem Statement) | Status | Gap |
|------------------------------|--------|-----|
| PoW | ✅ Implemented | Energy metrics need formalization for thesis benchmarks |
| PoS | ✅ Implemented | Slashing / stake dynamics need documented validation |
| DPoS | ✅ Implemented | Delegate election fairness metrics need benchmark inclusion |
| PBFT | ✅ Implemented | f < n/3 fault scenarios need systematic test matrix |
| PoET | ✅ Implemented | SGX abstraction documented; compare against PoW energy proxy |
| PoA, PoB, Raft, etc. | ❌ Enum only | Out of thesis critical path unless advisor requires |

### 4.2 Evaluation & Empirical Research Gaps

| Requirement | Current State | Required for Thesis |
|-------------|---------------|---------------------|
| Side-by-side comparative experiments | Ad-hoc via UI | **Standardized benchmark suite** with fixed scenarios |
| Load variation studies | Configurable but undocumented | **Documented load profiles** (low / medium / high TPS) |
| Fault condition studies | Fault types in code | **Reproducible fault-injection experiments** with published results |
| Structured export for analysis | CSV/JSON export | **Experiment result schema** + statistical summary scripts |
| Consensus health indicators | FinalityHealth page | **Formal metric definitions** (availability, finality time, leader fairness) |
| Reproducibility | Manual runs | **Seed-controlled simulations** + run configuration snapshots |

### 4.3 Engineering Gaps

| Area | Issue | Priority |
|------|-------|----------|
| E2E tests | Not executable / minimal | High — needed for thesis validation claims |
| API integration tests | Placeholder only | Medium |
| README vs reality | Lists "In Progress" items now complete | Low — update during thesis write-up |
| Analytics duplication | Multiple analytics pages | Medium — consolidate for thesis demo |
| Persistence of live runs | Partial | High — ensure all metrics persisted for export |
| Deterministic replay | Not implemented | Medium — supports reproducibility claim |

### 4.4 AI/DSE Integration Gaps

| AIDSE Angle | Current State | Thesis Opportunity |
|-------------|---------------|-------------------|
| Federated learning payload | Service stub exists | Run FL coordination experiments under different consensus protocols |
| Data-driven protocol selection | Manual comparison | **Scoring model** from simulation metrics (rule-based or ML-assisted ranking) |
| Anomaly detection on consensus health | Not implemented | Optional stretch: flag unhealthy rounds from telemetry |
| Export for downstream analytics | CSV/JSON | Notebook-friendly datasets for statistical analysis |

---

## 5. Thesis Objectives & Research Questions

### 5.1 Primary Objective

Design, implement, and empirically validate an **integrated, configurable simulation framework** that enables reproducible comparative evaluation of blockchain consensus mechanisms across security, performance, decentralization, and fault-tolerance dimensions.

### 5.2 Specific Objectives

1. **O1 — Unified simulation platform:** Complete and harden the multi-protocol simulator with configurable nodes, topologies, latency, and fault injection.
2. **O2 — Empirical benchmark suite:** Define and execute a standardized set of comparative experiments across PoW, PoS, DPoS, PBFT, and PoET.
3. **O3 — Metrics & analytics framework:** Formalize metrics (latency, throughput, leader distribution, finality, fault recovery) with dashboards and export.
4. **O4 — Fault & load behavior analysis:** Characterize protocol behavior under varying load and Byzantine/crash/partition scenarios.
5. **O5 — AI-relevant workload validation:** Evaluate at least one AI-integrated payload mode (Federated Learning) under multiple consensus protocols.
6. **O6 — Reusable research artifact:** Deliver documented open-source tool with reproducible experiment configurations.

### 5.3 Research Questions

| ID | Research Question |
|----|-------------------|
| **RQ1** | How do PoW, PoS, DPoS, PBFT, and PoET compare on latency, throughput, and leader fairness under identical network conditions? |
| **RQ2** | How does each protocol degrade under increasing Byzantine node ratios and network partitions? |
| **RQ3** | Which protocol offers the best trade-off for energy/latency vs fault tolerance in permissioned vs permissionless-style configurations? |
| **RQ4** | How does consensus protocol choice affect coordination overhead for federated learning payload workloads? |
| **RQ5** | Can a data-driven scoring model recommend protocol suitability given workload and fault-tolerance requirements? |

### 5.4 Hypotheses (To Be Validated Empirically)

- **H1:** BFT-family protocols (PBFT) achieve lower finality latency than PoW under equal node counts in low-latency networks.
- **H2:** PoW exhibits higher energy-proxy cost (hash attempts) than PoS/DPoS/PoET for equivalent block production rates.
- **H3:** DPoS shows lower leader distribution variance than PoS at scale due to fixed delegate sets.
- **H4:** Protocol performance degrades non-linearly beyond a threshold Byzantine fraction (protocol-specific).
- **H5:** Federated learning round coordination time correlates strongly with consensus round latency.

---

## 6. Scope Definition

### 6.1 In Scope (Final Semester)

#### A. Platform Hardening
- End-to-end simulation flow: create → run → persist → visualize → export
- Deterministic simulation seeds for reproducibility
- Consolidated analytics dashboard (single primary analytics entry point)
- Experiment configuration templates (JSON/YAML) for batch runs

#### B. Benchmark Suite (Minimum 5 Scenarios × 5 Protocols)

| Scenario ID | Description | Parameters |
|-------------|-------------|------------|
| **S1 — Baseline** | Healthy network, no faults | 10 nodes, low latency (50 ms), 100 rounds |
| **S2 — Scale** | Node scaling | 5, 10, 25, 50 nodes |
| **S3 — Latency stress** | WAN simulation | 50, 200, 500, 1000 ms inter-node latency |
| **S4 — Byzantine fault** | Malicious nodes | 0%, 10%, 20%, 33% Byzantine (where applicable) |
| **S5 — Partition fault** | Network split | Temporary partition mid-simulation, recovery measurement |
| **S6 — Load** | Transaction pressure | Low / medium / high transactions per block |
| **S7 — FL payload** | AI workload | Federated learning rounds over PoS vs PBFT vs DPoS |

#### C. Metrics Framework

| Category | Metrics |
|----------|---------|
| **Performance** | Block time (mean, p95, p99), rounds/sec, transactions/sec |
| **Fairness** | Leader/proposer distribution (Gini coefficient, entropy) |
| **Reliability** | Consensus success rate, failed round count, recovery time after fault |
| **Security proxy** | Byzantine tolerance threshold observed, invalid block rejection rate |
| **Efficiency** | PoW hash attempts; PoET wait times; message count (PBFT) |
| **Health** | Finality lag, chain height progression, orphaned block rate |

#### D. Thesis Deliverables
- Complete thesis document (IIT Patna format, 60–70 pages body)
- Working deployed demo (Docker)
- Benchmark dataset (CSV/JSON) with analysis
- Source code on GitHub (`release` → tagged thesis version)
- Viva presentation (derived from 3rd sem presentation baseline)

#### E. Documentation
- This scope specification (authoritative)
- Experiment protocol: [`docs/EXPERIMENT_PROTOCOL.md`](./EXPERIMENT_PROTOCOL.md)
- Metrics reference: [`docs/METRICS_REFERENCE.md`](./METRICS_REFERENCE.md)
- Benchmark templates: [`docs/experiments/`](./experiments/)
- API and deployment guide updates in `docs/` (pending)
- Reproducibility README section (pending)

### 6.2 Out of Scope

- Production blockchain deployment or mainnet integration
- Full cryptographic security proofs (simulation-level abstraction acceptable)
- Implementation of all enum-listed protocols (Raft, Tendermint, Algorand, etc.)
- Hardware SGX / real Intel PoET enclave integration
- Mobile client or multi-tenant SaaS deployment
- Patent filing or journal publication (optional stretch)

### 6.3 Assumptions & Constraints

- Simulations run on a single host with simulated network latency (not real distributed deployment)
- Byzantine behavior is modeled, not cryptographically adversarial
- Thesis page limit: 60–70 pages (IIT Patna); detailed code listings go to appendix/CD
- Timeline: one final semester (~4–5 months)

---

## 7. System Requirements (Target State)

### 7.1 Functional Requirements

| ID | Requirement | Acceptance Criteria |
|----|-------------|---------------------|
| FR-01 | User selects any of 5 implemented protocols and configures simulation | Protocol selector + validation enforced |
| FR-02 | User configures nodes (3–100), topology, latency, Byzantine count | Parameters persisted with run |
| FR-03 | Simulation runs with real-time round updates | SignalR broadcasts round events < 2 s delay |
| FR-04 | Results persisted to database | All rounds, blocks, events queryable post-run |
| FR-05 | Comparative analytics across runs | Filter by protocol, scenario, date range |
| FR-06 | Export results as CSV and JSON | Download from UI and API |
| FR-07 | Fault injection during active simulation | Partition, crash, Byzantine toggling |
| FR-08 | Federated learning payload mode runnable | FL metrics captured in event log |
| FR-09 | Benchmark scenarios loadable from template | One-click or scriptable batch execution |
| FR-10 | Role-based access | Viewer read-only; Operator runs sims; Admin full access |

### 7.2 Non-Functional Requirements

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-01 | Simulation throughput | ≥ 10 concurrent simulations (dev hardware) |
| NFR-02 | UI responsiveness | Dashboard refresh < 3 s for 100 rounds history |
| NFR-03 | Test coverage (core protocols) | Maintain ≥ 100 passing unit tests |
| NFR-04 | Reproducibility | Same seed + config → identical leader sequence (deterministic mode) |
| NFR-05 | Deployability | `docker-compose up` → working demo in < 5 min |
| NFR-06 | Maintainability | Layered architecture preserved; no circular dependencies |

---

## 8. Experimental Design & Methodology

### 8.1 Research Methodology

This thesis follows a **design science + empirical evaluation** methodology:

1. **Artifact design:** Extend the simulation framework (implementation).
2. **Evaluation:** Execute controlled experiments against defined scenarios.
3. **Analysis:** Statistical comparison of metrics across protocols and conditions.
4. **Knowledge contribution:** Document trade-offs, suitability guidelines, and reproducible benchmarks.

### 8.2 Independent Variables

- Consensus protocol (5 levels)
- Node count (4 levels)
- Network latency (4 levels)
- Byzantine node fraction (4 levels)
- Transaction load (3 levels)
- Payload mode (None vs FederatedLearning)

### 8.3 Dependent Variables

- Mean block time, p95 latency
- Consensus success rate
- Leader distribution fairness (Gini)
- Recovery time after fault
- Energy proxy (PoW hashes, PoET wait)
- FL round completion time

### 8.4 Controls

- Fixed random seed per experiment batch
- Identical hardware profile documented
- Same .NET runtime version
- Warm-up rounds excluded from analysis
- Minimum 30 repetitions per configuration (or justified lower bound with confidence intervals)

### 8.5 Analysis Methods

- Descriptive statistics (mean, median, std dev, percentiles)
- Comparative charts (bar, box plot, time series)
- Protocol ranking via weighted scoring model (AIDSE contribution)
- Optional: ANOVA or Kruskal-Wallis for multi-protocol comparison (if sample size permits)

---

## 9. Work Breakdown Structure (Final Semester)

### Phase 1: Foundation & Hardening (Weeks 1–4)

| Task | Description | Output |
|------|-------------|--------|
| T1.1 | Audit and fix simulation persistence pipeline | All metrics stored post-run |
| T1.2 | Add deterministic seed support | Reproducible runs |
| T1.3 | Consolidate analytics UI | Single primary analytics module |
| T1.4 | Fix/expand E2E smoke test | CI green on critical path |
| T1.5 | Update README and deployment docs | Accurate developer guide |

### Phase 2: Benchmark Infrastructure (Weeks 5–8)

| Task | Description | Output |
|------|-------------|--------|
| T2.1 | Define experiment configuration schema | JSON template files |
| T2.2 | Implement batch experiment runner | CLI or API batch mode |
| T2.3 | Formalize metrics definitions | Metrics reference in docs |
| T2.4 | Enhance export for analysis | Notebook-ready datasets |
| T2.5 | Complete FL payload integration | S7 scenario runnable |

### Phase 3: Experiments & Analysis (Weeks 9–12)

| Task | Description | Output |
|------|-------------|--------|
| T3.1 | Execute scenarios S1–S6 across all protocols | Raw result datasets |
| T3.2 | Execute S7 (FL payload) | FL-specific results |
| T3.3 | Statistical analysis | Charts, tables for thesis |
| T3.4 | Protocol suitability scoring model | Rankings + decision guide |
| T3.5 | Validate/refute hypotheses H1–H5 | Results section draft |

### Phase 4: Thesis Writing & Submission (Weeks 13–16)

| Task | Description | Output |
|------|-------------|--------|
| T4.1 | Draft thesis chapters 1–5 | Full manuscript |
| T4.2 | Prepare figures, tables, appendices | Camera-ready diagrams |
| T4.3 | Advisor review iterations | Revised thesis |
| T4.4 | Viva presentation | Slides + demo |
| T4.5 | Final submission (hard + soft copy) | IIT Patna submission |

---

## 10. Thesis Document Outline (IIT Patna Format)

*Target: 60–70 pages body; front matter additional.*

| Section | Content | Est. Pages |
|---------|---------|------------|
| **Title Page** | Approved title, student name, IIT Patna, copyright | 1 |
| **Certificate / Declaration / Approval** | Institute templates | 3 |
| **Acknowledgements** | Advisor, institute, collaborators | 1 |
| **Abstract** | 250–350 words summarizing problem, method, results | 1 |
| **Table of Contents, Lists of Figures/Tables** | Auto-generated | 3 |
| **List of Abbreviations** | PoW, PoS, DPoS, PBFT, PoET, BFT, TPS, etc. | 1 |
| **Chapter 1: Introduction** | Motivation, problem statement, objectives, contributions, organization | 8 |
| **Chapter 2: Literature Review** | Consensus families, simulators, comparative studies, research gap | 12 |
| **Chapter 3: System Design** | Architecture, data model, protocol abstractions, metrics framework | 14 |
| **Chapter 4: Implementation** | Technology stack, key modules, UI, deployment | 10 |
| **Chapter 5: Experimental Evaluation** | Methodology, scenarios, results, hypothesis validation, discussion | 14 |
| **Chapter 6: Conclusion & Future Work** | Summary, limitations, future research | 4 |
| **References** | IEEE format recommended | 4 |
| **Appendices** | Config samples, API summary, extra result tables | CD / appendix |
| **List of Publications** | If any | 1 |

### 10.1 Suggested Chapter 1 Structure

1. Background and motivation (blockchain in finance, healthcare, AI workflows)
2. Problem statement (from Annexure I)
3. Research objectives and questions
4. Scope and limitations
5. Thesis organization

### 10.2 Suggested Chapter 5 Result Tables

- **Table 5.1:** Baseline latency comparison (S1)
- **Table 5.2:** Scalability — block time vs node count (S2)
- **Table 5.3:** Byzantine fault tolerance thresholds (S4)
- **Table 5.4:** Leader fairness metrics (Gini by protocol)
- **Table 5.5:** FL payload coordination overhead (S7)
- **Figure 5.1:** Architecture diagram
- **Figure 5.2:** Latency box plots by protocol
- **Figure 5.3:** Fault recovery time series
- **Figure 5.4:** Protocol suitability radar chart

---

## 11. Deliverables Checklist

### 11.1 Software Artifacts

- [ ] Tagged release (`v1.0-thesis`) on GitHub
- [ ] Docker-compose demo verified
- [ ] 5 protocols passing all unit tests
- [ ] Benchmark scenario templates in `docs/experiments/`
- [ ] Exported datasets from all scenarios
- [ ] Batch experiment runner script

### 11.2 Documentation Artifacts

- [x] Thesis scope specification (this document)
- [x] Experiment protocol document ([EXPERIMENT_PROTOCOL.md](./EXPERIMENT_PROTOCOL.md))
- [x] Metrics reference guide ([METRICS_REFERENCE.md](./METRICS_REFERENCE.md))
- [x] Benchmark JSON templates ([experiments/](./experiments/))
- [x] Experiment suite expand script (`scripts/expand-experiment-suite.sh`)
- [ ] Updated README reflecting actual status
- [ ] Reproducibility guide (seed, config, hardware)

### 11.3 Academic Artifacts

- [ ] Thesis PDF (60–70 pages)
- [ ] Viva presentation
- [ ] Demo video (optional backup)
- [ ] Advisor approval forms
- [ ] Soft copy on Moodle

---

## 12. Evaluation Criteria (Thesis Defense)

The thesis will be evaluated against:

| Criterion | Weight | Evidence |
|-----------|--------|----------|
| **Problem relevance** | 15% | Alignment with Annexure I and AIDSE domain |
| **Technical depth** | 25% | Architecture, protocol implementations, engineering quality |
| **Experimental rigor** | 25% | Reproducible experiments, statistical analysis |
| **Results & insights** | 20% | Meaningful comparative findings, hypothesis outcomes |
| **Documentation & presentation** | 10% | Thesis quality, demo, clarity |
| **Originality & contribution** | 5% | Reusable tool + benchmark data |

---

## 13. Risk Register

| Risk | Impact | Mitigation |
|------|--------|------------|
| Non-deterministic simulation results | High | Implement seeded RNG; document variance |
| Insufficient experiment time | High | Prioritize S1, S4, S7; automate batch runs early |
| Scope creep (more protocols) | Medium | Strict adherence to 5-protocol scope |
| E2E test failures block CI | Medium | Focus on core path smoke test first |
| Thesis page limit exceeded | Medium | Move code listings to appendix/CD |
| Advisor requests ML component | Medium | Protocol scoring model satisfies AIDSE angle |
| Hardware limits large-scale sims | Low | Cap at 50 nodes; extrapolate with caution |

---

## 14. Repository Structure (Target)

```
blockchain-consensus-mechanism-simulator/
├── docs/
│   ├── THESIS_SCOPE_SPECIFICATION.md    ← this document
│   ├── EXPERIMENT_PROTOCOL.md           ← to be created
│   ├── METRICS_REFERENCE.md             ← to be created
│   └── experiments/                     ← benchmark JSON templates
│       ├── S1-baseline.json
│       ├── S4-byzantine-matrix.json
│       └── S7-federated-learning.json
├── mtech/
│   ├── ANNEXURE_I_ABSTRACT_OF_PROBLEM_STATEMENT.pdf
│   └── M. Tech Thesis Format Final_CET.pdf
├── src/                                 ← application code
├── tests/
├── scripts/
├── Dockerfile
└── docker-compose.yml
```

---

## 15. Success Criteria

The final-semester thesis work is **successful** when:

1. All five protocols (PoW, PoS, DPoS, PBFT, PoET) run end-to-end through the web UI with persisted, exportable results.
2. At least **six benchmark scenarios** are executed with documented configurations and published datasets.
3. Research questions **RQ1–RQ4** are answered with empirical evidence; RQ5 addressed via scoring model.
4. A comparative analysis demonstrates **actionable protocol suitability guidance** for at least two use cases (e.g., high-throughput ledger vs BFT permissioned network vs FL coordination).
5. Thesis document meets IIT Patna formatting requirements and is approved by the advisor.
6. Live demo succeeds during viva voce.

---

## 16. References (Initial — To Expand in Thesis)

1. Nakamoto, S. (2008). *Bitcoin: A Peer-to-Peer Electronic Cash System.*
2. Castro, M., & Liskov, B. (1999). *Practical Byzantine Fault Tolerance.* OSDI.
3. King, S., & Nadal, S. (2012). *PPCoin: Peer-to-Peer Crypto-Currency with Proof-of-Stake.*
4. Larimer, D. (2014). *Delegated Proof-of-Stake.* BitShares documentation.
5. Intel Corporation. *Hyperledger Sawtooth Proof of Elapsed Time.*
6. Zheng, Z., et al. (2018). *An Overview of Blockchain Technology: Architecture, Consensus, and Future Trends.* IEEE ICWS.
7. Androulaki, E., et al. (2018). *Hyperledger Fabric: A Distributed Operating System for Permissioned Blockchains.* EuroSys.
8. IIT Patna. *M.Tech Thesis Manual.* CET Office.

---

## 17. Document Approval

| Role | Name | Signature | Date |
|------|------|-----------|------|
| Student | Umesh Kumar | | |
| Thesis Advisor | | | |
| Program Coordinator | | | |

---

*This specification is maintained in the repository `docs/` folder and should be updated when scope changes are approved by the thesis advisor.*
