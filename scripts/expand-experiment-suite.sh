#!/usr/bin/env bash
# Expand experiment suite JSON into flat API-ready payloads.
# Usage: ./scripts/expand-experiment-suite.sh docs/experiments/S2-scale.json [--output dir]
set -euo pipefail

SUITE_FILE="${1:?Usage: $0 <suite.json> [--output dir]}"
OUTPUT_DIR=""

shift || true
while [[ $# -gt 0 ]]; do
  case "$1" in
    --output) OUTPUT_DIR="$2"; shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

if ! command -v jq &>/dev/null; then
  echo "Error: jq is required. Install with: brew install jq"
  exit 1
fi

SUITE_ID=$(jq -r '.experimentSuite.id' "$SUITE_FILE")
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

if [[ -z "$OUTPUT_DIR" ]]; then
  OUTPUT_DIR="$REPO_ROOT/docs/experiments/payloads/$SUITE_ID"
fi

mkdir -p "$OUTPUT_DIR"

# jq filter: convert topology string to enum int, add algorithm enum
JQ_PAYLOAD='
  def algo_enum:
    {"ProofOfWork":0,"ProofOfStake":1,"DelegatedProofOfStake":2,
     "PracticalByzantineFaultTolerance":3,"ProofOfElapsedTime":10};
  def topo_enum:
    {"FullMesh":0,"Ring":1,"Star":2,"Tree":3,"Random":4,
     "SmallWorld":5,"ScaleFree":6,"Grid":7,"Custom":8};
  def to_api($name; $protocol; $sim):
    $sim
    | .name = $name
    | .algorithm = algo_enum[$protocol]
    | if (.networkTopology | type) == "string"
      then .networkTopology = topo_enum[.networkTopology]
      else . end;
'

# Write explicit runs
count=0
while IFS= read -r run; do
  run_id=$(echo "$run" | jq -r '.runId')
  out_file="$OUTPUT_DIR/${run_id}.api.json"
  echo "$run" | jq --argjson f "$JQ_PAYLOAD" '
    ($f | . as $defs | .) | . # no-op placeholder
  ' >/dev/null 2>&1 || true

  echo "$run" | jq \
    --arg name "$(echo "$run" | jq -r '.name')" \
    --arg protocol "$(echo "$run" | jq -r '.protocol')" \
    '
    def algo_enum:
      {"ProofOfWork":0,"ProofOfStake":1,"DelegatedProofOfStake":2,
       "PracticalByzantineFaultTolerance":3,"ProofOfElapsedTime":10};
    def topo_enum:
      {"FullMesh":0,"Ring":1,"Star":2,"Tree":3,"Random":4,
       "SmallWorld":5,"ScaleFree":6,"Grid":7,"Custom":8};
    .simulation
    | .name = $name
    | .algorithm = algo_enum[$protocol]
    | if (.networkTopology | type) == "string"
      then .networkTopology = topo_enum[.networkTopology]
      else . end
    ' > "$out_file"
  echo "Wrote $out_file"
  count=$((count + 1))
done < <(jq -c '.experimentSuite.runs[]?' "$SUITE_FILE")

# Expand matrix runs (skip if output already exists)
if jq -e '.experimentSuite.expansion' "$SUITE_FILE" >/dev/null 2>&1; then
  jq -c '
    .experimentSuite as $s
    | $s.expansion as $e
    | $e.protocols[] as $protocol
    | $e.sweep.values[] as $val
    | ($e.sweep.parameter) as $param
    | ($e.protocolShortNames[$protocol] // $protocol) as $short
    | ($s.baseSimulation + {($param): $val}) as $sim
    | ($e.runIdPattern
        | gsub("\\{protocolShort\\}"; $short)
        | gsub("\\{" + $param + "\\}"; ($val|tostring))) as $runId
    | ($e.namePattern
        | gsub("\\{protocolShort\\}"; $short)
        | gsub("\\{" + $param + "\\}"; ($val|tostring))) as $runName
    | {runId: $runId, name: $runName, protocol: $protocol, simulation: $sim}
  ' "$SUITE_FILE" | while IFS= read -r run; do
    run_id=$(echo "$run" | jq -r '.runId')
    out_file="$OUTPUT_DIR/${run_id}.api.json"
    if [[ -f "$out_file" ]]; then
      continue
    fi
    echo "$run" | jq \
      --arg name "$(echo "$run" | jq -r '.name')" \
      --arg protocol "$(echo "$run" | jq -r '.protocol')" \
      '
      def algo_enum:
        {"ProofOfWork":0,"ProofOfStake":1,"DelegatedProofOfStake":2,
         "PracticalByzantineFaultTolerance":3,"ProofOfElapsedTime":10};
      def topo_enum:
        {"FullMesh":0,"Ring":1,"Star":2,"Tree":3,"Random":4,
         "SmallWorld":5,"ScaleFree":6,"Grid":7,"Custom":8};
      .simulation
      | .name = $name
      | .algorithm = algo_enum[$protocol]
      | if (.networkTopology | type) == "string"
        then .networkTopology = topo_enum[.networkTopology]
        else . end
      ' > "$out_file"
    echo "Expanded $out_file"
    count=$((count + 1))
  done
fi

echo ""
echo "Generated $count payload(s) in: $OUTPUT_DIR"
