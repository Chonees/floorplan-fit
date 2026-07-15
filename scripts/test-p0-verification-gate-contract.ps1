$ErrorActionPreference = 'Stop'

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Pattern,
        [Parameter(Mandatory = $true)][string] $Message
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Message Missing file: $Path"
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -notmatch $Pattern) {
        throw $Message
    }
}

$reportPath = 'src/FloorplanFit.Contracts/PlanSets/PlanSetVerificationReportDto.cs'
$auditDtoPath = 'src/FloorplanFit.Contracts/PlanSets/MultiSheetExportAuditDto.cs'
$writerInterfacePath = 'src/FloorplanFit.Application/Abstractions/IPlanSetExportManifestWriter.cs'
$handlerPath = 'src/FloorplanFit.Application/PlanSets/ExportAudit/CreateMultiSheetExportAuditHandler.cs'
$writerPath = 'src/FloorplanFit.Infrastructure/Storage/PlanSetExportManifestWriter.cs'
$segmentBuilderPath = 'src/FloorplanFit.Infrastructure/Storage/PlanSetOutlineSegmentCongruenceAuditBuilder.cs'
$viewModelPath = 'src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs'
$verifierPath = 'scripts/verify-latest-plan-set-recipe-manifest.ps1'
$testsPath = 'tests/FloorplanFit.Application.Tests/PlanSets/ExportAudit/CreateMultiSheetExportAuditHandlerTests.cs'
$writerTestsPath = 'tests/FloorplanFit.Infrastructure.Tests/PlanSets/PlanSetExportManifestWriterTests.cs'

Assert-FileContains $reportPath 'record\s+PlanSetVerificationReportDto' `
    'P0: the typed PlanSetVerificationReportDto contract is missing.'
Assert-FileContains $reportPath 'PlanSetVerificationDecision' `
    'P0: verification decision must be typed.'
Assert-FileContains $reportPath 'PlanSetVerificationReasonCode' `
    'P0: verification reasons must use typed reason codes.'
Assert-FileContains $reportPath 'SchemaVersion' `
    'P0: the verification report must expose an explicit schemaVersion.'
Assert-FileContains $reportPath 'Outputs' `
    'P0: expected/present output evidence is missing from the typed report.'
Assert-FileContains $reportPath 'DxfSafety' `
    'P0: DXF safety evidence is missing from the typed report.'
Assert-FileContains $reportPath 'FloorPlanOperations' `
    'P0: canonical FloorPlan operation evidence is missing from the typed report.'
Assert-FileContains $reportPath 'DependentOperations' `
    'P0: dependent operation evidence is missing from the typed report.'
Assert-FileContains $reportPath 'OutlineCongruence' `
    'P0: outline congruence evidence is missing from the typed report.'
Assert-FileContains $reportPath 'SegmentCongruence' `
    'P0: segment congruence evidence is missing from the typed report.'
Assert-FileContains $reportPath 'FinalOutputCongruence' `
    'P0: final FloorPlan-vs-Electrical evidence is missing from the typed report.'
Assert-FileContains $reportPath 'Capabilities' `
    'P0: unsupported capability evidence is missing from the typed report.'

Assert-FileContains $auditDtoPath 'PlanSetVerificationReportDto\?\s+Verification' `
    'P0: manifest DTO does not carry the typed verification report.'
Assert-FileContains $auditDtoPath 'SchemaVersion' `
    'P0: manifest DTO does not expose an explicit schemaVersion.'
Assert-FileContains $writerInterfacePath 'BuildVerificationReport' `
    'P0: the existing manifest/audit boundary cannot build the typed report before persistence.'
Assert-FileContains $handlerPath 'BuildVerificationReport' `
    'P0: CreateMultiSheetExportAuditHandler does not build the typed report.'
Assert-FileContains $handlerPath 'verificationReport\.IsGreen\s*\?\s*PlanSetExportStatus\.ReadyForExport' `
    'P0: PlanSetExportStatus is not derived exclusively from the green typed report.'

$handlerContent = Get-Content -LiteralPath $handlerPath -Raw
$reportIndex = $handlerContent.IndexOf('BuildVerificationReport', [System.StringComparison]::Ordinal)
$persistedStatusIndex = $handlerContent.IndexOf('new PlanSetExport(', [System.StringComparison]::Ordinal)
if ($reportIndex -lt 0 -or $persistedStatusIndex -lt 0 -or $reportIndex -gt $persistedStatusIndex) {
    throw 'P0: PlanSetExport status is calculated/persisted before the verification report exists.'
}
if ($handlerContent -match 'ManualConfirmationRequiredSheetCount\s*==\s*0\s*\?\s*PlanSetExportStatus\.ReadyForExport') {
    throw 'P0: legacy manual-count logic still governs ReadyForExport.'
}

Assert-FileContains $writerPath 'PlanSetVerificationReportDto\s+BuildVerificationReport' `
    'P0: the existing writer does not aggregate audits into the typed report.'
Assert-FileContains $segmentBuilderPath 'BuildVerification' `
    'P0: segment congruence is not exposed as typed verification evidence.'
Assert-FileContains $segmentBuilderPath 'BuildFinalOutputVerification' `
    'P0: final output congruence is not exposed as typed verification evidence.'
Assert-FileContains $viewModelPath 'Verification\?\.IsGreen\s*==\s*true' `
    'P0: Desktop can still call a package ready without a green typed report.'
Assert-FileContains $verifierPath 'Verification' `
    'P0: PowerShell verifier does not read the typed verification decision.'
Assert-FileContains $verifierPath 'schemaVersion' `
    'P0: PowerShell verifier does not enforce the report schemaVersion.'

Assert-FileContains $testsPath 'green_verification_report_is_the_only_path_to_ready_for_export' `
    'P0: missing pass-case regression for the typed ReadyForExport gate.'
Assert-FileContains $testsPath 'mismatch_verification_report_cannot_be_ready_for_export' `
    'P0: missing mismatch fail-closed regression.'
Assert-FileContains $testsPath 'missing_required_evidence_cannot_be_ready_for_export' `
    'P0: missing required-artifact fail-closed regression.'
Assert-FileContains $testsPath 'unsupported_capability_cannot_be_ready_for_export' `
    'P0: missing unsupported-capability fail-closed regression.'
Assert-FileContains $writerTestsPath 'BuildVerificationReport' `
    'P0: typed audit aggregation lacks focused infrastructure coverage.'

Write-Host 'P0 typed verification gate contract passed'
