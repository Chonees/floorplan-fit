param(
    [string]$Root = "src\FloorplanFit.Desktop\bin\Debug\net10.0\workspace\exports\plan-sets",
    [decimal]$ToleranceInches = 0.5
)

$ErrorActionPreference = "Stop"

$manifest = Get-ChildItem -Path $Root -Recurse -Filter manifest.json -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $manifest) {
    throw "No manifest.json found under '$Root'. Export a HousePlanSet package first."
}

$json = Get-Content -Raw -Path $manifest.FullName | ConvertFrom-Json
function Get-JsonString($Object, $Name) {
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property -or $null -eq $property.Value) {
        return ""
    }

    return [string]$property.Value
}

$floor = @($json.Sheets | Where-Object { (Get-JsonString $_ "SheetKind") -eq "CanonicalFloorPlan" } | Select-Object -First 1)
$electrical = @($json.Sheets | Where-Object { (Get-JsonString $_ "SheetKind") -eq "ElectricalPlan" } | Select-Object -First 1)
if ($floor.Count -eq 0 -or $electrical.Count -eq 0) {
    throw "Manifest '$($manifest.FullName)' must contain CanonicalFloorPlan and ElectricalPlan sheets."
}

$floorPath = Get-JsonString $floor[0] "StoragePath"
$electricalPath = Get-JsonString $electrical[0] "StoragePath"
if (!(Test-Path -LiteralPath $floorPath)) {
    throw "FloorPlan output DXF '$floorPath' does not exist."
}

if (!(Test-Path -LiteralPath $electricalPath)) {
    throw "ElectricalPlan output DXF '$electricalPath' does not exist."
}

$payload = @{
    manifest = $manifest.FullName
    floor = $floorPath
    electrical = $electricalPath
    tolerance = [double]$ToleranceInches
} | ConvertTo-Json -Compress
$env:FF_FINAL_OUTPUT_CONGRUENCE_PAYLOAD = $payload

$python = @'
import json
import os
import sys

try:
    import ezdxf
except Exception as exc:
    raise SystemExit(f"Python package ezdxf is required for this diagnostic script: {exc}")

payload = json.loads(os.environ["FF_FINAL_OUTPUT_CONGRUENCE_PAYLOAD"])
TOLERANCE = 0.05
MINIMUM_EDGE_SUPPORT = 24.0

def is_structural(layer, include_electrical):
    name = (layer or "").upper()
    if not include_electrical and "ELECTRICAL" in name:
        return False
    return "WALL" in name or "EXTERIOR" in name or "STRUCT" in name

def add_segment(segments, x1, y1, x2, y2):
    horizontal = abs(y1 - y2) <= TOLERANCE
    vertical = abs(x1 - x2) <= TOLERANCE
    length = abs(x2 - x1) if horizontal else abs(y2 - y1) if vertical else 0.0
    if (horizontal or vertical) and length >= 6.0:
        segments.append((x1, y1, x2, y2, horizontal, vertical, length))

def entity_points(entity):
    kind = entity.dxftype()
    if kind == "LINE":
        start = entity.dxf.start
        end = entity.dxf.end
        return [(start.x, start.y), (end.x, end.y)]
    if kind == "LWPOLYLINE":
        return [(point[0], point[1]) for point in entity.get_points()]
    if kind == "POLYLINE":
        return [(vertex.dxf.location.x, vertex.dxf.location.y) for vertex in entity.vertices]
    if kind in ("SOLID", "3DFACE"):
        points = []
        for name in ("vtx0", "vtx1", "vtx2", "vtx3"):
            if entity.dxf.hasattr(name):
                point = getattr(entity.dxf, name)
                points.append((point.x, point.y))
        return points
    return []

def raw_bounds(segments):
    xs = [value for segment in segments for value in (segment[0], segment[2])]
    ys = [value for segment in segments for value in (segment[1], segment[3])]
    return bounds_from_coordinates(xs, ys)

def snap(value):
    return round(value / TOLERANCE) * TOLERANCE

def supported_coordinates(support, fallback):
    supported = [coordinate for coordinate, length in support.items() if length >= MINIMUM_EDGE_SUPPORT]
    return supported or [snap(value) for value in fallback]

def supported_bounds(segments):
    x_support = {}
    y_support = {}
    for x1, y1, x2, y2, horizontal, vertical, length in segments:
        if vertical:
            key = snap(x1)
            x_support[key] = x_support.get(key, 0.0) + length
        elif horizontal:
            key = snap(y1)
            y_support[key] = y_support.get(key, 0.0) + length
    xs = supported_coordinates(x_support, [value for segment in segments for value in (segment[0], segment[2])])
    ys = supported_coordinates(y_support, [value for segment in segments for value in (segment[1], segment[3])])
    return bounds_from_coordinates(xs, ys)

def bounds_from_coordinates(xs, ys):
    return {
        "minX": min(xs),
        "minY": min(ys),
        "maxX": max(xs),
        "maxY": max(ys),
        "width": max(xs) - min(xs),
        "height": max(ys) - min(ys),
    }

def structural_footprint(path, include_electrical):
    doc = ezdxf.readfile(path)
    segments = []
    entity_count = 0
    for entity in doc.modelspace():
        layer = entity.dxf.layer if entity.dxf.hasattr("layer") else ""
        if not is_structural(layer, include_electrical):
            continue
        points = entity_points(entity)
        if len(points) < 2:
            continue
        entity_count += 1
        for index in range(len(points) - 1):
            add_segment(segments, points[index][0], points[index][1], points[index + 1][0], points[index + 1][1])
        if entity.dxftype() in ("SOLID", "3DFACE") and len(points) > 2:
            add_segment(segments, points[-1][0], points[-1][1], points[0][0], points[0][1])
    if not segments:
        raise SystemExit(f"No comparable structural line/polyline segments found in {path}")
    supported = supported_bounds(segments)
    raw = raw_bounds(segments)
    supported["entityCount"] = entity_count
    supported["segmentCount"] = len(segments)
    raw["entityCount"] = entity_count
    raw["segmentCount"] = len(segments)
    return supported, raw

floor, floor_raw = structural_footprint(payload["floor"], include_electrical=False)
electrical, electrical_raw = structural_footprint(payload["electrical"], include_electrical=True)
width_mismatch = electrical["width"] - floor["width"]
height_mismatch = electrical["height"] - floor["height"]
raw_width_mismatch = electrical_raw["width"] - floor_raw["width"]
raw_height_mismatch = electrical_raw["height"] - floor_raw["height"]
status = "FinalOutputCongruent" if abs(width_mismatch) <= payload["tolerance"] and abs(height_mismatch) <= payload["tolerance"] else "FinalOutputMismatchRequiresReview"
result = {
    "manifest": payload["manifest"],
    "status": status,
    "comparisonMode": "FinalExportedSupportedStructuralFootprint",
    "toleranceInches": payload["tolerance"],
    "floorPath": payload["floor"],
    "electricalPath": payload["electrical"],
    "floorBounds": floor,
    "electricalBounds": electrical,
    "floorRawBounds": floor_raw,
    "electricalRawBounds": electrical_raw,
    "widthMismatchInches": width_mismatch,
    "heightMismatchInches": height_mismatch,
    "rawWidthMismatchInches": raw_width_mismatch,
    "rawHeightMismatchInches": raw_height_mismatch,
}
print(json.dumps(result, indent=2))
raise SystemExit(0 if status == "FinalOutputCongruent" else 2)
'@

$python | python -
if ($LASTEXITCODE -ne 0) {
    throw "Final output FloorPlan/Electrical congruence failed for latest manifest '$($manifest.FullName)'."
}
