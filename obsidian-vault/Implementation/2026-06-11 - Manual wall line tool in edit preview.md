# Manual wall line tool in edit preview

## Type
Implementation

## Status
Implemented and verified.

## Current truth
Loop 1 edit/review preview now has a Fit toolbar tool named `Agregar pared` for adding straight manual wall lines.

- The operator clicks `Agregar pared`, clicks the first point, moves the cursor to preview a dashed ghost line, then clicks the second point.
- The draft line is preview-only until the second click.
- On the second click, the app persists a real accepted wall candidate on the latest wall extraction run.
- The new candidate uses `MANUAL-WALLS` as source layer and `MANUAL-WALL:{id}` as source reference.
- After persistence, the review session refreshes and selects the new wall candidate.
- Competing Fit placement modes are mutually exclusive: arming manual wall creation cancels pinch placement and measurement-node placement.

## Product loop
Loop 1: floor plan curation.

This gives the curator a minimal structural correction tool without turning the preview into a general CAD editor. The new wall line becomes structural candidate geometry, so Loop 2 can use it later for setback/fit reasoning just like detected accepted wall candidates.

## Architecture
- Desktop owns the two-click interaction, cursor draft, preview ghost line, and XAML event wiring.
- Application owns validation and orchestration through `AddManualWallCandidateHandler`.
- Infrastructure owns SQLite persistence for the accepted `ExtractedWallCandidate` and detected geometry path/segment.

## Implementation
- `src/FloorplanFit.Application/FloorPlans/Curation/AddManualWallCandidateHandler.cs` creates accepted manual wall candidates and rejects zero-length lines.
- `src/FloorplanFit.Application/Abstractions/IExtractedWallCandidateRepository.cs` adds single-candidate insert and next-sort-order methods.
- `src/FloorplanFit.Application/Abstractions/IWallExtractionRunRepository.cs` adds latest-run lookup by version.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedWallCandidateRepository.cs` persists the manual candidate and geometry by reusing the range insert path.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteWallExtractionRunRepository.cs` resolves the latest extraction run for the active floor-plan version.
- `src/FloorplanFit.Desktop/Controls/Preview/ManualWallLineDraft.cs` carries the in-progress draft endpoints.
- `src/FloorplanFit.Desktop/Controls/Preview/ManualWallLinePreviewLayerRenderer.cs` renders the clipped dashed draft line and endpoints.
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs` exposes manual-wall armed/draft properties and pointer events.
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs` coordinates arming, first click, hover preview, second-click persistence, refresh, and selection.
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml(.cs)` adds the `Agregar pared` button and preview event handlers.
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs` registers the handler.

## Verification
- RED/GREEN Desktop tests proved the missing `Agregar pared` UI wiring and DI registration before implementation.
- Final focused verification:
  - `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --artifacts-path .testartifacts\manual-wall-application-final --filter "FullyQualifiedName~AddManualWallCandidateHandlerTests|FullyQualifiedName~AddPinchMarkerHandlerTests|FullyQualifiedName~RejectWallCandidateHandlerTests|FullyQualifiedName~ExtractWallCandidatesHandlerTests"` -> 5/5 passed.
  - `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --artifacts-path .testartifacts\manual-wall-infra-final --filter "FullyQualifiedName~ExtractedWallCandidateRepository_persists_manual_wall_line_candidates|FullyQualifiedName~FloorPlanCurationPersistenceIntegrationTests"` -> 15/15 passed.
  - `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .testartifacts\manual-wall-desktop-final --filter "FullyQualifiedName~DesktopServiceRegistrationTests|FullyQualifiedName~ReviewFloorPlanWindowLayoutTests|FullyQualifiedName~AppXamlInitializationTests|FullyQualifiedName~FloorPlanReviewViewModelTests"` -> 82/82 passed.
  - `git diff --check` -> exit 0 with LF-to-CRLF warnings only.
