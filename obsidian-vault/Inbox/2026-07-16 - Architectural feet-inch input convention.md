---
type: inbox
date: 2026-07-16
status: researched
---

# Architectural feet-inch input convention

## Finding

Architectural drawings should present imperial building dimensions as feet and inches, using `'` for feet and `"` for inches, normally with fractional inches when needed.

Examples:

- `39.0 ft` -> `39'-0"`
- `77.5 ft` -> `77'-6"`
- `38.916666... ft` -> `38'-11"`

Values such as `38.9167 ft` are decimal-foot transport/input values, not the preferred architectural drawing notation.

## Current application behavior

`AdjustSitePlanSetupDialog` labels both fields in feet and accepts only decimal numbers through `decimal.TryParse`. Consequently, exact whole-inch changes such as one inch (`1/12 ft`) currently require rounded decimal-foot input.

Four decimal places are numerically adequate for the current tests (`38.9167 ft` differs from `38'-11"` by only `0.0004 in`), but this is a UI workaround rather than the correct human-facing convention.

## Recommended boundary

- UI input/display: architectural notation such as `38'-11"`, or separate feet/inches fields.
- Domain/calculation: exact total inches (decimal only when fractional inches are genuinely required).
- DXF: continue using the drawing's established unit convention; do not infer display notation from storage units.

## Sources

- Autodesk, Drawing Units: Architectural format produces feet-and-inches and assumes one drawing unit represents one inch: https://help.autodesk.com/cloudhelp/2025/ENU/AutoCAD-Core/files/GUID-75419F91-18B8-47EE-9272-3196ACC95977.htm
- Autodesk, Format Feet and Inches: accepted syntax includes `5'-9"` and fractional-inch variants: https://help.autodesk.com/view/ACD/2026/ENU/?caas=caas%2Fdocumentation%2FACDLT%2F2014%2FENU%2Ffiles%2FGUID-12747FD4-484B-4976-81BB-646B04E19C1E-htm.html
- Autodesk, `-UNITS`: Architectural example is `1'-3 1/2"`; Engineering example is `1'-3.50"`: https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-MAC-Core/files/GUID-D396FBFE-6171-4A89-9E68-6CB082EBE0E1.htm
- USACE CAD standard supplement: architectural master units are feet and sub-units are inches: https://www.mvd.usace.army.mil/portals/52/docs/mvd-cad-std-40.pdf

