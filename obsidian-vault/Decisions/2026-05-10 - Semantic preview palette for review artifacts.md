---
type: decision
date: 2026-05-10
project: floorplan-fit
tags:
  - desktop
  - review
  - preview
  - palette
  - loop-1
---
# Semantic preview palette for review artifacts
## Decision
Loop 1 Review now uses one unified selection color for any selected artifact and no longer falls back to black for selected states.
The active preview palette is now:
- **Windows**: cyan family
- **Doors**: base color aligned to the current seed original plan (`SEMINOLE2000.dxf` layer `DOORS`, ACI 157 = `#455668`), with a lighter highlight in the same blue-gray family (`#6B829B`)
- **Fixed elements**: red family by default, except **cabinets**, which use the same cyan family as windows
- **Selected artifacts**: exact same green as the selected pinch marker (`SeaGreen` / `#2E8B57`) for walls, openings, fixed elements, protected details, room labels, and opening labels
- **Wall lines**: slate gray by default, unified selection green when selected
- **Pinch markers**: sea green for active group, dodger blue for active axis, slate gray for inactive
## Why
The previous all-black preview language produced weak visual hierarchy and made selection states harder to read. Review is a CAD-faithful curation surface, but it still needs semantic contrast so an admin can distinguish windows, doors, fixtures and selection state at a glance.
## Consequences
- Semantic palette now overrides DXF color for fixed elements so toilets, tubs and blocks stay visually consistent in Review.
- Opening geometry always uses semantic colors instead of neutral black.
- Selection highlights must stay expressive and can no longer fall back to black.
## Related
- [[Decisions/2026-05-10 - Unified plan elements review UI and exclude action]]
- [[Implementation/2026-05-10 - Unified plan elements review UI and room label exclusion]]




