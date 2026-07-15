$ErrorActionPreference = "Stop"

$writer = Join-Path $PSScriptRoot "..\src\FloorplanFit.Infrastructure\Storage\PlanSetExportManifestWriter.cs"
$source = Get-Content -Raw $writer

@(
  "input-audit.json",
  "canonical-recipe-audit.json",
  "floorplan-impact-audit.json",
  "electrical-registration-audit.json",
  "electrical-projection-audit.json",
  "outline-congruence-audit.json",
  "outline-segment-congruence-audit.json",
  "final-output-congruence-audit.json",
  "dxf-safety-audit.json"
) | ForEach-Object {
  if ($source -notmatch [regex]::Escape($_)) {
    throw "Missing HousePlanSet observability artifact in PlanSetExportManifestWriter: $_"
  }
}

"PlanSet observability audit artifacts are wired in PlanSetExportManifestWriter."
