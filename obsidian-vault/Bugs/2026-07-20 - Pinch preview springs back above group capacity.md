---
type: bug
status: fixed_static_pending_runtime
date: 2026-07-20
project: FloorplanFit
area: Loop 1 interactive pinch preview
---

# Pinch preview springs back above group capacity

## Verified cause

`CadStretchRecipeCompiler.CompileGroup(...)` correctly rejects a requested trim above the safe combined group capacity. The Desktop preview passed the unrestricted mouse distance directly to that fail-closed compiler and rendered the original geometry when compilation failed.

For low-capacity groups, dragging slightly beyond the limit therefore changed the preview from “partially reduced” straight back to “original”, creating a spring-loaded-door effect.

## Fix

- The Desktop preview derives the active group's paired-wall capacity with the same ordered marker-pair rule as the compiler.
- Mouse-requested trim is clamped to that safe capacity before compilation.
- The Application compiler remains strict and still rejects over-capacity recipes outside this transient UI preview.
- A source-level regression requests `20` units from a `4`-unit group and proves the preview remains at the safe `4`-unit reduction instead of returning to the original geometry.
- Static diff validation passes; executable Desktop proof remains pending.

