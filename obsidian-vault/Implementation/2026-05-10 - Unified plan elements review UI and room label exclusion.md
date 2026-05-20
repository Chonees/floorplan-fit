---
type: implementation
date: 2026-05-10
project: floorplan-fit
tags:
  - loop-1
  - curation
  - review-ui
  - room-labels
  - avalonia
---
# Unified plan elements review UI and room label exclusion
## What changed
Loop 1 review now uses one unified curation surface for plan artifacts:
- the left panel is now Plan Elements
- all curable artifact families live together there: Lines, Rooms, Openings, Opening Labels, Fixed Elements, and Protected Details
- the right panel is now focused on Selected Item plus Pinch Tools
- the visible curation action is unified as Exclude from Curation
- wall candidates still use auditable rejection under the hood
- room labels gained a real backend removal path, so false-positive labels can now be excluded like other artifacts
## Why
The previous Review window leaked implementation language into UX and split related plan artifacts across multiple noisy panels. The approved product direction is subtractive curation: everything enters accepted by default, and the admin excludes only what should not survive into the published reusable floor plan.
## Files touched
- src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml
- src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs
- src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs
- src/FloorplanFit.Application/Abstractions/IExtractedRoomLabelRepository.cs
- src/FloorplanFit.Application/FloorPlans/Curation/RemoveRoomLabelHandler.cs
- src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedRoomLabelRepository.cs
- src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs
- desktop/application/infrastructure tests covering review curation
## Important rule
Exclude from Curation is a UX contract, not a storage contract. Walls are still rejected for auditability, while room labels, openings, opening labels, fixed components, and protected details are removed from the extracted artifact set when the user excludes false positives.
## Verification
- dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~RemoveOpeningArtifactHandlerTests|FullyQualifiedName~RejectWallCandidateHandlerTests|FullyQualifiedName~ExtractWallCandidatesHandlerTests" --artifacts-path .\.artifacts-test\application-review-final
- dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~FloorPlanCurationPersistenceIntegrationTests|FullyQualifiedName~FloorPlanReviewSessionReaderIntegrationTests|FullyQualifiedName~OpenFloorPlanReviewSessionIntegrationTests" --artifacts-path .\.artifacts-test\infrastructure-review-final
- dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~ReviewFloorPlanWindowLayoutTests|FullyQualifiedName~AddDesktopSlice1_registers_review_session_services" --artifacts-path .\.artifacts-test\desktop-review-final
## Related notes
- [[Decisions/2026-05-10 - Unified plan elements review UI and exclude action]]
- [[Implementation/2026-05-09 - Subtractive wall candidate curation]]
