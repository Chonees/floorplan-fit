# 2026-06-18 - Library toolbar cleanup

## Type
Implementation

## What changed
Removed duplicate top-level Library toolbar actions:
- `Extract Selected Version`
- `Edit Selected Review`
- `Delete Selected Version`

The top toolbar now keeps only `Import DXF` plus selected/status text. Version-specific actions remain on each version row: `Select`, `Edit`, `Adjust to Site Plan`, and `Delete`.

## Why
Import already extracts automatically, and Edit/Delete already exist in the version rows. Keeping duplicate top buttons made the UI noisier and made accidental re-extract easier.

## Verification
- Source check confirmed removed buttons/handlers/properties are gone from production files.
- `git diff --check` exited `0` with CRLF warnings only.
- No .NET build was run per repo rule.

## Follow-up: removed row Select
The per-version `Select` button was also removed because row actions (`Edit`, `Adjust to Site Plan`, `Delete`) already select their target version internally. Keeping `Select` only added an extra click with no product value.

Verification:
- Production source check confirmed `Content="Select"` and `SelectVersionButton_OnClick` are gone.
- Row actions `Edit`, `Adjust to Site Plan`, and `Delete` remain.
- `git diff --check` exited `0` with CRLF warnings only.

