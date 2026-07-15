$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$files = @(
    "src/FloorplanFit.Infrastructure/Storage/PlanSetOutlineSegmentCongruenceAuditBuilder.cs",
    "src/FloorplanFit.Infrastructure/Storage/PlanSetExportManifestWriter.cs",
    "src/FloorplanFit.Infrastructure/Dxf/IxMiliaAdjustedSitePlanExporter.cs",
    "src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs",
    "scripts/verify-latest-plan-set-recipe-manifest.ps1",
    "scripts/verify-latest-plan-set-final-output-congruence.ps1"
)

$banned = @(
    "SEMINOLE",
    "TEST A",
    "Downloads",
    "PointAIData",
    "39x77",
    "39\.0",
    "77\.5",
    "473\.903794",
    "529\.776184",
    "1000\.460736",
    "1001\.669227",
    "[0-9a-f]{32}"
)

foreach ($relativePath in $files) {
    $path = Join-Path $repoRoot $relativePath
    if (!(Test-Path -LiteralPath $path)) {
        throw "Expected final-output pipeline file '$relativePath' to exist."
    }

    $content = Get-Content -Raw -Path $path
    foreach ($pattern in $banned) {
        if ($content -cmatch $pattern) {
            throw "Final-output pipeline file '$relativePath' contains case-specific hardcode pattern '$pattern'."
        }
    }
}

Write-Host "PlanSet final-output no-hardcodes contract passed"
