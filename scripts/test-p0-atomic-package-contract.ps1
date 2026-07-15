$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

function Read-RepoFile([string] $relativePath) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "P0 atomic package contract is missing '$relativePath'."
    }

    return Get-Content -LiteralPath $path -Raw
}

function Require-Match([string] $content, [string] $pattern, [string] $message) {
    if ($content -notmatch $pattern) {
        throw $message
    }
}

function Require-Order(
    [string] $content,
    [string] $firstPattern,
    [string] $secondPattern,
    [string] $message) {
    $first = [regex]::Match($content, $firstPattern)
    $second = [regex]::Match($content, $secondPattern)
    if (-not $first.Success -or -not $second.Success -or $first.Index -ge $second.Index) {
        throw $message
    }
}

$status = Read-RepoFile 'src/FloorplanFit.Domain/PlanSets/PlanSetExportStatus.cs'
$failure = Read-RepoFile 'src/FloorplanFit.Contracts/PlanSets/PlanSetExportFailureDto.cs'
$publisher = Read-RepoFile 'src/FloorplanFit.Application/PlanSets/Export/AtomicDirectoryPublisher.cs'
$packageHandler = Read-RepoFile 'src/FloorplanFit.Application/PlanSets/Export/ExportMultiSheetPlanSetPackageHandler.cs'
$auditHandler = Read-RepoFile 'src/FloorplanFit.Application/PlanSets/ExportAudit/CreateMultiSheetExportAuditHandler.cs'
$projectionRequest = Read-RepoFile 'src/FloorplanFit.Contracts/PlanSets/MultiSheetExportProjectionRequestDto.cs'
$exportedSheet = Read-RepoFile 'src/FloorplanFit.Contracts/PlanSets/ExportedPlanSheetDto.cs'
$manifestWriter = Read-RepoFile 'src/FloorplanFit.Infrastructure/Storage/PlanSetExportManifestWriter.cs'
$congruenceBuilder = Read-RepoFile 'src/FloorplanFit.Infrastructure/Storage/PlanSetOutlineSegmentCongruenceAuditBuilder.cs'
$packageTests = Read-RepoFile 'tests/FloorplanFit.Application.Tests/PlanSets/Export/ExportMultiSheetPlanSetPackageHandlerTests.cs'
$writerTests = Read-RepoFile 'tests/FloorplanFit.Infrastructure.Tests/PlanSets/PlanSetExportManifestWriterTests.cs'

Require-Match $status '\bFailed\s*=\s*3\b' `
    'P0: PlanSetExportStatus does not expose the fail-closed Failed state.'
Require-Match $failure 'record\s+PlanSetExportFailureDto' `
    'P0: export failure persistence has no typed JSON contract.'
Require-Match $failure 'PlanSetExportFailureStage' `
    'P0: export failure persistence has no typed failure stage.'

Require-Match $publisher '\.staging-' `
    'P0: atomic publication does not create a sibling staging directory.'
Require-Match $publisher 'Directory\.Move\s*\(\s*stagingDirectory\s*,\s*finalDirectory' `
    'P0: atomic publication must expose the final directory with one directory rename.'
Require-Match $publisher 'Directory\.Exists\s*\(\s*finalDirectory\s*\).*throw' `
    'P0: atomic publication does not reject an existing final directory.'
Require-Match $publisher 'Directory\.Delete\s*\(\s*stagingDirectory\s*,\s*recursive:\s*true\s*\)' `
    'P0: failed/cancelled publication does not clean staging.'
Require-Match $publisher 'Func<Func<CancellationToken,\s*Task>,\s*CancellationToken,\s*Task<' `
    'P0 RED: atomic publication still has no callback boundary between staging and final rename.'
Require-Order $publisher `
    'await\s+populateStagingAsync' `
    'await\s+completePublicationAsync' `
    'P0: the external publication callback must run only after staging is complete.'

Require-Match $packageHandler 'AtomicDirectoryPublisher\.PublishAsync' `
    'P0: the user HousePlanSet package is not published through atomic staging.'
Require-Match $packageHandler 'return\s+await\s+AtomicDirectoryPublisher\.PublishAsync' `
    'P0: the atomic publication result is not returned by the package handler.'
Require-Match $packageHandler 'BuildOutputPath\s*\(\s*stagingDirectory' `
    'P0: dependent DXFs are still written directly to the final PackageDirectory.'
Require-Match $packageHandler 'BuildOutputPath\s*\(\s*request\.PackageDirectory' `
    'P0: persisted dependent-sheet paths are not remapped to their final package paths.'
Require-Match $packageHandler 'exportAuditHandler\.HandleAsync[\s\S]*publishPackageAsync' `
    'P0: the package rename is not delegated to the audit gate after workspace publication.'

Require-Match $projectionRequest '\[JsonIgnore\][\s\S]*VerificationPath' `
    'P0: staged dependent DXFs have no typed non-serialized verification path.'
Require-Match $exportedSheet '\[JsonIgnore\][\s\S]*VerificationPath' `
    'P0: exported sheet verification still cannot read staging without persisting staging paths.'
Require-Match $manifestWriter 'VerificationPath\s*\?\?\s*sheet\.StoragePath' `
    'P0: manifest verification does not resolve physical staging paths.'
Require-Match $congruenceBuilder 'VerificationPath\s*\?\?' `
    'P0: DXF congruence builders still read logical final paths before final publication.'

Require-Match $auditHandler 'PlanSetExportStatus\.Failed' `
    'P0: package generation failures are not persisted as Failed.'
Require-Match $auditHandler 'JsonSerializer\.Serialize\s*\(\s*failure' `
    'P0: typed package failure reasons are not persisted as JSON.'
Require-Match $auditHandler 'TryRecordFailureAsync' `
    'P0: pre-audit exporter/publication failures have no best-effort failure persistence path.'
Require-Order $auditHandler `
    'BuildVerificationReport' `
    'planSetExportManifestWriter\.WriteAsync' `
    'P0: typed verification must complete before workspace audits/manifest publication.'
Require-Order $auditHandler `
    'planSetExportManifestWriter\.WriteAsync' `
    'await\s+publishPackageAsync\s*\(' `
    'P0 RED: final user PackageDirectory is renamed before the workspace manifest is published.'
Require-Order $auditHandler `
    'await\s+publishPackageAsync\s*\(' `
    'planSetExportRepository\.AddAsync' `
    'P0: success persistence must happen only after the final user PackageDirectory rename.'

Require-Match $manifestWriter 'AtomicDirectoryPublisher\.PublishAsync' `
    'P0: the workspace manifest/audit package is not atomically published.'
Require-Order $manifestWriter `
    'WriteAuditArtifactsAsync\s*\(\s*stagingDirectory' `
    'WriteJsonAsync\s*\(\s*Path\.Combine\s*\(\s*stagingDirectory\s*,\s*"manifest\.json"' `
    'P0: workspace audits must be written before manifest.json, which is the final staged file.'

foreach ($testName in @(
    'HandleAsync_publishes_complete_package_from_sibling_staging',
    'HandleAsync_keeps_final_hidden_until_verification_and_workspace_manifest_then_renames',
    'HandleAsync_exporter_failure_cleans_staging_and_persists_failed',
    'HandleAsync_manifest_writer_failure_rolls_back_package_and_persists_failed',
    'HandleAsync_cancellation_cleans_staging_and_persists_failed')) {
    Require-Match $packageTests ([regex]::Escape($testName)) `
        "P0: missing focused package regression '$testName'."
}

foreach ($testName in @(
    'WriteAsync_publishes_audits_then_manifest_atomically',
    'WriteAsync_existing_final_is_preserved',
    'WriteAsync_cancellation_leaves_no_final_or_staging')) {
    Require-Match $writerTests ([regex]::Escape($testName)) `
        "P0: missing focused manifest writer regression '$testName'."
}

Write-Output 'P0 atomic package contract passed'
