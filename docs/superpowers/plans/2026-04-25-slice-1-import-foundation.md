# Slice 1 Import Foundation Implementation Plan

> **Historical note (synced 2026-04-29):** este plan describe la etapa de foundation previa al slice ejecutable. Ya NO es la fuente principal del estado actual del repo. La verdad mas nueva vive en:
>
> - `docs/explicacion del proyecto/2026-04-29 - explicacion exhaustiva de archivos del slice 1 ejecutable.md`
> - `docs/superpowers/plans/2026-04-29-slice-1-executable-implementation.md` (como plan historico del slice ejecutable)
> - `obsidian-vault/Current State.md`

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the first real Loop 1 slice that turns `SANTA-BARBARA.dxf` into a persisted floor plan entry with measurement context and version metadata.

**Architecture:** Keep the first slice intentionally thin: contracts define inputs/outputs, domain holds the main business objects, application coordinates the import flow, and infrastructure is only scaffolded enough to receive concrete adapters later.

**Tech Stack:** C# / .NET 10, SDK-style projects, xUnit, future SQLite + IxMilia.Dxf integration.

---

## Progress Sync (2026-04-29)

This plan was synchronized against the actual repository state on **2026-04-29**.

What is verified right now:

- The solution skeleton and project files already exist.
- The first import use-case code already exists.
- The first application-level happy-path test file already exists.
- Concrete DXF/SQLite/filesystem adapters now **do** exist as part of the executable slice.
- Local compile/test verification is **no longer blocked**: the machine now has `.NET SDK 10.0.100`, tests were executed, and Desktop build was verified later in the same day.

What is **not** verifiable from the repository history:

- Whether the test was written in a true RED-first sequence before the implementation.
- Any historical build/test execution that may have happened outside the current repository evidence.

---

## Blocking Environment Note

This note is historical only. The environment blocker described here was resolved later on **2026-04-29** by installing `.NET SDK 10.0.100` and disabling Smart App Control temporarily for validation.

### Task 1: Create the solution skeleton

**Files:**
- Create: `FloorplanFit.sln`
- Create: `global.json`
- Create: `Directory.Build.props`
- Create: `src/FloorplanFit.Contracts/FloorplanFit.Contracts.csproj`
- Create: `src/FloorplanFit.Domain/FloorplanFit.Domain.csproj`
- Create: `src/FloorplanFit.Application/FloorplanFit.Application.csproj`
- Create: `src/FloorplanFit.Infrastructure/FloorplanFit.Infrastructure.csproj`
- Create: `tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj`

- [x] **Step 1: Add root SDK/config files**

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
```

- [x] **Step 2: Add the four source projects and the first test project**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup />
</Project>
```

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\..\src\FloorplanFit.Application\FloorplanFit.Application.csproj" />
    <ProjectReference Include="..\..\src\FloorplanFit.Contracts\FloorplanFit.Contracts.csproj" />
    <ProjectReference Include="..\..\src\FloorplanFit.Domain\FloorplanFit.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

- [x] **Step 3: Create the solution file and add all five projects**

```text
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "FloorplanFit.Contracts", "src\FloorplanFit.Contracts\FloorplanFit.Contracts.csproj", "{E2D99A26-9B35-4D6E-A69F-64F1E2A0F101}"
```

- [x] **Step 4: Keep build commands skipped in this environment**

Because of the repository rule and the missing SDK, keep `dotnet build` and `dotnet test` skipped for now.

### Task 2: Write the first application-level import test

**Files:**
- Test: `tests/FloorplanFit.Application.Tests/FloorPlans/Import/ImportFloorPlanHandlerTests.cs`

- [x] **Step 1: Add the first application-level happy-path import test file**

```csharp
[Fact]
public async Task HandleAsync_creates_imported_floor_plan_and_returns_library_item()
{
    // arrange fake gateway + fake repositories
    // act
    // assert library item code/name/status/version and persisted in-memory entities
}
```

- [x] **Step 2: Record the verification note**

We still cannot run this test locally because the machine does not have a .NET SDK.

- [ ] **Historical RED-first proof remains unavailable**

The repository shows the test file exists, but it does not preserve proof that the test was executed in a failing RED state before the implementation.

### Task 3: Add the minimum contracts and business objects

**Files:**
- Create: `src/FloorplanFit.Contracts/FloorPlans/ImportFloorPlanRequest.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/ImportFloorPlanResponse.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/FloorPlanLibraryItemDto.cs`
- Create: `src/FloorplanFit.Domain/Measurement/LengthUnit.cs`
- Create: `src/FloorplanFit.Domain/Measurement/MeasurementContext.cs`
- Create: `src/FloorplanFit.Domain/Documents/ImportedDocumentType.cs`
- Create: `src/FloorplanFit.Domain/Documents/ImportedDocument.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/FloorPlanTemplate.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/FloorPlanVersion.cs`

- [x] **Step 1: Add the request/response DTOs**
- [x] **Step 2: Add the measurement object**
- [x] **Step 3: Add the imported document object**
- [x] **Step 4: Add the floor plan main objects**

### Task 4: Implement the use case with abstractions only

**Files:**
- Create: `src/FloorplanFit.Application/Abstractions/DetectedFloorPlanDocument.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IDxfGateway.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IFloorPlanTemplateRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IFloorPlanVersionRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IImportedDocumentRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IMeasurementContextRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IUnitOfWork.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IFileHashService.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IClock.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Import/FloorPlanCodeNormalizer.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Import/ImportFloorPlanResultFactory.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Import/ImportFloorPlanHandler.cs`

- [x] **Step 1: Add the DXF/document abstraction**
- [x] **Step 2: Add repository, clock, hash, and transaction interfaces**
- [x] **Step 3: Implement code normalization**
- [x] **Step 4: Implement the import handler**
- [x] **Step 5: Wire the response factory**

### Task 5: Prepare infrastructure for the next slice

**Files:**
- Create: `src/FloorplanFit.Infrastructure/InfrastructureAssemblyMarker.cs`

- [x] **Step 1: Add an infrastructure marker file**
- [x] **Step 2: Keep concrete SQLite and DXF adapters for the next implementation pass**
