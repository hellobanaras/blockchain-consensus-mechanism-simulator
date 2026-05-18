# Experiment Protocol

**Thesis:** An Integrated Simulation Framework for the Design and Empirical Evaluation of Configurable Multi-Protocol Blockchain Consensus Mechanisms  
**Student:** Umesh Kumar — Executive M.Tech. AIDSE, IIT Patna  
**Version:** 1.0  
**Date:** 18 May 2026  
**Related documents:** [THESIS_SCOPE_SPECIFICATION.md](./THESIS_SCOPE_SPECIFICATION.md) · [METRICS_REFERENCE.md](./METRICS_REFERENCE.md)

---

## 1. Purpose

This protocol defines **how** comparative consensus experiments are designed, executed, recorded, and analyzed for the M.Tech thesis. It ensures reproducibility, fair cross-protocol comparison, and traceability from research questions to published results.

All benchmark configurations live in [`docs/experiments/`](./experiments/).

---

## 2. Research Mapping

| Scenario | ID | Research Questions | Hypotheses | Config File |
|----------|----|--------------------|------------|-------------|
| Baseline comparison | S1 | RQ1 | H1, H2, H3 | `S1-baseline.json` |
| Node scaling | S2 | RQ1, RQ3 | H1 | `S2-scale.json` |
| Latency stress | S3 | RQ1, RQ3 | H1 | `S3-latency-stress.json` |
| Byzantine fault | S4 | RQ2, RQ3 | H4 | `S4-byzantine-matrix.json` |
| Network partition | S5 | RQ2 | H4 | `S5-partition-fault.json` |
| Transaction load | S6 | RQ1 | — | `S6-load.json` |
| Federated learning payload | S7 | RQ4, RQ5 | H5 | `S7-federated-learning.json` |

---

## 3. Protocols Under Test

| Protocol | Enum Value | Min Nodes | Byzantine Limit | Notes |
|----------|------------|-----------|-----------------|-------|
| Proof of Work | `ProofOfWork` (0) | 2 (engine: 3) | ≤ ⌊n/3⌋ | Energy proxy via hash attempts |
| Proof of Stake | `ProofOfStake` (1) | 3 | ≤ ⌊n/3⌋ | Stake-weighted leader selection |
| Delegated PoS | `DelegatedProofOfStake` (2) | 3 | ≤ ⌊n/3⌋ | Fixed witness set; fairness focus |
| PBFT | `PracticalByzantineFaultTolerance` (3) | 4 | ≤ ⌊n/3⌋ | Requires 3f+1 nodes |
| Proof of Elapsed Time | `ProofOfElapsedTime` (10) | 3 | ≤ ⌊n/3⌋ | Simulated wait-time lottery |

**Standard comparison set:** All five protocols above. Runs use identical node counts, latency, topology, and duration unless the scenario explicitly varies one dimension.

---

## 4. Environment & Reproducibility

### 4.1 Hardware Profile (Record for Every Batch)

Document before each experiment batch:

```
CPU model:
Core count:
RAM (GB):
OS:
.NET version: 9.0.x
Database: PostgreSQL version / SQLite (dev)
Docker image tag (if used):
Host load: idle / moderate
```

Store this in the experiment log (`docs/experiments/results/<batch-id>/environment.json`).

### 4.2 Software Baseline

- Repository: `release` branch (tag `v1.0-thesis` at submission)
- Application started via Docker Compose or `dotnet run --project src/Consensus.Web`
- Default credentials for automated runs: `admin@consensus-lab.dev` / `Admin@123!` (Operator role)

### 4.3 Randomness & Seeds

| Control | Value | Implementation Status |
|---------|-------|----------------------|
| Suite-level seed | Defined per JSON file (`randomSeed`) | **Target** — pass via `algorithmConfiguration.seed` |
| Repetition index | `rep-001` … `rep-030` | Derive: `seed + repetitionIndex` |
| Warm-up exclusion | First 5 rounds excluded from analysis | Post-processing rule |

Until deterministic seed support is merged, record the simulation `Id` (GUID) and timestamp for each run and note non-determinism in the thesis limitations section.

### 4.4 Repetitions

| Scenario | Repetitions | Rationale |
|----------|-------------|-----------|
| S1 Baseline | 30 per protocol | Stable central tendency for RQ1 |
| S2 Scale | 10 per (protocol × node count) | 4 levels × 5 protocols × 10 = 200 runs |
| S3 Latency | 10 per (protocol × latency) | 4 levels × 5 protocols × 10 = 200 runs |
| S4 Byzantine | 10 per (protocol × fraction) | Focus on degradation curve |
| S5 Partition | 10 per protocol | Recovery time variance |
| S6 Load | 10 per (protocol × load level) | Throughput sensitivity |
| S7 FL payload | 15 per protocol | H5 correlation analysis |

**Total estimated runs:** ~500–700 (adjust if time-constrained; minimum 10 repetitions for thesis validity).

---

## 5. Configuration Schema

Each file in `docs/experiments/` follows this structure:

```json
{
  "schemaVersion": "1.0",
  "experimentSuite": {
    "id": "S1-baseline",
    "name": "Human-readable title",
    "description": "What this suite tests",
    "researchQuestions": ["RQ1"],
    "hypotheses": ["H1"],
    "repetitions": 30,
    "randomSeed": 42,
    "warmupRounds": 5,
    "runs": [ { ... } ]
  }
}
```

### 5.1 Run Object Fields

| Field | Required | Description |
|-------|----------|-------------|
| `runId` | Yes | Unique ID, e.g. `S1-PoW-rep-001` |
| `name` | Yes | Display name stored in simulation record |
| `protocol` | Yes | Enum string: `ProofOfWork`, `ProofOfStake`, etc. |
| `simulation` | Yes | Parameters matching `StartSimulationRequest` |
| `payloadMode` | No | `None`, `SupplyChain`, `FederatedLearning` |
| `payloadParameters` | No | Mode-specific parameters |
| `faultInjection` | No | Fault schedule (S4, S5) |
| `tags` | No | Filtering labels for export/analysis |
| `expectedDurationSeconds` | No | Planning aid |

### 5.2 Simulation Parameters (API-Compatible)

Maps to `POST /api/Simulation/start`:

| Parameter | Type | Range | Default |
|-----------|------|-------|---------|
| `nodeCount` | int | 3–100 | 10 |
| `byzantineNodeCount` | int | 0–⌊n/3⌋ | 0 |
| `durationSeconds` | int | 10–3600 | 300 |
| `networkTopology` | string | See enum | `FullMesh` |
| `blockTimeMs` | int | 1000–60000 | 5000 |
| `transactionsPerBlock` | int | 1–1000 | 10 |
| `networkLatencyMs` | int | 0–2000 | 50 |
| `algorithmConfiguration` | object | Protocol-specific | `{}` |
| `enableDetailedLogging` | bool | — | true for thesis runs |
| `autoStart` | bool | — | true |

---

## 6. Scenario Procedures

### S1 — Baseline (Healthy Network)

**Objective:** Establish fair cross-protocol comparison under identical benign conditions.

**Fixed parameters:**
- 10 nodes, 0 Byzantine, FullMesh topology
- 50 ms latency, 5000 ms block time, 10 tx/block
- 600 s duration (~100+ rounds depending on protocol)

**Procedure:**
1. Load `S1-baseline.json`
2. For each protocol × repetition: submit run via API or UI
3. Wait for `Completed` status
4. Export JSON results → `results/S1/<runId>.json`
5. Exclude first 5 rounds in analysis

**Primary metrics:** mean block time, p95 latency, TPS, leader Gini, consensus success rate.

---

### S2 — Node Scaling

**Objective:** Measure scalability as node count increases.

**Varied parameter:** `nodeCount` ∈ {5, 10, 25, 50}  
**Fixed:** S1 defaults otherwise.

**Analysis:** Plot block time and TPS vs node count; one chart per protocol.

---

### S3 — Latency Stress

**Objective:** Simulate LAN vs WAN vs high-latency conditions.

**Varied parameter:** `networkLatencyMs` ∈ {50, 200, 500, 1000}  
**Fixed:** 10 nodes, 0 Byzantine.

**Analysis:** Compare PBFT message-phase impact vs PoW/PoET lottery delay.

---

### S4 — Byzantine Fault Matrix

**Objective:** Find degradation thresholds per protocol.

**Varied parameter:** Byzantine count as fraction of n ∈ {0%, 10%, 20%, 33%}  
**Fixed:** 10 nodes (Byzantine counts: 0, 1, 2, 3).

| Nodes | 0% | 10% | 20% | 33% |
|-------|----|----|-----|-----|
| 10 | 0 | 1 | 2 | 3 |

**Procedure:**
1. Mark Byzantine nodes at simulation start (via `byzantineNodeCount`)
2. Record consensus success rate and failed round count
3. For PBFT, note behavior at f=3 (n=10, Byzantine=3)

**Safety note:** At 33% Byzantine, PBFT may fail by design — document as expected boundary, not platform bug.

---

### S5 — Network Partition

**Objective:** Measure recovery after temporary split.

**Fault injection schedule (target behavior):**

```json
"faultInjection": {
  "type": "NetworkPartition",
  "triggerAtSeconds": 120,
  "durationSeconds": 60,
  "affectedNodeFraction": 0.4,
  "recovery": "automatic"
}
```

**Procedure:**
1. Run 600 s simulation
2. Inject partition at t=120 s for 60 s
3. Measure: rounds failed during partition, recovery time (first successful round post-heal), chain height delta

**Fallback (if fault injection API not ready):** Pre-set `byzantineNodeCount` + document as static fault scenario; note limitation in thesis.

---

### S6 — Transaction Load

**Objective:** Throughput under increasing block pressure.

**Varied parameter:** `transactionsPerBlock` ∈ {5, 50, 200} (low / medium / high)  
**Fixed:** S1 network defaults.

**Analysis:** TPS, mean block time, failed validations.

---

### S7 — Federated Learning Payload

**Objective:** AI-relevant workload — FL round coordination under different consensus protocols.

**Protocols:** PoS, DPoS, PBFT (exclude PoW for energy irrelevance in FL setting)  
**Payload:**

```json
"payloadMode": "FederatedLearning",
"payloadParameters": {
  "participantCount": 10,
  "modelUpdateSizeKb": 256,
  "roundsPerSimulation": 20,
  "aggregationStrategy": "fedavg"
}
```

**Primary metrics:** FL round completion time, correlation with consensus round latency (H5), payload events in export.

---

## 7. Execution Methods

### 7.1 Manual (UI)

1. Login as Operator
2. Dashboard → New Simulation
3. Enter parameters from JSON run definition
4. Start → wait for completion
5. Simulation Results → Export JSON

### 7.2 API (Recommended for Batch)

```bash
# Authenticate (cookie or bearer token depending on deployment)
curl -X POST http://localhost:5027/api/Simulation/start \
  -H "Content-Type: application/json" \
  -d @docs/experiments/payloads/S1-PoW-rep-001.json
```

Poll status:

```bash
curl http://localhost:5027/api/Simulation/{simulationId}/status
```

Export:

```bash
curl http://localhost:5027/api/SimulationResults/{simulationId}/export?format=JSON \
  -o results/S1-PoW-rep-001.json
```

### 7.3 Batch Runner (To Be Implemented)

Target script: `scripts/run-experiment-suite.sh`

```
./scripts/run-experiment-suite.sh docs/experiments/S1-baseline.json \
  --output docs/experiments/results/S1 \
  --repetitions 30
```

---

## 8. Data Collection & Storage

### 8.1 Directory Layout

```
docs/experiments/
├── S1-baseline.json
├── ...
├── payloads/              ← single-run API bodies (optional, generated)
└── results/
    ├── S1/
    │   ├── environment.json
    │   ├── S1-PoW-rep-001.json
    │   └── summary.csv
    ├── S4/
    └── analysis/
        ├── S1-latency-comparison.ipynb
        └── all-runs-aggregated.csv
```

### 8.2 Required Fields per Result File

Extract from export or database:

- `simulationId`, `runId`, `protocol`, `scenarioId`
- Configuration snapshot (all input parameters)
- Per-round: round number, duration, leader, success, block hash
- Aggregates: see [METRICS_REFERENCE.md](./METRICS_REFERENCE.md)
- Timestamps: created, started, completed

### 8.3 Aggregation CSV (Cross-Run)

Minimum columns for thesis tables:

```
runId, scenarioId, protocol, repetition, nodeCount, byzantineCount,
networkLatencyMs, transactionsPerBlock, durationSeconds,
consensusSuccessRate, meanBlockTimeMs, p95BlockTimeMs, p99BlockTimeMs,
throughputTps, leaderGini, totalRounds, failedRounds, recoveryTimeMs,
energyProxy, payloadMode, flRoundMeanMs, simulationId, exportedAt
```

---

## 9. Statistical Analysis Plan

### 9.1 Descriptive Statistics

For each (scenario × protocol): mean, median, standard deviation, 95% CI for mean block time and TPS.

### 9.2 Comparative Tests

| Comparison | Method |
|------------|--------|
| All protocols on S1 | Kruskal-Wallis + pairwise Mann-Whitney (Bonferroni correction) |
| Byzantine fraction trend | Spearman correlation (fraction vs success rate) |
| FL vs consensus latency (H5) | Pearson correlation (r, p-value) |
| Scale/latency curves | Linear regression slope comparison |

Use Python (`scipy`, `pandas`) or R; notebooks stored under `docs/experiments/results/analysis/`.

### 9.3 Protocol Suitability Scoring (RQ5)

Weighted score per protocol:

```
Score = w1·norm(TPS) + w2·norm(1/latency) + w3·norm(successRate)
      + w4·norm(1/energyProxy) + w5·norm(1/leaderGini)
```

Default weights for **throughput-priority**: w1=0.3, w2=0.25, w3=0.25, w4=0.1, w5=0.1  
Default weights for **BFT-priority**: w3=0.4, w2=0.3, w5=0.2, w1=0.05, w4=0.05

Present as radar chart in thesis Chapter 5.

---

## 10. Quality Gates

Before including results in the thesis:

- [ ] All runs reached `Completed` status (failed runs logged and excluded with reason)
- [ ] Export JSON validates against expected schema
- [ ] Warm-up rounds excluded consistently
- [ ] Environment.json recorded for the batch
- [ ] At least minimum repetitions met per scenario
- [ ] Outliers (>3σ) investigated and documented
- [ ] Cross-protocol comparisons use identical non-varied parameters
- [ ] Advisor review of S1 and S4 results before full batch

---

## 11. Timeline (Experiment Phase)

| Week | Activity |
|------|----------|
| 9 | S1 baseline — all protocols, 30 reps |
| 10 | S2 scale + S3 latency |
| 11 | S4 Byzantine + S5 partition |
| 12 | S6 load + S7 FL; aggregate + statistical analysis |

---

## 12. Deviations & Limitations

Document any deviation from this protocol in `docs/experiments/results/DEVIATIONS.md`:

- Parameter changes and rationale
- Failed runs and exclusion criteria
- Platform bugs affecting results
- Non-determinism observations

**Known platform limitations (as of May 2026):**
- Simulated network (single host), not physical distribution
- Byzantine behavior is modeled, not adversarial cryptography
- Deterministic seed support planned but not yet enforced
- S5 dynamic fault injection may require static fallback

---

## 13. References

- Thesis scope: [THESIS_SCOPE_SPECIFICATION.md](./THESIS_SCOPE_SPECIFICATION.md)
- Metrics definitions: [METRICS_REFERENCE.md](./METRICS_REFERENCE.md)
- API: `POST /api/Simulation/start` — see `src/Consensus.Api/Controllers/SimulationController.cs`
- Problem statement: `mtech/ANNEXURE_I_ABSTRACT_OF_PROBLEM_STATEMENT.pdf`
