---
type: inbox
date: 2026-07-16
status: implemented-static-runtime-pending
topic_key: ux/editable-pinch-capacity-architectural-units
related:
  - "[[2026-07-16 - Architectural feet-inch input convention]]"
  - "[[2026-06-08 - Auto fit suggestion engine with named pinch groups request]]"
  - "[[../Implementation/2026-07-16 - AutoCAD architectural lengths and editable pinch capacity]]"
---

# Professional architectural units and editable pinch capacity

## User intent

In Loop 1 Edit, an already-placed pinch must expose an Edit action for its maximum reducible length. The value should be entered and displayed in professional US architectural notation rather than decimal feet.

## Verified current truth

- Creation UI binds a decimal-inch text field to `NewPinchMaxTrimInches`.
- Desktop converts inches to millimeters and rounds to three decimal millimeters before persistence.
- Domain, Contracts, SQLite, and band projection currently use `MaxTrimMm`.
- Existing markers can be selected from a list and removed, but their capacity cannot be updated.
- `PinchMarker.MaxTrimMm` is immutable and `IPinchMarkerRepository` has no update operation.
- Published curation is edited by cloning it to a draft; a capacity mutation must be draft-only and must verify marker ownership in Application, not only disable the Desktop button.
- Band capacity is currently the sum of marker capacities, so changing one marker changes Loop 2 fit capacity.

## Recommended human boundary

- Accept architectural values such as `2"`, `6 1/2"`, or `1'-2"`.
- Display zero feet suppressed for small values, as AutoCAD does (`2"`, not `0'-2"`).
- Do not store feet and inches as separate domain fields or persist formatted strings.
- Preserve DXF source units and `$INSUNITS`; formatting is a UI concern.

## User-confirmed input compatibility

The system must accept both:

1. current decimal input for backward compatibility; and
2. AutoCAD-style architectural input such as `30'-6 1/2"` (the text equivalent of the visually stacked fraction).

Architectural fractions must retain exact precision through the full parse/save/read round trip. Match AutoCAD's architectural fractional denominators: `1, 2, 4, 8, 16, 32, 64, 128, 256`. A field's existing unit remains the fallback only when the user supplies an unsuffixed decimal: feet for buildable width/height, inches for pinch capacity. Explicit `'` and `"` always win.

## AutoCAD behavior verified before implementation

Architectural notation is a decomposition of one total length, not two independent counters:

- feet = whole groups of 12 inches;
- inches = the remainder from `0` through less than `12`;
- the fractional part advances according to the selected display precision.

At `1/16"` display precision, decreasing `30'-6 1/2"` proceeds as `30'-6 7/16"`, `30'-6 3/8"`, and so on. Crossing zero inches borrows one foot: `30'-0"` minus `1/16"` becomes `29'-11 15/16"`.

AutoCAD separates three concerns that FloorplanFit must also keep separate:

1. the actual geometry/numeric value;
2. architectural display precision (smallest displayed fraction, up to `1/256"`);
3. optional dimension rounding (`DIMRND`), which changes displayed dimension values but must not mutate FloorplanFit's stored exact length.

Stacked fractions are presentation. The textual input equivalent of the screenshot is `30'-6 1/2"`; AutoCAD's `DIMFRAC` controls horizontal, diagonal, or non-stacked rendering.

## Minimal safe internal choice

Keep the existing exact decimal millimeter storage initially, remove premature three-decimal rounding, and centralize architectural parse/format at the UI boundary. For example, `1/16"` converts exactly to `1.5875 mm` with `decimal`.

Migrating all persisted capacity data from `MaxTrimMm` to inches would align naming with the US product domain but is a broader schema/contracts migration and is not required merely to achieve professional architectural input/output.

## Product semantic that must be resolved before implementation planning

Determine whether capacity belongs to:

1. each marker and is additive inside a group; or
2. the physical articulation band/group, while multiple markers are synchronized geometry anchors.

The current sum is unsafe if two markers merely represent the two faces of one wall, because the same physical trim may be counted twice. The UI location of Edit depends on this decision: marker row for additive capacities, group/band header for one shared capacity.

## Expected edit behavior after that decision

- Select existing marker/group.
- Show current formatted capacity and an `Editar` action.
- Edit inline or in a small dialog with Save/Cancel.
- Save through a draft-only Application mutation; do not delete and recreate the marker.
- Refresh the review session, articulation-band capacity, preview, and Loop 2 readiness summary.
