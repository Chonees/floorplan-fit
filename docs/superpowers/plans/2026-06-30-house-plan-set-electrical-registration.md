# HousePlanSet Electrical Registration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Phase 3's first useful slice: store a whole-sheet similarity registration for an electrical sheet against the canonical floor-plan version.

**Architecture:** Keep the current bridge: `PlanSetVersionId` is still the current `FloorPlanVersion.Id`. Registration records relate one dependent electrical `PlanSheet` to that canonical floor-plan version and store method, transform, confidence, warning, and confirmation status. No projection, no export, no separate electrical fit engine.

**Tech Stack:** C#/.NET 10, existing Application/Domain/Contracts/Infrastructure projects, SQLite via `Microsoft.Data.Sqlite`, xUnit-style tests. No `dotnet build` after changes.

---

## Scope Boundary

In scope:

- domain registration model
- electrical whole-sheet similarity registration use case
- SQLite persistence for registration records
- Desktop DI wiring for the handler/repository
- tests proving electrical-only registration and no independent fit engine

Out of scope:

- automatic anchor extraction
- projection of canonical adjustment to electrical
- roof/facade registration
- export/audit package generation
- Desktop UI behavior

## File Structure

Create:

- `src/FloorplanFit.Domain/PlanSets/SheetRegistrationMethod.cs` - registration method vocabulary.
- `src/FloorplanFit.Domain/PlanSets/SheetRegistrationStatus.cs` - confirmation status vocabulary.
- `src/FloorplanFit.Domain/PlanSets/SheetRegistrationTransform.cs` - whole-sheet similarity transform.
- `src/FloorplanFit.Domain/PlanSets/SheetRegistration.cs` - registration aggregate.
- `src/FloorplanFit.Contracts/PlanSets/RegisterElectricalSheetRequest.cs` - request contract.
- `src/FloorplanFit.Contracts/PlanSets/SheetRegistrationDto.cs` - response/read DTO.
- `src/FloorplanFit.Contracts/PlanSets/SheetRegistrationTransformDto.cs` - transform DTO.
- `src/FloorplanFit.Application/Abstractions/ISheetRegistrationRepository.cs` - persistence port.
- `src/FloorplanFit.Application/PlanSets/Registration/RegisterElectricalSheetHandler.cs` - use case.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSheetRegistrationRepository.cs` - SQLite repository.
- `tests/FloorplanFit.Application.Tests/PlanSets/Registration/RegisterElectricalSheetHandlerTests.cs` - app tests.

Modify:

- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs` - create/migrate `sheet_registrations`.
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs` - register repository and handler.
- `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md` - record Phase 3 bridge after implementation.

---

### Task 1: Write RED tests for electrical registration

**Files:**
- Create: `tests/FloorplanFit.Application.Tests/PlanSets/Registration/RegisterElectricalSheetHandlerTests.cs`

- [ ] **Step 1: Create test folder**

```powershell
New-Item -ItemType Directory -Force tests\FloorplanFit.Application.Tests\PlanSets\Registration
```

- [ ] **Step 2: Write tests**

The tests must prove:

1. An electrical dependent sheet can be registered with a whole-sheet similarity transform, confidence, warning, and pending confirmation.
2. A non-electrical sheet is rejected by the electrical registration handler.

- [ ] **Step 3: Verify RED without compiling**

```powershell
Test-Path src\FloorplanFit.Application\PlanSets\Registration\RegisterElectricalSheetHandler.cs
Test-Path src\FloorplanFit.Application\Abstractions\ISheetRegistrationRepository.cs
```

Expected:

```text
False
False
```

Do not run `dotnet build`.

### Task 2: Add registration domain/contracts/application

**Files:**
- Create: domain registration files listed above.
- Create: contract files listed above.
- Create: `src/FloorplanFit.Application/Abstractions/ISheetRegistrationRepository.cs`
- Create: `src/FloorplanFit.Application/PlanSets/Registration/RegisterElectricalSheetHandler.cs`

- [ ] **Step 1: Add domain model**

Rules:

- `SheetRegistrationMethod.WholeSheetSimilarity = 1`.
- `SheetRegistrationStatus.PendingConfirmation = 1`, `Confirmed = 2`, `Rejected = 3`.
- `SheetRegistrationTransform.Scale` must be greater than zero.
- `SheetRegistration.Confidence` must be between `0m` and `1m`.
- `Confirmed` registration must have `ConfirmedAtUtc`.
- Pending/rejected registration must not have `ConfirmedAtUtc`.

- [ ] **Step 2: Add request/DTOs**

`RegisterElectricalSheetRequest` accepts `PlanSetVersionId`, `ElectricalSheetId`, transform values, `Confidence`, `ConfirmRegistration`, and optional `Warning`.

- [ ] **Step 3: Add handler**

Handler flow:

1. Load dependent sheet using `IPlanSheetRepository.GetByIdAsync`.
2. Reject missing sheet.
3. Reject sheet not belonging to the requested `PlanSetVersionId`.
4. Reject sheet type other than `ElectricalPlan`.
5. Create `SheetRegistration` using `PlanSetVersionId` as the current canonical floor-plan version id bridge.
6. Save via `ISheetRegistrationRepository` and `IUnitOfWork`.
7. Return `SheetRegistrationDto`.

- [ ] **Step 4: Verify diff**

```powershell
git diff --check -- src/FloorplanFit.Domain/PlanSets src/FloorplanFit.Contracts/PlanSets src/FloorplanFit.Application/Abstractions src/FloorplanFit.Application/PlanSets/Registration tests/FloorplanFit.Application.Tests/PlanSets/Registration
```

### Task 3: Add SQLite persistence and DI

**Files:**
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteSheetRegistrationRepository.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- Modify: `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`

- [ ] **Step 1: Add schema**

Create `sheet_registrations`:

```sql
CREATE TABLE IF NOT EXISTS sheet_registrations (
    id TEXT PRIMARY KEY,
    plan_set_version_id TEXT NOT NULL,
    dependent_sheet_id TEXT NOT NULL,
    canonical_floor_plan_version_id TEXT NOT NULL,
    method TEXT NOT NULL,
    transform_json TEXT NOT NULL,
    confidence TEXT NOT NULL,
    status TEXT NOT NULL,
    warning TEXT NULL,
    created_at_utc TEXT NOT NULL,
    confirmed_at_utc TEXT NULL
);
```

Call an idempotent `EnsureSheetRegistrationsSchema(connection)` after the initial schema command.

- [ ] **Step 2: Add repository**

Repository implements `ISheetRegistrationRepository` with `AddAsync` and `GetByIdAsync`.

- [ ] **Step 3: Wire DI**

Register:

```csharp
services.AddScoped<ISheetRegistrationRepository, SqliteSheetRegistrationRepository>();
services.AddScoped<RegisterElectricalSheetHandler>();
```

- [ ] **Step 4: Verify and commit**

```powershell
git diff --check -- src/FloorplanFit.Infrastructure/Persistence/SqliteSheetRegistrationRepository.cs src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs
git add -- src/FloorplanFit.Domain/PlanSets/SheetRegistrationMethod.cs src/FloorplanFit.Domain/PlanSets/SheetRegistrationStatus.cs src/FloorplanFit.Domain/PlanSets/SheetRegistrationTransform.cs src/FloorplanFit.Domain/PlanSets/SheetRegistration.cs src/FloorplanFit.Contracts/PlanSets/RegisterElectricalSheetRequest.cs src/FloorplanFit.Contracts/PlanSets/SheetRegistrationDto.cs src/FloorplanFit.Contracts/PlanSets/SheetRegistrationTransformDto.cs src/FloorplanFit.Application/Abstractions/ISheetRegistrationRepository.cs src/FloorplanFit.Application/PlanSets/Registration/RegisterElectricalSheetHandler.cs src/FloorplanFit.Infrastructure/Persistence/SqliteSheetRegistrationRepository.cs src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs tests/FloorplanFit.Application.Tests/PlanSets/Registration/RegisterElectricalSheetHandlerTests.cs
git diff --cached --check
git commit -m "feat: register electrical plan sheets"
```

### Task 4: Document Phase 3 bridge

**Files:**
- Modify: `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md`

- [ ] **Step 1: Add Phase 3 implementation bridge**

Under `## Phase 3 - Electrical Registration`, record:

```markdown
### Phase 3 implementation bridge

The first Phase 3 implementation stores a whole-sheet similarity registration for electrical sheets in `sheet_registrations`. The current `PlanSetVersionId` still maps to the canonical `FloorPlanVersion.Id`, so `canonical_floor_plan_version_id` uses that same bridge until explicit `PlanSetVersion` records exist.

This phase records method, transform, confidence, warning, and confirmation status only. It does not project adjustments, export dependent sheets, or create an electrical fit engine.
```

- [ ] **Step 2: Verify and commit docs**

```powershell
git diff --check -- docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md
git add -- docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md docs/superpowers/plans/2026-06-30-house-plan-set-electrical-registration.md
git diff --cached --check
git commit -m "docs: plan electrical sheet registration"
```

## Completion Checklist

- [ ] Electrical sheet registration is stored.
- [ ] Registration captures method, transform, confidence, warning, and confirmation status.
- [ ] Non-electrical sheets cannot use electrical registration.
- [ ] No projection/export behavior is added.
- [ ] No independent fit engine is created.
- [ ] No `dotnet build` command is run.
