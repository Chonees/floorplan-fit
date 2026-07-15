$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$registrationPaths = @(
    "src/FloorplanFit.Application/PlanSets/Registration/RegisterElectricalSheetHandler.cs",
    "src/FloorplanFit.Application/PlanSets/Registration/RegisterRoofSheetHandler.cs",
    "src/FloorplanFit.Application/PlanSets/Registration/RegisterFacadeElevationSheetHandler.cs"
) | ForEach-Object { Join-Path $repoRoot $_ }
$planSetRepositoryPortPath = Join-Path $repoRoot "src/FloorplanFit.Application/Abstractions/IPlanSetVersionRepository.cs"
$removeHandlerPath = Join-Path $repoRoot "src/FloorplanFit.Application/FloorPlans/Library/RemoveFloorPlanVersionHandler.cs"
$initializerPath = Join-Path $repoRoot "src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs"
$cleanupPath = Join-Path $repoRoot "src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanVersionCleanupService.cs"
$registrationTestPath = Join-Path $repoRoot "tests/FloorplanFit.Application.Tests/PlanSets/Registration/RegisterElectricalSheetHandlerTests.cs"
$removeTestPath = Join-Path $repoRoot "tests/FloorplanFit.Application.Tests/FloorPlans/Library/RemoveFloorPlanVersionHandlerTests.cs"
$schemaTestPath = Join-Path $repoRoot "tests/FloorplanFit.Infrastructure.Tests/Persistence/SqliteSchemaInitializerTests.cs"
$cleanupTestPath = Join-Path $repoRoot "tests/FloorplanFit.Infrastructure.Tests/Library/SqliteFloorPlanVersionCleanupServiceIntegrationTests.cs"

foreach ($path in $registrationPaths) {
    $source = Get-Content -Raw -LiteralPath $path
    if ($source -notmatch 'IPlanSetVersionRepository' -or
        $source -notmatch 'planSetVersion\.CanonicalFloorPlanVersionId') {
        throw "P0: '$path' does not resolve canonical identity from the owning PlanSetVersion."
    }
}

$port = Get-Content -Raw -LiteralPath $planSetRepositoryPortPath
if ($port -notmatch 'GetByIdAsync') {
    throw "P0: PlanSetVersion repository cannot resolve aggregate ownership by id."
}

$removeHandler = Get-Content -Raw -LiteralPath $removeHandlerPath
if ($removeHandler -notmatch 'GetByCanonicalFloorPlanVersionAsync') {
    throw "P0: canonical FloorPlan versions can still be removed while referenced by a HousePlanSet."
}

$initializer = Get-Content -Raw -LiteralPath $initializerPath
foreach ($requiredMigrationBehavior in @(
    'RepairSheetRegistrationCanonicalIds',
    'validate_sheet_registration_identity_insert',
    'prevent_canonical_floorplan_version_soft_delete'
)) {
    if ($initializer -notmatch [regex]::Escape($requiredMigrationBehavior)) {
        throw "P0: identity migration behavior '$requiredMigrationBehavior' is missing."
    }
}

$cleanup = Get-Content -Raw -LiteralPath $cleanupPath
foreach ($requiredCleanupBehavior in @(
    'cleanup_geometry_paths',
    'floorplan_dimension_binding_override_anchors',
    'floorplan_dimension_binding_overrides',
    'floorplan_dimension_interval_bindings',
    'measurement_nodes',
    'measurement_corridors'
)) {
    if ($cleanup -notmatch [regex]::Escape($requiredCleanupBehavior)) {
        throw "P0: cleanup ownership behavior '$requiredCleanupBehavior' is missing."
    }
}

$registrationTests = Get-Content -Raw -LiteralPath $registrationTestPath
$removeTests = Get-Content -Raw -LiteralPath $removeTestPath
$schemaTests = Get-Content -Raw -LiteralPath $schemaTestPath
$cleanupTests = Get-Content -Raw -LiteralPath $cleanupTestPath

foreach ($requiredTest in @(
    @{ Source = $registrationTests; Name = 'HandleAsync_persists_the_owning_canonical_floor_plan_version' },
    @{ Source = $registrationTests; Name = 'HandleAsync_rejects_a_sheet_from_another_plan_set_version' },
    @{ Source = $removeTests; Name = 'HandleAsync_rejects_a_version_referenced_by_a_plan_set' },
    @{ Source = $schemaTests; Name = 'InitializeAsync_repairs_legacy_registration_identity_before_enforcing_guards' },
    @{ Source = $cleanupTests; Name = 'CleanupAsync_removes_owned_curation_geometry_without_leaving_orphans' }
)) {
    if ($requiredTest.Source -notmatch [regex]::Escape($requiredTest.Name)) {
        throw "P0: missing canonical identity regression '$($requiredTest.Name)'."
    }
}

Write-Host "P0 canonical identity contract passed"
