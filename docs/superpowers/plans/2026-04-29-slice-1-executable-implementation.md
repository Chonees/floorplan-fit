# Slice 1 Executable Implementation Plan

> **Historical note (synced 2026-04-29):** este archivo queda como plan de implementacion historico del slice. El cuerpo de tareas y snippets de abajo refleja el estado previo a la ejecucion y por eso puede mostrar pasos que hoy ya quedaron superados. La verdad actual del codigo/documentacion vive en:
>
> - `docs/explicacion del proyecto/2026-04-29 - explicacion exhaustiva de archivos del slice 1 ejecutable.md`
> - `docs/explicacion del proyecto/2026-04-29 - validacion real del slice 1 ejecutable.md`
> - `obsidian-vault/Current State.md`

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first executable Loop 1 path: thin Desktop import of `SANTA-BARBARA.dxf` into managed storage, real DXF metadata read, real SHA-256 hash, real SQLite persistence, and visible Library item output.

**Architecture:** Keep the slice thin but real. Desktop owns only the minimal import interaction and Library display, Application continues to orchestrate use-case flow, Domain stays limited to import entities and invariants, and Infrastructure provides the real DXF/filesystem/SQLite implementations. Read and hash the **managed copy**, not the original external file path.

**Tech Stack:** C# / .NET 10 SDK, Avalonia 11.3.14, CommunityToolkit.Mvvm 8.4.2, Microsoft.Extensions.Hosting 10.0.0, Microsoft.Data.Sqlite 10.0.6, IxMilia.Dxf 0.8.4, xUnit 2.9.2

---

## Important execution constraints

- Repository rule: **never build after changes**
- Historical machine state at plan time: **runtime present, no .NET SDK installed**
- This blocker was resolved later on **2026-04-29** by installing `.NET SDK 10.0.100`
- Verification commands below were eventually executed later the same day in a corrected environment

## Implementation progress checkpoint — 2026-04-29

### Authored and documented

- `FloorplanFit.Desktop` thin shell added and wired into the solution
- `FloorplanFit.Infrastructure.Tests` project added
- `IManagedFileStorage` introduced in Application
- `ImportFloorPlanHandler` reordered to `copy managed file -> read managed DXF -> hash managed DXF -> persist`
- Infrastructure tests authored first for:
  - managed file storage
  - real DXF metadata read
  - end-to-end SQLite import integration
- Infrastructure implementations authored for:
  - `AppWorkspace`
  - `ManagedFileStorage`
  - `Sha256FileHashService`
  - `IxMiliaDxfGateway`
  - `SqliteSession`
  - `SqliteSchemaInitializer`
  - `SqliteMeasurementContextRepository`
  - `SqliteImportedDocumentRepository`
  - `SqliteFloorPlanTemplateRepository`
  - `SqliteFloorPlanVersionRepository`
  - `SqliteUnitOfWork`

### Verification outcome later that same day

- `FloorplanFit.Application.Tests` passed: **4/4**
- `FloorplanFit.Infrastructure.Tests` passed: **6/6**
- `FloorplanFit.Desktop` build verification passed with `0 warnings / 0 errors` using `net10.0-verify`
- manual smoke test of Desktop import was completed
- remaining manual follow-up is to keep validating startup hydration from the **dev launcher** (`scripts/dev-desktop.bat`) instead of the stale `.exe` shortcut

---

## File map before implementation

### Root / solution

- `FloorplanFit.sln` — add `FloorplanFit.Desktop` and `FloorplanFit.Infrastructure.Tests`

### Desktop

- `src/FloorplanFit.Desktop/FloorplanFit.Desktop.csproj` — Avalonia executable project
- `src/FloorplanFit.Desktop/Program.cs` — composition root + host bootstrap
- `src/FloorplanFit.Desktop/App.axaml`
- `src/FloorplanFit.Desktop/App.axaml.cs`
- `src/FloorplanFit.Desktop/MainWindow.axaml`
- `src/FloorplanFit.Desktop/MainWindow.axaml.cs` — file picker plumbing only
- `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs` — import orchestration and visible Library state
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs` — DI registrations

### Application

- `src/FloorplanFit.Application/Abstractions/IManagedFileStorage.cs` — copy source DXF into app-managed storage
- `src/FloorplanFit.Application/FloorPlans/Import/ImportFloorPlanHandler.cs` — reorder flow to `copy -> read managed copy -> hash managed copy -> persist`

### Infrastructure

- `src/FloorplanFit.Infrastructure/Runtime/AppWorkspace.cs`
- `src/FloorplanFit.Infrastructure/Storage/ManagedFileStorage.cs`
- `src/FloorplanFit.Infrastructure/Security/Sha256FileHashService.cs`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaDxfGateway.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSession.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteUnitOfWork.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteMeasurementContextRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteImportedDocumentRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanTemplateRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanVersionRepository.cs`

### Tests

- `tests/FloorplanFit.Application.Tests/FloorPlans/Import/ImportFloorPlanHandlerTests.cs` — add failing managed-copy unit test
- `tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj`
- `tests/FloorplanFit.Infrastructure.Tests/Imports/ManagedFileStorageTests.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/IxMiliaDxfGatewayTests.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Imports/ImportFloorPlanIntegrationTests.cs`

### Durable docs

- `obsidian-vault/Implementation/2026-04-29 - Slice 1 Executable Kickoff.md`
- `obsidian-vault/Current State.md`

---

### Task 1: Add the missing Desktop and Infrastructure test projects

**Files:**
- Modify: `FloorplanFit.sln`
- Create: `src/FloorplanFit.Desktop/FloorplanFit.Desktop.csproj`
- Create: `src/FloorplanFit.Desktop/Program.cs`
- Create: `src/FloorplanFit.Desktop/App.axaml`
- Create: `src/FloorplanFit.Desktop/App.axaml.cs`
- Create: `src/FloorplanFit.Desktop/MainWindow.axaml`
- Create: `src/FloorplanFit.Desktop/MainWindow.axaml.cs`
- Create: `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`
- Create: `tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj`

- [ ] **Step 1: Add the Desktop executable project**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\FloorplanFit.Application\FloorplanFit.Application.csproj" />
    <ProjectReference Include="..\FloorplanFit.Contracts\FloorplanFit.Contracts.csproj" />
    <ProjectReference Include="..\FloorplanFit.Infrastructure\FloorplanFit.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.3.14" />
    <PackageReference Include="Avalonia.Desktop" Version="11.3.14" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.14" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="10.0.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Add the initial Avalonia bootstrap**

```csharp
// src/FloorplanFit.Desktop/Program.cs
using Avalonia;

namespace FloorplanFit.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace();
}
```

```xml
<!-- src/FloorplanFit.Desktop/App.axaml -->
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="FloorplanFit.Desktop.App">
  <Application.Styles>
    <FluentTheme />
  </Application.Styles>
</Application>
```

```csharp
// src/FloorplanFit.Desktop/App.axaml.cs
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace FloorplanFit.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 3: Add the Infrastructure test project**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\..\src\FloorplanFit.Application\FloorplanFit.Application.csproj" />
    <ProjectReference Include="..\..\src\FloorplanFit.Contracts\FloorplanFit.Contracts.csproj" />
    <ProjectReference Include="..\..\src\FloorplanFit.Domain\FloorplanFit.Domain.csproj" />
    <ProjectReference Include="..\..\src\FloorplanFit.Infrastructure\FloorplanFit.Infrastructure.csproj" />
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

- [ ] **Step 4: Wire both projects into the solution**

```text
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "FloorplanFit.Desktop", "src\FloorplanFit.Desktop\FloorplanFit.Desktop.csproj", "{A4E3A111-9D34-4E2B-A1D6-1D9C58A20106}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "FloorplanFit.Infrastructure.Tests", "tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj", "{B7F7B222-2E17-4DCC-9DA8-9A6F42A30107}"
EndProject
```

- [ ] **Step 5: Deferred verification (SDK environment only)**

```bash
dotnet sln FloorplanFit.sln list
```

Expected: `FloorplanFit.Desktop` and `FloorplanFit.Infrastructure.Tests` appear.

- [ ] **Step 6: Commit**

```bash
git add FloorplanFit.sln src/FloorplanFit.Desktop tests/FloorplanFit.Infrastructure.Tests
git commit -m "feat: add desktop and infrastructure test project shells"
```

---

### Task 2: Drive Application import through managed storage first

**Files:**
- Create: `src/FloorplanFit.Application/Abstractions/IManagedFileStorage.cs`
- Modify: `src/FloorplanFit.Application/FloorPlans/Import/ImportFloorPlanHandler.cs`
- Modify: `tests/FloorplanFit.Application.Tests/FloorPlans/Import/ImportFloorPlanHandlerTests.cs`

- [ ] **Step 1: Write the failing Application test for managed-copy orchestration**

```csharp
[Fact]
public async Task HandleAsync_reads_and_hashes_the_managed_copy_instead_of_the_external_source_path()
{
    var sourcePath = @"C:\imports\SANTA-BARBARA.dxf";
    var managedPath = @"C:\workspace\library\raw-dxf\SANTA-BARBARA.dxf";

    var dxfGateway = new RecordingDxfGateway(
        new DetectedFloorPlanDocument(
            "SANTA-BARBARA.dxf",
            "SANTA-BARBARA",
            LengthUnit.Inch,
            25.4m,
            "AC1032",
            "bbox:0,0,1633.6555599104756,1079.9999999999998"));

    var storage = new FakeManagedFileStorage(managedPath);
    var templateRepository = new InMemoryFloorPlanTemplateRepository();
    var versionRepository = new InMemoryFloorPlanVersionRepository();
    var documentRepository = new InMemoryImportedDocumentRepository();
    var measurementRepository = new InMemoryMeasurementContextRepository();
    var unitOfWork = new FakeUnitOfWork();
    var fileHashService = new RecordingFileHashService("abc123");
    var clock = new FakeClock(new DateTime(2026, 4, 29, 12, 0, 0, DateTimeKind.Utc));

    var handler = new ImportFloorPlanHandler(
        dxfGateway,
        storage,
        templateRepository,
        versionRepository,
        documentRepository,
        measurementRepository,
        unitOfWork,
        fileHashService,
        clock,
        new ImportFloorPlanResultFactory());

    await handler.HandleAsync(new ImportFloorPlanRequest(sourcePath), CancellationToken.None);

    Assert.Equal(sourcePath, storage.SourcePathReceived);
    Assert.Equal(managedPath, dxfGateway.FilePathReceived);
    Assert.Equal(managedPath, fileHashService.FilePathReceived);
    Assert.Equal(managedPath, Assert.Single(documentRepository.Items).StoragePath);
}
```

Helpers to add in the same test file:

```csharp
private sealed class FakeManagedFileStorage : IManagedFileStorage
{
    private readonly string managedPath;

    public FakeManagedFileStorage(string managedPath)
    {
        this.managedPath = managedPath;
    }

    public string? SourcePathReceived { get; private set; }

    public Task<string> CopyIntoLibraryAsync(string sourceFilePath, CancellationToken cancellationToken)
    {
        SourcePathReceived = sourceFilePath;
        return Task.FromResult(managedPath);
    }
}
```

```csharp
private sealed class RecordingDxfGateway : IDxfGateway
{
    private readonly DetectedFloorPlanDocument document;

    public RecordingDxfGateway(DetectedFloorPlanDocument document)
    {
        this.document = document;
    }

    public string? FilePathReceived { get; private set; }

    public Task<DetectedFloorPlanDocument> ReadFloorPlanAsync(string filePath, CancellationToken cancellationToken)
    {
        FilePathReceived = filePath;
        return Task.FromResult(document);
    }
}
```

```csharp
private sealed class RecordingFileHashService : IFileHashService
{
    private readonly string hash;

    public RecordingFileHashService(string hash)
    {
        this.hash = hash;
    }

    public string? FilePathReceived { get; private set; }

    public Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        FilePathReceived = filePath;
        return Task.FromResult(hash);
    }
}
```

- [ ] **Step 2: Deferred RED verification (SDK environment only)**

```bash
dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --filter HandleAsync_reads_and_hashes_the_managed_copy_instead_of_the_external_source_path
```

Expected: FAIL because `ImportFloorPlanHandler` still reads the external path directly.

- [ ] **Step 3: Add the new abstraction**

```csharp
namespace FloorplanFit.Application.Abstractions;

public interface IManagedFileStorage
{
    Task<string> CopyIntoLibraryAsync(string sourceFilePath, CancellationToken cancellationToken);
}
```

- [ ] **Step 4: Update `ImportFloorPlanHandler` to use managed storage first**

Constructor injection order:

```csharp
public ImportFloorPlanHandler(
    IDxfGateway dxfGateway,
    IManagedFileStorage managedFileStorage,
    IFloorPlanTemplateRepository floorPlanTemplateRepository,
    IFloorPlanVersionRepository floorPlanVersionRepository,
    IImportedDocumentRepository importedDocumentRepository,
    IMeasurementContextRepository measurementContextRepository,
    IUnitOfWork unitOfWork,
    IFileHashService fileHashService,
    IClock clock,
    ImportFloorPlanResultFactory resultFactory)
```

Core flow replacement:

```csharp
var managedFilePath = await managedFileStorage.CopyIntoLibraryAsync(request.FilePath, cancellationToken);
var detectedDocument = await dxfGateway.ReadFloorPlanAsync(managedFilePath, cancellationToken);
var importedAtUtc = clock.UtcNow;

var measurementContext = new MeasurementContext(
    Guid.NewGuid(),
    detectedDocument.SourceUnit,
    detectedDocument.ToMillimetersFactor,
    1m,
    0.5m,
    importedAtUtc);

var sha256 = await fileHashService.ComputeSha256Async(managedFilePath, cancellationToken);

var importedDocument = new ImportedDocument(
    Guid.NewGuid(),
    ImportedDocumentType.FloorPlanDxf,
    detectedDocument.OriginalFileName,
    managedFilePath,
    sha256,
    detectedDocument.DxfVersion,
    measurementContext.Id,
    importedAtUtc);
```

- [ ] **Step 5: Update existing constructor calls in tests**

```csharp
var storage = new FakeManagedFileStorage(@"C:\workspace\library\raw-dxf\SANTA-BARBARA.dxf");

var handler = new ImportFloorPlanHandler(
    dxfGateway,
    storage,
    templateRepository,
    versionRepository,
    documentRepository,
    measurementRepository,
    unitOfWork,
    fileHashService,
    clock,
    new ImportFloorPlanResultFactory());
```

- [ ] **Step 6: Deferred GREEN verification (SDK environment only)**

```bash
dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj
```

Expected: managed-copy orchestration test and original happy-path test both PASS.

- [ ] **Step 7: Commit**

```bash
git add src/FloorplanFit.Application/Abstractions/IManagedFileStorage.cs src/FloorplanFit.Application/FloorPlans/Import/ImportFloorPlanHandler.cs tests/FloorplanFit.Application.Tests/FloorPlans/Import/ImportFloorPlanHandlerTests.cs
git commit -m "feat: drive import from managed dxf copies"
```

---

### Task 3: Implement managed workspace paths, file copy, and SHA-256 hashing

**Files:**
- Create: `src/FloorplanFit.Infrastructure/Runtime/AppWorkspace.cs`
- Create: `src/FloorplanFit.Infrastructure/Storage/ManagedFileStorage.cs`
- Create: `src/FloorplanFit.Infrastructure/Security/Sha256FileHashService.cs`
- Modify: `src/FloorplanFit.Infrastructure/FloorplanFit.Infrastructure.csproj`
- Create: `tests/FloorplanFit.Infrastructure.Tests/Imports/ManagedFileStorageTests.cs`

- [ ] **Step 1: Write the failing managed-storage test**

```csharp
public sealed class ManagedFileStorageTests
{
    [Fact]
    public async Task CopyIntoLibraryAsync_copies_the_source_file_into_raw_dxf_folder()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var sourceFile = Path.Combine(tempRoot, "source.dxf");
        await File.WriteAllTextAsync(sourceFile, "sample dxf content");

        var workspace = new AppWorkspace(tempRoot);
        var storage = new ManagedFileStorage(workspace);

        var managedPath = await storage.CopyIntoLibraryAsync(sourceFile, CancellationToken.None);

        Assert.StartsWith(workspace.LibraryRawDxfDirectory, managedPath, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(managedPath));
        Assert.Equal("sample dxf content", await File.ReadAllTextAsync(managedPath));
    }
}
```

- [ ] **Step 2: Add Infrastructure package references**

```xml
<ItemGroup>
  <PackageReference Include="IxMilia.Dxf" Version="0.8.4" />
  <PackageReference Include="Microsoft.Data.Sqlite" Version="10.0.6" />
</ItemGroup>
```

- [ ] **Step 3: Implement workspace ownership**

```csharp
namespace FloorplanFit.Infrastructure.Runtime;

public sealed class AppWorkspace
{
    public AppWorkspace(string rootDirectory)
    {
        RootDirectory = rootDirectory;
        DatabasePath = Path.Combine(rootDirectory, "app.db");
        LibraryRawDxfDirectory = Path.Combine(rootDirectory, "library", "raw-dxf");
    }

    public string RootDirectory { get; }

    public string DatabasePath { get; }

    public string LibraryRawDxfDirectory { get; }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(LibraryRawDxfDirectory);
    }
}
```

- [ ] **Step 4: Implement real managed copy**

```csharp
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Infrastructure.Runtime;

namespace FloorplanFit.Infrastructure.Storage;

public sealed class ManagedFileStorage : IManagedFileStorage
{
    private readonly AppWorkspace workspace;

    public ManagedFileStorage(AppWorkspace workspace)
    {
        this.workspace = workspace;
    }

    public async Task<string> CopyIntoLibraryAsync(string sourceFilePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            throw new ArgumentException("Source file path is required.", nameof(sourceFilePath));
        }

        workspace.EnsureCreated();

        var destinationPath = Path.Combine(workspace.LibraryRawDxfDirectory, Path.GetFileName(sourceFilePath));

        await using var source = File.OpenRead(sourceFilePath);
        await using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination, cancellationToken);

        return destinationPath;
    }
}
```

- [ ] **Step 5: Implement real SHA-256 hashing**

```csharp
using System.Security.Cryptography;
using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Infrastructure.Security;

public sealed class Sha256FileHashService : IFileHashService
{
    public async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var bytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
```

- [ ] **Step 6: Deferred verification (SDK environment only)**

```bash
dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --filter CopyIntoLibraryAsync_copies_the_source_file_into_raw_dxf_folder
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add src/FloorplanFit.Infrastructure/Runtime/AppWorkspace.cs src/FloorplanFit.Infrastructure/Storage/ManagedFileStorage.cs src/FloorplanFit.Infrastructure/Security/Sha256FileHashService.cs src/FloorplanFit.Infrastructure/FloorplanFit.Infrastructure.csproj tests/FloorplanFit.Infrastructure.Tests/Imports/ManagedFileStorageTests.cs
git commit -m "feat: add managed file storage and hashing"
```

---

### Task 4: Read real DXF metadata and fingerprint from the managed copy

**Files:**
- Create: `src/FloorplanFit.Infrastructure/Dxf/IxMiliaDxfGateway.cs`
- Create: `tests/FloorplanFit.Infrastructure.Tests/Dxf/IxMiliaDxfGatewayTests.cs`

- [ ] **Step 1: Write the failing DXF gateway test**

```csharp
public sealed class IxMiliaDxfGatewayTests
{
    [Fact]
    public async Task ReadFloorPlanAsync_reads_real_metadata_from_santa_barbara_fixture()
    {
        var fixturePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf"));

        var gateway = new IxMiliaDxfGateway();

        var result = await gateway.ReadFloorPlanAsync(fixturePath, CancellationToken.None);

        Assert.Equal("SANTA-BARBARA.dxf", result.OriginalFileName);
        Assert.Equal("SANTA-BARBARA", result.SuggestedName);
        Assert.Equal(LengthUnit.Inch, result.SourceUnit);
        Assert.Equal(25.4m, result.ToMillimetersFactor);
        Assert.Equal("AC1032", result.DxfVersion);
        Assert.StartsWith("bbox:", result.GeometryFingerprint);
    }
}
```

- [ ] **Step 2: Implement `IxMiliaDxfGateway`**

```csharp
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.Measurement;
using IxMilia.Dxf;

namespace FloorplanFit.Infrastructure.Dxf;

public sealed class IxMiliaDxfGateway : IDxfGateway
{
    public async Task<DetectedFloorPlanDocument> ReadFloorPlanAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var dxf = await DxfFile.LoadAsync(stream, cancellationToken);

        var unit = MapLengthUnit(dxf.Header.InsUnits);
        var factor = unit switch
        {
            LengthUnit.Inch => 25.4m,
            LengthUnit.Foot => 304.8m,
            LengthUnit.Millimeter => 1m,
            LengthUnit.Centimeter => 10m,
            LengthUnit.Meter => 1000m,
            _ => throw new InvalidOperationException("Unsupported or unknown DXF unit.")
        };

        var extMin = dxf.Header.ExtMin;
        var extMax = dxf.Header.ExtMax;
        var fingerprint = $"bbox:{extMin.X},{extMin.Y},{extMax.X},{extMax.Y}";

        return new DetectedFloorPlanDocument(
            Path.GetFileName(filePath),
            Path.GetFileNameWithoutExtension(filePath),
            unit,
            factor,
            dxf.Header.Version.ToString(),
            fingerprint);
    }

    private static LengthUnit MapLengthUnit(DxfUnits units) => units switch
    {
        DxfUnits.Inches => LengthUnit.Inch,
        DxfUnits.Feet => LengthUnit.Foot,
        DxfUnits.Millimeters => LengthUnit.Millimeter,
        DxfUnits.Centimeters => LengthUnit.Centimeter,
        DxfUnits.Meters => LengthUnit.Meter,
        _ => LengthUnit.Unknown
    };
}
```

- [ ] **Step 3: Deferred verification (SDK environment only)**

```bash
dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --filter ReadFloorPlanAsync_reads_real_metadata_from_santa_barbara_fixture
```

Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add src/FloorplanFit.Infrastructure/Dxf/IxMiliaDxfGateway.cs tests/FloorplanFit.Infrastructure.Tests/Dxf/IxMiliaDxfGatewayTests.cs
git commit -m "feat: add ixmilia dxf gateway"
```

---

### Task 5: Persist the import into SQLite in one transaction

**Files:**
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteSession.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteUnitOfWork.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteMeasurementContextRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteImportedDocumentRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanTemplateRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanVersionRepository.cs`
- Create: `tests/FloorplanFit.Infrastructure.Tests/Imports/ImportFloorPlanIntegrationTests.cs`

- [ ] **Step 1: Write the failing end-to-end integration test**

```csharp
[Fact]
public async Task HandleAsync_persists_real_import_into_sqlite_and_returns_library_item()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var workspace = new AppWorkspace(tempRoot);
    workspace.EnsureCreated();

    var fixturePath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf"));

    await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);
    await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);

    var handler = new ImportFloorPlanHandler(
        new IxMiliaDxfGateway(),
        new ManagedFileStorage(workspace),
        new SqliteFloorPlanTemplateRepository(session),
        new SqliteFloorPlanVersionRepository(session),
        new SqliteImportedDocumentRepository(session),
        new SqliteMeasurementContextRepository(session),
        new SqliteUnitOfWork(session),
        new Sha256FileHashService(),
        new FixedClock(new DateTime(2026, 4, 29, 12, 30, 0, DateTimeKind.Utc)),
        new ImportFloorPlanResultFactory());

    var response = await handler.HandleAsync(new ImportFloorPlanRequest(fixturePath), CancellationToken.None);

    Assert.Equal("santa-barbara", response.Item.Code);
    Assert.Equal("Imported", response.Item.Status);
    Assert.Single(Directory.GetFiles(workspace.LibraryRawDxfDirectory, "*.dxf"));
}
```

- [ ] **Step 2: Implement the shared SQLite session**

```csharp
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteSession : IAsyncDisposable
{
    private SqliteSession(SqliteConnection connection, SqliteTransaction transaction)
    {
        Connection = connection;
        Transaction = transaction;
    }

    public SqliteConnection Connection { get; }

    public SqliteTransaction Transaction { get; }

    public static async Task<SqliteSession> OpenAsync(string databasePath, CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(cancellationToken);
        var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        return new SqliteSession(connection, transaction);
    }

    public async ValueTask DisposeAsync()
    {
        await Transaction.DisposeAsync();
        await Connection.DisposeAsync();
    }
}
```

- [ ] **Step 3: Implement schema creation**

```csharp
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public static class SqliteSchemaInitializer
{
    public static async Task InitializeAsync(string databasePath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS measurement_contexts (
                id TEXT PRIMARY KEY,
                source_unit INTEGER NOT NULL,
                to_millimeters_factor TEXT NOT NULL,
                linear_tolerance_mm TEXT NOT NULL,
                angular_tolerance_deg TEXT NOT NULL,
                created_at_utc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS imported_documents (
                id TEXT PRIMARY KEY,
                document_type INTEGER NOT NULL,
                original_file_name TEXT NOT NULL,
                storage_path TEXT NOT NULL,
                sha256 TEXT NOT NULL,
                dxf_version TEXT NULL,
                measurement_context_id TEXT NOT NULL,
                imported_at_utc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS floorplan_templates (
                id TEXT PRIMARY KEY,
                code TEXT NOT NULL UNIQUE,
                name TEXT NOT NULL,
                current_version_id TEXT NULL,
                is_active INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS floorplan_versions (
                id TEXT PRIMARY KEY,
                floorplan_template_id TEXT NOT NULL,
                imported_document_id TEXT NOT NULL,
                geometry_fingerprint TEXT NOT NULL,
                version_number INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
```

- [ ] **Step 4: Implement repository/transaction pattern**

Shared command helper pattern:

```csharp
private SqliteCommand CreateCommand(string sql)
{
    var command = session.Connection.CreateCommand();
    command.Transaction = session.Transaction;
    command.CommandText = sql;
    return command;
}
```

Repository insert pattern to use in all four repositories:

```csharp
await using var command = CreateCommand(
    "INSERT INTO measurement_contexts (id, source_unit, to_millimeters_factor, linear_tolerance_mm, angular_tolerance_deg, created_at_utc) VALUES ($id, $source_unit, $to_mm, $linear_tol, $angular_tol, $created_at)");

command.Parameters.AddWithValue("$id", context.Id.ToString());
command.Parameters.AddWithValue("$source_unit", (int)context.SourceUnit);
command.Parameters.AddWithValue("$to_mm", context.ToMillimetersFactor.ToString(System.Globalization.CultureInfo.InvariantCulture));
command.Parameters.AddWithValue("$linear_tol", context.LinearToleranceMm.ToString(System.Globalization.CultureInfo.InvariantCulture));
command.Parameters.AddWithValue("$angular_tol", context.AngularToleranceDeg.ToString(System.Globalization.CultureInfo.InvariantCulture));
command.Parameters.AddWithValue("$created_at", context.CreatedAtUtc.ToString("O"));

await command.ExecuteNonQueryAsync(cancellationToken);
```

`SqliteUnitOfWork`:

```csharp
using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteUnitOfWork : IUnitOfWork
{
    private readonly SqliteSession session;

    public SqliteUnitOfWork(SqliteSession session)
    {
        this.session = session;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return session.Transaction.CommitAsync(cancellationToken);
    }
}
```

`GetByCodeAsync` pattern:

```csharp
await using var command = CreateCommand(
    "SELECT id, code, name, current_version_id, is_active FROM floorplan_templates WHERE code = $code LIMIT 1");
command.Parameters.AddWithValue("$code", code);
```

`GetNextVersionNumberAsync` pattern:

```csharp
await using var command = CreateCommand(
    "SELECT COALESCE(MAX(version_number), 0) + 1 FROM floorplan_versions WHERE floorplan_template_id = $floorplan_template_id");
command.Parameters.AddWithValue("$floorplan_template_id", floorPlanTemplateId.ToString());
```

- [ ] **Step 5: Deferred integration verification (SDK environment only)**

```bash
dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --filter HandleAsync_persists_real_import_into_sqlite_and_returns_library_item
```

Expected: PASS and one row per table in SQLite.

- [ ] **Step 6: Commit**

```bash
git add src/FloorplanFit.Infrastructure/Persistence tests/FloorplanFit.Infrastructure.Tests/Imports/ImportFloorPlanIntegrationTests.cs
git commit -m "feat: persist imported floor plans in sqlite"
```

---

### Task 6: Build the thin Desktop Library and import action

**Files:**
- Modify: `src/FloorplanFit.Desktop/Program.cs`
- Modify: `src/FloorplanFit.Desktop/App.axaml.cs`
- Modify: `src/FloorplanFit.Desktop/MainWindow.axaml`
- Modify: `src/FloorplanFit.Desktop/MainWindow.axaml.cs`
- Modify: `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`
- Create: `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`

- [ ] **Step 1: Implement the Library view model**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.ViewModels;

public sealed partial class LibraryViewModel : ObservableObject
{
    private readonly ImportFloorPlanHandler importFloorPlanHandler;

    public LibraryViewModel(ImportFloorPlanHandler importFloorPlanHandler)
    {
        this.importFloorPlanHandler = importFloorPlanHandler;
    }

    public ObservableCollection<FloorPlanLibraryItemDto> Items { get; } = [];

    [ObservableProperty]
    private string statusMessage = "Ready";

    public async Task ImportAsync(string filePath, CancellationToken cancellationToken)
    {
        StatusMessage = "Importing...";

        var response = await importFloorPlanHandler.HandleAsync(new ImportFloorPlanRequest(filePath), cancellationToken);
        Items.Add(response.Item);

        StatusMessage = $"Imported {response.Item.Name} v{response.Item.ActiveVersionNumber}";
    }
}
```

- [ ] **Step 2: Register the slice services**

```csharp
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Desktop.ViewModels;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using FloorplanFit.Infrastructure.Security;
using FloorplanFit.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FloorplanFit.Desktop.Composition;

public static class DesktopServiceRegistration
{
    public static IServiceCollection AddDesktopSlice1(this IServiceCollection services, string workspaceRoot)
    {
        services.AddSingleton(new AppWorkspace(workspaceRoot));
        services.AddSingleton<IManagedFileStorage, ManagedFileStorage>();
        services.AddSingleton<IDxfGateway, IxMiliaDxfGateway>();
        services.AddSingleton<IFileHashService, Sha256FileHashService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ImportFloorPlanResultFactory>();
        services.AddSingleton<LibraryViewModel>();
        services.AddSingleton<MainWindow>();

        services.AddScoped(provider =>
            SqliteSession.OpenAsync(provider.GetRequiredService<AppWorkspace>().DatabasePath, CancellationToken.None)
                .GetAwaiter().GetResult());
        services.AddScoped<IMeasurementContextRepository, SqliteMeasurementContextRepository>();
        services.AddScoped<IImportedDocumentRepository, SqliteImportedDocumentRepository>();
        services.AddScoped<IFloorPlanTemplateRepository, SqliteFloorPlanTemplateRepository>();
        services.AddScoped<IFloorPlanVersionRepository, SqliteFloorPlanVersionRepository>();
        services.AddScoped<IUnitOfWork, SqliteUnitOfWork>();
        services.AddScoped<ImportFloorPlanHandler>();

        return services;
    }

    private sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
```

- [ ] **Step 3: Wire host bootstrap and schema initialization**

```csharp
using Avalonia;
using FloorplanFit.Desktop.Composition;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FloorplanFit.Desktop;

internal static class Program
{
    public static IHost? Host { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        var workspaceRoot = Path.Combine(AppContext.BaseDirectory, "workspace");

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureServices(services => services.AddDesktopSlice1(workspaceRoot))
            .Build();

        var workspace = Host.Services.GetRequiredService<AppWorkspace>();
        workspace.EnsureCreated();
        SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None).GetAwaiter().GetResult();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace();
}
```

`App.axaml.cs` adjustment:

```csharp
if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
{
    desktop.MainWindow = Program.Host!.Services.GetRequiredService<MainWindow>();
}
```

- [ ] **Step 4: Add the minimal Library UI and import plumbing**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="FloorplanFit.Desktop.MainWindow"
        Width="900"
        Height="600"
        Title="Floorplan Fit - Library">
  <Grid RowDefinitions="Auto,Auto,*" Margin="24">
    <TextBlock FontSize="24" FontWeight="Bold" Text="Floorplan Library" />

    <StackPanel Grid.Row="1" Orientation="Horizontal" Spacing="12" Margin="0,16,0,16">
      <Button Name="ImportButton" Content="Import DXF" Click="ImportButton_OnClick" />
      <TextBlock VerticalAlignment="Center" Text="{Binding StatusMessage}" />
    </StackPanel>

    <DataGrid Grid.Row="2" ItemsSource="{Binding Items}" AutoGenerateColumns="False" IsReadOnly="True">
      <DataGrid.Columns>
        <DataGridTextColumn Header="Code" Binding="{Binding Code}" />
        <DataGridTextColumn Header="Name" Binding="{Binding Name}" />
        <DataGridTextColumn Header="Status" Binding="{Binding Status}" />
        <DataGridTextColumn Header="Version" Binding="{Binding ActiveVersionNumber}" />
        <DataGridTextColumn Header="Unit" Binding="{Binding SourceUnit}" />
      </DataGrid.Columns>
    </DataGrid>
  </Grid>
</Window>
```

```csharp
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FloorplanFit.Desktop.ViewModels;

namespace FloorplanFit.Desktop;

public partial class MainWindow : Window
{
    private readonly LibraryViewModel viewModel;

    public MainWindow(LibraryViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void ImportButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select floor plan DXF",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("DXF files") { Patterns = ["*.dxf"] }]
        });

        var file = files.FirstOrDefault();

        if (file is null)
        {
            return;
        }

        await viewModel.ImportAsync(file.Path.LocalPath, CancellationToken.None);
    }
}
```

- [ ] **Step 5: Deferred manual verification (SDK environment only)**

```bash
dotnet run --project src/FloorplanFit.Desktop/FloorplanFit.Desktop.csproj
```

Expected manual result:

1. Desktop opens
2. click `Import DXF`
3. choose `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`
4. status updates
5. grid shows `santa-barbara`, `SANTA-BARBARA`, `Imported`, `1`, `inch`
6. `workspace/library/raw-dxf/` contains a managed copy
7. `workspace/app.db` exists

- [ ] **Step 6: Commit**

```bash
git add src/FloorplanFit.Desktop
git commit -m "feat: add thin desktop import flow"
```

---

### Task 7: Sync durable project knowledge after implementation milestones

**Files:**
- Modify: `obsidian-vault/Current State.md`
- Modify: `obsidian-vault/Implementation/2026-04-29 - Slice 1 Executable Kickoff.md`
- Create or modify implementation notes under `obsidian-vault/Implementation/`

- [ ] **Step 1: After Task 2, document the managed-copy import rule**

Record that Application now imports through `IManagedFileStorage` before DXF read and hash.

- [ ] **Step 2: After Task 5, document the real persistence milestone**

Record that SQLite now persists:

- `measurement_contexts`
- `imported_documents`
- `floorplan_templates`
- `floorplan_versions`

- [ ] **Step 3: After Task 6, document the first live Desktop path**

Record that `FloorplanFit.Desktop` can import a real DXF and show the resulting Library item.

- [ ] **Step 4: Commit docs with the code milestone they describe**

```bash
git add obsidian-vault docs
git commit -m "docs: record slice 1 executable progress"
```

---

## Self-review

### Spec coverage

- thin Desktop included — Tasks 1 and 6
- managed storage copy — Tasks 2 and 3
- real DXF metadata read — Task 4
- real SQLite persistence — Task 5
- visible Library item — Task 6
- durable project documentation — Task 7

### Placeholder scan

- no `TODO`
- no `TBD`
- no “implement later”

### Type consistency

- `IManagedFileStorage.CopyIntoLibraryAsync` returns `string` consistently
- `ImportFloorPlanHandler` constructor order is consistent across code snippets
- `AppWorkspace.DatabasePath` and `AppWorkspace.LibraryRawDxfDirectory` are used consistently

---

## Execution handoff

The user already asked to **start immediately**, so treat that as approval to proceed **inline** with Task 1 in this session.
