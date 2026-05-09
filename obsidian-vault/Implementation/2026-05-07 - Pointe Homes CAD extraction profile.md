---
type: Implementation
date: 2026-05-07
project: floorplan-fit
status: current
tags:
  - floorplan-fit
  - loop1
  - dxf
  - extraction-profile
  - pointe-homes
---

# Pointe Homes CAD extraction profile

## What changed

The weak CAD convention logic used by the DXF extractors was moved into an explicit `DxfExtractionProfile.PointeHomes` profile.

## Why

The current seed floor plan is `SEMINOLE2000`, but the product must curate many Pointe Homes floor plans. The system should not hide Seminole/current-DXF conventions inside generic extractors.

## Current behavior

- `DxfExtractionProfile.PointeHomes` centralizes current conventions:
  - seed plans: `SEMINOLE2000`, `SANTA-BARBARA`
  - wall candidate layers: layers containing `WALL`
  - physical wall layers: wall layers excluding `ELECTRICAL`
  - room label layer: `ROOM LBLS`
  - room name keywords and excluded room-label tokens
  - opening geometry layers: `DOORS`, `WIN`, `WINS`
  - opening label layers: `DOORTEXT`, `WINDWS LBLS`
  - opening model/size label regex
  - fixed component layers: `FIXTURES`, `CABS`, `CABS-FLOORPLAN`
  - fixed component block tokens: `TOILET`, `STOVE`, `DISHWASHER`, `WASH`, `DRY`, `SINK`, `TUB`, `SHOWER`
- `IxMiliaWallExtractor`, `IxMiliaRoomLabelExtractor`, `IxMiliaOpeningExtractor`, and `IxMiliaFixedPlanComponentExtractor` now accept a profile and default to `PointeHomes`.
- Desktop DI registers `DxfExtractionProfile.PointeHomes` so runtime extractors use the explicit profile.

## Where

- `src/FloorplanFit.Infrastructure/Dxf/DxfExtractionProfile.cs`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaWallExtractor.cs`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaRoomLabelExtractor.cs`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaOpeningExtractor.cs`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaFixedPlanComponentExtractor.cs`
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Extraction/DxfExtractionProfileTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Composition/DesktopServiceRegistrationTests.cs`

## Verification

- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~DxfExtractionProfileTests" --artifacts-path .\.artifacts-test\green-dxf-extraction-profile` -> 4/4
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Extraction" --artifacts-path .\.artifacts-test\infrastructure-extraction-profile-focused` -> 18/18
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~DesktopServiceRegistrationTests" --artifacts-path .\.artifacts-test\green-dxf-profile-registration` -> 1/1
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --artifacts-path .\.artifacts-test\infrastructure-dxf-profile` -> 38/38
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-dxf-profile` -> 50/50
- `dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --artifacts-path .\.artifacts-test\application-dxf-profile` -> 23/23
- `git diff --check` -> exit 0, with LF/CRLF warnings only
- `.artifacts-test` removed after verification

## Gotchas

- This does not make every future Pointe convention automatic. It makes the current convention layer explicit and centralized.
- Adding future conventions should happen in the profile first, not by scattering `if layer == ...` across extractors.
- Manual curation still remains the final source of truth for false positives and floorplan-specific corrections.
