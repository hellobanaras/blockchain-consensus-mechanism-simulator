---
description: Rebuild the thesis presentation deck (.pptx + .pdf) from Marp Markdown.
---

Run `./scripts/build-deck.sh` and report the output paths and file sizes. The script invokes `npx @marp-team/marp-cli@latest` twice (once for PPTX, once for PDF) against `docs/presentation/thesis-presentation.md`.

After build, confirm with:

```bash
ls -lh docs/presentation/thesis-presentation.{md,pptx,pdf}
```

If the user has captured fresh demo screenshots, they should drop them in `docs/presentation/assets/` and the deck will pick them up automatically — no Markdown edit needed unless the filenames change.

If `npx` is missing, instruct the user to install Node.js ≥ 18 (`brew install node` on macOS).
