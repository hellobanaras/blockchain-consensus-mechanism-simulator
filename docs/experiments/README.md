# Experiment Configuration Templates

Benchmark suites for the M.Tech thesis comparative evaluation. See [EXPERIMENT_PROTOCOL.md](../EXPERIMENT_PROTOCOL.md) for execution procedures.

## Files

| File | Scenario | Expanded Runs |
|------|----------|---------------|
| `S1-baseline.json` | Healthy 10-node baseline | 5 (one per protocol) |
| `S2-scale.json` | Node count sweep | 20 (4 × 5 protocols) |
| `S3-latency-stress.json` | Network latency sweep | 20 (4 × 5) |
| `S4-byzantine-matrix.json` | Byzantine fraction sweep | 20 (4 × 5) |
| `S5-partition-fault.json` | Network partition injection | 5 |
| `S6-load.json` | Transaction load sweep | 15 (3 × 5) |
| `S7-federated-learning.json` | FL payload (AIDSE) | 3 (PoS, DPoS, PBFT) |

**Total unique configurations:** 88  
**With 10–30 repetitions:** 500–700+ executed simulations (see protocol doc).

## Schema

```json
{
  "schemaVersion": "1.0",
  "experimentSuite": {
    "id": "S1-baseline",
    "name": "...",
    "repetitions": 30,
    "randomSeed": 42,
    "warmupRounds": 5,
    "runs": [ { "runId", "protocol", "simulation", ... } ]
  }
}
```

### Matrix expansion (S2–S4, S6)

Files with `expansion` block define a Cartesian product:

```
runs = protocols × sweep.values
runId = {suiteId}-{protocolShort}-{paramName}{value}
```

Expand locally:

```bash
# Planned — scripts/expand-experiment-suite.sh docs/experiments/S2-scale.json
```

Until the script exists, use the sample `runs` array at the bottom of each matrix file or expand manually.

## API Usage

Single run payload (from any `runs[]` entry):

```bash
curl -X POST http://localhost:5027/api/Simulation/start \
  -H "Content-Type: application/json" \
  -H "Cookie: <auth-cookie>" \
  -d '{
    "name": "S1-PoW-baseline",
    "algorithm": 0,
    "nodeCount": 10,
    "byzantineNodeCount": 0,
    "durationSeconds": 600,
    "networkTopology": 0,
    "blockTimeMs": 5000,
    "transactionsPerBlock": 10,
    "networkLatencyMs": 50,
    "algorithmConfiguration": { "seed": 42 },
    "enableDetailedLogging": true,
    "autoStart": true
  }'
```

**Enum reference:**

| Field | Values |
|-------|--------|
| `algorithm` | 0=PoW, 1=PoS, 2=DPoS, 3=PBFT, 10=PoET |
| `networkTopology` | 0=FullMesh, 1=Ring, 2=Star, … |

## Results

Store exports under:

```
docs/experiments/results/<scenario-id>/
```

Do not commit large result JSON to git — add `results/` to `.gitignore` if needed.
