# Finite external proof for the commissioned automatic site-fitting goal (Phase 8.6).
# The agent cannot run .NET in this repository, so this script is the single external
# sequence that proves the statically-green work end to end.
#
# Usage:
#   1) pwsh scripts/run-commissioned-autofit-proof.ps1
#      Runs every focused test suite added or touched by the goal.
#   2) Export SEMINOLE packages from the Desktop app (Width-only, Depth-only, combined
#      when supported; rigid-fit for the full-package path), then:
#      pwsh scripts/run-commissioned-autofit-proof.ps1 -SkipTests -PackageDirectories "C:\...\caso1-plan-set","C:\...\caso2-plan-set"
#      Validates each user package: exactly five named artifacts, coherent stem,
#      no .staging- leakage, and a green manifest when Verification is present.
#   3) The script finishes by running the existing workspace verifier with
#      -RequireAutomatic against the latest export (disable with -SkipWorkspaceVerifier).

param(
    [string[]]$PackageDirectories = @(),
    [switch]$SkipTests,
    [switch]$SkipWorkspaceVerifier,
    [string]$WorkspaceRoot = "src\FloorplanFit.Desktop\bin\Debug\net10.0\workspace\exports\plan-sets"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    if (-not $SkipTests) {
        $filters = @(
            "FullyQualifiedName~SeminoleCommissionedHouseFitContractTests",
            "FullyQualifiedName~CommissionedHouseFitPlannerTests",
            "FullyQualifiedName~CommissionedHouseFitRequestFactoryTests",
            "FullyQualifiedName~CommissionedHouseAdaptationProfileReadinessTests",
            "FullyQualifiedName~CommissionedHouseAdaptationProfileHandlerTests",
            "FullyQualifiedName~CommissionExistingCurationProfileCompilerTests",
            "FullyQualifiedName~CommissionedHouseFitPreviewProjectorTests",
            "FullyQualifiedName~CommissionedSitePlanAdjustmentIntegrationTests",
            "FullyQualifiedName~ExportMultiSheetPlanSetPackageHandlerTests",
            "FullyQualifiedName~ExportProjectedPlanSheetHandlerTests",
            "FullyQualifiedName~ProjectedPlanSheetDxfExporterTests",
            "FullyQualifiedName~IxMiliaAdjustedSitePlanExporterTests",
            "FullyQualifiedName~SitePlanAdjustmentWindowLayoutTests",
            "FullyQualifiedName~SitePlanAdjustmentPreviewProjectorTests",
            "FullyQualifiedName~LibraryViewModelTests"
        )
        $filter = $filters -join "|"
        Write-Host "== Stage 1: focused test suites ==" -ForegroundColor Cyan
        Write-Host "dotnet test --filter `"$filter`""
        dotnet test --filter $filter
        if ($LASTEXITCODE -ne 0) {
            throw "Focused test run failed. Fix RED contracts before runtime proof."
        }

        if (-not (Test-Path -LiteralPath "D:\PointAIData\PLANS\originalFloorPlans\SEMINOLE2000.dxf") -and
            -not $env:FLOORPLANFIT_SEMINOLE_FIXTURE) {
            Write-Warning "SEMINOLE fixture not found; SeminoleCommissionedHouseFitContractTests were skipped, not proven."
        }
    }

    foreach ($packageDirectory in $PackageDirectories) {
        Write-Host "== Stage 2: validating package '$packageDirectory' ==" -ForegroundColor Cyan
        if (-not (Test-Path -LiteralPath $packageDirectory)) {
            throw "Package directory '$packageDirectory' does not exist."
        }

        $files = Get-ChildItem -LiteralPath $packageDirectory -File
        $floorplan = @($files | Where-Object { $_.Name -like "*-floorplan.dxf" })
        if ($floorplan.Count -ne 1) {
            throw "Expected exactly one '*-floorplan.dxf' in '$packageDirectory', found $($floorplan.Count)."
        }

        $stem = $floorplan[0].Name.Substring(0, $floorplan[0].Name.Length - "-floorplan.dxf".Length)
        $expected = @(
            "$stem-floorplan.dxf",
            "$stem-electrical.dxf",
            "$stem-comparison.json",
            "manifest.json",
            "$stem-audit.txt"
        )
        foreach ($name in $expected) {
            if (-not (Test-Path -LiteralPath (Join-Path $packageDirectory $name))) {
                throw "Package '$packageDirectory' is missing required artifact '$name'."
            }
        }

        $unexpected = @($files | Where-Object { $expected -notcontains $_.Name })
        if ($unexpected.Count -gt 0) {
            throw "Package '$packageDirectory' contains unexpected loose files: $(($unexpected | ForEach-Object Name) -join ', ')."
        }

        foreach ($jsonName in @("manifest.json", "$stem-comparison.json")) {
            $raw = Get-Content -Raw -LiteralPath (Join-Path $packageDirectory $jsonName)
            if ($raw.Contains(".staging-")) {
                throw "'$jsonName' in '$packageDirectory' leaks a transient .staging- path."
            }
        }

        $manifest = Get-Content -Raw -LiteralPath (Join-Path $packageDirectory "manifest.json") | ConvertFrom-Json
        if ([string]::IsNullOrWhiteSpace([string]$manifest.Status)) {
            throw "Package manifest in '$packageDirectory' has no Status."
        }
        if ($null -ne $manifest.Verification -and [string]$manifest.Verification.Decision -ne "ReadyForExport") {
            throw "Package manifest in '$packageDirectory' is not ReadyForExport: $([string]$manifest.Verification.Decision)."
        }

        $emptyArtifacts = @($files | Where-Object { $_.Length -le 0 })
        if ($emptyArtifacts.Count -gt 0) {
            throw "Package '$packageDirectory' has empty artifacts: $(($emptyArtifacts | ForEach-Object Name) -join ', ')."
        }

        Write-Host "Package '$packageDirectory' OK: five named artifacts, stem '$stem', no staging leakage." -ForegroundColor Green
    }

    if (-not $SkipWorkspaceVerifier -and (Test-Path -LiteralPath $WorkspaceRoot)) {
        Write-Host "== Stage 3: workspace verifier (-RequireAutomatic) against the latest export ==" -ForegroundColor Cyan
        & (Join-Path $PSScriptRoot "verify-latest-plan-set-recipe-manifest.ps1") -Root $WorkspaceRoot -RequireAutomatic
    }

    Write-Host ""
    Write-Host "Runtime handoff checklist (Phase 8.7):" -ForegroundColor Yellow
    Write-Host " 1. dotnet test stage green (above)."
    Write-Host " 2. In the Desktop app: commission SEMINOLE (publish curation), confirm 'Auto-fit ready' in the Library."
    Write-Host " 3. Rigid-fit case: pick a site the house fits; confirm/export; re-run this script with -SkipTests -PackageDirectories <folder>."
    Write-Host " 4. Deforming cases (Width-only / Depth-only / combined): today these FAIL CLOSED on Electrical"
    Write-Host "    overlay reconciliation until commissioning authors device-host/wire-route evidence."
    Write-Host "    Expected proof: no package directory is created and the status reports the exact Electrical reason."
    Write-Host " 5. Open exported Floor/Electrical DXFs in AutoCAD; confirm overlay, openings, and devices visually."
    Write-Host "Done." -ForegroundColor Green
}
finally {
    Pop-Location
}
