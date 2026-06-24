# 2026-06-22 - Synthetic site plans are Seminole-calibrated

## Type
Bug investigation / Scale diagnosis

## Context
The user clarified that the relevant Dawson/site-plan library is the real site-plan picker/library, not the mistakenly imported `SitePlanDawson.dxf` floor-plan row. The symptom: SEMINOLE2000 behaves as expected against the synthetic site plans (`±1/2"` deficits), but other floor plans appear to overflow much more even though their labeled dimensions look similar.

## Verified evidence
- The synthetic generator `generate_synthetic_siteplans.py` hardcodes SEMINOLE2000 extraction run `c2496c35-ad07-4846-a68c-6bcc34027d69`.
- It measures SEMINOLE2000 structural wall footprint at about:
  - `468"` width (`39 ft`)
  - `930.000286"` height (`77.5 ft`)
- The generated site-plan setbacks in `D:\PointAIData\PLANS\originalsSitePlans` are inch drawings (`$INSUNITS = 1`) with exact SEMINOLE-based sizes:
  - width-deficit synths: `467/466" x 930.000286"`
  - height-deficit synths: `468" x 929/928"`
- Current DB measurements:
  - SEMINOLE2000 non-rejected structural footprint: `468" x 930.000286"` → expected `1/2"` synthetic deficits.
  - SANTA-BARBARA non-rejected structural footprint: about `456" x 791"` (`38 ft x 65'11"`) → should have slack and no deficit against SEMINOLE-sized synths.
  - SANTA-BARBARA all extracted wall candidates including rejected outliers: about `1633.656" x 1080"` (`136.1 ft x 90 ft`) → huge overflow.

## Conclusion
The synthetic site plans are not universal "every floor plan should miss by 1-2 inches" fixtures. They are calibrated to SEMINOLE2000. Other plans should only show the same tiny deficit if their curated structural footprint is essentially the same as SEMINOLE2000.

If SANTA-BARBARA or another non-SEMINOLE plan visibly overflows these synths by a lot, the likely bug is that the Adjust flow/render/session is using uncurated/raw extraction geometry or stale data, not that the `12:1` unit scale is wrong.

## Next verification target
Dump, from the exact Adjust screen/session:
1. selected floor-plan code/version/curation id,
2. site-plan file and detected `$INSUNITS`,
3. buildable setback bbox,
4. projected placement/fit bbox,
5. rendered floor-plan bbox,
6. whether rejected wall candidate geometry paths are present.

