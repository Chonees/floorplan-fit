$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$statusPath = Join-Path $repoRoot "src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjectionStatus.cs"
$capabilityPath = Join-Path $repoRoot "src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjectionCapabilities.cs"
$roofHandlerPath = Join-Path $repoRoot "src/FloorplanFit.Application/PlanSets/Projection/ProjectRoofSheetAdjustmentHandler.cs"
$facadeHandlerPath = Join-Path $repoRoot "src/FloorplanFit.Application/PlanSets/Projection/ProjectFacadeElevationSheetAdjustmentHandler.cs"
$confirmationHandlerPath = Join-Path $repoRoot "src/FloorplanFit.Application/PlanSets/Confirmation/ConfirmSheetAdjustmentProjectionHandler.cs"
$exportHandlerPath = Join-Path $repoRoot "src/FloorplanFit.Application/PlanSets/Export/ExportProjectedPlanSheetHandler.cs"
$roofTestsPath = Join-Path $repoRoot "tests/FloorplanFit.Application.Tests/PlanSets/Projection/ProjectRoofSheetAdjustmentHandlerTests.cs"
$facadeTestsPath = Join-Path $repoRoot "tests/FloorplanFit.Application.Tests/PlanSets/Projection/ProjectFacadeElevationSheetAdjustmentHandlerTests.cs"
$confirmationTestsPath = Join-Path $repoRoot "tests/FloorplanFit.Application.Tests/PlanSets/Confirmation/ConfirmSheetAdjustmentProjectionHandlerTests.cs"
$exportTestsPath = Join-Path $repoRoot "tests/FloorplanFit.Application.Tests/PlanSets/Export/ExportProjectedPlanSheetHandlerTests.cs"

$status = Get-Content -Raw -LiteralPath $statusPath
if ($status -notmatch 'Unsupported\s*=\s*3') {
    throw "P0: SheetAdjustmentProjectionStatus does not expose the fail-closed Unsupported state."
}

if (!(Test-Path -LiteralPath $capabilityPath)) {
    throw "P0: typed sheet adjustment projection capability policy is missing."
}

$capability = Get-Content -Raw -LiteralPath $capabilityPath
foreach ($requiredBehavior in @(
    'SheetAdjustmentProjectionCapability',
    'SupportsAffinePlacement',
    'SupportsCanonicalCompression',
    'ElectricalWholeSheetSimilarity',
    'RoofOverhangPreserving',
    'FacadeHorizontalPreservingVerticals',
    'TryGetUnsupportedReason',
    'EnsureSupported'
)) {
    if ($capability -notmatch [regex]::Escape($requiredBehavior)) {
        throw "P0: typed capability behavior '$requiredBehavior' is missing."
    }
}

foreach ($expectedCapability in @(
    @{ Method = 'ElectricalWholeSheetSimilarity'; SupportsCompression = 'true' },
    @{ Method = 'RoofOverhangPreserving'; SupportsCompression = 'false' },
    @{ Method = 'FacadeHorizontalPreservingVerticals'; SupportsCompression = 'false' }
)) {
    $pattern = '{0}\s*=>[\s\S]{{0,180}}SupportsCanonicalCompression:\s*{1}' -f
        [regex]::Escape($expectedCapability.Method),
        $expectedCapability.SupportsCompression
    if ($capability -notmatch $pattern) {
        throw "P0: '$($expectedCapability.Method)' has the wrong canonical compression capability."
    }
}

if ($capability -match 'RuleSummary|RecipeHandlingSummary') {
    throw "P0: capability gating must not depend on human-readable summaries."
}

$roofHandler = Get-Content -Raw -LiteralPath $roofHandlerPath
$facadeHandler = Get-Content -Raw -LiteralPath $facadeHandlerPath
foreach ($handler in @(
    @{ Source = $roofHandler; Name = 'Roof' },
    @{ Source = $facadeHandler; Name = 'Facade' }
)) {
    if ($handler.Source -notmatch 'TryGetUnsupportedReason' -or
        $handler.Source -notmatch 'SheetAdjustmentProjectionStatus\.Unsupported') {
        throw "P0: $($handler.Name) compression is not classified through the typed capability policy as Unsupported."
    }
}

$confirmationHandler = Get-Content -Raw -LiteralPath $confirmationHandlerPath
$confirmationGuardIndex = $confirmationHandler.IndexOf('EnsureSupported', [StringComparison]::Ordinal)
$confirmationReadyIndex = $confirmationHandler.IndexOf('projection.Status is SheetAdjustmentProjectionStatus.ReadyForExport', [StringComparison]::Ordinal)
if ($confirmationGuardIndex -lt 0 -or
    $confirmationReadyIndex -lt 0 -or
    $confirmationGuardIndex -gt $confirmationReadyIndex) {
    throw "P0: confirmation capability guard must run before the legacy ReadyForExport early return."
}

$exportHandler = Get-Content -Raw -LiteralPath $exportHandlerPath
$exportGuardIndex = $exportHandler.IndexOf('EnsureSupported', [StringComparison]::Ordinal)
$exportReadyIndex = $exportHandler.IndexOf('projection.Status is not SheetAdjustmentProjectionStatus.ReadyForExport', [StringComparison]::Ordinal)
if ($exportGuardIndex -lt 0 -or
    $exportReadyIndex -lt 0 -or
    $exportGuardIndex -gt $exportReadyIndex) {
    throw "P0: projected export must reject unsupported legacy Ready rows before status-based export."
}

$roofTests = Get-Content -Raw -LiteralPath $roofTestsPath
$facadeTests = Get-Content -Raw -LiteralPath $facadeTestsPath
$confirmationTests = Get-Content -Raw -LiteralPath $confirmationTestsPath
$exportTests = Get-Content -Raw -LiteralPath $exportTestsPath

foreach ($requiredTest in @(
    @{ Source = $roofTests; Name = 'HandleAsync_marks_compressed_roof_projection_unsupported' },
    @{ Source = $facadeTests; Name = 'HandleAsync_marks_compressed_facade_projection_unsupported' },
    @{ Source = $confirmationTests; Name = 'HandleAsync_rejects_compressed_affine_only_projection' },
    @{ Source = $confirmationTests; Name = 'HandleAsync_marks_compression_recipe_as_confirmed_for_recipe_aware_export' },
    @{ Source = $exportTests; Name = 'HandleAsync_rejects_legacy_ready_compressed_affine_only_projection_before_writing_artifact' },
    @{ Source = $exportTests; Name = 'HandleAsync_passes_canonical_recipe_for_ready_electrical_projection_with_compression' }
)) {
    if ($requiredTest.Source -notmatch [regex]::Escape($requiredTest.Name)) {
        throw "P0: missing honest capability regression '$($requiredTest.Name)'."
    }
}

Write-Host "P0 sheet capability contract passed"
