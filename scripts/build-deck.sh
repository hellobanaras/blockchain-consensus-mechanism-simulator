#!/bin/bash
# Build the M.Tech presentation deck from Markdown to both .pptx (editable) and .pdf (projector-safe).
# Requires Node.js + npx; uses the official Marp CLI without a global install.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DECK_MD="$REPO_ROOT/docs/presentation/thesis-presentation.md"
OUT_PPTX="$REPO_ROOT/docs/presentation/thesis-presentation.pptx"
OUT_PDF="$REPO_ROOT/docs/presentation/thesis-presentation.pdf"

if [[ ! -f "$DECK_MD" ]]; then
  echo "Slide source not found: $DECK_MD" >&2
  exit 1
fi

if ! command -v npx >/dev/null 2>&1; then
  echo "npx is required (install Node.js >= 18)." >&2
  exit 1
fi

echo "[deck] Building PPTX → $OUT_PPTX"
npx --yes @marp-team/marp-cli@latest "$DECK_MD" --pptx --allow-local-files -o "$OUT_PPTX"

echo "[deck] Building PDF  → $OUT_PDF"
npx --yes @marp-team/marp-cli@latest "$DECK_MD" --pdf  --allow-local-files -o "$OUT_PDF"

echo "[deck] Done. Open the PPTX in PowerPoint/Keynote to make any final tweaks before the viva."
