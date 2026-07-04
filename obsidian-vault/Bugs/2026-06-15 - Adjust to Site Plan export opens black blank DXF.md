---
type: Bug
date: 2026-06-15
project: floorplan-fit
status: fixed
related:
  - 2026-06-05 - Adjusted DXF dedup reintroduced AutoCAD blank Drawing1.md
  - 2026-06-05 - Adjusted DXF export wrote invalid layout owners.md
tags:
  - floorplan-fit
  - loop2
  - site-plan
  - export
  - dxf-validity
  - autocad
---

# Adjust to Site Plan export opens black/blank DXF

## Reported symptom

User reports this flow:

1. In the Loop 1 preview/edit inspector, exporting the edited floor-plan DXF produces a DXF that can be opened and viewed.
2. Leaving the preview and using **Adjust to Site Plan**, applying a suggested adjustment, and exporting the adjusted/site-plan DXF produces a file that opens as a black/blank screen and cannot be viewed.

This resembles the previous AutoCAD black-screen / blank `Drawing1` failure seen in adjusted DXF export, but this specific Loop 2 **Adjust to Site Plan export** case was not previously documented as its own bug.

## Verified documentation/history

Existing documented bug:

- `Bugs/2026-06-05 - Adjusted DXF dedup reintroduced AutoCAD blank Drawing1.md`

That bug was for Loop 1 adjusted floor-plan export. Verified root cause there was unsafe whole-file IxMilia serialization: the export lost source DXF structural metadata such as `ACDSDATA`, and `ezdxf.readfile(...)` failed with `required BLOCK_RECORD #0 for layout 'Layout1' does not exist`.

Current Obsidian search did not find an existing note for `IxMiliaAdjustedSitePlanExporter`, `DXF combinado`, `Exportar DXF ajustado al sitio`, or Adjust-to-Site-Plan black/blank export behavior.

## Current code evidence

The current Loop 2 exporter is `src/FloorplanFit.Infrastructure/Dxf/IxMiliaAdjustedSitePlanExporter.cs`.

Observed implementation shape:

- loads the floor-plan source through `DxfFile.Load(floorPlanSourcePath)`;
- applies compression transforms to the loaded DXF model;
- injects site-plan entities;
- writes with `floor.Save(outputFilePath, asText: true)`.

This is not yet a confirmed root cause, but it is suspicious because the previous AutoCAD black/blank failure came from whole-file IxMilia serialization of real AutoCAD-authored floor-plan DXFs. The previous fix required a source-preserving/surgical patcher instead of full reserialization.

## UI wording gap

User also clarified the export action should be named **Exportar DXF**, not **Exportar DXF ajustado** / **Exportar DXF ajustado al sitio**.

Current code still contains old wording:

- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml` — `Exportar DXF ajustado`
- `src/FloorplanFit.Desktop/SitePlanAdjustmentWindow.axaml` — `Exportar DXF ajustado al sitio`
- `src/FloorplanFit.Desktop/SitePlanAdjustmentWindow.axaml.cs` — save picker title `Exportar DXF ajustado al sitio`

## Next investigation

1. Reproduce with the exact exported file from the Adjust-to-Site-Plan flow.
2. Compare the exported DXF against the source floor-plan DXF for critical sections such as `ACDSDATA`, layout/block-record references, modelspace entity counts, and `ezdxf.audit()` errors/fixes.
3. Add a RED regression around real SEMINOLE/site-plan export validity before changing exporter architecture.
4. If confirmed, avoid full `DxfFile.Save(...)` for real floor-plan output and move toward the same source-preserving principle used by the fixed adjusted DXF exporter.

## Verified reproduction

The stale exported file `C:\Users\lucas\OneDrive\Escritorio\exports\plano-ajustado-al-sitio.dxf` reproduced the same structural failure pattern:

- `ACDSDATA` was missing.
- `ezdxf.readfile(...)` failed with `DXFStructureError: required BLOCK_RECORD #0 for layout 'Layout1' does not exist`.

A RED regression on the current exporter also proved the output sections differed from source SEMINOLE2000: the source had `ACDSDATA`, but the exported Adjust-to-Site-Plan DXF did not.

## Fix implemented

`IxMiliaAdjustedSitePlanExporter` no longer saves the real floor-plan DXF with `DxfFile.Save(...)`.

Current behavior:

- reads the original floor-plan DXF as Latin-1 group-code pairs;
- preserves original floor-plan sections/object graph, including `ACDSDATA`;
- applies auto-fit compression by patching supported source entity coordinate pairs in modelspace plus anonymous dimension blocks;
- injects supported site-plan entities into the preserved `ENTITIES` section with fresh handles and layer records;
- skips object-dependent site entities such as inserts, dimensions, and hatches with warnings instead of injecting broken references.

UI copy was also corrected so visible export actions say **Exportar DXF**.

## Verification

- RED: `IxMiliaAdjustedSitePlanExporterTests.ExportAsync_preserves_real_floor_plan_sections_when_combining_site_plan` failed because output lacked `ACDSDATA`.
- GREEN/final: `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaAdjustedSitePlanExporterTests" --artifacts-path .testartifacts\dotnet-test-artifacts-adjusted-site-final2-infra -v minimal` passed 3/3.
- RED/GREEN copy: `Dxf_export_actions_use_simple_exportar_dxf_copy` failed on old labels and passed after changing visible copy.
- Final Desktop verification: `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~AppXamlInitializationTests|FullyQualifiedName~SitePlanAdjustmentPreviewProjectorTests.ExportAdjustedSitePlanAsync_invokes_exporter_with_current_placement_and_paths" --artifacts-path .testartifacts\dotnet-test-artifacts-adjusted-site-final2-desktop -v minimal` passed 11/11.
- `git diff --check` exited 0 with CRLF warnings only.

## Follow-up: fresh export still failed in AutoCAD because HANDSEED was stale

User reported the same visible AutoCAD failure after the source-preserving exporter fix.

Verification showed this was no longer the original `ACDSDATA` / missing `BLOCK_RECORD` failure:

- fresh `C:\Users\lucas\OneDrive\Escritorio\exports\plano-ajustado-al-sitio.dxf` had `ACDSDATA`;
- `ezdxf.readfile(...)` loaded it;
- `ezdxf.audit()` reported `errors=0`, `fixes=0`;
- but `ezdxf` emitted warnings: `Found non-unique entity handle #4B56...#4B5A`.

Root cause: the exporter injected new handles up to `4B74`, but preserved the original header `$HANDSEED = 4B55`. CAD readers can use `$HANDSEED` as the next-handle allocator, so leaving it behind the injected handles makes duplicate handle allocation possible.

Fix: after layer/entity injection, `IxMiliaAdjustedSitePlanExporter` now rewrites `$HANDSEED` to `DxfHandleGenerator.NextAvailableHandle`, i.e. one past the highest injected handle.

A temporary operator-facing diagnostic copy was written because AutoCAD/OneDrive had the original export locked:

```txt
C:\Users\lucas\OneDrive\Escritorio\exports\plano-ajustado-al-sitio-handseed-fixed.dxf
```

That copy has `ACDSDATA`, loads with `ezdxf`, audits with `errors=0`, `fixes=0`, and no duplicate-handle warning was emitted during the isolated check.

Verification:

- RED: exporter regression failed because `$HANDSEED` was not greater than the max emitted entity handle.
- GREEN: `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaAdjustedSitePlanExporterTests" --artifacts-path .testartifacts\dotnet-test-artifacts-adjusted-site-handseed-final-infra -v minimal` passed 3/3.
- `git diff --check` exited 0 with CRLF warnings only.

## Follow-up: AutoCAD rejected injected LAYER records without PlotStyleName

User reported AutoCAD still showed the black screen / Enter / `Drawing1` loop. This time the diagnosis was validated with AutoCAD itself, not only `ezdxf`.

AutoCAD Core Console reproduced the real rejection on `C:\Users\lucas\OneDrive\Escritorio\exports\plano-ajustado-al-sitio.dxf`:

```txt
Error in LAYER Table
Did not receive PlotStyleName on line 2894.
Invalid or incomplete DXF input -- drawing discarded.
ERROR: ... ErrorStatus=53
```

Root cause: the source-preserving exporter injected new `LAYER` table records with only the minimal fields (`2`, `70`, `62`, `6`) but SEMINOLE2000's AutoCAD DXF layer records include required CAD metadata such as:

```txt
370
-3
390
F
347
98
348
0
```

AutoCAD LT 2026 rejects this file when injected layer records do not include `390 PlotStyleName`.

Fix: `IxMiliaAdjustedSitePlanExporter.BuildLayerRecord(...)` now writes the required layer-table tail (`370`, `390`, `347`, `348`) for injected layers.

Diagnostic file generated for immediate QA:

```txt
C:\Users\lucas\OneDrive\Escritorio\exports\plano-ajustado-al-sitio-layer390-fixed.dxf
```

AutoCAD Core Console verification on that file:

```txt
Regenerating model.
Total errors found 0 fixed 0
exit=0
```

Verification:

- RED: layer-table regression failed because 3 injected layer records did not include group code `390`.
- GREEN: same test passed after adding the required layer metadata.
- Final focused suite: `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaAdjustedSitePlanExporterTests" --artifacts-path .testartifacts\dotnet-test-artifacts-adjusted-site-layer390-final-infra -v minimal` passed 3/3.
- `git diff --check` exited 0 with CRLF warnings only.

