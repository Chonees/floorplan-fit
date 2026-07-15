$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$programPath = Join-Path $repoRoot "src/FloorplanFit.Desktop/Program.cs"
$durabilityPath = Join-Path $repoRoot "src/FloorplanFit.Infrastructure/Runtime/AppWorkspaceDurability.cs"
$testPath = Join-Path $repoRoot "tests/FloorplanFit.Infrastructure.Tests/Runtime/AppWorkspaceDurabilityTests.cs"

$program = Get-Content -Raw -LiteralPath $programPath
if ($program -notmatch 'LocalApplicationData') {
    throw "P0: production workspace must live under LocalApplicationData."
}

if ($program -match 'var\s+workspaceRoot\s*=\s*Path\.Combine\(AppContext\.BaseDirectory') {
    throw "P0: executable-relative workspace remains the active write root."
}

if (!(Test-Path -LiteralPath $durabilityPath)) {
    throw "P0: workspace migration and pre-migration backup behavior is missing."
}

$durability = Get-Content -Raw -LiteralPath $durabilityPath
foreach ($requiredBehavior in @(
    'CopyLegacyWorkspaceIfNeeded',
    'CreatePreMigrationBackupIfNeeded',
    'BackupDatabase',
    'Directory.Move'
)) {
    if ($durability -notmatch [regex]::Escape($requiredBehavior)) {
        throw "P0: workspace durability behavior '$requiredBehavior' is missing."
    }
}

if ($durability -match 'Directory\.Delete\s*\(\s*legacyWorkspaceRoot') {
    throw "P0: legacy workspace migration must copy, never delete, user data."
}

if (!(Test-Path -LiteralPath $testPath)) {
    throw "P0: workspace durability regressions are required."
}

$tests = Get-Content -Raw -LiteralPath $testPath
foreach ($requiredTest in @(
    'Stable_paths_are_rooted_under_local_application_data',
    'CopyLegacyWorkspaceIfNeeded_copies_nested_state_without_deleting_legacy',
    'CopyLegacyWorkspaceIfNeeded_does_not_merge_into_non_empty_target',
    'CreatePreMigrationBackupIfNeeded_snapshots_database_and_managed_files_once'
)) {
    if ($tests -notmatch [regex]::Escape($requiredTest)) {
        throw "P0: missing workspace durability regression '$requiredTest'."
    }
}

Write-Host "P0 workspace durability contract passed"
