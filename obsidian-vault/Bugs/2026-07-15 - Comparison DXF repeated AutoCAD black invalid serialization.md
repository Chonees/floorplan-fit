---
type: bug
date: 2026-07-15
status: mitigated
replaces:
  - "[[2026-07-15 - Atomic Floor Electrical package comparison]]"
---

# Comparison DXF repeated AutoCAD black/invalid serialization

## Verified cause

`PlanSetComparisonDxfComposer` rebuilt both CAD-authored AC1032 drawings through a new IxMilia object graph and saved an AC1009 subset. The result lost source sections and object relationships: AutoCAD displayed it black/invalid and normal `ezdxf` loading failed in `TABLES`.

## Safe correction

- Keep the existing top-level `X.dxf` unchanged.
- Publish the technical package atomically with `X-plan-set/X-floorplan.dxf` plus every ready dependent sheet using deterministic role names; an ElectricalPlan is `X-electrical.dxf`.
- Do not create or advertise `X-comparison.dxf` or a `ComparisonReview` artifact.
- A failed/absent comparison is not a package failure. The valid technical outputs publish normally.

## Deferred capability

A comparison can return only after a source-preserving AC1032 merger exists and preserves the complete handle/owner/pointer, block-record, table, class, object, and referenced-object graph. That merger is explicitly outside this correction.

## Code removed

- `IPlanSetComparisonDxfComposer`
- `PlanSetComparisonDxfComposer`
- the exclusive composer tests and Desktop DI registration

The Floor/Electrical exporters, their audits, atomic publication, and generic package-artifact DTOs remain unchanged.
