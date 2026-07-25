---
type: implementation
date: 2026-07-16
status: superseded
replaces: "[[2026-07-16 - Point.ai and FloorplanFit technical reports generated]]"
replaced_by: "[[2026-07-16 - Point.ai and FloorplanFit system manuals generated]]"
---

# English technical reports with data models and pivot analysis

## What

Generated the external-facing English editions of the Point.ai and FloorplanFit technical system reports.

They preserve the visual language, system history, verified technology stack, implementation-status matrices, evidence lists, and complete Clockify chronology of the Spanish editions.

## Deliverables

- `output/pdf/Point.ai-technical-system-report-2026.pdf`
  - 40 pages.
  - 31 workdays and 269h16m.
  - Includes active and historical data structures, persistence relationships, the strategic blockers, product-direction change, branch divergence, and all daily statements in English.

- `output/pdf/FloorplanFit-technical-system-report-2026.pdf`
  - 38 pages.
  - 61 workdays and 542h38m.
  - Includes Contracts/Domain/Application/Infrastructure/Desktop data ownership, SQLite lifecycle, Loop 1, Loop 2, HousePlanSet aggregates, the transition from Point.ai, and all daily statements in English.

## Verified Point.ai blocker

No single contemporaneous pivot note was found. The causal account is a synthesis grounded in dated evidence.

The production blocker was not one bug or a lack of output. AutoCAD-grade raster-to-CAD required:

- proprietary vector ground truth rather than image labels alone;
- a representative 10-20 plan structural benchmark;
- a 200-500 plan gold vector dataset for the production-oriented hybrid path;
- wall and opening models with shared coordinates;
- global junction, continuity, closed-loop, and wall-thickness reasoning;
- a typed wall graph and opening ownership;
- a geometric/topological solver;
- a consistent DXF renderer and AutoCAD validator.

The documented roadmap warned that end-to-end raster-to-graph work without sufficient proprietary data could consume months and still miss the contract. It offered a future 90-day implementation plan, but no committed authoritative 90-day completion plan exists.

## Why FloorplanFit reduced risk

FloorplanFit made real DXF files the primary source of truth, persisted reusable human-reviewed curation, used a local-first modular desktop architecture, and applied deterministic/auditable geometry only within explicitly authorized adjustment zones.

This removed production-grade raster reconstruction from the critical path while preserving Point.ai's durable lessons:

- human-in-the-loop review;
- artifacts and provenance;
- canonical plan data;
- site-fit constraints;
- reversible and auditable mutations;
- CAD output validation.

## Visual and text verification

- Point.ai: 40 pages rendered and inspected.
- FloorplanFit: 38 pages rendered and inspected.
- Contact sheets and representative full-resolution pages were reviewed.
- pypdf extraction found no replacement characters or unsupported square glyphs.
- The body text is English; Point.ai retains only English punctuation plus original code/file identifiers.
- No build, tests, restore, or application execution was performed.
