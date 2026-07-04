# Loop 2 Selectable Auto-Fit Options Design

## Goal
Adjust to Site Plan must generate multiple valid fit plans, let the user choose one, and apply only the selected pinch-group reductions.

## Architecture
Application owns deterministic option generation and validation. OpenAI is demoted from geometry authority to ranking/explanation: it may reorder or explain already-valid options, but cannot invent groups or reductions. Desktop renders the validated options as selectable cards and applies the selected plan to the preview geometry.

## Components
- `AutoFitSuggestionOptionGenerator`: enumerates exact-fit plans from `AutoFitSuggestionFacts`.
- `IAutoFitPlanSuggester`: receives deterministic candidates and returns ranked/explained candidates.
- `SitePlanAdjustmentViewModel`: populates option cards, handles Apply, and keeps OpenAI fallback non-blocking.
- `SitePlanAdjustmentWindow.axaml`: renders option cards with Apply buttons.

## Rules
- Every option must pass `AutoFitSuggestionPlanValidator`.
- Per axis, total reduction must equal the measured deficit exactly.
- A step cannot exceed its group capacity.
- OpenAI failure must not prevent deterministic options from being shown.
- Apply is preview-only for this slice and must only modify selected plan groups.

## Testing
- Application tests for single-group and split-option enumeration.
- Desktop tests for option population and selected-option apply behavior.
- Infrastructure tests verify OpenAI request includes candidate options and validates/ranks returned options.
