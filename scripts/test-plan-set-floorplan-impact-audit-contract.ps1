$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$placementDto = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Contracts/FloorPlans/AdjustedSitePlanPlacementDto.cs')
$viewModel = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs')
$manifestWriter = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Infrastructure/Storage/PlanSetExportManifestWriter.cs')
$verifier = Get-Content -Raw (Join-Path $root 'scripts/verify-latest-plan-set-recipe-manifest.ps1')

function Assert-Contains([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -notmatch [regex]::Escape($Pattern)) {
        throw $Message
    }
}

function Assert-NotContains([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -match [regex]::Escape($Pattern)) {
        throw $Message
    }
}

Assert-Contains $placementDto 'FloorPlanAdjustmentOperationImpactDto' 'AdjustedSitePlanPlacementDto must carry floor-plan operation impact audit rows.'
Assert-Contains $placementDto 'FloorPlanImpactAudit' 'AdjustedSitePlanPlacementDto must expose FloorPlanImpactAudit.'
Assert-Contains $viewModel 'BuildFloorPlanImpactAudits' 'SitePlanAdjustmentViewModel must count FloorPlan impact while applying auto-fit operations.'
Assert-Contains $viewModel 'appliedFloorPlanImpactAudit' 'SitePlanAdjustmentViewModel must persist applied floor-plan impact audit into placement.'
Assert-Contains $manifestWriter 'FloorPlanImpactAudit' 'floorplan-impact-audit.json must use persisted floor-plan impact audit.'
Assert-Contains $manifestWriter 'affectedEntities' 'floorplan-impact-audit.json must expose affected entities.'
Assert-Contains $manifestWriter 'affectedVertices' 'floorplan-impact-audit.json must expose affected vertices.'
Assert-NotContains $manifestWriter 'FloorPlan impact entity/vertex counts are not persisted yet.' 'floorplan-impact-audit.json must not keep the old placeholder reason.'
Assert-Contains $verifier 'floorplan-impact-audit.json' 'Verifier must read floorplan-impact-audit.json.'
Assert-Contains $verifier 'floorPlanImpactOperations' 'Verifier must reject missing floor-plan operation impact rows.'

Write-Output 'PlanSet floor-plan impact audit contract is wired.'
