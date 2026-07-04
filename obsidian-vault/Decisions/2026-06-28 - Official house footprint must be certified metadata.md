# Official house footprint must be certified metadata

Date: 2026-06-28
Type: Decision
Scope: Loop 1 curation + Loop 2 Adjust-to-Site-Plan

## Decision
The real-world house size shown to users must be an official/certified footprint stored with the floorplan/version, not a fresh raw DXF bounding box calculation shown as architectural truth.

## Why
The user needs to know: "SEMINOLE is X by Y in real life" and "SANTA-BARBARA is X by Y in real life" in feet/inches, exactly as an architect or CAD drafter would state it.

The engine can calculate and validate candidates, but the product needs a named source of truth:

- what was measured,
- from which DXF/version,
- whether patio/porch/garage is included,
- who/what approved it,
- and the exact width/height in inches.

## Product rule
Loop 1 should certify the floorplan footprint during curation/publish.

Loop 2 should consume that certified footprint:

- display `House footprint: 38'0" x 65'7"` style values,
- use exact inches internally,
- let buildable/site plan inputs use architectural feet/inches,
- compare exact house footprint vs exact buildable envelope.

## Source hierarchy
The official footprint should come from this order:

1. Architect/drafting-team declared overall dimensions for the exact plan/version.
2. Existing AutoCAD dimension entities/cotas, using their measured geometry value rather than text overrides.
3. Manual curation in the app: user measures/selects the exterior wall-to-wall width and height, confirms included features, and publishes that as certified metadata.

The engine may pre-fill a candidate from wall geometry, but a human/curation step must approve it before it becomes official.

## Current data availability
The local DB already stores native DXF dimensions in `extracted_dimensions`, including:

- `display_text`
- `raw_text_override`
- `measurement_source_units`
- `measurement_millimeters`
- definition points and render text position

So the system can use the same CAD dimension data as a candidate source for the official footprint. The missing product piece is selecting/validating which dimensions represent the overall house width/height.

## Not enough
Only calculating a structural footprint at runtime is not enough for user-facing construction truth. It is useful for validation and diagnostics, but the user-facing value must be explicitly curated/certified.
