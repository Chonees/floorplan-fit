$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$initializerPath = Join-Path $repoRoot "src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs"
$regressionTestPath = Join-Path $repoRoot "tests/FloorplanFit.Infrastructure.Tests/Persistence/SqliteSchemaInitializerTests.cs"

if (!(Test-Path -LiteralPath $initializerPath)) {
    throw "SQLite schema initializer was not found."
}

$initializer = Get-Content -Raw -LiteralPath $initializerPath
if ($initializer -match [regex]::Escape('DROP TABLE IF EXISTS pinch_markers')) {
    throw "P0: unknown pinch_markers schema must never be dropped without preserving its rows."
}

if (!(Test-Path -LiteralPath $regressionTestPath)) {
    throw "P0: a pinch-marker migration regression test is required."
}

$regressionTest = Get-Content -Raw -LiteralPath $regressionTestPath
if ($regressionTest -notmatch 'InitializeAsync_preserves_unknown_pinch_marker_schema_and_fails_closed') {
    throw "P0: regression coverage must prove unknown pinch-marker data is preserved and startup fails closed."
}

Write-Host "P0 pinch-marker migration safety contract passed"
