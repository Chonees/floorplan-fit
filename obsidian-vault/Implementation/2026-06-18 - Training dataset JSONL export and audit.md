# 2026-06-18 - Training dataset JSONL export and audit

## Type
Implementation

## Product loop
Loop 1 Edit / Review curation data, training readiness.

## What changed
- Added `scripts/export_training_dataset.py`.
- Added `scripts/check_training_dataset_export.py` as a no-build smoke check.
- Added `.gitignore` rule for generated local dataset exports: `artifacts/training-datasets/`.
- Generated local dataset output at `artifacts/training-datasets/floorplan-bindings-v1/`.

## Export files
- `manifest.json` — schema/version/document-kind manifest.
- `audit.json` — counts, hard errors, warnings.
- `floorplan_dimension_bindings.v1.jsonl` — one training example per accepted binding or unbound dimension.

## Schema direction
- Current V1 `document_kind`: `floor_plan`.
- Future document kinds reserved in the manifest: `electrical_plan`, `facade`.
- This lets electrical plans and facades join later without breaking the floor-plan dataset contract.

## Latest generated audit
- Exported records: `4026`.
- Accepted binding records: `1685`.
- Unbound dimension records: `2341`.
- Curation status counts: `Draft=2`, `Published=11`.
- Hard errors: `0`.
- Warning: draft curations are exported with `is_training_approved=false`.

## Verification
- No .NET build was run per repo rule.
- RED check first failed because `scripts/export_training_dataset.py` did not exist.
- GREEN `python scripts\check_training_dataset_export.py` passed.
- GREEN `git diff --check` exited `0`.
