---
type: Bugs
date: 2026-07-09
replaces: []
replaced_by: null
---

# ExportProjectedPlanSheetHandler exportAudit scope compile fix

## Problem
`dotnet watch` failed with CS0103:

`The name 'exportAudit' does not exist in the current context`

## Root cause
`exportAudit` was declared with `var` inside the `try` block in `ExportProjectedPlanSheetHandler.HandleAsync`, then used after the `try/catch` when constructing `ExportProjectedPlanSheetResponse`. In C#, a local variable declared inside a block is not visible outside that block.

## Fix
Declare `ProjectedPlanSheetExportAuditDto? exportAudit = null;` before the `try`, then assign it inside the `try`.

## Verification
No build was run because AGENTS.md forbids builds after changes. Static verification confirms there is no `var exportAudit` declaration scoped inside the try block and the response uses the outer variable.
