---
type: Decision
date: 2026-06-03
project: floorplan-fit
status: accepted
tags:
  - floorplan-fit
  - loop1
  - publish
  - review-session
  - curation-lifecycle
---

# Review opens the latest published curation by default

## Decision

When a floor plan version has an active published curation, opening Review must show that latest published curation by default. It must not auto-create or show a new draft just because the user opened the plan.

## Why

The operator expectation after publishing is: “show me what I published.” Creating an empty post-publish draft on open makes it look like dimensions, franjas, pinches, and bindings disappeared, even though the published curation still has them.

## Implementation consequence

- Opening a published curation returns `DraftCurationId = Guid.Empty`.
- Review session reads Fit data from `active_published_curation_id` first.
- Existing stale empty drafts are ignored while an active published curation exists.

## Tradeoff

This favors safe read/display semantics over immediate editing. If we later need “edit published curation,” that should be an explicit action that creates/copies a draft intentionally, not a side effect of opening Review.

## Follow-up implemented

The explicit edit action now exists. Published Review remains read-only by default, and **Editar** creates/resumes a copied draft for the next set of changes. See [[2026-06-03 - Edit published curation creates an explicit draft copy]].
