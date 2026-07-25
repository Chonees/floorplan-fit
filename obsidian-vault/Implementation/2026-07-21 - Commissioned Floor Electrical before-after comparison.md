---
type: implementation
status: superseded
date: 2026-07-21
project: FloorplanFit
area: Loop 1 commissioned daily Auto-fit
implements: "[[2026-07-20 - Finite goal for automatic site fitting UX]]"
replaced_by: "[[Bugs/2026-07-21 - Commissioned comparison used global compression]]"
---

# Commissioned Floor Electrical before-after comparison

> [!warning] Superseded
> The after-comparison model below incorrectly converted commissioned CAD-stretch actions into coordinate-global legacy operations. Replaced by [[Bugs/2026-07-21 - Commissioned comparison used global compression]].

## What changed

- The commissioned Site Plan adjustment screen now reuses `RegistrationOverlayPreviewControl` for two structural overlays:
  - **Before:** resolved FloorPlan WALL geometry plus confirmed registered Electrical WALL geometry.
  - **After:** the same FloorPlan/Electrical structural geometry projected through the commissioned adjustment and site placement.
- The labels explicitly state that the preview shows structural `WALL` traces only. Devices and wiring are not invented or implied.
- `Confirmar y exportar` remains the sole commissioned primary action.

## Truth and safety boundary

- The preview resolves exactly one ElectricalPlan and requires a confirmed `WholeSheetSimilarity` registration.
- The persisted whole-plan proof must be authoritative and bound to the active canonical/dependent identities.
- Both resolved DXF SHA-256 values must still match that proof before WALL extraction is accepted.
- The existing registration transform and `ElectricalRecipeProjection.ProjectPoint` perform the visual registration/projection.
- Missing sheets, stale source bytes, invalid proof, missing WALL geometry, or unsafe projection produce an explicit unavailable reason and keep commissioned confirm/export disabled.

## Scope

- Desktop presentation and focused Desktop contracts only.
- Legacy noncommissioned adjustment remains on its existing preview and export path.
- No Roof/Facade support, CAD IDs, device/wire simulation, dependency, or fallback was added.

## Verification

- Focused static contracts were added before production changes.
- `git diff --check` passed for the touched slice.
- `SitePlanAdjustmentWindow.axaml` is well-formed XML.
- Static inspection finds exactly one `Confirmar y exportar`, two registration overlay controls, and no `EntityId` in the commissioned XAML.
- No `.NET` command was run; executable proof remains external by repository policy.
