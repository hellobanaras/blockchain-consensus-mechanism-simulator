---
description: Resume work from the .claude/PROGRESS.md cursor. Use after any interruption.
---

You are resuming this repo after an interruption (new session, crash, or pause). Do this in order:

1. **Read** `CLAUDE.md` (you may already have it in context — if so, skim the section list).
2. **Read** `.claude/PROGRESS.md` from top to bottom. The "Cursor: NEXT STEP" section tells you the immediate next action; the "Phase 4" backlog at the bottom is the post-MVP queue.
3. **Verify the working tree:**
   ```bash
   git status
   git log --oneline -5
   dotnet build BlockchainConsensusSimulator.sln -c Debug -nologo 2>&1 | tail -5
   ```
   - Uncommitted changes? Read them with `git diff` and decide if they should be staged before continuing.
   - Build broken? Fix that *first* — don't pile new work onto a red build.
4. **Propose the next concrete step** to the user in 2–3 sentences, citing file paths and the exact item from `PROGRESS.md`. Wait for confirmation before making changes unless the user has already authorised a long-running task.

If `PROGRESS.md` is out of date relative to the actual repo state (e.g. it says "Day 1 ✅" but `SimulationService.cs` doesn't have the persistence methods), **trust the code, not the log**, and update `PROGRESS.md` before continuing.

End by reporting:
- Where the previous session stopped (one sentence)
- What you're about to do (one sentence)
- Anything that needs the user's input first (one sentence, or "nothing")
