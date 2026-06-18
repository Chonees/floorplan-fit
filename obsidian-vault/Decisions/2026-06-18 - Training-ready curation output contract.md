# 2026-06-18 - Training-ready curation output contract

## Type
Decision / Product direction

## Context
The user expects at least ~50 curated floor plans and wants to ensure continued curation is structured for future AI/model training, especially for automatic dimension-node binding.

## Verified current state
- The SQLite app data already stores useful atomic supervision signals:
  - `measurement_corridors`
  - `measurement_nodes`
  - `floorplan_dimension_interval_bindings`
  - `extracted_dimensions`
  - `extracted_dimension_primitives`
  - template/version/extraction metadata
- Local `workspace/app.db` currently has substantial rows, but most examples are repeated curation versions rather than many independent plan styles.
- No dedicated training/dataset export contract was found in source/scripts/tests for normalized JSONL/manifest style output.

## Decision
Future curation should be treated as training-data collection. Before heavy curation, define/export a stable dataset contract that captures:
1. plan identity and version/fingerprint,
2. source DXF/entity references and geometry primitives,
3. measurement context/units,
4. curated corridors/nodes/bindings,
5. negative/unbound examples,
6. curator/published status/provenance,
7. validation metrics and confidence/quality flags.

## Preferred path
Do not start with model training. First build a deterministic **training export / audit** layer so the next 50 curated plans produce clean examples. Then use that dataset for auto-suggestion and later training.

## Why
If the app only stores enough to run the product, future AI training can be noisy or ambiguous. A training-ready output contract preserves intent and makes future model work possible without re-curating.

## Implemented V1
- `scripts/export_training_dataset.py` now exports floor-plan dimension binding supervision as JSONL plus manifest/audit.
- `scripts/check_training_dataset_export.py` verifies the contract without building .NET.
- Generated datasets are local artifacts under `artifacts/training-datasets/` and are ignored by git.
