$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$builder = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Infrastructure/Storage/PlanSetOutlineSegmentCongruenceAuditBuilder.cs')
$writer = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Infrastructure/Storage/PlanSetExportManifestWriter.cs')
$verifier = Get-Content -Raw (Join-Path $root 'scripts/verify-latest-plan-set-recipe-manifest.ps1')
$verifierSelfCheck = Get-Content -Raw (Join-Path $root 'scripts/test-verify-latest-plan-set-recipe-manifest.ps1')
$summary = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Application/PlanSets/ExportAudit/CreateMultiSheetExportAuditHandler.cs')

function Assert-Contains([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -notmatch [regex]::Escape($Pattern)) {
        throw $Message
    }
}

Assert-Contains $writer 'outline-segment-congruence-audit.json' 'Manifest writer must emit segment congruence audit artifact.'
Assert-Contains $builder 'LINE' 'Segment audit must inspect LINE entities.'
Assert-Contains $builder 'LWPOLYLINE' 'Segment audit must inspect LWPOLYLINE entities.'
Assert-Contains $builder 'POLYLINE' 'Segment audit must inspect POLYLINE/VERTEX entities.'
Assert-Contains $builder 'AddEntitySegments' 'Segment audit must use a structural entity allow-list instead of accepting TEXT/INSERT/cables.'
Assert-Contains $builder 'MinimumSegmentLength' 'Segment audit must discard small noise segments.'
Assert-Contains $builder 'NormalizeWallCenterlines' 'Segment audit must compare logical wall centerline runs, not raw drafting wall edges.'
Assert-Contains $builder 'MergeCollinear' 'Segment audit must merge fragmented collinear wall runs before comparing.'
Assert-Contains $builder 'SelectOutlineSegments' 'Segment audit must gate on required outline coverage instead of every internal wall drafting difference.'
Assert-Contains $builder 'BuildOutlineEdgeCoverage' 'Segment audit must report left/right/top/bottom edge coverage explicitly.'
Assert-Contains $builder 'BuildCornerCoverage' 'Segment audit must report principal corner coverage explicitly.'
Assert-Contains $builder 'AdvisoryExtraElectricalWallRun' 'Segment audit must keep dependent-sheet extra wall runs observable without making them automatic blockers.'
Assert-Contains $builder 'SegmentMismatchRequiresManualReview' 'Segment mismatch must not silently pass.'
Assert-Contains $builder 'BinaryDxfSentinel' 'Segment audit must be able to read exported binary Electrical DXF files.'
Assert-Contains $verifier 'OutlineSegmentCongruenceStatus' 'Verifier must print segment congruence status.'
Assert-Contains $verifier 'StructuralOutlineCoverageWithWallRunAdvisory' 'Verifier must reject stale segment audit comparison modes.'
Assert-Contains $verifier 'RequiredOutlineSegmentCount' 'Verifier must require required outline segment evidence.'
Assert-Contains $verifier 'OutlineEdges' 'Verifier must require left/right/top/bottom edge evidence.'
Assert-Contains $verifier 'CornerCoverage' 'Verifier must require principal corner evidence.'
Assert-Contains $verifier 'BottomLeft' 'Verifier must require named principal corner rows, not just any four rows.'
Assert-Contains $verifier 'Left' 'Verifier must require named outline edge rows, not just any four rows.'
Assert-Contains $verifier 'outline-segment-congruence-audit.json' 'Verifier must require segment congruence audit artifact.'
Assert-Contains $verifierSelfCheck 'bbox OK but bad structural segment congruence' 'Verifier self-check must fail bbox-OK/segment-bad proof.'
Assert-Contains $summary 'Segment congruence' 'Human summary must mention segment-level evidence.'

Write-Output 'Outline segment congruence contract is wired.'
