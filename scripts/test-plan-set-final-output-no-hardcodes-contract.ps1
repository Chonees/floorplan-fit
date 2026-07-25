$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

# Final-output pipeline files. These additionally may not carry the fixture's
# measured numbers, because those numbers are evidence of one audited export.
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

# A hand-maintained file list cannot protect the repository. The SEMINOLE2000
# seed catalogue survived inside DxfExtractionProfile.cs precisely because that
# file was never listed above, so every production source file is now swept for
# identifiers that name one customer house, one operator machine, or one private
# dataset. Production must stay house-agnostic; house names belong to test
# fixtures and harness configuration only.
$houseAndMachineHardcodes = @(
    "SEMINOLE",
    "SANTA.BARBARA",
    "SANTA_BARBARA",
    "PointAIData",
    "Downloads",
    "OneDrive",
    "Escritorio",
    "AppData"
)

$sourceRoot = Join-Path $repoRoot "src"
if (!(Test-Path -LiteralPath $sourceRoot)) {
    throw "Expected production source root 'src' to exist."
}

# Build output is excluded deliberately: generated AssemblyInfo and Avalonia
# .g.cs files embed the absolute build path, which legitimately contains the
# operator's machine folders and would otherwise fail this sweep.
$productionFiles = @(
    Get-ChildItem -LiteralPath $sourceRoot -Recurse -File |
        Where-Object {
            ($_.Extension -eq ".cs" -or $_.Extension -eq ".axaml") -and
            $_.FullName -notmatch "[\\/](bin|obj)[\\/]"
        }
)

if ($productionFiles.Count -eq 0) {
    throw "Expected production source files under 'src' to exist."
}

$prefixLength = $repoRoot.Path.Length + 1
foreach ($file in $productionFiles) {
    $content = Get-Content -Raw -LiteralPath $file.FullName
    if ([string]::IsNullOrEmpty($content)) {
        continue
    }

    foreach ($pattern in $houseAndMachineHardcodes) {
        if ($content -imatch $pattern) {
            $relative = $file.FullName.Substring($prefixLength)
            throw "Production file '$relative' contains house or machine hardcode pattern '$pattern'. Production must stay house-agnostic; move the value to a test fixture or harness configuration."
        }
    }
}

Write-Host "PlanSet final-output no-hardcodes contract passed"
Write-Host "Production hardcode sweep passed over $($productionFiles.Count) production source files"
