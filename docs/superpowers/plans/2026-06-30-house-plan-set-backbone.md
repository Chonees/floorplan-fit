# House Plan Set Backbone Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Phase 1 of the HousePlanSet architecture by exposing existing floor-plan library items as one-sheet House Plan Sets without moving current floor-plan behavior.

**Architecture:** This is a thin compatibility backbone. Existing `FloorPlanTemplate` / `FloorPlanVersion` remain the persisted source; a new PlanSets application/read-model surface projects them as `HousePlanSet` summaries with a canonical `FloorPlan` sheet. No schema migration, UI rewrite, or dependent-sheet import is included in this phase.

**Tech Stack:** C#/.NET 10, xUnit, existing Application/Contracts/Domain projects. No build command; use targeted `dotnet test --no-restore` only.

---

## Scope Boundary

This plan implements **Phase 1 - Conceptual PlanSet Backbone** from `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md`.

It intentionally does less:

- no SQLite schema changes
- no electrical/roof/facade import
- no sheet registration engine
- no projection/export changes
- no Desktop UI changes

The point is to create the first real product seam: current floor plans can be read as `HousePlanSet` summaries where the current floor-plan version is the canonical sheet.

## File Structure

Create:

- `src/FloorplanFit.Domain/PlanSets/PlanSheetType.cs` - domain vocabulary for sheet types.
- `src/FloorplanFit.Contracts/PlanSets/PlanSetSheetDto.cs` - flat sheet DTO for plan-set read models.
- `src/FloorplanFit.Contracts/PlanSets/PlanSetLibraryItemDto.cs` - flat plan-set summary DTO.
- `src/FloorplanFit.Application/PlanSets/Library/GetPlanSetLibraryHandler.cs` - application handler that projects existing floor-plan library rows.
- `tests/FloorplanFit.Application.Tests/PlanSets/Library/GetPlanSetLibraryHandlerTests.cs` - tests for the compatibility projection.

No existing files need modification for this first slice unless formatting or namespace imports require it.

---

### Task 1: Add PlanSet sheet vocabulary

**Files:**
- Create: `src/FloorplanFit.Domain/PlanSets/PlanSheetType.cs`

- [ ] **Step 1: Create the domain folder**

Run:

```powershell
New-Item -ItemType Directory -Force src\FloorplanFit.Domain\PlanSets
```

Expected: directory exists.

- [ ] **Step 2: Add the enum**

Create `src/FloorplanFit.Domain/PlanSets/PlanSheetType.cs`:

```csharp
namespace FloorplanFit.Domain.PlanSets;

public enum PlanSheetType
{
    Unknown = 0,
    FloorPlan = 1,
    ElectricalPlan = 2,
    RoofPlan = 3,
    FacadeElevation = 4
}
```

- [ ] **Step 3: Commit**

```powershell
git add -- src/FloorplanFit.Domain/PlanSets/PlanSheetType.cs
git commit -m "feat: add plan sheet type vocabulary"
```

---

### Task 2: Add PlanSet contracts

**Files:**
- Create: `src/FloorplanFit.Contracts/PlanSets/PlanSetSheetDto.cs`
- Create: `src/FloorplanFit.Contracts/PlanSets/PlanSetLibraryItemDto.cs`

- [ ] **Step 1: Create the contracts folder**

Run:

```powershell
New-Item -ItemType Directory -Force src\FloorplanFit.Contracts\PlanSets
```

Expected: directory exists.

- [ ] **Step 2: Add sheet DTO**

Create `src/FloorplanFit.Contracts/PlanSets/PlanSetSheetDto.cs`:

```csharp
namespace FloorplanFit.Contracts.PlanSets;

public sealed record PlanSetSheetDto(
    Guid SheetId,
    string SheetType,
    string Name,
    Guid ImportedDocumentId,
    Guid? SourceFloorPlanVersionId,
    bool IsCanonical,
    string RegistrationStatus,
    string ProjectionStatus);
```

Why strings instead of the domain enum: `Contracts` currently stays flat and independent from `Domain`.

- [ ] **Step 3: Add plan-set summary DTO**

Create `src/FloorplanFit.Contracts/PlanSets/PlanSetLibraryItemDto.cs`:

```csharp
namespace FloorplanFit.Contracts.PlanSets;

public sealed record PlanSetLibraryItemDto(
    Guid HousePlanSetId,
    string Code,
    string Name,
    Guid? ActivePlanSetVersionId,
    Guid? CanonicalFloorPlanVersionId,
    Guid? ActivePublishedCurationId,
    IReadOnlyList<PlanSetSheetDto> Sheets)
{
    public bool HasCanonicalFloorPlan => CanonicalFloorPlanVersionId.HasValue;

    public bool CanProduceCanonicalAdjustment =>
        CanonicalFloorPlanVersionId.HasValue && ActivePublishedCurationId.HasValue;
}
```

- [ ] **Step 4: Commit**

```powershell
git add -- src/FloorplanFit.Contracts/PlanSets/PlanSetSheetDto.cs src/FloorplanFit.Contracts/PlanSets/PlanSetLibraryItemDto.cs
git commit -m "feat: add plan set read contracts"
```

---

### Task 3: Write failing PlanSet library tests

**Files:**
- Create: `tests/FloorplanFit.Application.Tests/PlanSets/Library/GetPlanSetLibraryHandlerTests.cs`

- [ ] **Step 1: Create test folder**

Run:

```powershell
New-Item -ItemType Directory -Force tests\FloorplanFit.Application.Tests\PlanSets\Library
```

Expected: directory exists.

- [ ] **Step 2: Add failing tests**

Create `tests/FloorplanFit.Application.Tests/PlanSets/Library/GetPlanSetLibraryHandlerTests.cs`:

```csharp
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Library;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.PlanSets.Library;

public sealed class GetPlanSetLibraryHandlerTests
{
    [Fact]
    public async Task HandleAsync_projects_current_floor_plan_as_canonical_plan_set_sheet()
    {
        var templateId = Guid.NewGuid();
        var currentVersionId = Guid.NewGuid();
        var activePublishedCurationId = Guid.NewGuid();
        var importedAtUtc = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        var item = new FloorPlanLibraryItemDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            1,
            currentVersionId,
            1,
            [
                new FloorPlanLibraryVersionDto(
                    currentVersionId,
                    1,
                    "Published",
                    importedAtUtc,
                    "inch",
                    IsCurrent: true,
                    activePublishedCurationId)
            ]);

        var handler = new GetPlanSetLibraryHandler(new FakeFloorPlanLibraryReader([item]));

        var result = await handler.HandleAsync(CancellationToken.None);

        var planSet = Assert.Single(result);
        Assert.Equal(templateId, planSet.HousePlanSetId);
        Assert.Equal("seminole2000", planSet.Code);
        Assert.Equal("SEMINOLE2000", planSet.Name);
        Assert.Equal(currentVersionId, planSet.ActivePlanSetVersionId);
        Assert.Equal(currentVersionId, planSet.CanonicalFloorPlanVersionId);
        Assert.Equal(activePublishedCurationId, planSet.ActivePublishedCurationId);
        Assert.True(planSet.HasCanonicalFloorPlan);
        Assert.True(planSet.CanProduceCanonicalAdjustment);

        var sheet = Assert.Single(planSet.Sheets);
        Assert.Equal(currentVersionId, sheet.SheetId);
        Assert.Equal("FloorPlan", sheet.SheetType);
        Assert.Equal("SEMINOLE2000 Floor Plan", sheet.Name);
        Assert.Equal(currentVersionId, sheet.SourceFloorPlanVersionId);
        Assert.True(sheet.IsCanonical);
        Assert.Equal("Canonical", sheet.RegistrationStatus);
        Assert.Equal("CanonicalSource", sheet.ProjectionStatus);
    }

    [Fact]
    public async Task HandleAsync_keeps_plan_set_visible_when_floor_plan_has_no_current_version()
    {
        var templateId = Guid.NewGuid();
        var item = new FloorPlanLibraryItemDto(
            templateId,
            "empty-template",
            "Empty Template",
            0,
            null,
            null,
            []);

        var handler = new GetPlanSetLibraryHandler(new FakeFloorPlanLibraryReader([item]));

        var result = await handler.HandleAsync(CancellationToken.None);

        var planSet = Assert.Single(result);
        Assert.Equal(templateId, planSet.HousePlanSetId);
        Assert.Null(planSet.ActivePlanSetVersionId);
        Assert.Null(planSet.CanonicalFloorPlanVersionId);
        Assert.False(planSet.HasCanonicalFloorPlan);
        Assert.False(planSet.CanProduceCanonicalAdjustment);
        Assert.Empty(planSet.Sheets);
    }

    private sealed class FakeFloorPlanLibraryReader : IFloorPlanLibraryReader
    {
        private readonly IReadOnlyList<FloorPlanLibraryItemDto> items;

        public FakeFloorPlanLibraryReader(IReadOnlyList<FloorPlanLibraryItemDto> items)
        {
            this.items = items;
        }

        public Task<IReadOnlyList<FloorPlanLibraryItemDto>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(items);
        }
    }
}
```

- [ ] **Step 3: Run test to verify RED**

Run:

```powershell
dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --no-restore --filter FullyQualifiedName~PlanSets.Library.GetPlanSetLibraryHandlerTests
```

Expected: FAIL because `FloorplanFit.Application.PlanSets.Library.GetPlanSetLibraryHandler` does not exist.

---

### Task 4: Implement minimal PlanSet library projection

**Files:**
- Create: `src/FloorplanFit.Application/PlanSets/Library/GetPlanSetLibraryHandler.cs`

- [ ] **Step 1: Create application folder**

Run:

```powershell
New-Item -ItemType Directory -Force src\FloorplanFit.Application\PlanSets\Library
```

Expected: directory exists.

- [ ] **Step 2: Add handler**

Create `src/FloorplanFit.Application/PlanSets/Library/GetPlanSetLibraryHandler.cs`:

```csharp
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;

namespace FloorplanFit.Application.PlanSets.Library;

public sealed class GetPlanSetLibraryHandler
{
    private const string FloorPlanSheetType = "FloorPlan";
    private const string CanonicalRegistrationStatus = "Canonical";
    private const string CanonicalProjectionStatus = "CanonicalSource";

    private readonly IFloorPlanLibraryReader floorPlanLibraryReader;

    public GetPlanSetLibraryHandler(IFloorPlanLibraryReader floorPlanLibraryReader)
    {
        this.floorPlanLibraryReader = floorPlanLibraryReader;
    }

    public async Task<IReadOnlyList<PlanSetLibraryItemDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var floorPlans = await floorPlanLibraryReader.ListAsync(cancellationToken);
        return floorPlans.Select(Project).ToArray();
    }

    private static PlanSetLibraryItemDto Project(FloorPlanLibraryItemDto floorPlan)
    {
        var currentVersion = floorPlan.CurrentVersion;
        var canonicalVersionId = floorPlan.CurrentVersionId ?? currentVersion?.VersionId;
        var sheets = canonicalVersionId.HasValue
            ? new[]
            {
                new PlanSetSheetDto(
                    canonicalVersionId.Value,
                    FloorPlanSheetType,
                    $"{floorPlan.Name} Floor Plan",
                    Guid.Empty,
                    canonicalVersionId.Value,
                    IsCanonical: true,
                    CanonicalRegistrationStatus,
                    CanonicalProjectionStatus)
            }
            : [];

        return new PlanSetLibraryItemDto(
            floorPlan.TemplateId,
            floorPlan.Code,
            floorPlan.Name,
            canonicalVersionId,
            canonicalVersionId,
            floorPlan.ActivePublishedCurationId,
            sheets);
    }
}
```

`Guid.Empty` is deliberate in Phase 1 because the existing floor-plan library DTO does not expose imported document id. Do not add a repository just to fill that field yet.

- [ ] **Step 3: Run focused tests to verify GREEN**

Run:

```powershell
dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --no-restore --filter FullyQualifiedName~PlanSets.Library.GetPlanSetLibraryHandlerTests
```

Expected: PASS for the two PlanSet library tests.

- [ ] **Step 4: Commit**

```powershell
git add -- src/FloorplanFit.Application/PlanSets/Library/GetPlanSetLibraryHandler.cs tests/FloorplanFit.Application.Tests/PlanSets/Library/GetPlanSetLibraryHandlerTests.cs
git commit -m "feat: expose floor plans as plan sets"
```

---

### Task 5: Document the Phase 1 bridge in the architecture spec

**Files:**
- Modify: `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md`

- [ ] **Step 1: Add Phase 1 evidence note**

Append this under `## Phase 1 - Conceptual PlanSet Backbone` after the `Scope` list:

```markdown
### Phase 1 implementation bridge

The first implementation slice exposes existing `FloorPlanLibraryItemDto` rows as `PlanSetLibraryItemDto` rows. This is intentionally a read-model bridge:

- `HousePlanSetId` maps to the existing `FloorPlanTemplate.TemplateId`.
- `ActivePlanSetVersionId` maps to the current `FloorPlanVersion` id.
- The only sheet is a canonical `FloorPlan` sheet.
- Dependent electrical, roof, and facade/elevation sheets are not persisted in Phase 1.

This keeps the current app working while giving future PlanSet features a stable product vocabulary.
```

- [ ] **Step 2: Run diff check for touched docs/code**

Run:

```powershell
git diff --check -- src/FloorplanFit.Domain/PlanSets/PlanSheetType.cs src/FloorplanFit.Contracts/PlanSets/PlanSetSheetDto.cs src/FloorplanFit.Contracts/PlanSets/PlanSetLibraryItemDto.cs src/FloorplanFit.Application/PlanSets/Library/GetPlanSetLibraryHandler.cs tests/FloorplanFit.Application.Tests/PlanSets/Library/GetPlanSetLibraryHandlerTests.cs docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md
```

Expected: no output.

- [ ] **Step 3: Commit**

```powershell
git add -- docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md
git commit -m "docs: record plan set phase one bridge"
```

---

## Completion Checklist

- [ ] Existing floor-plan import/review/adjustment files are untouched.
- [ ] New PlanSets read-model exists in Contracts/Application.
- [ ] Current floor-plan library can be projected as one-sheet HousePlanSets.
- [ ] The canonical floor-plan sheet is explicit.
- [ ] There is no independent fit engine for dependent sheets.
- [ ] Focused PlanSet Application tests pass.
- [ ] No `dotnet build` command was run.

## Self-Review Notes

Spec coverage:

- Covers Phase 1 only, as requested by the incremental design.
- Establishes `HousePlanSet` read vocabulary without database migration.
- Leaves Phase 2+ modules for later plans.

Red-flag scan:

- No empty implementation markers are present.
- The only intentionally empty value is `Guid.Empty` for imported document id in the Phase 1 projection because the existing floor-plan library DTO does not expose that id.

Type consistency:

- Tests reference `GetPlanSetLibraryHandler`, `PlanSetLibraryItemDto`, and `PlanSetSheetDto` exactly as created in later tasks.
- Contracts use strings for sheet type/status to keep `Contracts` independent from `Domain`.
