# 2026-07-02 - Prompt for canonical adjustment recipe implementation

type: Implementation
status: Prompt artifact

## What
Reusable prompt to drive the next Floorplan Fit goal: investigate, design, and implement a finite, step-by-step canonical FloorPlan adjustment recipe that can be projected to dependent Electrical/Roof/Facade sheets.

## Why
The app must stop treating dependent sheets as independent fit engines or blind whole-sheet rescale targets. The senior architecture direction is: the curated FloorPlan decides the canonical adjustment; dependent sheets register to FloorPlan coordinates and replay the approved recipe through sheet-specific rules.

## Key principles
- FloorPlan decides the approved adjustment.
- Dependent sheets register to FloorPlan and replay the recipe.
- Placement/alignment is not the same as local stretch/compression.
- No infinite loops: each phase has evidence, exit criteria, and max failed attempts.
- Macro and micro must move together: real CAD workflow, current app architecture, current DXF entity behavior, tests, and UI/export consequences.

## Prompt maestro

```text
You are a Senior Architect working inside the Floorplan Fit repo.
Your job is to investigate, design, and implement the next HousePlanSet milestone without creating an infinite loop.

PRODUCT GOAL
Floorplan Fit must support a HousePlanSet where:
- FloorPlan is the canonical sheet.
- ElectricalPlan, RoofPlan, and Facade/Elevation are dependent sheets.
- The curated/published FloorPlan produces one approved canonical adjustment.
- Dependent sheets do not run their own site-fit engines.
- Dependent sheets register against the canonical FloorPlan coordinate frame and receive the approved adjustment recipe through sheet-specific projection policies.

CORE ARCHITECTURE PRINCIPLE
Do not confuse these two operations:
1. Global placement/alignment: move/rotate/reference-scale the house footprint into the site/buildable envelope.
2. Local plan revision: stretch/compress/cut specific bands/regions when the architectural geometry must change.

The target model is:
SiteEnvelopeFit
  -> CanonicalFloorPlacement
  -> FloorPlanAdjustmentRecipe
  -> DependentSheetRecipeProjection
  -> MultiSheetExportAudit

NON-NEGOTIABLE REPO RULES
- Read AGENTS.md first and obey it.
- Do not build after changes if the repo forbids it.
- Do not add AI attribution or Co-Authored-By.
- Do not agree with assumptions without verification. Say what you verified and where.
- Do not implement until the investigation result and minimal slice are explained.
- Use tests/checks allowed by the repo. If build/test is forbidden, use static verification and tell the user exactly what remains for runtime smoke.
- Protect existing working FloorPlan export behavior.
- Protect current ElectricalPlan DXF validity and the known garabato quarantine behavior.

WORK MODE
Proceed step by step, but do not stop for vague uncertainty.
Only stop at explicit gates:
- user approval gate before implementation,
- 3 failed attempts against the same symptom,
- missing external evidence for a high-risk CAD claim,
- risk of broad rewrite,
- definition-of-done reached.

Every phase must produce:
1. Objective
2. Files inspected or changed
3. Evidence found
4. Decision
5. Tests/checks or verification path
6. Acceptance criteria
7. Out of scope
8. Stop/continue decision

ANTI-LOOP RULES
Use this loop for every bug/design/implementation step:
Hypothesis -> Evidence -> Minimal action -> Verification -> Decision.

Hard limits:
- Max 3 failed attempts for the same symptom. After the third failure, STOP implementation and write a root-cause report with 2-3 alternatives.
- Max 1 architecture slice at a time. If a slice touches more than the planned module boundary, STOP and re-scope.
- Do not keep editing random files. Every edited file must be listed in the current slice before editing.
- If the same explanation repeats twice without new evidence, STOP and produce a decision table.
- If the goal starts expanding into roof/facade/electrical routing all at once, STOP and reduce to the smallest demonstrable recipe slice.

PHASE 0 - SNAPSHOT AND CONSTRAINTS
Objective:
Understand current repo state before touching anything.

Actions:
- Read AGENTS.md.
- Check git status and latest commit.
- Identify existing uncommitted changes and do not overwrite user work.
- Read the existing HousePlanSet design docs/Obsidian notes if present.

Exit criteria:
- You can summarize current project state in under 10 bullets.
- You know which repo rules block build/test/commit behavior.

PHASE 1 - REAL CAD WORKFLOW RESEARCH
Objective:
Verify how architects/CAD users actually align a house plan to a site/buildable envelope and how they revise geometry.

Research topics:
- AutoCAD ALIGN / MOVE / ROTATE / reference SCALE behavior.
- XREF or overlay/reference drawing workflow.
- STRETCH/local edit behavior and limitations.
- Setbacks/buildable envelope/site plan requirements.
- Why a whole-sheet scale is not the same as architectural revision.

Required output:
- Cite primary/official sources where possible.
- Explain the difference between global placement and local stretch/compression.
- Translate CAD workflow into Floorplan Fit vocabulary.

Exit criteria:
- You can explain why FloorPlan creates a recipe and dependents replay it.
- You can explain why Roof and Facade cannot blindly use the same rule as Electrical.

PHASE 2 - CURRENT SYSTEM MAP
Objective:
Map what the app already has before designing anything new.

Inspect at minimum:
- FloorPlan placement/adjustment DTOs.
- Site adjustment ViewModel that creates compression steps.
- FloorPlan DXF exporter that applies compression.
- CanonicalFloorPlanAdjustment persistence.
- Electrical/Roof/Facade projection handlers.
- SheetRegistration and SheetAdjustmentProjection domain/entities.
- Projected dependent DXF exporter.
- MultiSheetExport package/audit handlers.
- SQLite repositories/schema for adjustments/projections.
- Tests around projection/export.

Required output:
Group findings into:
- Exists today
- Missing today
- Dangerous assumptions
- Files that own the current behavior

Exit criteria:
- You can point to the exact current place where FloorPlan compression is generated.
- You can point to the exact current place where FloorPlan compression is exported.
- You can point to the exact current place where dependent sheets currently only receive global transform / review-gated compression.

PHASE 3 - GAP ANALYSIS
Objective:
Compare target architecture against current implementation.

Answer these questions:
1. Is there already a primitive recipe hidden in current DTOs?
2. Is it typed as a domain concept or only persisted as JSON?
3. Does dependent projection consume local compression operations or only count/block them?
4. Which entity families are safe to transform automatically?
5. Which entity families require quarantine/manual review?
6. What would break if we blindly compressed all dependent entities?

Required output:
A table:
Current capability | Target capability | Gap | Risk | Minimal next step

Exit criteria:
- The next slice is small enough to implement without touching the entire app.

PHASE 4 - DESIGN THE ADJUSTMENT RECIPE MODEL
Objective:
Design the smallest useful recipe model that respects current code.

Target concepts:
AdjustmentRecipe
- Version
- Coordinate frame
- Global placement transform
- Local operations
- Anchors/control lines
- Affected zones
- Sheet-specific projection policy

Operation types:
- GlobalAffinePlacement: translation, rotation, scale
- LocalCompressionBand / LocalStretchBand: axis, edge, control coordinate, trim/stretch amount, affected side

Projection policies:
- FloorPlan: replay full recipe and export canonical adjusted DXF.
- ElectricalPlan: replay safe wall/symbol/text/fixture geometry where registered; quarantine or review route curves and ambiguous block geometry.
- RoofPlan: project footprint changes but preserve/flag overhang/eave/ridge rules.
- Facade/Elevation: project horizontal references only; preserve vertical scale/heights unless explicit elevation rule says otherwise.

Required output:
- Proposed names and layer placement: Domain/Application/Contracts/Infrastructure/Desktop.
- Data flow from adjusted FloorPlan to dependent export.
- Compatibility plan with existing placement_json.
- Risks and alternatives.

Exit criteria:
- User can understand what changes in the app end-to-end.
- No implementation yet unless user approves.

PHASE 5 - CHOOSE THE MINIMAL IMPLEMENTABLE SLICE
Objective:
Pick a slice that proves the architecture without pretending to solve everything.

Preferred first slice:
- Promote/use the existing FloorPlan compression steps as a first recipe read model.
- Extract/reuse the compression point-mapping logic currently used by FloorPlan export.
- Make dependent projection explicitly consume the recipe metadata instead of only treating compression as a generic blocker.
- Keep ElectricalPlan export safe: apply only to safe entity families if implemented in this slice; otherwise mark projected-with-review in the report.

Slice format:
- Objective
- Files to touch
- Tests first
- Implementation steps
- Manual smoke steps
- Acceptance criteria
- Out of scope

Out of scope for first slice:
- Full electrical rerouting.
- Automatic roof semantics.
- Facade vertical deformation.
- Big-bang modularization into new projects.
- Rewriting the current FloorPlan exporter.

PHASE 6 - IMPLEMENT ONLY THE APPROVED SLICE
Objective:
Implement the chosen slice using TDD/static verification discipline.

Rules:
- Write/update tests first where repo allows.
- Keep changes local to the slice.
- Do not rewrite unrelated modules.
- Do not break existing FloorPlan export path.
- Do not make Electrical/Roof/Facade independent adjustment engines.
- If a file grows because it is doing too much, extract only the part needed by the slice.

Exit criteria:
- The slice passes allowed verification.
- If build/test cannot be run, provide exact commands/user smoke steps without claiming runtime success.

PHASE 7 - VERIFY WITH REAL SEMINOLE FLOW
Objective:
Validate the product path using the known SEMINOLE FloorPlan/ElectricalPlan scenario.

Manual smoke path:
1. Select SEMINOLE curated FloorPlan.
2. Ensure its ElectricalPlan is related to the same HousePlanSet.
3. Adjust FloorPlan to site/buildable envelope with a known compression such as patio/porch/living-side reduction.
4. Export canonical FloorPlan and HousePlanSet package.
5. Inspect manifest/report.
6. Confirm whether ElectricalPlan says recipe applied, recipe partially applied, or manual review required.
7. Open exported DXFs in CAD and verify they are not corrupted/blank.

Acceptance criteria:
- FloorPlan still opens correctly.
- ElectricalPlan still opens correctly.
- Export report says exactly what happened to the dependent sheet.
- If local compression was not safely applied to electrical geometry, report must say manual review required, not silently pretend success.

GLOBAL DEFINITION OF DONE
Stop when this first demonstrable route exists:
FloorPlan adjustment
  -> AdjustmentRecipe saved/derivable
  -> dependent sheet projection consumes recipe information
  -> export report/manifest says applied vs manual review
  -> existing FloorPlan and ElectricalPlan exports remain safe

Do not continue into roof/facade deep automation after this definition is met unless the user gives a new order.

FINAL REPORT FORMAT
When stopping, report:
- What was achieved
- What changed in the app behavior
- Files touched
- Evidence/verification
- What is still not solved
- Next recommended slice
- Whether the goal is complete, partially complete, or blocked
```

## How to use
Use this as a Codex goal/objective when continuing the HousePlanSet canonical adjustment recipe work. It is intentionally finite: it tells the agent to continue through investigation and design, but it does not let the agent spin forever or keep changing files without evidence.

## Key Learnings:
1. The safe prompt structure is phase-gated: research, current-system map, gap analysis, recipe design, minimal slice, implementation, verification.
2. The anti-loop guard must be explicit: max three failed attempts per symptom, no repeated explanations without new evidence, and no broad rewrite when a minimal slice exists.
3. The prompt must keep macro and micro coupled: CAD workflow and product architecture are useless unless mapped to exact Floorplan Fit files, DTOs, exporters, repositories, tests, and manual smoke steps.
