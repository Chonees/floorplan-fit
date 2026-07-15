$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$exporter = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs')
$projection = Get-Content -Raw (Join-Path $root 'src/FloorplanFit.Application/PlanSets/Projection/ElectricalRecipeProjection.cs')
$tests = Get-Content -Raw (Join-Path $root 'tests/FloorplanFit.Infrastructure.Tests/Dxf/ProjectedPlanSheetDxfExporterTests.cs')
$projectionTests = Get-Content -Raw (Join-Path $root 'tests/FloorplanFit.Application.Tests/PlanSets/Projection/ElectricalRecipeProjectionTests.cs')

function Assert-Contains([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -notmatch [regex]::Escape($Pattern)) {
        throw $Message
    }
}

Assert-Contains $exporter 'TryCollectRegisteredAnchorBounds' 'Exporter must compute dependent-sheet anchor bounds before replaying the canonical recipe.'
Assert-Contains $exporter 'IsRegistrationAnchorLayer' 'Exporter must prefer generic wall/exterior/structural layer families for registration anchors.'
Assert-Contains $exporter 'dependent-sheet edge anchor' 'Exporter audit must explain when an out-of-bounds canonical pinch used the dependent sheet edge anchor.'
Assert-Contains $exporter 'cumulativeEdgeOffset' 'Exporter must offset repeated edge-anchor operations so multiple top/right pinches stack instead of collapsing to one delta.'
Assert-Contains $exporter 'CoordinateTolerance' 'Exporter audit matching must tolerate CAD edge-coordinate jitter.'
Assert-Contains $projection 'CoordinateTolerance' 'Electrical recipe projection must tolerate CAD edge-coordinate jitter.'
Assert-Contains $tests 'ExportAsync_anchors_out_of_bounds_top_recipe_to_electrical_wall_edge' 'A regression test must cover out-of-bounds top recipe anchoring.'
Assert-Contains $tests '89.995' 'Exporter regression must cover near-edge wall geometry, not only exact max-edge geometry.'
Assert-Contains $projectionTests 'ProjectPoint_treats_tiny_coordinate_jitter_as_same_pinch_edge' 'Application projection regression must cover tiny coordinate jitter at pinch edges.'

Write-Output 'Electrical edge-anchor contract is wired.'
