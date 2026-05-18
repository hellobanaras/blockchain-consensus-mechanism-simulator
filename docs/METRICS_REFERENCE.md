# Metrics Reference

**Thesis:** An Integrated Simulation Framework for the Design and Empirical Evaluation of Configurable Multi-Protocol Blockchain Consensus Mechanisms  
**Version:** 1.0  
**Date:** 18 May 2026  
**Related:** [EXPERIMENT_PROTOCOL.md](./EXPERIMENT_PROTOCOL.md) · [THESIS_SCOPE_SPECIFICATION.md](./THESIS_SCOPE_SPECIFICATION.md)

---

## 1. Overview

This document defines **every metric** collected, computed, and reported for thesis experiments. Metrics are grouped into six categories aligned with the problem statement: performance, fairness, reliability, security proxy, efficiency, and health.

All metrics should be derivable from:
- Simulation export JSON (`SimulationResultsExportService`)
- Protocol `GetMetrics()` dictionaries
- Database entities: `SimulationRun`, `ConsensusRound`, `Block`, `EventLog`, `Node`

---

## 2. Metric Categories

| Category | Thesis Dimension | Primary Use |
|----------|------------------|-------------|
| Performance | Scalability, latency | RQ1, S1–S3, S6 |
| Fairness | Decentralization | RQ1, H3 |
| Reliability | Fault tolerance | RQ2, S4–S5, H4 |
| Security proxy | Byzantine tolerance | S4 |
| Efficiency | Energy / resource cost | RQ3, H2 |
| Health | Consensus stability | Dashboard, finality monitoring |
| Payload (AIDSE) | AI workload overhead | RQ4, S7, H5 |

---

## 3. Core Performance Metrics

### 3.1 Block Time (`meanBlockTimeMs`)

| Property | Value |
|----------|-------|
| **Definition** | Mean elapsed time between consecutive successful block productions |
| **Unit** | ms |
| **Formula** | `mean(t_block[i] - t_block[i-1])` for i = 1…n |
| **Source** | `ConsensusRound.Duration`, block timestamps |
| **Also report** | median, p95, p99, std dev |

### 3.2 Round Duration (`meanRoundDurationMs`)

| Property | Value |
|----------|-------|
| **Definition** | Mean time to complete one consensus round (success or fail) |
| **Unit** | ms |
| **Formula** | `mean(ConsensusResult.Duration)` per run |
| **Source** | `ConsensusResult.Duration` |

### 3.3 Throughput (`throughputTps`)

| Property | Value |
|----------|-------|
| **Definition** | Confirmed transactions per second |
| **Unit** | tx/s |
| **Formula** | `totalConfirmedTransactions / simulationDurationSeconds` |
| **Source** | `SimulationMetrics.ThroughputTps`, export summary |

### 3.4 Rounds per Second (`roundsPerSecond`)

| Property | Value |
|----------|-------|
| **Definition** | Completed consensus rounds per second |
| **Unit** | rounds/s |
| **Formula** | `successfulRounds / simulationDurationSeconds` |

### 3.5 Chain Height (`finalChainHeight`)

| Property | Value |
|----------|-------|
| **Definition** | Number of blocks on canonical chain at simulation end |
| **Unit** | blocks |
| **Source** | Block repository count for simulation |

---

## 4. Fairness Metrics

### 4.1 Leader / Proposer Distribution

| Property | Value |
|----------|-------|
| **Definition** | Count of times each node was selected as block proposer/leader |
| **Source** | Protocol `GetMetrics()["leaderDistribution"]` (PoW); witness/validator counts (PoS, DPoS, PBFT, PoET) |

**PoW keys:** `leaderDistribution`, `nodeHashrates`  
**PoS keys:** stake-based selection counts in round events  
**DPoS keys:** `witnessPerformance`, `currentWitness`, `activeWitnesses`  
**PBFT keys:** primary rotation in round events  
**PoET keys:** lottery winner counts in round events

### 4.2 Gini Coefficient (`leaderGini`)

| Property | Value |
|----------|-------|
| **Definition** | Inequality of leader selection across nodes (0 = perfect equality, 1 = one node leads all) |
| **Unit** | dimensionless [0, 1] |
| **Formula** | Standard Gini on vector `[c_1, c_2, …, c_n]` where c_i = leader count for node i |

```text
G = (2 · Σ(i · c_i)) / (n · Σ c_i) - (n + 1) / n    (sorted ascending)
```

**Thesis use:** H3 — compare DPoS vs PoS Gini at n=25, 50.

### 4.3 Selection Entropy (`leaderEntropy`)

| Property | Value |
|----------|-------|
| **Definition** | Shannon entropy of normalized leader distribution |
| **Unit** | bits |
| **Formula** | `-Σ p_i · log2(p_i)` where p_i = c_i / Σ c_j |

Higher entropy → more decentralized leader selection.

### 4.4 Stake Concentration (PoS / DPoS only)

| Property | Value |
|----------|-------|
| **Definition** | Gini coefficient on stake distribution |
| **Source** | DPoS `GetMetrics()["nodeStakes"]`, `delegations` |

---

## 5. Reliability Metrics

### 5.1 Consensus Success Rate (`consensusSuccessRate`)

| Property | Value |
|----------|-------|
| **Definition** | Fraction of rounds that produced a valid block |
| **Unit** | ratio [0, 1] |
| **Formula** | `successfulRounds / totalRounds` |
| **Source** | `ConsensusRoundStatus.Completed` vs total |

### 5.2 Failed Round Count (`failedRounds`)

| Property | Value |
|----------|-------|
| **Definition** | Rounds ending in Failed, Aborted, TimedOut, or Cancelled |
| **Source** | `ConsensusRoundStatus` aggregation |

### 5.3 Recovery Time (`recoveryTimeMs`)

| Property | Value |
|----------|-------|
| **Definition** | Time from fault end to first subsequent successful round |
| **Unit** | ms |
| **Applies to** | S5 partition, S4 Byzantine scenarios |
| **Formula** | `t_first_success_after_fault - t_fault_end` |

### 5.4 Fault Tolerance Score (`faultTolerance`)

| Property | Value |
|----------|-------|
| **Definition** | Platform-computed resilience indicator |
| **Source** | `SimulationMetrics.FaultTolerance` |
| **Interpretation** | Higher = maintained consensus under configured fault ratio |

### 5.5 Availability (`nodeAvailability`)

| Property | Value |
|----------|-------|
| **Definition** | Mean fraction of simulation time nodes were `Online` |
| **Formula** | `Σ(online_duration_i) / (n · simulationDuration)` |

---

## 6. Security Proxy Metrics

*Simulation-level indicators — not cryptographic proofs.*

### 6.1 Byzantine Tolerance Observed (`byzantineToleranceObserved`)

| Property | Value |
|----------|-------|
| **Definition** | Maximum Byzantine fraction at which success rate ≥ 95% |
| **Unit** | fraction |
| **Method** | Sweep S4 matrix; find threshold per protocol |

### 6.2 Invalid Block Rejection Rate

| Property | Value |
|----------|-------|
| **Definition** | Rejected proposals / total proposals |
| **Source** | `EventType` votes and validation events in PBFT/PoS |

### 6.3 PBFT Phase Completion (`pbftPrepareRatio`, `pbftCommitRatio`)

| Property | Value |
|----------|-------|
| **Definition** | Fraction of rounds completing prepare/commit phases |
| **Source** | PBFT event log (pre-prepare, prepare, commit counts) |

---

## 7. Efficiency Metrics

### 7.1 Energy Proxy — PoW (`energyProxyPow`)

| Property | Value |
|----------|-------|
| **Definition** | Total hash attempts across all nodes |
| **Unit** | hash attempts |
| **Source** | PoW `GetMetrics()`: `miningEfficiency`, nonce/hash loop counters |
| **Normalized** | `hashAttemptsPerBlock = totalAttempts / blocksProduced` |

**Thesis use:** H2 — compare against PoS/DPoS/PoET proxies.

### 7.2 Energy Proxy — PoS / DPoS (`energyProxyPos`)

| Property | Value |
|----------|-------|
| **Definition** | Computational steps for stake selection + validation |
| **Proxy** | Round count × node validation operations (simulated) |

### 7.3 Wait Time — PoET (`poetMeanWaitMs`)

| Property | Value |
|----------|-------|
| **Definition** | Mean simulated Trusted Execution Environment wait time |
| **Unit** | ms |
| **Source** | PoET `GetMetrics()`, round events |

### 7.4 Message Complexity — PBFT (`pbftMessageCount`)

| Property | Value |
|----------|-------|
| **Definition** | Total consensus messages (pre-prepare + prepare + commit) |
| **Unit** | messages |
| **Formula** | Approximately `O(n²)` per round × rounds |
| **Source** | PBFT `EventType.VoteCast` count |

### 7.5 Mining Efficiency (PoW)

| Property | Value |
|----------|-------|
| **Definition** | Platform metric from PoW protocol |
| **Source** | `GetMetrics()["miningEfficiency"]` |

---

## 8. Health Metrics

### 8.1 Finality Lag (`finalityLagMs`)

| Property | Value |
|----------|-------|
| **Definition** | Delay between block proposal and network finality confirmation |
| **Unit** | ms |
| **Source** | FinalityHealth dashboard, round event timestamps |

### 8.2 Orphaned Block Rate

| Property | Value |
|----------|-------|
| **Definition** | Blocks produced but not on canonical chain / total blocks |
| **Unit** | ratio |

### 8.3 Chain Progression Rate

| Property | Value |
|----------|-------|
| **Definition** | `finalChainHeight / simulationDurationSeconds` |
| **Unit** | blocks/s |

### 8.4 Network Latency (Configured vs Observed)

| Property | Value |
|----------|-------|
| **Configured** | `SimulationRun.NetworkLatencyMs` |
| **Observed** | Mean additional delay in round events beyond configured baseline |

---

## 9. Protocol-Specific `GetMetrics()` Keys

### Proof of Work

| Key | Type | Description |
|-----|------|-------------|
| `totalRounds` | int | Completed mining rounds |
| `averageRoundTime` | double | Mean round duration (ms) |
| `difficulty` | int | PoW difficulty target |
| `leaderDistribution` | dict | Leader wins per node |
| `nodeHashrates` | dict | Relative hash power |
| `miningEfficiency` | double | Platform efficiency score |

### Proof of Stake

| Key | Type | Description |
|-----|------|-------------|
| `totalRounds` | int | Staking rounds |
| `averageRoundTime` | double | Mean round duration |
| `totalStake` | decimal | Aggregate stake |
| `leaderDistribution` | dict | Validator selection counts |
| `slashingEvents` | int | Penalty events (if enabled) |

### Delegated Proof of Stake

| Key | Type | Description |
|-----|------|-------------|
| `activeWitnesses` | int | Current witness count |
| `witnessCount` | int | Configured witness set size |
| `witnessPerformance` | dict | Blocks per witness |
| `missedBlocks` | dict | Missed slots per witness |
| `votingParticipation` | decimal | Delegation participation ratio |
| `delegations` | dict | Stake delegations map |

### PBFT

| Key | Type | Description |
|-----|------|-------------|
| `totalRounds` | int | BFT rounds |
| `averageRoundTime` | double | Mean round duration |
| `viewChanges` | int | Primary failover events |
| `messageCount` | int | Total protocol messages |
| `leaderDistribution` | dict | Primary rotation counts |

### Proof of Elapsed Time

| Key | Type | Description |
|-----|------|-------------|
| `totalRounds` | int | Lottery rounds |
| `averageRoundTime` | double | Mean round duration |
| `averageWaitTime` | double | Mean PoET wait |
| `leaderDistribution` | dict | Lottery winners |

---

## 10. Payload Metrics (AIDSE / S7)

### 10.1 Federated Learning

| Metric | Definition | Unit |
|--------|------------|------|
| `flRoundsCompleted` | FL aggregation rounds finished | count |
| `flRoundMeanMs` | Mean time per FL round | ms |
| `flModelUpdateSizeKb` | Payload size per update | KB |
| `flParticipantCount` | Active FL nodes | count |
| `flConsensusOverheadMs` | FL round time − bare consensus time | ms |

**H5 test:** Pearson correlation between `flRoundMeanMs` and `meanRoundDurationMs`.

### 10.2 Supply Chain

| Metric | Definition | Unit |
|--------|------------|------|
| `scEventsProcessed` | Product events committed | count |
| `scTraceLatencyMs` | Event → block confirmation time | ms |

---

## 11. Aggregated Export Schema

### 11.1 Simulation Summary (per run)

```json
{
  "simulationId": "uuid",
  "runId": "S1-PoW-rep-001",
  "scenarioId": "S1-baseline",
  "protocol": "ProofOfWork",
  "configuration": { },
  "performance": {
    "meanBlockTimeMs": 5123.4,
    "p95BlockTimeMs": 6200.0,
    "p99BlockTimeMs": 7100.0,
    "throughputTps": 1.95,
    "roundsPerSecond": 0.19,
    "finalChainHeight": 114
  },
  "fairness": {
    "leaderGini": 0.12,
    "leaderEntropy": 2.87
  },
  "reliability": {
    "consensusSuccessRate": 0.98,
    "failedRounds": 2,
    "totalRounds": 116
  },
  "efficiency": {
    "energyProxy": 1250000,
    "energyProxyUnit": "hashAttempts"
  },
  "health": {
    "finalityLagMs": 450,
    "orphanedBlockRate": 0.01
  },
  "payload": null,
  "exportedAt": "2026-05-18T12:00:00Z"
}
```

### 11.2 Per-Round Record

```json
{
  "roundNumber": 42,
  "durationMs": 4980,
  "leaderNodeId": "uuid",
  "success": true,
  "blockHash": "abc123…",
  "timestamp": "2026-05-18T12:01:00Z",
  "events": ["LeaderSelection", "BlockCreated", "ConsensusReached"]
}
```

---

## 12. Dashboard & UI Mapping

| UI Component | Metrics Displayed |
|--------------|-------------------|
| Dashboard | Active nodes, running sims, live stats |
| AnalyticsV3 | Algorithm comparison, time series |
| SpecializedAnalytics | Protocol-specific charts (PoW hashrate, DPoS witnesses, PBFT phases) |
| PerformanceBaselines | Cross-run baseline comparison |
| FinalityHealth | Finality lag, chain health score |
| SimulationResults | Per-run export trigger |

---

## 13. Thesis Table Mapping

| Thesis Table | Metrics |
|--------------|---------|
| Table 5.1 Baseline latency | meanBlockTimeMs, p95BlockTimeMs, throughputTps — S1 |
| Table 5.2 Scalability | meanBlockTimeMs vs nodeCount — S2 |
| Table 5.3 Byzantine tolerance | consensusSuccessRate vs byzantineFraction — S4 |
| Table 5.4 Leader fairness | leaderGini, leaderEntropy — S1, S2 |
| Table 5.5 FL overhead | flRoundMeanMs, flConsensusOverheadMs — S7 |
| Table 5.6 Energy comparison | energyProxy by protocol — S1, H2 |

---

## 14. Computation Notes

### 14.1 Warm-Up Exclusion

Exclude rounds 1 through `warmupRounds` (default 5) from all aggregate metrics unless analyzing cold-start behavior.

### 14.2 Percentiles

Use linear interpolation (NumPy default) for p95/p99 across round durations within a run.

### 14.3 Cross-Run Aggregation

Pool repetitions at the (scenario × protocol) level. Report mean of per-run means ± 95% CI:

```text
CI = mean ± 1.96 · (std / sqrt(n))
```

### 14.4 Missing Data

| Condition | Handling |
|-----------|----------|
| Run status `Failed` | Exclude; log in DEVIATIONS.md |
| Partial completion | Include if ≥ 80% duration reached; flag in CSV |
| Missing export field | Compute from raw round data if available; else null |

---

## 15. Future Metrics (Optional Stretch)

| Metric | Purpose |
|--------|---------|
| Anomaly score on round latency | ML-based health alerting |
| Protocol recommendation confidence | RQ5 scoring model output |
| Carbon proxy (PoW) | kWh estimate from hash attempts × documented J/hash |

---

*Metrics definitions align with Annexure I expected outcomes: block progression, proposer distribution, latency, and consensus health indicators.*
