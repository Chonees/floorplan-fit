$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$auditDto = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Contracts/PlanSets/MultiSheetExportAuditDto.cs')
$exportAuditHandler = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Application/PlanSets/ExportAudit/CreateMultiSheetExportAuditHandler.cs')
$viewModel = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs')

function Assert-Contains([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -notmatch [regex]::Escape($Pattern)) {
        throw $Message
    }
}

Assert-Contains $auditDto 'HumanSummary' 'MultiSheetExportAuditDto must carry human summary lines so manifest.json explains the export.'
Assert-Contains $exportAuditHandler 'BuildHumanSummary' 'CreateMultiSheetExportAuditHandler must build human summary lines from export evidence.'
Assert-Contains $exportAuditHandler 'Resumen AI' 'Human summary must explain what the user asked to shrink.'
Assert-Contains $exportAuditHandler 'Que no se achico en Electrical' 'Human summary must explain what did not shrink in Electrical and why.'
Assert-Contains $exportAuditHandler 'no habia geometria electrica en esa zona para mover' 'Human summary must translate known electrical no-geometry reasons.'
Assert-Contains $exportAuditHandler 'borde registrado' 'Human summary must explain when Electrical used the registered edge anchor fallback.'
Assert-Contains $exportAuditHandler 'zona electrica registrada realmente vacia' 'Human summary must distinguish truly empty registered Electrical zones from registration mismatch.'
Assert-Contains $exportAuditHandler 'Outline congruence' 'Human summary must explain whether Electrical outline overlay is trustworthy.'
Assert-Contains $exportAuditHandler 'Electrical normalizado antes de la receta' 'Human summary must explain when Electrical outline normalization was applied.'
Assert-Contains $exportAuditHandler 'Segment congruence' 'Human summary must point to structural segment congruence evidence.'
Assert-Contains $exportAuditHandler 'outline-segment-congruence-audit.json' 'Human summary must point users to the segment-level audit artifact.'
Assert-Contains $exportAuditHandler 'Datos insuficientes' 'Human summary must be explicit when evidence is missing.'
Assert-Contains $viewModel 'TryBuildSegmentCongruenceLine' 'SitePlanAdjustmentViewModel must enrich the UI audit panel with the live segment congruence result.'
Assert-Contains $viewModel 'AdvisoryMissingInternalWallRunCount' 'UI segment summary must expose advisory internal wall-run differences without dumping raw JSON.'
Assert-Contains $viewModel 'BuildSegmentProblemList' 'UI segment summary must identify failing outline edges/corners when segment congruence is not OK.'
Assert-Contains $viewModel 'OutlineEdges' 'UI segment summary must read edge-level segment evidence.'
Assert-Contains $viewModel 'CornerCoverage' 'UI segment summary must read corner-level segment evidence.'
Assert-Contains $viewModel 'TryBuildFinalOutputCongruenceLine' 'SitePlanAdjustmentViewModel must show final FloorPlan-vs-Electrical output congruence.'
Assert-Contains $viewModel 'final-output-congruence-audit.json' 'UI final output summary must point users to the output-vs-output audit artifact.'
Assert-Contains $viewModel 'Final output overlay' 'UI final output summary must explain whether exported DXFs overlay correctly.'
if ($viewModel -match [regex]::Escape('recipe: {sheet.RecipeHandlingSummary}') -or $viewModel -match [regex]::Escape('{sheet.StoragePath}')) {
    throw 'UI audit panel must not dump raw recipe/path sheet lines; keep those in JSON artifacts.'
}
if ($viewModel -match 'Quality:') {
    throw 'UI audit panel must not show raw English quality dump.'
}

Write-Output 'PlanSet human summary contract is wired.'



