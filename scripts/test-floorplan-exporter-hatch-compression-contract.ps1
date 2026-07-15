$ErrorActionPreference = "Stop"

$exporterPath = Join-Path $PSScriptRoot "..\src\FloorplanFit.Infrastructure\Dxf\IxMiliaAdjustedSitePlanExporter.cs"
$exporter = Get-Content -Raw -Path $exporterPath

if ($exporter -notmatch 'case\s+"HATCH"\s*:\s*\r?\n\s*ReplaceRepeatedPointPairs\(record,\s*"10",\s*"20",\s*steps\);') {
    throw "IxMiliaAdjustedSitePlanExporter must compress HATCH 10/20 boundary points so FloorPlan visible footprint does not keep stale source geometry."
}

Write-Host "floorplan exporter HATCH compression contract passed"
