# Verified current floor plan footprints

Date: 2026-06-28
Type: Current State
Scope: Loop 2 Adjust-to-Site-Plan sizing inputs

## Verified against local DB
Database: `src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace/app.db`

## Usable structural footprint for fit/simulation
These are the numbers to use when simulating buildable area around the house footprint:

- `SEMINOLE2000`: `468.000" x 930.000286"` = `39'0" x 77'6"` = `39.000 ft x 77.500 ft`, area `3,022.501 sq ft`.
- `SANTA-BARBARA`: `456.000" x 787.000"` = `38'0" x 65'7"` = `38.000 ft x 65.583 ft`, area `2,492.167 sq ft`.

## Current-version library rows
- Current-version DB rows are `SANTA-BARBARA`, `SEMINOLE2000`, and `SitePlanDawson`.
- `SANTA-BARBARA-2` and `SANTA-BARBARA-3` are active template placeholders with no current version.
- `SitePlanDawson` computes `468.000 ft x 930.000 ft` / `435,240.134 sq ft` because the row uses feet (`304.8`); treat it as a site-plan/reference row, not a house footprint.

## Raw accepted wall bbox
Raw accepted-wall bbox includes small structural/extraction tails and can be bigger than the fit footprint:

- `SEMINOLE2000`: `483.786" x 930.000"` = about `40.315 ft x 77.500 ft` (`40'4" x 77'6"`). The structural fit footprint ignores the small left protrusion and uses `468"` width.
- `SANTA-BARBARA`: `456.000" x 791.000"` = about `38.000 ft x 65.917 ft` (`38'0" x 65'11"`). The structural fit footprint trims the negligible top/bottom tail to `787"` height.

## Not a published house footprint
- `SitePlanDawson` exists in the DB as a floor-plan-looking row, but it is not published and its measurement factor is feet (`304.8`). Do not use its computed `468 ft x 930 ft` as a house size.
