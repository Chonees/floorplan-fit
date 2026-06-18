# 2026-06-18 - Re-extract hid Seminole curated state

## Type
Bug / Recovery

## Symptom
User clicked/select **Extract** on `SEMINOLE2000` and the app appeared to erase the existing curation.

## Verified root cause
The curation was not deleted. A new `wall_extraction_runs` row was created on `2026-06-18T19:36:08Z` with status `Completed`:
- new run: `fc119d23-fe8f-4323-ac5e-8c9b849aee9a`
- old aligned run: `c2496c35-ad07-4846-a68c-6bcc34027d69`

The review/session reader selects the latest `Completed` extraction run for a floor-plan version. That made the app read newly extracted geometry while the existing SEMINOLE curation still referenced the prior extraction geometry, so the curated state looked gone.

## Recovery applied
- Created SQLite backup: `artifacts/db-backups/app-before-seminole-recovery-20260618-163700.db`.
- Marked the accidental new run as `IgnoredRecovery` instead of deleting it.
- The latest `Completed` run for `SEMINOLE2000` is again the original aligned run `c2496c35-ad07-4846-a68c-6bcc34027d69`.

## Verified recovered state
- Active published SEMINOLE curation: version `11`, id `acb24c30-791f-4fc3-bc89-25415283649c`.
- Latest draft SEMINOLE curation: version `12`, id `39c6f918-6dd0-4bfd-9de7-fec002e479d3`.
- Published curation still has `159` dimension bindings, `330` measurement nodes, and `8` pinches.
- Latest draft still has `159` dimension bindings, `330` measurement nodes, and `8` pinches.
- `python scripts\check_training_dataset_export.py` passed after recovery.

## Prevention needed
The app should guard **Extract** when a version already has published/draft curation. Minimal product fix:
1. show confirmation before re-extracting a curated plan,
2. explain that re-extraction can invalidate geometry ids used by curations,
3. either block by default or create a new floor-plan version instead of replacing the latest run used by the curated version.

## Follow-up fix applied on 2026-06-18
After the first recovery, the UI could still appear uncurated because `SqliteFloorPlanReviewSessionReader.GetLatestExtractionRunId(...)` ordered runs by date without filtering by status. That meant the newer `IgnoredRecovery` run could still be selected by the review reader.

Code prevention now applied:
- review session reader only uses extraction runs with `status = 'Completed'`;
- Library rows show curation history per floor-plan version, e.g. `Published v11 ? Draft v12 ? 11 published total`;
- `Extract Selected Version` is disabled/guarded when the selected version has published or draft curation history.

Verification after fix:
- DB still contains accidental latest-any run `fc119d23-fe8f-4323-ac5e-8c9b849aee9a` with `IgnoredRecovery`.
- Fixed reader path uses latest completed run `c2496c35-ad07-4846-a68c-6bcc34027d69`.
- SEMINOLE still has `11` published curations, latest published v11, latest draft v12, and active published curation has `159` bindings.
- Source checks passed; `python scripts\check_training_dataset_export.py` passed; `git diff --check` exited 0 with CRLF warnings only.

