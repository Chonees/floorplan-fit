$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$initializerPath = Join-Path $repoRoot "src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs"
$sessionPath = Join-Path $repoRoot "src/FloorplanFit.Infrastructure/Persistence/SqliteSession.cs"
$cleanupPath = Join-Path $repoRoot "src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanVersionCleanupService.cs"
$policyPath = Join-Path $repoRoot "src/FloorplanFit.Infrastructure/Persistence/SqliteConnectionPolicy.cs"
$testPath = Join-Path $repoRoot "tests/FloorplanFit.Infrastructure.Tests/Persistence/SqliteSchemaInitializerTests.cs"

$initializer = Get-Content -Raw -LiteralPath $initializerPath
if ($initializer -notmatch 'CurrentSchemaVersion' -or $initializer -notmatch 'PRAGMA user_version') {
    throw "P0: SQLite initialization must use an explicit monotonic user_version."
}

if (!(Test-Path -LiteralPath $policyPath)) {
    throw "P0: every production SQLite connection needs one shared safety policy."
}

$policy = Get-Content -Raw -LiteralPath $policyPath
if ($policy -notmatch 'foreign_keys' -or $policy -notmatch 'busy_timeout') {
    throw "P0: the shared SQLite policy must enable foreign keys and a busy timeout."
}

foreach ($path in @($initializerPath, $sessionPath, $cleanupPath)) {
    $source = Get-Content -Raw -LiteralPath $path
    if ($source -match 'new\s+SqliteConnection\s*\(') {
        throw "P0: direct SQLite connection construction remains in '$path'."
    }
}

$tests = Get-Content -Raw -LiteralPath $testPath
foreach ($requiredTest in @(
    'InitializeAsync_sets_and_preserves_current_user_version',
    'InitializeAsync_rejects_newer_user_version_without_mutating_database',
    'InitializeAsync_migrates_axis_tagged_pinch_markers_once',
    'InitializeAsync_preserves_curated_wall_legacy_rows_when_dependencies_are_unavailable',
    'OpenAsync_applies_foreign_keys_and_busy_timeout'
)) {
    if ($tests -notmatch [regex]::Escape($requiredTest)) {
        throw "P0: missing SQLite durability regression '$requiredTest'."
    }
}

Write-Host "P0 SQLite durability contract passed"
