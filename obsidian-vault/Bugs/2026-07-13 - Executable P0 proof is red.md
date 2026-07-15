# Executable P0 proof is red

## Links
- `replaces`: the pending external-proof statement in [[Current State#2026-07-13 - P0 HousePlanSet hardening reached static closure]].
- `replaced_by`: pending; this note remains active until a fresh full `dotnet test .\FloorplanFit.sln` passes.

## What was verified
The user ran `dotnet test .\FloorplanFit.sln`. All five production projects compiled, but the solution proof failed:

- `FloorplanFit.Desktop.Tests`: 2 compile errors around missing `PlanSetSheetDto` resolution and an incompatible `IPlanSheetReader` fake signature.
- `FloorplanFit.Application.Tests`: 2 compile errors because `ResolvePlanSetVersionRequest` is not resolved.
- `FloorplanFit.Infrastructure.Tests`: 164 tests executed; 153 passed and 11 failed.

## Failure groups

1. Workspace backup cleanup: locked `app.db` while deleting staging.
2. Sheet registration fixtures: two tests violate the new canonical PlanSet identity trigger.
3. Pinch-marker migration fixtures: three legacy-schema tests reach repositories without the current columns.
4. DXF regressions: stale exception expectations, one dimension-block crossing expectation, binary DXF parsing and header-extents preservation.

## Current truth

- The seven P0 static contracts, verifier self-check and `git diff --check` are still green.
- Executable/runtime proof is NOT green and the P0 work must not be described as fully runtime-verified.
- The supplied run is the RED evidence. Fixes must distinguish obsolete test fixtures from production defects and must not weaken data-loss or DXF-safety guards merely to make tests pass.

## Evidence

- `Build failed with 15 error(s) and 2 warning(s)`.
- Infrastructure summary: `total: 164, failed: 11, succeeded: 153, skipped: 0`.
- Raw evidence: `C:\Users\lucas\.codex\attachments\0a35c6bf-e8d7-4de7-b217-7e80d361b149\pasted-text.txt`.

## Static repair status

All reported groups now have minimal repairs in the current worktree:

- Both test projects import the existing `FloorplanFit.Contracts.PlanSets` namespace; production contracts were not moved.
- The transient backup destination disables SQLite pooling locally.
- Registration fixtures seed valid canonical ownership; exactly three fabricated legacy databases reset `user_version`.
- Binary DXF group-code framing is preserved, booleans remain one-byte payloads, truncated streams fail closed, and dimension-block curves retain typed manual review.
- Stale DXF exception expectations and the HEADER test helper were corrected without weakening production guards.

Final current-state static evidence is green: all eight allowed scripts and `git diff --check` exit `0`. This note remains active until the user reruns the full solution test and supplies a green result or new RED evidence.
