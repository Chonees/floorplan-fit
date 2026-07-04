# Canonical Adjustment Recipe Slice 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the first demonstrable route explicit: FloorPlan adjustment produces a derivable AdjustmentRecipe, dependent projection consumes recipe information, and the export/audit report says what applied vs what requires review.

**Architecture:** Ponytail slice: do not create a new persistence table yet. Reuse the existing `AdjustedSitePlanPlacementDto` / `placement_json` as the recipe payload, add small typed summary records, and surface recipe handling through projection/audit DTOs. No dependent DXF local deformation in this slice.

**Tech Stack:** C#/.NET, Avalonia ViewModel, SQLite repositories, existing xUnit tests. Repo rule: do **not** run `dotnet build`; prefer targeted static inspection and user/runtime smoke.

---

## File Structure

- Modify: `src/FloorplanFit.Contracts/FloorPlans/AdjustedSitePlanPlacementDto.cs`
  - Responsibility: expose a tiny read-only recipe summary derived from existing placement/compression steps.
- Modify: `src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjection.cs`
  - Responsibility: persist projection-side recipe handling summary as text; no new table.
- Modify: `src/FloorplanFit.Contracts/PlanSets/SheetAdjustmentProjectionDto.cs`
  - Responsibility: surface recipe handling to Application/Desktop.
- Modify: `src/FloorplanFit.Contracts/PlanSets/ExportedPlanSheetDto.cs`
  - Responsibility: carry recipe handling into package audit lines/manifest.
- Modify: `src/FloorplanFit.Application/PlanSets/Projection/ProjectElectricalSheetAdjustmentHandler.cs`
  - Responsibility: consume recipe info; mark local compression as review-required with explicit summary.
- Modify: `src/FloorplanFit.Application/PlanSets/Projection/ProjectRoofSheetAdjustmentHandler.cs`
  - Responsibility: same explicit review summary for roof.
- Modify: `src/FloorplanFit.Application/PlanSets/Projection/ProjectFacadeElevationSheetAdjustmentHandler.cs`
  - Responsibility: same explicit review summary for facade/elevation.
- Modify: `src/FloorplanFit.Application/PlanSets/ExportAudit/CreateMultiSheetExportAuditHandler.cs`
  - Responsibility: copy projection recipe handling to exported sheet DTO/domain.
- Modify: `src/FloorplanFit.Domain/PlanSets/PlanSetExportedSheet.cs`
  - Responsibility: persist recipe handling summary per exported sheet.
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteSheetAdjustmentProjectionRepository.cs`
  - Responsibility: store/load projection recipe handling summary.
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
  - Responsibility: add nullable column for projection recipe handling summary.
- Modify: `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs`
  - Responsibility: show recipe handling in audit lines.
- Test: existing projection/export audit tests in `tests/FloorplanFit.Application.Tests/PlanSets/...`

---

## Task 1: Add tiny recipe summary contracts

**Files:**
- Modify: `src/FloorplanFit.Contracts/FloorPlans/AdjustedSitePlanPlacementDto.cs`

- [ ] **Step 1: Add records at bottom of file**

```csharp
public sealed record AdjustmentRecipeSummaryDto(
    string Version,
    IReadOnlyList<AdjustmentRecipeOperationDto> Operations)
{
    public static AdjustmentRecipeSummaryDto FromPlacement(AdjustedSitePlanPlacementDto placement)
    {
        ArgumentNullException.ThrowIfNull(placement);

        return new AdjustmentRecipeSummaryDto(
            "v1",
            placement.CompressionSteps
                .SelectMany(step => step.Markers.Select(marker =>
                    new AdjustmentRecipeOperationDto(
                        ResolveOperationKind(step),
                        step.AxisTag,
                        step.Edge,
                        marker.Coordinate,
                        marker.TrimSourceUnits)))
                .ToArray());
    }

    private static string ResolveOperationKind(AdjustedCompressionStepDto step)
        => string.Equals(step.AxisTag, "Height", StringComparison.OrdinalIgnoreCase)
            ? "VerticalCompression"
            : "HorizontalCompression";
}

public sealed record AdjustmentRecipeOperationDto(
    string Kind,
    string AxisTag,
    string Edge,
    decimal Coordinate,
    decimal DeltaSourceUnits);
```

- [ ] **Step 2: Add focused contract test**

Create/modify: `tests/FloorplanFit.Application.Tests/PlanSets/Projection/AdjustmentRecipeSummaryDtoTests.cs`

```csharp
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.PlanSets.Projection;

public sealed class AdjustmentRecipeSummaryDtoTests
{
    [Fact]
    public void FromPlacement_maps_compression_steps_to_named_recipe_operations()
    {
        var placement = new AdjustedSitePlanPlacementDto(
            FloorToSiteScale: 1m,
            SiteOffsetX: 0m,
            SiteOffsetY: 0m,
            CompressionSteps:
            [
                new AdjustedCompressionStepDto(
                    "Width",
                    "Right",
                    [new AdjustedCompressionMarkerDto(50m, 2m)]),
                new AdjustedCompressionStepDto(
                    "Height",
                    "Top",
                    [new AdjustedCompressionMarkerDto(80m, 1.5m)])
            ]);

        var recipe = AdjustmentRecipeSummaryDto.FromPlacement(placement);

        Assert.Equal("v1", recipe.Version);
        Assert.Collection(
            recipe.Operations,
            operation =>
            {
                Assert.Equal("HorizontalCompression", operation.Kind);
                Assert.Equal("Width", operation.AxisTag);
                Assert.Equal("Right", operation.Edge);
                Assert.Equal(50m, operation.Coordinate);
                Assert.Equal(2m, operation.DeltaSourceUnits);
            },
            operation =>
            {
                Assert.Equal("VerticalCompression", operation.Kind);
                Assert.Equal("Height", operation.AxisTag);
                Assert.Equal("Top", operation.Edge);
                Assert.Equal(80m, operation.Coordinate);
                Assert.Equal(1.5m, operation.DeltaSourceUnits);
            });
    }
}
```

**Acceptance:** recipe summary can be derived without new persistence.

---

## Task 2: Add projection recipe handling summary

**Files:**
- Modify: `src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjection.cs`
- Modify: `src/FloorplanFit.Contracts/PlanSets/SheetAdjustmentProjectionDto.cs`

- [ ] **Step 1: Extend domain constructor/property**

Add optional constructor arg after `ruleSummary`:

```csharp
string? recipeHandlingSummary = null
```

Set property:

```csharp
RecipeHandlingSummary = string.IsNullOrWhiteSpace(recipeHandlingSummary) ? null : recipeHandlingSummary.Trim();
```

Add property:

```csharp
public string? RecipeHandlingSummary { get; }
```

- [ ] **Step 2: Extend DTO**

```csharp
public sealed record SheetAdjustmentProjectionDto(
    Guid ProjectionId,
    Guid PlanSetVersionId,
    Guid DependentSheetId,
    Guid SheetRegistrationId,
    Guid CanonicalAdjustmentId,
    string Method,
    SheetAdjustmentProjectionTransformDto Transform,
    decimal Confidence,
    string Status,
    string? Warning,
    int CanonicalCompressionStepCount,
    DateTime CreatedAtUtc,
    string? RuleSummary = null,
    string? RecipeHandlingSummary = null);
```

**Acceptance:** existing callers compile after adding named/optional param support.

---

## Task 3: Make sheet projectors consume recipe summary

**Files:**
- Modify: `ProjectElectricalSheetAdjustmentHandler.cs`
- Modify: `ProjectRoofSheetAdjustmentHandler.cs`
- Modify: `ProjectFacadeElevationSheetAdjustmentHandler.cs`
- Test: corresponding existing projection handler tests

- [ ] **Step 1: Add failing electrical test**

Add to `ProjectElectricalSheetAdjustmentHandlerTests`:

```csharp
[Fact]
public async Task HandleAsync_reports_canonical_recipe_compression_as_electrical_review_required()
{
    var clock = new FakeClock(new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc));
    var registration = CreateRegistration(SheetRegistrationStatus.Confirmed, 0.92m, clock.UtcNow);
    var projectionRepository = new CapturingSheetAdjustmentProjectionRepository();
    var handler = new ProjectElectricalSheetAdjustmentHandler(
        new FakeSheetRegistrationRepository(registration),
        projectionRepository,
        new CapturingUnitOfWork(),
        clock);

    var response = await handler.HandleAsync(
        new ProjectElectricalSheetAdjustmentRequest(
            registration.Id,
            Guid.NewGuid(),
            new AdjustedSitePlanPlacementDto(
                1m,
                0m,
                0m,
                [new AdjustedCompressionStepDto("Width", "Right", [new AdjustedCompressionMarkerDto(50m, 2m)])])),
        CancellationToken.None);

    Assert.Equal("RequiresManualConfirmation", response.Status);
    Assert.Equal(1, response.CanonicalCompressionStepCount);
    Assert.Contains("HorizontalCompression", response.RecipeHandlingSummary);
    Assert.Contains("ElectricalPlan", response.RecipeHandlingSummary);
    Assert.Contains("review", response.RecipeHandlingSummary, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 2: Implement helper in each projector**

Use the existing placement as source of truth:

```csharp
private static string BuildRecipeHandlingSummary(
    string sheetKind,
    AdjustedSitePlanPlacementDto canonicalPlacement)
{
    var recipe = AdjustmentRecipeSummaryDto.FromPlacement(canonicalPlacement);
    if (recipe.Operations.Count == 0)
    {
        return $"{sheetKind}: affine placement applied; no local compression operations.";
    }

    var operations = string.Join(
        ", ",
        recipe.Operations.Select(operation =>
            $"{operation.Kind} {operation.Edge} @{operation.Coordinate} delta {operation.DeltaSourceUnits}"));

    return $"{sheetKind}: affine placement applied; local recipe requires review before DXF deformation: {operations}.";
}
```

- [ ] **Step 3: Pass summary into `SheetAdjustmentProjection`**

Electrical example:

```csharp
var projection = new SheetAdjustmentProjection(
    Guid.NewGuid(),
    registration.PlanSetVersionId,
    registration.DependentSheetId,
    registration.Id,
    request.CanonicalAdjustmentId,
    SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity,
    ComposeTransform(registration.Transform, request.CanonicalPlacement),
    registration.Confidence,
    status,
    warning,
    compressionStepCount,
    clock.UtcNow,
    ruleSummary: null,
    recipeHandlingSummary: BuildRecipeHandlingSummary("ElectricalPlan", request.CanonicalPlacement));
```

Roof/facade keep their existing `ruleSummary: registration.RuleSummary` and add `recipeHandlingSummary`.

- [ ] **Step 4: Include in DTO mapper**

```csharp
projection.RuleSummary,
projection.RecipeHandlingSummary);
```

- [ ] **Step 5: Include in audit event payload**

Add:

```csharp
recipeHandlingSummary = projection.RecipeHandlingSummary
```

**Acceptance:** projection explicitly consumes canonical recipe info and reports review-required local operations.

---

## Task 4: Persist projection recipe handling summary

**Files:**
- Modify: `SqliteSchemaInitializer.cs`
- Modify: `SqliteSheetAdjustmentProjectionRepository.cs`

- [ ] **Step 1: Add schema column**

In `EnsureSheetAdjustmentProjectionsSchema`:

```csharp
EnsureColumnExists(connection, "sheet_adjustment_projections", "recipe_handling_summary", "TEXT NULL");
```

- [ ] **Step 2: Insert/load column**

Add `recipe_handling_summary` to INSERT, parameter:

```csharp
command.Parameters.AddWithValue("$recipe_handling_summary", (object?)projection.RecipeHandlingSummary ?? DBNull.Value);
```

Update SELECT column list and `MapProjection` indices carefully. Safer path: append the column at the end of every SELECT and pass:

```csharp
reader.IsDBNull(recipeHandlingIndex) ? null : reader.GetString(recipeHandlingIndex)
```

as the final constructor argument.

**Acceptance:** persisted projections round-trip recipe handling text.

---

## Task 5: Carry recipe handling into export audit/UI

**Files:**
- Modify: `PlanSetExportedSheet.cs`
- Modify: `ExportedPlanSheetDto.cs`
- Modify: `CreateMultiSheetExportAuditHandler.cs`
- Modify: `SitePlanAdjustmentViewModel.cs`
- Test: `CreateMultiSheetExportAuditHandlerTests.cs`

- [ ] **Step 1: Extend exported sheet domain/DTO**

Add optional `recipeHandlingSummary` beside `ruleSummary`.

- [ ] **Step 2: Copy projection summary into exported sheet**

In `BuildProjectedSheet`:

```csharp
recipeHandlingSummary: projection.RecipeHandlingSummary
```

- [ ] **Step 3: Show it in Desktop audit line**

In `BuildPlanSetExportAuditLines`:

```csharp
var recipe = string.IsNullOrWhiteSpace(sheet.RecipeHandlingSummary)
    ? string.Empty
    : $" - recipe: {sheet.RecipeHandlingSummary}";
return $"{sheet.SheetKind}: {sheet.Status}{confidence}{warning}{recipe}{path}";
```

- [ ] **Step 4: Add export audit assertion**

Create manual projection with `recipeHandlingSummary: "ElectricalPlan: affine placement applied; local recipe requires review..."` and assert response sheet carries it.

**Acceptance:** manifest/audit/UI says what happened with recipe handling.

---

## Task 6: Verification checklist

**Do not run `dotnet build`.** Repo forbids builds.

- [ ] Static search check:

```powershell
rg -n "RecipeHandlingSummary|AdjustmentRecipeSummaryDto|recipe_handling_summary|HorizontalCompression|VerticalCompression" src tests
```

Expected: hits in contracts, projectors, persistence, audit/UI, tests.

- [ ] Diff check:

```powershell
git diff --check
```

Expected: no whitespace errors except existing line-ending noise if present.

- [ ] User runtime smoke after app recompiles via existing `dotnet watch`:
  1. Open SEMINOLE curated FloorPlan.
  2. Ensure SEMINOLE ElectricalPlan is related/registered.
  3. Apply patio/porch/living compression.
  4. Export HousePlanSet.
  5. Confirm audit line says recipe local compression requires review for ElectricalPlan.
  6. Confirm FloorPlan DXF opens.
  7. Confirm ElectricalPlan DXF opens and no garabato regression.

---

## No-go decisions

- Do not deform ElectricalPlan local DXF geometry in this slice.
- Do not touch roof/facade deep geometry logic.
- Do not add a new table for recipes yet.
- Do not create a second fit engine.
- Do not rewrite `IxMiliaAdjustedSitePlanExporter`.

## Risks

- Adding ctor params to domain records touches many tests/callers. Keep final args optional to reduce blast radius.
- SQLite SELECT index drift can break loading. Append columns and update mapper deliberately.
- UI line can get long. Accept for now; rich UI can come later.

## Completion for Slice 1

The slice is complete when:

```text
FloorPlan adjustment
-> existing placement_json contains compression recipe payload
-> projection derives recipe summary from placement
-> dependent projection stores recipe handling summary
-> export audit/UI surfaces applied affine vs local compression review
```

No claim of full electrical mimicry yet.
