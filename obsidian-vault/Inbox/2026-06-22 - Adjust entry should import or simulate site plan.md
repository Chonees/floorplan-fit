# 2026-06-22 - Adjust entry should import or simulate site plan

## Type
Feature direction / UX

## User intent
When the user clicks **Adjust to Site Plan**, the app should not immediately force a DXF picker. It should enter an Adjust setup flow with two choices:

1. **Importar site plan** — existing behavior: pick a DXF and read its buildable area.
2. **Simular site plan** — create a synthetic site plan directly inside the Adjust flow.

## Product constraints
- Simulated site plans must use the same layer vocabulary as the current synth generator:
  - `SETBACKS`
  - `2312-001-BM$0$C-PROP-SUBD`
  - `E`
  - `TEXT`
  - `0`
- The simulation should be useful for any floor plan, not only SEMINOLE2000.
- The simulation must make units explicit so scale bugs are visible, not hidden.

## Proposed minimal direction
- Keep import as the existing picker path.
- Add a setup dialog/screen before Adjust preview.
- For simulation, start with a rectangular site plan/buildable area using user-entered width/height in feet, converted consistently to the synthetic source unit.
- Prefer generating a real temporary DXF or a reusable generator service so the same code path/layers are used by reader/export/testing.


## Implemented
- Implemented by `Implementation/2026-06-22 - Adjust site plan import or simulate setup.md`.
- Current behavior: Adjust opens a setup dialog with import or simulation; simulation writes a temp synth-compatible inch DXF and reuses the normal site-plan reader.
