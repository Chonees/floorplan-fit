---
type: decision
date: 2026-07-16
status: superseded
replaced_by: "[[2026-07-16 - External documents are system manuals, not work reports]]"
---

# Technical reports use English and document the Point.ai pivot

## Decision

The system-construction reports for Point.ai and FloorplanFit should be delivered in English while preserving the visual design and complete work chronology of the Spanish editions.

Both reports must include:

- the system's data structures and persistence model;
- an honest description of current, historical, experimental, partial, and pending capabilities;
- the documented Point.ai blockers;
- the reason the product direction moved toward FloorplanFit;
- the architectural relationship between both systems.

## Evidence rule

The reports must not claim that a single explicit "pivot note" exists unless one is found.

The rationale should distinguish:

- explicit Point.ai evidence: repeated model/data failures, domain shift, misleading metrics, missing structural benchmarks, the need for a 200-500 plan gold vector dataset, graph/topology inference, a geometric solver, and CAD validation;
- explicit FloorplanFit decisions: authoritative DXF input, canonical persisted curation, local-first desktop architecture, deterministic and auditable geometry, and optional AI that is not the geometry authority;
- synthesis: the new direction reduced delivery risk by starting from existing CAD truth rather than requiring production-grade raster-to-CAD reconstruction first.

## Consequence

The English PDFs supersede the Spanish editions as the primary external-facing technical reports. The Spanish files remain useful as source-language references.
