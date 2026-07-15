$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$placementDto = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Contracts/FloorPlans/AdjustedSitePlanPlacementDto.cs')
$viewModel = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs')
$manifestWriter = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Infrastructure/Storage/PlanSetExportManifestWriter.cs')

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

Assert-Contains $placementDto 'AdjustmentInputAuditDto' 'AdjustedSitePlanPlacementDto must carry explicit input audit dimensions.'
Assert-Contains $placementDto 'InputAudit' 'AdjustedSitePlanPlacementDto must expose InputAudit.'
Assert-Contains $viewModel 'BuildAdjustmentInputAudit' 'SitePlanAdjustmentViewModel must derive input audit dimensions at export time.'
Assert-Contains $viewModel 'InputAudit = BuildAdjustmentInputAudit()' 'BuildAdjustedSitePlanPlacement must attach InputAudit.'
Assert-Contains $manifestWriter 'originalWidthInches' 'input-audit.json must write original width in inches.'
Assert-Contains $manifestWriter 'requestedWidthInches' 'input-audit.json must write requested width in inches.'
Assert-Contains $manifestWriter 'requiredWidthDeltaInches' 'input-audit.json must write required width delta in inches.'
Assert-NotContains $manifestWriter 'UnavailableInCurrentExportContract' 'input-audit.json can no longer hide missing dimensions behind UnavailableInCurrentExportContract.'

Write-Output 'PlanSet input audit contract is wired.'
