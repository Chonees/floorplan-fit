$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$contracts = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Contracts/PlanSets/ProjectedPlanSheetExportAuditDto.cs')
$exportHandler = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Application/PlanSets/Export/ExportProjectedPlanSheetHandler.cs')
$exporter = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs')
$registrationEstimator = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Infrastructure/Dxf/DxfElectricalFloorRegistrationEstimator.cs')
$outlineSelector = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Infrastructure/Geometry/DominantAxisAlignedOutlineSelector.cs')
$writer = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Infrastructure/Storage/PlanSetExportManifestWriter.cs')
$humanSummary = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Application/PlanSets/ExportAudit/CreateMultiSheetExportAuditHandler.cs')
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

Assert-Contains $contracts 'ProjectedPlanSheetOutlineCongruenceAuditDto' 'Export audit contract must carry outline congruence data.'
Assert-Contains $contracts 'ProjectedPlanSheetOutlineDto' 'Outline audit must expose structural bbox data.'
Assert-Contains $exportHandler 'ReadOriginalInputAuditDimensions' 'Export handler must read canonical source dimensions without deserializing the placement record.'
Assert-NotContains $exportHandler 'Deserialize<AdjustedSitePlanPlacementDto>' 'Export handler must not deserialize AdjustedSitePlanPlacementDto; it has multiple constructors and crashes System.Text.Json.'
Assert-NotContains $exporter 'BuildOutlineNormalization' 'DXF exporter must not reintroduce hidden or inferred outline normalization.'
Assert-Contains $registrationEstimator 'DominantAxisAlignedOutlineSelector.Select' 'Registration estimator must select dominant structural outlines from evidence.'
Assert-Contains $registrationEstimator 'accepted.Length == 1 && accepted[0].EvidenceComplete' 'Registration estimator must require exactly one evidence-complete candidate before estimating registration.'
Assert-Contains $registrationEstimator 'estimated.Evidence.Scale.GetValueOrDefault()' 'Non-unit scale must come from the accepted candidate evidence and be persisted in the registration transform.'
Assert-Contains $outlineSelector 'canonical dimensions cannot supply a hidden registration scale' 'Canonical dimensions must validate structural evidence, not infer hidden bbox normalization.'
Assert-Contains $exporter 'OutlineNormalization = null' 'DXF exporter must disable legacy outline normalization in the effective recipe.'
Assert-Contains $exporter 'ElectricalRecipeProjection.ProjectPoint' 'DXF exporter must project Electrical geometry through the shared recipe projection path.'
Assert-Contains $exporter 'recipe.RegistrationTransform' 'DXF exporter must apply the persisted registration transform during projection.'
Assert-Contains $exporter 'new SheetRegistrationTransform(1m, 0m, 0m, 0m)' 'Final exported outline audit must inspect native projected coordinates without another inferred transform.'
Assert-Contains $exporter 'ThrowDominantOutlineManualReview("Exported Electrical"' 'Final exported outline audit must fail closed when the dominant structural outline is not evidenced.'
Assert-Contains $writer 'outline-congruence-audit.json' 'Plan-set manifest writer must emit outline-congruence-audit.json.'
Assert-Contains $humanSummary 'Outline congruence' 'Human summary must explain outline trust.'
Assert-Contains $verifier 'OutlineCongruenceStatus' 'Verifier output must print outline congruence status.'
Assert-Contains $verifier 'FinalNativeDominantStructuralOutlineEdgesAndSize' 'Verifier must require the native dominant structural final-output audit mode.'
Assert-Contains $verifier 'NativeEdgeResidualsWithinTolerance' 'Verifier must gate automatic proof on native final edge residuals.'

Write-Output 'Outline congruence contract is wired.'
