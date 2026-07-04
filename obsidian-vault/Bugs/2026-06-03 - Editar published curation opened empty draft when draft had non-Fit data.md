---
type: Bug
date: 2026-06-03
project: floorplan-fit
status: fixed
tags:
  - floorplan-fit
  - loop1
  - edit-mode
  - clone
  - publish
---

# Editar published curation opened an empty draft when the draft already had non-Fit data

## Symptom

Clicking **Editar** on the published SEMINOLE2000 session showed an editable draft with no published Fit objects: no pinch groups, no pinches, no franjas, no nodes, and no dimension interval bindings.

## Verified evidence

Local `app.db` showed the active published curation `06c496a5-e8c8-45f7-900d-3bb3319d9343` still had Fit data:

- 11 pinch groups
- 1 pinch marker
- 93 measurement corridors/franjas
- 187 measurement nodes
- 87 dimension interval bindings

The existing edit draft `faebd608-0b33-41ac-8a97-e1c4b482b11f` had 0 Fit rows but had 1 non-Fit dimension override. The published data was not deleted; the edit draft failed to copy it.

## Root cause

`SqliteFloorPlanCurationDataCloneService` treated **any** destination curation data as a reason to skip the entire clone. That meant one unrelated override in the draft blocked copying Fit-owned rows.

There was also a transaction boundary problem: the explicit Editar use case relied on `StartOrResumeCurationHandler`, whose responsibility is just starting/resuming a draft and saving that draft. The clone needed to be part of the same edit operation and committed after copying.

## Fix

- The clone service now only treats existing Fit-owned rows as a reason to skip Fit-owned cloning.
- Simple override tables are still copied with `INSERT OR IGNORE`, so existing draft overrides do not block Fit rows.
- `EditPublishedFloorPlanCurationHandler` now owns the edit transaction: create/resume draft, clone published curation data, read the draft session, and commit the operation.

## Verification

- RED test reproduced the non-Fit-data blocker.
- RED integration test reproduced the persistence problem after closing/reopening the DB.
- Application tests: 87/87 passed.
- Infrastructure tests: 80/80 passed.
- Desktop tests: 192/192 passed via temporary artifacts path.
- `git diff --check`: exit 0, only LF→CRLF warnings.
