---
name: collaboration-prefs
description: How Umesh wants Claude to operate — autonomy level, communication style, pacing.
metadata:
  type: feedback
---

**Push through multi-day plans without waiting for approval between phases.** When given a long plan (e.g. the May-23 MVP), execute Day 1 → Day N end-to-end and report when ready to launch and test. Don't pause to check in unless something is genuinely blocked.
- **Why:** Umesh is highly time-constrained (exam preparation overlapping the thesis crunch). Mid-plan check-ins waste his cycles without changing the outcome.
- **How to apply:** Validate with `dotnet build` between days; only stop if the build breaks or a step requires user-specific input (credentials, port choices, advisor preferences).

**Commit and push when authorised; never on initiative.** "Commit and push" in a user message is an explicit authorisation for that round of work only — not a standing instruction. Future sessions still need explicit asks for git push.
- **Why:** Standard care for an action with shared-state side effects.
- **How to apply:** Stage scoped changes by named files (avoid `git add -A`), use a HEREDOC for the commit message, and never `--no-verify` unless explicitly asked.

**Default to terse responses with file:line citations.** No headers/sections for simple questions, no trailing "let me know if…" preambles. End-of-turn summary: one or two sentences max.
- **Why:** He reads the diff and code himself; the AI summary is a navigation aid, not the deliverable.
- **How to apply:** Match response length to question. A status update after each tool call when working on a long task is fine; a wall of prose is not.
