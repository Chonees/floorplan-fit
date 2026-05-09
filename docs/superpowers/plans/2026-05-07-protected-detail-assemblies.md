# Protected Detail Assemblies Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a first-class protected detail assembly stream so wet-area CAD details can be extracted, rendered, selected, removed, and persisted separately from structural walls.

**Architecture:** This is a Loop 1 curation change. Domain/Contracts/Application define the artifact, Infrastructure extracts and persists it, and Desktop renders/selects/removes it. The implementation mirrors fixed plan components where useful, but keeps protected details semantically separate so future fit logic can preserve them rather than treating them as editable walls.

**Tech Stack:** C#/.NET 10, Avalonia, SQLite/Microsoft.Data.Sqlite, IxMilia.Dxf, xUnit.

---

### Task 1: Protected detail extraction contract

**Files:**
- Create: `src/FloorplanFit.Application/Abstractions/DetectedProtectedDetailAssembly.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IProtectedDetailAssemblyExtractor.cs`
- Modify: `src/FloorplanFit.Infrastructure/Dxf/DxfExtractionProfile.cs`
- Test: `tests/FloorplanFit.Infrastructure.Tests/Extraction/DxfExtractionProfileTests.cs`

- [ ] **Step 1: Write failing profile tests**

Add tests that prove `MISC`, `L1`, and `HATCH` are protected detail candidate layers, while random layers are not.

- [ ] **Step 2: Run focused test and verify RED**

Run: `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~DxfExtractionProfileTests" --artifacts-path .\.artifacts-test\red-protected-detail-profile`
Expected: FAIL because the profile method does not exist yet.

- [ ] **Step 3: Add the contract and profile methods**

Create detected DTO/interface in Application abstractions and add profile methods to centralize protected detail CAD conventions.

- [ ] **Step 4: Run focused test and verify GREEN**

Run same command with `.\.artifacts-test\green-protected-detail-profile`.
Expected: PASS.

### Task 2: Domain, repository, schema, and reader

**Files:**
- Create: `src/FloorplanFit.Domain/FloorPlans/ExtractedProtectedDetailAssembly.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/ProtectedDetailAssemblyDto.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IExtractedProtectedDetailAssemblyRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedProtectedDetailAssemblyRepository.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- Modify: `src/FloorplanFit.Contracts/FloorPlans/FloorPlanReviewSessionDto.cs`
- Test: `tests/FloorplanFit.Infrastructure.Tests/Review/FloorPlanReviewSessionReaderIntegrationTests.cs`

- [ ] **Step 1: Write failing reader/persistence tests**

Add seeded protected detail data and assert it appears in `FloorPlanReviewSessionDto.ProtectedDetailAssemblies` and its geometry paths are loaded.

- [ ] **Step 2: Run focused test and verify RED**

Run: `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewSessionReaderIntegrationTests" --artifacts-path .\.artifacts-test\red-protected-detail-reader`
Expected: FAIL because DTO/schema/repository do not exist yet.

- [ ] **Step 3: Implement persistence slice**

Add schema tables `extracted_protected_detail_assemblies` and `extracted_protected_detail_assembly_paths`, repository add/list/remove, review DTO mapping, and geometry inclusion.

- [ ] **Step 4: Run focused test and verify GREEN**

Run same command with `.\.artifacts-test\green-protected-detail-reader`.
Expected: PASS.

### Task 3: DXF extractor and extraction orchestration

**Files:**
- Create: `src/FloorplanFit.Infrastructure/Dxf/IxMiliaProtectedDetailAssemblyExtractor.cs`
- Modify: `src/FloorplanFit.Application/FloorPlans/Extraction/ExtractWallCandidatesHandler.cs`
- Test: `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaProtectedDetailAssemblyExtractorTests.cs`
- Test: `tests/FloorplanFit.Application.Tests/FloorPlans/Extraction/ExtractWallCandidatesHandlerTests.cs`

- [ ] **Step 1: Write failing extractor/orchestration tests**

Assert Seminole 2000 extraction returns protected wet detail geometry from protected layers, and the extraction handler persists detected protected details.

- [ ] **Step 2: Run focused tests and verify RED**

Run Infrastructure extractor test and Application extraction test with isolated artifacts paths.
Expected: FAIL because extractor/orchestration do not exist yet.

- [ ] **Step 3: Implement extractor and handler wiring**

Extract line/arc/polyline/circle/ellipse detail paths from profile candidate layers, classify as `WetAreaDetail`, and persist through the extraction handler.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the same focused tests.
Expected: PASS.

### Task 4: Desktop preview, selection, and removal

**Files:**
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/RemoveProtectedDetailAssemblyHandler.cs`
- Create: `src/FloorplanFit.Desktop/Controls/Preview/ProtectedDetailPreviewLayerRenderer.cs`
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- Modify: `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`
- Test: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/RemoveOpeningArtifactHandlerTests.cs`

- [ ] **Step 1: Write failing UI/viewmodel/removal tests**

Assert protected details are exposed on preview, hit-tested above openings/walls, selected from preview, and removed through a handler.

- [ ] **Step 2: Run focused tests and verify RED**

Run Desktop control/viewmodel and Application curation tests with isolated artifacts paths.
Expected: FAIL because UI/removal types are missing.

- [ ] **Step 3: Implement Desktop slice**

Bind protected details to preview, render them with original color or semantic fallback, add side-panel list and remove button, register DI services.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the same focused tests.
Expected: PASS.

### Task 5: Verification and durable notes

**Files:**
- Modify: `obsidian-vault/Current State.md`
- Create: `obsidian-vault/Implementation/2026-05-07 - Protected detail assemblies for wet-area curation.md`

- [ ] **Step 1: Run focused suites**

Run Infrastructure, Application, and Desktop tests with `--artifacts-path`.

- [ ] **Step 2: Run `git diff --check`**

Expected: no whitespace errors beyond existing line-ending warnings.

- [ ] **Step 3: Delete `.artifacts-test`**

Remove only the repo-local test artifacts folder.

- [ ] **Step 4: Save Obsidian and Engram memory**

Capture what changed, why, where, and gotchas for future sessions.
