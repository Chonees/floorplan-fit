---
type: Implementation
date: 2026-06-05
project: floorplan-fit
status: active
tags:
  - floorplan-fit
  - loop1
  - export
  - desktop
  - adjusted-dxf
---

# Adjusted DXF exports go to Desktop exports

## Change

Adjusted DXF exports now write to the operator-facing Desktop exports folder:

```txt
C:\Users\lucas\OneDrive\Escritorio\exports
```

The app still keeps the internal workspace, database, and raw DXF library under the executable workspace. Only the adjusted DXF export directory moved.

## Why

The previous location was buried inside the Debug executable workspace:

```txt
src\FloorplanFit.Desktop\bin\Debug\net10.0\workspace\library\adjusted-dxf
```

That made the button feel like it did nothing because the status message only showed the file name and the exported file was not in an operator-visible folder.

## Design

- `DesktopServiceRegistration.AddDesktopSlice1` now resolves the Windows Desktop folder and appends `exports`.
- `AppWorkspace` accepts an optional adjusted-DXF directory override, so infrastructure tests and non-Desktop callers keep the old default unless they opt in.
- `ManagedFileStorage.ReserveAdjustedDxfPathAsync` continues to use `workspace.LibraryAdjustedDxfDirectory`, so the storage abstraction did not need to know Desktop rules.
- Review status now shows the full exported path instead of only the file name.

## Cleanup

Deleted the previous adjusted DXF files from the old Debug workspace adjusted-DXF folder:

- `SEMINOLE2000-adjusted.dxf`
- `SEMINOLE2000-adjusted-2.dxf`
- `SEMINOLE2000-adjusted-3.dxf`

## Verification

- RED: `AddDesktopSlice1_routes_adjusted_dxf_exports_to_desktop_exports_folder` failed while Desktop still used the internal workspace adjusted-DXF directory.
- RED: `ExportAdjustedDxfAsync_runs_handler_refreshes_review_and_reports_status` failed while the Review status only showed the file name.
- GREEN: `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~AddDesktopSlice1_routes_adjusted_dxf_exports_to_desktop_exports_folder|FullyQualifiedName~ExportAdjustedDxfAsync_runs_handler_refreshes_review_and_reports_status" --no-restore --nologo --output .artifacts-test\adjusted-dxf-export-path-final -v minimal` passed 2/2.
- Filesystem check: `C:\Users\lucas\OneDrive\Escritorio\exports` exists and the old adjusted-DXF folder has 0 remaining `.dxf` files.
