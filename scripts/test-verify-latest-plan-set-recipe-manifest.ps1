Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$verifier = Join-Path $PSScriptRoot "verify-latest-plan-set-recipe-manifest.ps1"
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("ff-manifest-check-" + [guid]::NewGuid().ToString("N"))

function Write-TestDxf($Path) {
    @"
0
SECTION
2
ENTITIES
0
LINE
8
ELECTRICAL WALLS
10
0
20
0
11
1
21
1
0
INSERT
8
ELECTRICAL SYMBOLS
10
1
20
1
0
TEXT
8
NOTES
10
2
20
2
1
NOTE
0
CIRCLE
8
ELECTRICAL WALLS
10
3
20
3
40
0.25
0
ARC
8
ELECTRICAL WALLS
10
4
20
4
40
0.25
50
0
51
90
0
ELLIPSE
8
ELECTRICAL WALLS
10
5
20
5
11
0.5
21
0
0
DIMENSION
8
DIMS
10
6
20
6
0
LWPOLYLINE
8
WIRING
10
7
20
7
10
8
20
8
0
SPLINE
8
WIRING
10
9
20
9
0
LINE
8
WIRING
10
10
20
10
11
11
21
11
0
TEXT
8
NOTES
10
12
20
12
1
NOTE2
0
ENDSEC
0
EOF
"@ | Set-Content -Path $Path -Encoding Ascii
}

function Write-SparseDxf($Path) {
    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.AddRange([string[]]@("0", "SECTION", "2", "ENTITIES"))
    for ($i = 0; $i -lt 12; $i++) {
        $lines.AddRange([string[]]@(
            "0", "LINE",
            "8", "ELECTRICAL WALLS",
            "10", [string]$i,
            "20", [string]$i,
            "11", [string]($i + 1),
            "21", [string]($i + 1)))
    }
    $lines.AddRange([string[]]@("0", "ENDSEC", "0", "EOF"))
    $lines | Set-Content -Path $Path -Encoding Ascii
}

function Write-BinaryStringPair($Writer, [int16]$Code, [string]$Value) {
    $Writer.Write($Code)
    $Writer.Write([System.Text.Encoding]::ASCII.GetBytes($Value))
    $Writer.Write([byte]0)
}

function Write-BinaryTestDxf($Path) {
    $writer = [System.IO.BinaryWriter]::new([System.IO.File]::Create($Path), [System.Text.Encoding]::GetEncoding(28591))
    try {
        $sentinel = "AutoCAD Binary DXF`r`n" + [string][char]0x1A + [string][char]0
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes($sentinel))
        Write-BinaryStringPair $writer 0 "SECTION"
        Write-BinaryStringPair $writer 2 "ENTITIES"
        Write-BinaryStringPair $writer 0 "LINE"
        Write-BinaryStringPair $writer 0 "INSERT"
        Write-BinaryStringPair $writer 0 "TEXT"
        Write-BinaryStringPair $writer 0 "CIRCLE"
        Write-BinaryStringPair $writer 0 "ARC"
        Write-BinaryStringPair $writer 0 "ELLIPSE"
        Write-BinaryStringPair $writer 0 "DIMENSION"
        Write-BinaryStringPair $writer 0 "LWPOLYLINE"
        Write-BinaryStringPair $writer 0 "SPLINE"
        Write-BinaryStringPair $writer 0 "LINE"
        Write-BinaryStringPair $writer 0 "TEXT"
        Write-BinaryStringPair $writer 0 "ENDSEC"
        Write-BinaryStringPair $writer 0 "EOF"
    }
    finally {
        $writer.Dispose()
    }
}

function Write-Manifest($Directory, $RecipeSummary, $Status = "RequiresManualConfirmation", $StoragePath = $null, $Warning = "Needs review", $CanonicalAdjustmentId = ([guid]::NewGuid().ToString()), [bool]$IncludeInputDimensions = $true, [bool]$IncludeProjectionOperations = $true, [bool]$IncludeFloorPlanImpactOperations = $true, [int]$DxfSafetyUnsupportedCrossingEntityCount = 0, [bool]$IncludeElectricalOperationFields = $true) {
    New-Item -ItemType Directory -Path $Directory | Out-Null
    $storageJson = if ([string]::IsNullOrWhiteSpace($StoragePath)) { "null" } else { '"' + ($StoragePath -replace '\\', '\\') + '"' }
    $warningJson = if ([string]::IsNullOrWhiteSpace($Warning)) { "null" } else { '"' + $Warning + '"' }
    $canonicalAdjustmentJson = if ([string]::IsNullOrWhiteSpace($CanonicalAdjustmentId)) { "" } else { '  "CanonicalAdjustmentId": "' + $CanonicalAdjustmentId + '",' + [Environment]::NewLine }
    $verificationDecision = if ($Status -eq "ProjectedAutomatically") { "ReadyForExport" } else { "Blocked" }
    $verificationReasonsJson = if ($verificationDecision -eq "ReadyForExport") {
        "[]"
    }
    else {
        '[{ "Code": "MissingRequiredEvidence", "Check": "fixture", "SheetId": null, "Detail": "Synthetic fixture requires manual confirmation." }]'
    }
    @"
{
  "schemaVersion": 2,
${canonicalAdjustmentJson}
  "Status": "$Status",
  "Verification": {
    "schemaVersion": 1,
    "Decision": "$verificationDecision",
    "Reasons": $verificationReasonsJson
  },
  "Sheets": [
    { "SheetKind": "CanonicalFloorPlan" },
    {
      "SheetKind": "ElectricalPlan",
      "Status": "$Status",
      "StoragePath": $storageJson,
      "Warning": $warningJson,
      "RecipeHandlingSummary": "$RecipeSummary"
    }
  ]
}
"@ | Set-Content -Path (Join-Path $Directory "manifest.json") -Encoding UTF8
    Write-AuditFiles $Directory $Status $Warning $IncludeInputDimensions $IncludeProjectionOperations $IncludeFloorPlanImpactOperations $DxfSafetyUnsupportedCrossingEntityCount $IncludeElectricalOperationFields
}

function Write-AuditFiles($Directory, $Status, $Warning, [bool]$IncludeInputDimensions = $true, [bool]$IncludeProjectionOperations = $true, [bool]$IncludeFloorPlanImpactOperations = $true, [int]$DxfSafetyUnsupportedCrossingEntityCount = 0, [bool]$IncludeElectricalOperationFields = $true) {
    $audit = Join-Path $Directory "audit"
    New-Item -ItemType Directory -Path $audit | Out-Null
    $operationStatus = if ($Status -eq "RequiresManualConfirmation") { "RequiresManualReview" } else { "Applied" }
    $reasonJson = if ([string]::IsNullOrWhiteSpace($Warning)) { "null" } else { '"' + $Warning + '"' }

    if ($IncludeInputDimensions) {
        @"
{
  "stage": "input",
  "dimensionsInches": {
    "originalWidthInches": 39.0,
    "originalHeightInches": 77.5,
    "requestedWidthInches": 38.7,
    "requestedHeightInches": 77.4,
    "requiredWidthDeltaInches": 0.3,
    "requiredHeightDeltaInches": 0.1
  }
}
"@ | Set-Content -Path (Join-Path $audit "input-audit.json") -Encoding UTF8
    }
    else {
        '{ "stage": "input" }' | Set-Content -Path (Join-Path $audit "input-audit.json") -Encoding UTF8
    }
    @"
{
  "stage": "canonical-recipe",
  "operationCount": 1,
  "recipe": {
    "Operations": [
      {
        "Kind": "HorizontalCompression",
        "AxisTag": "Width",
        "Edge": "Right",
        "Coordinate": 50.0,
        "DeltaSourceUnits": 2.0
      }
    ]
  }
}
"@ | Set-Content -Path (Join-Path $audit "canonical-recipe-audit.json") -Encoding UTF8
    $floorPlanOperationsJson = if ($IncludeFloorPlanImpactOperations) {
        @"
[
    {
      "operationId": "operation-0",
      "operationIndex": 0,
      "Kind": "HorizontalCompression",
      "AxisTag": "Width",
      "Edge": "Right",
      "Coordinate": 50.0,
      "expectedDeltaSourceUnits": 2.0,
      "affectedEntities": 2,
      "affectedVertices": 4,
      "measuredMinDeltaSourceUnits": 2.0,
      "measuredMaxDeltaSourceUnits": 2.0,
      "Status": "Applied",
      "Warning": null
    }
  ]
"@
    }
    else {
        "[]"
    }
    @"
{
  "stage": "floorplan-impact",
  "operations": $floorPlanOperationsJson
}
"@ | Set-Content -Path (Join-Path $audit "floorplan-impact-audit.json") -Encoding UTF8
    '{ "stage": "electrical-registration", "sheets": [] }' | Set-Content -Path (Join-Path $audit "electrical-registration-audit.json") -Encoding UTF8
    $operationsJson = if ($IncludeProjectionOperations -and $IncludeElectricalOperationFields) {
        @"
[
        {
          "OperationId": "operation-0",
          "OperationIndex": 0,
          "Kind": "HorizontalCompression",
          "AxisTag": "Width",
          "Edge": "Right",
          "Coordinate": 50.0,
          "ExpectedDeltaSourceUnits": 2.0,
          "AffectedEntities": 2,
          "AffectedVertices": 4,
          "MeasuredMinDeltaSourceUnits": 2.0,
          "MeasuredMaxDeltaSourceUnits": 2.0,
          "Status": "$operationStatus",
          "Reason": $reasonJson
        }
      ]
"@
    }
    elseif ($IncludeProjectionOperations) {
        @"
[
        {
          "Status": "$operationStatus",
          "Reason": $reasonJson
        }
      ]
"@
    }
    else {
        "[]"
    }

    @"
{
  "stage": "electrical-projection",
  "sheets": [
    {
      "operations": $operationsJson
    }
  ]
}
"@ | Set-Content -Path (Join-Path $audit "electrical-projection-audit.json") -Encoding UTF8
    @"
{
  "stage": "dxf-safety",
  "dependentSheets": [
    {
      "SheetKind": "ElectricalPlan",
      "Status": "$Status",
      "dxfSafety": {
        "OutputFileExists": true,
        "OutputFileBytes": 1024,
        "EntityCountBefore": 12,
        "EntityCountAfter": 12,
        "InsertCountAfter": 1,
        "DimensionCountAfter": 1,
        "EllipseCountAfter": 1,
        "WireOrCurveCountAfter": 1,
        "MissingHandleCountAfter": 0,
        "MissingOwnerCountAfter": 0,
        "UnsupportedCrossingEntityCount": $DxfSafetyUnsupportedCrossingEntityCount
      }
    }
  ]
}
"@ | Set-Content -Path (Join-Path $audit "dxf-safety-audit.json") -Encoding UTF8
    @"
{
  "stage": "outline-congruence",
  "sheets": [
    {
      "outlineCongruence": {
        "Status": "Congruent",
        "Reason": "Electrical structural outline already matches the canonical FloorPlan outline within tolerance.",
        "ToleranceInches": 0.05,
        "NormalizationApplied": false,
        "CanonicalSourceOutline": { "MinX": 0.0, "MinY": 0.0, "MaxX": 39.0, "MaxY": 77.5, "Width": 39.0, "Height": 77.5 },
        "ElectricalSourceOutline": { "MinX": 0.0, "MinY": 0.0, "MaxX": 39.0, "MaxY": 77.5, "Width": 39.0, "Height": 77.5 },
        "ElectricalNormalizedSourceOutline": { "MinX": 0.0, "MinY": 0.0, "MaxX": 39.0, "MaxY": 77.5, "Width": 39.0, "Height": 77.5 },
        "ElectricalExportOutline": { "MinX": 0.0, "MinY": 0.0, "MaxX": 37.0, "MaxY": 77.5, "Width": 37.0, "Height": 77.5 },
        "SourceWidthMismatchInches": 0.0,
        "SourceHeightMismatchInches": 0.0,
        "ExportWidthMismatchInches": 0.0,
        "ExportHeightMismatchInches": 0.0,
        "AnchorX": null,
        "AnchorY": null,
        "ScaleX": null,
        "ScaleY": null
      }
    }
  ]
}
"@ | Set-Content -Path (Join-Path $audit "outline-congruence-audit.json") -Encoding UTF8
    @"
{
  "stage": "outline-segment-congruence",
  "toleranceInches": 0.05,
  "sheets": [
    {
      "segmentCongruence": {
        "Status": "SegmentCongruent",
        "Reason": "Required structural outline wall runs are covered after bbox-normalized comparison.",
        "ToleranceInches": 0.05,
        "CenterlineToleranceInches": 0.5,
        "OutlineMatchToleranceInches": 2.0,
        "ComparisonMode": "StructuralOutlineCoverageWithWallRunAdvisory",
        "FloorSegmentCount": 4,
        "ElectricalSegmentCount": 4,
        "RequiredOutlineSegmentCount": 4,
        "OutlineEdges": [
          { "Edge": "Left", "RequiredCount": 1, "MissingCount": 0, "Mismatches": [] },
          { "Edge": "Right", "RequiredCount": 1, "MissingCount": 0, "Mismatches": [] },
          { "Edge": "Bottom", "RequiredCount": 1, "MissingCount": 0, "Mismatches": [] },
          { "Edge": "Top", "RequiredCount": 1, "MissingCount": 0, "Mismatches": [] }
        ],
        "CornerCoverage": [
          { "Corner": "BottomLeft", "DeltaX": 0, "DeltaY": 0, "IsCovered": true },
          { "Corner": "BottomRight", "DeltaX": 0, "DeltaY": 0, "IsCovered": true },
          { "Corner": "TopLeft", "DeltaX": 0, "DeltaY": 0, "IsCovered": true },
          { "Corner": "TopRight", "DeltaX": 0, "DeltaY": 0, "IsCovered": true }
        ],
        "MissingInElectricalCount": 0,
        "ExtraInElectricalCount": 0,
        "AdvisoryMissingInternalWallRunCount": 0,
        "AdvisoryExtraElectricalWallRunCount": 0,
        "AdvisoryMismatches": [],
        "Mismatches": []
      }
    }
  ]
}
"@ | Set-Content -Path (Join-Path $audit "outline-segment-congruence-audit.json") -Encoding UTF8
    @"
{
  "stage": "final-output-congruence",
  "toleranceInches": 0.5,
  "sheets": [
    {
      "finalOutputCongruence": {
        "Status": "FinalOutputCongruent",
        "Reason": "Final exported FloorPlan and ElectricalPlan native dominant structural outlines are congruent within tolerance.",
        "ToleranceInches": 0.5,
        "ComparisonMode": "FinalNativeDominantStructuralOutlineEdgesAndSize",
        "FloorOutputPath": "floor.dxf",
        "ElectricalOutputPath": "electrical.dxf",
        "FloorStructuralSegmentCount": 4,
        "ElectricalStructuralSegmentCount": 4,
        "FloorStructuralBounds": { "MinX": 0.0, "MinY": 0.0, "MaxX": 37.0, "MaxY": 77.5, "Width": 37.0, "Height": 77.5 },
        "ElectricalStructuralBounds": { "MinX": 0.0, "MinY": 0.0, "MaxX": 37.0, "MaxY": 77.5, "Width": 37.0, "Height": 77.5 },
        "WidthMismatchInches": 0.0,
        "HeightMismatchInches": 0.0,
        "SizeResidualsWithinTolerance": true,
        "NativeEdgeResidualsWithinTolerance": true,
        "EdgeDeltas": [
          { "Edge": "Left", "FloorCoordinate": 0.0, "ElectricalCoordinate": 0.0, "Delta": 0.0, "IsCovered": true },
          { "Edge": "Right", "FloorCoordinate": 37.0, "ElectricalCoordinate": 37.0, "Delta": 0.0, "IsCovered": true },
          { "Edge": "Bottom", "FloorCoordinate": 0.0, "ElectricalCoordinate": 0.0, "Delta": 0.0, "IsCovered": true },
          { "Edge": "Top", "FloorCoordinate": 77.5, "ElectricalCoordinate": 77.5, "Delta": 0.0, "IsCovered": true }
        ],
        "MissingInElectricalCount": 0,
        "ExtraInElectricalCount": 0,
        "Mismatches": []
      }
    }
  ]
}
"@ | Set-Content -Path (Join-Path $audit "final-output-congruence-audit.json") -Encoding UTF8
}

function Invoke-ManifestVerifier {
    param(
        [string]$Root,
        [switch]$AllowAffineOnly,
        [switch]$RequireAutomatic
    )

    $script:lastVerifierError = $null
    try {
        if ($AllowAffineOnly -and $RequireAutomatic) {
            & $verifier -Root $Root -AllowAffineOnly -RequireAutomatic *> $null
        }
        elseif ($AllowAffineOnly) {
            & $verifier -Root $Root -AllowAffineOnly *> $null
        }
        elseif ($RequireAutomatic) {
            & $verifier -Root $Root -RequireAutomatic *> $null
        }
        else {
            & $verifier -Root $Root *> $null
        }
        return 0
    }
    catch {
        $script:lastVerifierError = $_.Exception.Message
        return 1
    }
}

function Update-FinalOutputAudit {
    param(
        [string]$Directory,
        [scriptblock]$Update
    )

    $path = Join-Path (Join-Path $Directory "audit") "final-output-congruence-audit.json"
    $audit = Get-Content -Raw -LiteralPath $path | ConvertFrom-Json
    & $Update $audit.sheets[0].finalOutputCongruence
    $audit | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $path -Encoding UTF8
}

function Assert-AutomaticVerifierFailsOnField {
    param(
        [string]$Root,
        [string]$ExpectedField,
        [string]$Scenario
    )

    if ((Invoke-ManifestVerifier -Root $Root -RequireAutomatic) -eq 0) {
        throw "Expected $Scenario to fail on field '$ExpectedField', but the verifier passed."
    }

    if ($script:lastVerifierError -notmatch [regex]::Escape($ExpectedField)) {
        throw "Expected $Scenario error to name field '$ExpectedField', but got: $script:lastVerifierError"
    }
}

try {
    New-Item -ItemType Directory -Path $temp | Out-Null
    $compression = Join-Path $temp "compression"
    $proofAuthorized = Join-Path $temp "proof-authorized"
    $affine = Join-Path $temp "affine"
    $manual = Join-Path $temp "manual"
    $stale = Join-Path $temp "stale"
    $contradictoryManual = Join-Path $temp "contradictory-manual"
    $automaticWithoutRecipeAware = Join-Path $temp "automatic-without-recipe-aware"
    $missingCanonicalAdjustment = Join-Path $temp "missing-canonical-adjustment"
    $missingCanonicalRecipeOperations = Join-Path $temp "missing-canonical-recipe-operations"
    $missingInputAudit = Join-Path $temp "missing-input-audit"
    $missingFloorPlanImpactOperations = Join-Path $temp "missing-floorplan-impact-operations"
    $missingFloorPlanCanonicalOperationIndex = Join-Path $temp "missing-floorplan-canonical-operation-index"
    $floorPlanNoImpactMissingWarning = Join-Path $temp "floorplan-no-impact-missing-warning"
    $missingElectricalProjectionOperations = Join-Path $temp "missing-electrical-projection-operations"
    $missingElectricalProjectionOperationFields = Join-Path $temp "missing-electrical-projection-operation-fields"
    $missingElectricalCanonicalOperationIndex = Join-Path $temp "missing-electrical-canonical-operation-index"
    $ambiguousNoGeometryReason = Join-Path $temp "ambiguous-no-geometry-reason"
    $outlineMismatchNotNormalized = Join-Path $temp "outline-mismatch-not-normalized"
    $segmentMismatch = Join-Path $temp "segment-mismatch"
    $finalOutputMismatch = Join-Path $temp "final-output-mismatch"
    $formerFinalOutputMode = Join-Path $temp "former-final-output-mode"
    $missingFinalOutputSizeField = Join-Path $temp "missing-final-output-size-field"
    $missingFinalOutputNativeEdgeField = Join-Path $temp "missing-final-output-native-edge-field"
    $finalOutputSizeMismatch = Join-Path $temp "final-output-size-mismatch"
    $finalOutputNativeEdgeMismatch = Join-Path $temp "final-output-native-edge-mismatch"
    $unsafeDxfSafetyAudit = Join-Path $temp "unsafe-dxf-safety-audit"
    $binaryCompression = Join-Path $temp "binary-compression"
    $dxf = Join-Path $temp "electrical.dxf"
    $sparseDxf = Join-Path $temp "sparse-electrical.dxf"
    $binaryDxf = Join-Path $temp "binary-electrical.dxf"
    Write-TestDxf $dxf
    Write-SparseDxf $sparseDxf
    Write-BinaryTestDxf $binaryDxf

    Write-Manifest `
        $compression `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $dxf `
        $null
    Copy-Item -LiteralPath $compression -Destination $proofAuthorized -Recurse
    $proofOutlinePath = Join-Path (Join-Path $proofAuthorized "audit") "outline-congruence-audit.json"
    $proofOutlineAudit = Get-Content -Raw -LiteralPath $proofOutlinePath | ConvertFrom-Json
    $proofOutline = $proofOutlineAudit.sheets[0].outlineCongruence
    $proofOutline.Status = "RegistrationProofAuthorized"
    $proofOutline.Reason = "Source-bound whole-plan registration proof authorizes the canonical frame; bounds remain unmeasured at this stage."
    $proofOutline.NormalizationApplied = $false
    foreach ($propertyName in @(
            "CanonicalSourceOutline",
            "ElectricalSourceOutline",
            "ElectricalNormalizedSourceOutline",
            "ElectricalExportOutline",
            "SourceWidthMismatchInches",
            "SourceHeightMismatchInches",
            "ExportWidthMismatchInches",
            "ExportHeightMismatchInches",
            "ScaleX",
            "ScaleY")) {
        $proofOutline.$propertyName = $null
    }
    $proofOutlineAudit | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $proofOutlinePath -Encoding UTF8
    Write-Manifest `
        $affine `
        "ElectricalPlan: affine placement applied; no local compression operations."
    Write-Manifest `
        $manual `
        "ElectricalPlan: manual review required before recipe-aware DXF export: HorizontalCompression Right @50 delta 2." `
        "RequiresManualConfirmation" `
        $null `
        "CIRCLE crosses a canonical recipe pinch line."
    Write-Manifest `
        $stale `
        "ElectricalPlan: affine placement applied; local recipe requires review before DXF deformation: HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $dxf `
        $null
    Write-Manifest `
        $contradictoryManual `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations; manual review required: ELLIPSE crosses a canonical recipe pinch line." `
        "ProjectedAutomatically" `
        $dxf `
        $null
    Write-Manifest `
        $automaticWithoutRecipeAware `
        "ElectricalPlan: affine placement applied; HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $dxf `
        $null
    Write-Manifest `
        $missingCanonicalAdjustment `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $dxf `
        $null `
        ""
    Write-Manifest `
        $missingCanonicalRecipeOperations `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $dxf `
        $null
    '{ "stage": "canonical-recipe", "operationCount": 1 }' |
        Set-Content -Path (Join-Path (Join-Path $missingCanonicalRecipeOperations "audit") "canonical-recipe-audit.json") -Encoding UTF8
    Write-Manifest `
        $missingInputAudit `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $dxf `
        $null `
        ([guid]::NewGuid().ToString()) `
        $false
    Write-Manifest `
        $missingElectricalProjectionOperations `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $dxf `
        $null `
        ([guid]::NewGuid().ToString()) `
        $true `
        $false
    Write-Manifest `
        $missingElectricalProjectionOperationFields `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $dxf `
        $null `
        ([guid]::NewGuid().ToString()) `
        $true `
        $true `
        $true `
        0 `
        $false
    Write-Manifest `
        $missingElectricalCanonicalOperationIndex `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2, VerticalCompression Top @75 delta 1." `
        "ProjectedAutomatically" `
        $dxf `
        $null
    $missingIndexAudit = Join-Path $missingElectricalCanonicalOperationIndex "audit"
    '{ "stage": "canonical-recipe", "operationCount": 2 }' | Set-Content -Path (Join-Path $missingIndexAudit "canonical-recipe-audit.json") -Encoding UTF8
    @"
{
  "stage": "floorplan-impact",
  "operations": [
    { "operationId": "operation-0", "operationIndex": 0, "Kind": "HorizontalCompression", "AxisTag": "Width", "Edge": "Right", "Coordinate": 50.0, "expectedDeltaSourceUnits": 2.0, "affectedEntities": 2, "affectedVertices": 4, "measuredMinDeltaSourceUnits": 2.0, "measuredMaxDeltaSourceUnits": 2.0, "Status": "Applied", "Warning": null },
    { "operationId": "operation-1", "operationIndex": 1, "Kind": "VerticalCompression", "AxisTag": "Height", "Edge": "Top", "Coordinate": 75.0, "expectedDeltaSourceUnits": 1.0, "affectedEntities": 3, "affectedVertices": 6, "measuredMinDeltaSourceUnits": 1.0, "measuredMaxDeltaSourceUnits": 1.0, "Status": "Applied", "Warning": null }
  ]
}
"@ | Set-Content -Path (Join-Path $missingIndexAudit "floorplan-impact-audit.json") -Encoding UTF8
    @"
{
  "stage": "electrical-projection",
  "sheets": [
    {
      "operations": [
        {
          "OperationId": "operation-0",
          "OperationIndex": 0,
          "Kind": "HorizontalCompression",
          "AxisTag": "Width",
          "Edge": "Right",
          "Coordinate": 50.0,
          "ExpectedDeltaSourceUnits": 2.0,
          "AffectedEntities": 2,
          "AffectedVertices": 4,
          "MeasuredMinDeltaSourceUnits": 2.0,
          "MeasuredMaxDeltaSourceUnits": 2.0,
          "Status": "Applied",
          "Reason": null
        },
        {
          "OperationId": "operation-0-duplicate",
          "OperationIndex": 0,
          "Kind": "HorizontalCompression",
          "AxisTag": "Width",
          "Edge": "Right",
          "Coordinate": 50.0,
          "ExpectedDeltaSourceUnits": 2.0,
          "AffectedEntities": 1,
          "AffectedVertices": 2,
          "MeasuredMinDeltaSourceUnits": 2.0,
          "MeasuredMaxDeltaSourceUnits": 2.0,
          "Status": "Applied",
          "Reason": null
        }
      ]
    }
  ]
}
"@ | Set-Content -Path (Join-Path $missingIndexAudit "electrical-projection-audit.json") -Encoding UTF8
    Write-Manifest `
        $missingFloorPlanImpactOperations `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $dxf `
        $null `
        ([guid]::NewGuid().ToString()) `
        $true `
        $true `
        $false
    Write-Manifest `
        $missingFloorPlanCanonicalOperationIndex `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2, VerticalCompression Top @75 delta 1." `
        "ProjectedAutomatically" `
        $dxf `
        $null
    $missingFloorPlanIndexAudit = Join-Path $missingFloorPlanCanonicalOperationIndex "audit"
    @"
{
  "stage": "canonical-recipe",
  "operationCount": 2,
  "recipe": {
    "Operations": [
      { "Kind": "HorizontalCompression", "AxisTag": "Width", "Edge": "Right", "Coordinate": 50.0, "DeltaSourceUnits": 2.0 },
      { "Kind": "VerticalCompression", "AxisTag": "Height", "Edge": "Top", "Coordinate": 75.0, "DeltaSourceUnits": 1.0 }
    ]
  }
}
"@ | Set-Content -Path (Join-Path $missingFloorPlanIndexAudit "canonical-recipe-audit.json") -Encoding UTF8
    @"
{
  "stage": "floorplan-impact",
  "operations": [
    { "operationId": "operation-0", "operationIndex": 0, "Kind": "HorizontalCompression", "AxisTag": "Width", "Edge": "Right", "Coordinate": 50.0, "expectedDeltaSourceUnits": 2.0, "affectedEntities": 2, "affectedVertices": 4, "measuredMinDeltaSourceUnits": 2.0, "measuredMaxDeltaSourceUnits": 2.0, "Status": "Applied", "Warning": null },
    { "operationId": "operation-0-duplicate", "operationIndex": 0, "Kind": "HorizontalCompression", "AxisTag": "Width", "Edge": "Right", "Coordinate": 50.0, "expectedDeltaSourceUnits": 2.0, "affectedEntities": 1, "affectedVertices": 2, "measuredMinDeltaSourceUnits": 2.0, "measuredMaxDeltaSourceUnits": 2.0, "Status": "Applied", "Warning": null }
  ]
}
"@ | Set-Content -Path (Join-Path $missingFloorPlanIndexAudit "floorplan-impact-audit.json") -Encoding UTF8
    @"
{
  "stage": "electrical-projection",
  "sheets": [
    {
      "operations": [
        {
          "OperationId": "operation-0",
          "OperationIndex": 0,
          "Kind": "HorizontalCompression",
          "AxisTag": "Width",
          "Edge": "Right",
          "Coordinate": 50.0,
          "ExpectedDeltaSourceUnits": 2.0,
          "AffectedEntities": 2,
          "AffectedVertices": 4,
          "MeasuredMinDeltaSourceUnits": 2.0,
          "MeasuredMaxDeltaSourceUnits": 2.0,
          "Status": "Applied",
          "Reason": null
        },
        {
          "OperationId": "operation-1",
          "OperationIndex": 1,
          "Kind": "VerticalCompression",
          "AxisTag": "Height",
          "Edge": "Top",
          "Coordinate": 75.0,
          "ExpectedDeltaSourceUnits": 1.0,
          "AffectedEntities": 3,
          "AffectedVertices": 6,
          "MeasuredMinDeltaSourceUnits": 1.0,
          "MeasuredMaxDeltaSourceUnits": 1.0,
          "Status": "Applied",
          "Reason": null
        }
      ]
    }
  ]
}
"@ | Set-Content -Path (Join-Path $missingFloorPlanIndexAudit "electrical-projection-audit.json") -Encoding UTF8
    Write-Manifest `
        $floorPlanNoImpactMissingWarning `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $dxf `
        $null
    @"
{
  "stage": "floorplan-impact",
  "operations": [
    {
      "operationId": "operation-0",
      "operationIndex": 0,
      "Kind": "HorizontalCompression",
      "AxisTag": "Width",
      "Edge": "Right",
      "Coordinate": 50.0,
      "expectedDeltaSourceUnits": 2.0,
      "affectedEntities": 0,
      "affectedVertices": 0,
      "measuredMinDeltaSourceUnits": 0.0,
      "measuredMaxDeltaSourceUnits": 0.0,
      "Status": "NoGeometryAffected",
      "Warning": null
    }
  ]
}
"@ | Set-Content -Path (Join-Path (Join-Path $floorPlanNoImpactMissingWarning "audit") "floorplan-impact-audit.json") -Encoding UTF8
    Write-Manifest `
        $binaryCompression `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $binaryDxf `
        $null
    Write-Manifest `
        $ambiguousNoGeometryReason `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $dxf `
        $null
    @"
{
  "stage": "electrical-projection",
  "sheets": [
    {
      "operations": [
        {
          "OperationId": "operation-0",
          "OperationIndex": 0,
          "Kind": "HorizontalCompression",
          "AxisTag": "Width",
          "Edge": "Right",
          "Coordinate": 50.0,
          "ExpectedDeltaSourceUnits": 2.0,
          "AffectedEntities": 0,
          "AffectedVertices": 0,
          "MeasuredMinDeltaSourceUnits": 0.0,
          "MeasuredMaxDeltaSourceUnits": 0.0,
          "Status": "NoGeometryAffected",
          "Reason": "No electrical geometry matched this canonical operation."
        }
      ]
    }
  ]
}
"@ | Set-Content -Path (Join-Path (Join-Path $ambiguousNoGeometryReason "audit") "electrical-projection-audit.json") -Encoding UTF8
    Write-Manifest `
        $outlineMismatchNotNormalized `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $dxf `
        $null
    @"
{
  "stage": "outline-congruence",
  "sheets": [
    {
      "outlineCongruence": {
        "Status": "MismatchRequiresNormalization",
        "Reason": "Electrical starts wider than canonical FloorPlan.",
        "ToleranceInches": 0.05,
        "NormalizationApplied": false,
        "CanonicalSourceOutline": { "MinX": 0.0, "MinY": 0.0, "MaxX": 39.0, "MaxY": 77.5, "Width": 39.0, "Height": 77.5 },
        "ElectricalSourceOutline": { "MinX": 0.0, "MinY": 0.0, "MaxX": 41.0, "MaxY": 77.5, "Width": 41.0, "Height": 77.5 },
        "ElectricalNormalizedSourceOutline": { "MinX": 0.0, "MinY": 0.0, "MaxX": 41.0, "MaxY": 77.5, "Width": 41.0, "Height": 77.5 },
        "ElectricalExportOutline": { "MinX": 0.0, "MinY": 0.0, "MaxX": 39.0, "MaxY": 77.5, "Width": 39.0, "Height": 77.5 },
        "SourceWidthMismatchInches": 2.0,
        "SourceHeightMismatchInches": 0.0,
        "ExportWidthMismatchInches": 2.0,
        "ExportHeightMismatchInches": 0.0,
        "AnchorX": null,
        "AnchorY": null,
        "ScaleX": null,
        "ScaleY": null
      }
    }
  ]
}
"@ | Set-Content -Path (Join-Path (Join-Path $outlineMismatchNotNormalized "audit") "outline-congruence-audit.json") -Encoding UTF8
    $sparse = Join-Path $temp "sparse"
    Write-Manifest `
        $sparse `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $sparseDxf `
        $null
    Write-Manifest `
        $unsafeDxfSafetyAudit `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $dxf `
        $null `
        ([guid]::NewGuid().ToString()) `
        $true `
        $true `
        $true `
        1

    Copy-Item -LiteralPath $compression -Destination $formerFinalOutputMode -Recurse
    Update-FinalOutputAudit $formerFinalOutputMode {
        param($finalOutput)
        $finalOutput.ComparisonMode = "FinalExportedSupportedStructuralFootprint"
    }

    Copy-Item -LiteralPath $compression -Destination $missingFinalOutputSizeField -Recurse
    Update-FinalOutputAudit $missingFinalOutputSizeField {
        param($finalOutput)
        $finalOutput.PSObject.Properties.Remove("SizeResidualsWithinTolerance")
    }

    Copy-Item -LiteralPath $compression -Destination $missingFinalOutputNativeEdgeField -Recurse
    Update-FinalOutputAudit $missingFinalOutputNativeEdgeField {
        param($finalOutput)
        $finalOutput.PSObject.Properties.Remove("NativeEdgeResidualsWithinTolerance")
    }

    Copy-Item -LiteralPath $compression -Destination $finalOutputSizeMismatch -Recurse
    Update-FinalOutputAudit $finalOutputSizeMismatch {
        param($finalOutput)
        $delta = [decimal]$finalOutput.ToleranceInches + 0.1
        $finalOutput.ElectricalStructuralBounds.MaxX = [decimal]$finalOutput.ElectricalStructuralBounds.MaxX + $delta
        $finalOutput.ElectricalStructuralBounds.Width = [decimal]$finalOutput.ElectricalStructuralBounds.Width + $delta
        $finalOutput.WidthMismatchInches = $delta
        $finalOutput.SizeResidualsWithinTolerance = $false
        $rightEdge = $finalOutput.EdgeDeltas | Where-Object { $_.Edge -eq "Right" }
        $rightEdge.ElectricalCoordinate = [decimal]$rightEdge.ElectricalCoordinate + $delta
        $rightEdge.Delta = $delta
        $rightEdge.IsCovered = $false
        $finalOutput.NativeEdgeResidualsWithinTolerance = $false
    }

    Copy-Item -LiteralPath $compression -Destination $finalOutputNativeEdgeMismatch -Recurse
    Update-FinalOutputAudit $finalOutputNativeEdgeMismatch {
        param($finalOutput)
        $delta = [decimal]$finalOutput.ToleranceInches + 0.1
        $finalOutput.ElectricalStructuralBounds.MinX = [decimal]$finalOutput.ElectricalStructuralBounds.MinX + $delta
        $finalOutput.ElectricalStructuralBounds.MaxX = [decimal]$finalOutput.ElectricalStructuralBounds.MaxX + $delta
        foreach ($edgeName in @("Left", "Right")) {
            $edge = $finalOutput.EdgeDeltas | Where-Object { $_.Edge -eq $edgeName }
            $edge.ElectricalCoordinate = [decimal]$edge.ElectricalCoordinate + $delta
            $edge.Delta = $delta
            $edge.IsCovered = $false
        }
        $finalOutput.NativeEdgeResidualsWithinTolerance = $false
    }

    Assert-AutomaticVerifierFailsOnField `
        $formerFinalOutputMode `
        "ComparisonMode" `
        "the former normalized/size-only final-output comparison mode"

    if ((Invoke-ManifestVerifier -Root $compression) -ne 0) {
        throw "Expected compression manifest to pass. Last verifier error: $script:lastVerifierError"
    }

    if ((Invoke-ManifestVerifier -Root $compression -RequireAutomatic) -ne 0) {
        throw "Expected automatic compression manifest to pass with -RequireAutomatic. Last verifier error: $script:lastVerifierError"
    }

    if ((Invoke-ManifestVerifier -Root $proofAuthorized -RequireAutomatic) -ne 0) {
        throw "Expected source-bound registration proof authorization to pass only the outline stage while measured segment/final-output gates remain green. Last verifier error: $script:lastVerifierError"
    }

    $compressionOutput = & $verifier -Root $compression -RequireAutomatic
    foreach ($propertyName in @(
            "FinalOutputCongruenceStatus",
            "FinalOutputComparisonMode",
            "FinalOutputReason",
            "FinalOutputTolerance",
            "FinalOutputFloorPath",
            "FinalOutputElectricalPath",
            "FinalOutputFloorWidth",
            "FinalOutputFloorHeight",
            "FinalOutputElectricalWidth",
            "FinalOutputElectricalHeight",
            "FinalOutputWidthMismatch",
            "FinalOutputHeightMismatch",
            "FinalOutputSizeResidualsWithinTolerance",
            "FinalOutputNativeEdgeResidualsWithinTolerance")) {
        if ($null -eq $compressionOutput.PSObject.Properties[$propertyName]) {
            throw "Verifier output is missing '$propertyName'."
        }
    }

    Assert-AutomaticVerifierFailsOnField `
        $missingFinalOutputSizeField `
        "SizeResidualsWithinTolerance" `
        "final-output proof missing its size-residual status"

    Assert-AutomaticVerifierFailsOnField `
        $missingFinalOutputNativeEdgeField `
        "NativeEdgeResidualsWithinTolerance" `
        "final-output proof missing its native-edge-residual status"

    Assert-AutomaticVerifierFailsOnField `
        $finalOutputSizeMismatch `
        "SizeResidualsWithinTolerance" `
        "final-output proof with out-of-tolerance dimensions"

    Assert-AutomaticVerifierFailsOnField `
        $finalOutputNativeEdgeMismatch `
        "NativeEdgeResidualsWithinTolerance" `
        "equal-size final-output proof with an out-of-tolerance native edge"

    if ((Invoke-ManifestVerifier -Root $binaryCompression -RequireAutomatic) -ne 0) {
        throw "Expected automatic binary compression manifest to pass with -RequireAutomatic. Last verifier error: $script:lastVerifierError"
    }

    if ((Invoke-ManifestVerifier -Root $ambiguousNoGeometryReason -RequireAutomatic) -eq 0) {
        throw "Expected automatic manifest with ambiguous NoGeometryAffected reason to fail."
    }

    if ((Invoke-ManifestVerifier -Root $outlineMismatchNotNormalized -RequireAutomatic) -eq 0) {
        throw "Expected automatic manifest with unnormalized outline mismatch to fail."
    }

    Write-Manifest `
        $segmentMismatch `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $dxf `
        $null
    @"
{
  "stage": "outline-segment-congruence",
  "toleranceInches": 0.05,
  "sheets": [
    {
      "segmentCongruence": {
        "Status": "SegmentMismatchRequiresManualReview",
        "Reason": "Bbox is OK but a structural segment is missing.",
        "ToleranceInches": 0.05,
        "CenterlineToleranceInches": 0.5,
        "OutlineMatchToleranceInches": 2.0,
        "ComparisonMode": "StructuralOutlineCoverageWithWallRunAdvisory",
        "FloorSegmentCount": 4,
        "ElectricalSegmentCount": 3,
        "RequiredOutlineSegmentCount": 4,
        "OutlineEdges": [
          {
            "Edge": "Left",
            "RequiredCount": 1,
            "MissingCount": 1,
            "Mismatches": [
              {
                "Kind": "MissingLeftOutlineInElectrical",
                "Axis": "Vertical",
                "Reason": "No structural segment covered at least 80% of this run within 2\"."
              }
            ]
          },
          { "Edge": "Right", "RequiredCount": 1, "MissingCount": 0, "Mismatches": [] },
          { "Edge": "Bottom", "RequiredCount": 1, "MissingCount": 0, "Mismatches": [] },
          { "Edge": "Top", "RequiredCount": 1, "MissingCount": 0, "Mismatches": [] }
        ],
        "CornerCoverage": [
          { "Corner": "BottomLeft", "DeltaX": 0, "DeltaY": 0, "IsCovered": true },
          { "Corner": "BottomRight", "DeltaX": 0, "DeltaY": 0, "IsCovered": true },
          { "Corner": "TopLeft", "DeltaX": 0, "DeltaY": 0, "IsCovered": true },
          { "Corner": "TopRight", "DeltaX": 0, "DeltaY": 0, "IsCovered": true }
        ],
        "MissingInElectricalCount": 1,
        "ExtraInElectricalCount": 0,
        "AdvisoryMissingInternalWallRunCount": 0,
        "AdvisoryExtraElectricalWallRunCount": 0,
        "AdvisoryMismatches": [],
        "Mismatches": [
          {
            "Kind": "MissingInElectrical",
            "Axis": "Horizontal",
            "Reason": "No structural segment covered at least 80% of this run within 0.05\"."
          }
        ]
      }
    }
  ]
}
"@ | Set-Content -Path (Join-Path (Join-Path $segmentMismatch "audit") "outline-segment-congruence-audit.json") -Encoding UTF8

    if ((Invoke-ManifestVerifier -Root $segmentMismatch -RequireAutomatic) -eq 0) {
        throw "Expected automatic manifest with bbox OK but bad structural segment congruence to fail."
    }

    Write-Manifest `
        $finalOutputMismatch `
        "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations: HorizontalCompression Right @50 delta 2." `
        "ProjectedAutomatically" `
        $dxf `
        $null
    @"
{
  "stage": "final-output-congruence",
  "toleranceInches": 0.5,
  "sheets": [
    {
      "finalOutputCongruence": {
        "Status": "FinalOutputMismatchRequiresReview",
        "Reason": "Final exported ElectricalPlan native dominant structural outline does not match the final exported FloorPlan outline.",
        "ToleranceInches": 0.5,
        "ComparisonMode": "FinalNativeDominantStructuralOutlineEdgesAndSize",
        "FloorOutputPath": "floor.dxf",
        "ElectricalOutputPath": "electrical.dxf",
        "FloorStructuralSegmentCount": 4,
        "ElectricalStructuralSegmentCount": 4,
        "FloorStructuralBounds": { "MinX": 0.0, "MinY": 0.0, "MaxX": 40.0, "MaxY": 77.5, "Width": 40.0, "Height": 77.5 },
        "ElectricalStructuralBounds": { "MinX": 0.0, "MinY": 0.0, "MaxX": 37.0, "MaxY": 77.5, "Width": 37.0, "Height": 77.5 },
        "WidthMismatchInches": -3.0,
        "HeightMismatchInches": 0.0,
        "SizeResidualsWithinTolerance": false,
        "NativeEdgeResidualsWithinTolerance": false,
        "EdgeDeltas": [
          { "Edge": "Left", "FloorCoordinate": 0.0, "ElectricalCoordinate": 0.0, "Delta": 0.0, "IsCovered": true },
          { "Edge": "Right", "FloorCoordinate": 40.0, "ElectricalCoordinate": 37.0, "Delta": -3.0, "IsCovered": false },
          { "Edge": "Bottom", "FloorCoordinate": 0.0, "ElectricalCoordinate": 0.0, "Delta": 0.0, "IsCovered": true },
          { "Edge": "Top", "FloorCoordinate": 77.5, "ElectricalCoordinate": 77.5, "Delta": 0.0, "IsCovered": true }
        ],
        "MissingInElectricalCount": 1,
        "ExtraInElectricalCount": 0,
        "Mismatches": [
          { "Kind": "FinalOutputEdgeMismatch", "Edge": "Right", "Reason": "Final output Right edge differs." }
        ]
      }
    }
  ]
}
"@ | Set-Content -Path (Join-Path (Join-Path $finalOutputMismatch "audit") "final-output-congruence-audit.json") -Encoding UTF8

    Assert-AutomaticVerifierFailsOnField `
        $finalOutputMismatch `
        "Status" `
        "automatic manifest with final FloorPlan/Electrical output mismatch"

    if ((Invoke-ManifestVerifier -Root $affine) -eq 0) {
        throw "Expected affine-only manifest to fail without -AllowAffineOnly."
    }

    if ((Invoke-ManifestVerifier -Root $affine -AllowAffineOnly) -ne 0) {
        throw "Expected affine-only manifest to pass with -AllowAffineOnly. Last verifier error: $script:lastVerifierError"
    }

    if ((Invoke-ManifestVerifier -Root $manual) -ne 0) {
        throw "Expected manual compression manifest to pass. Last verifier error: $script:lastVerifierError"
    }

    if ((Invoke-ManifestVerifier -Root $manual -RequireAutomatic) -eq 0) {
        throw "Expected manual compression manifest to fail with -RequireAutomatic."
    }

    if ((Invoke-ManifestVerifier -Root $stale) -eq 0) {
        throw "Expected stale projected manifest to fail."
    }

    if ((Invoke-ManifestVerifier -Root $contradictoryManual) -eq 0) {
        throw "Expected projected manifest with manual-review-required summary to fail."
    }

    if ((Invoke-ManifestVerifier -Root $automaticWithoutRecipeAware -RequireAutomatic) -eq 0) {
        throw "Expected automatic final proof without recipe-aware summary to fail."
    }

    if ((Invoke-ManifestVerifier -Root $missingCanonicalAdjustment) -eq 0) {
        throw "Expected manifest without CanonicalAdjustmentId to fail."
    }

    if ((Invoke-ManifestVerifier -Root $missingCanonicalRecipeOperations) -eq 0) {
        throw "Expected manifest without canonical recipe operations to fail."
    }

    if ((Invoke-ManifestVerifier -Root $missingInputAudit) -eq 0) {
        throw "Expected manifest without captured input dimensions to fail."
    }

    if ((Invoke-ManifestVerifier -Root $missingFloorPlanImpactOperations) -eq 0) {
        throw "Expected manifest without floor-plan impact operation audit to fail."
    }

    if ((Invoke-ManifestVerifier -Root $missingFloorPlanCanonicalOperationIndex) -eq 0) {
        throw "Expected manifest missing a FloorPlan canonical operation index to fail."
    }

    if ((Invoke-ManifestVerifier -Root $floorPlanNoImpactMissingWarning) -eq 0) {
        throw "Expected FloorPlan no-impact operation without warning to fail."
    }

    if ((Invoke-ManifestVerifier -Root $missingElectricalProjectionOperations) -eq 0) {
        throw "Expected manifest without electrical projection operation audit to fail."
    }

    if ((Invoke-ManifestVerifier -Root $missingElectricalProjectionOperationFields) -eq 0) {
        throw "Expected manifest with incomplete electrical projection operation fields to fail."
    }

    if ((Invoke-ManifestVerifier -Root $missingElectricalCanonicalOperationIndex) -eq 0) {
        throw "Expected manifest missing an Electrical canonical operation index to fail."
    }

    if ((Invoke-ManifestVerifier -Root $unsafeDxfSafetyAudit -RequireAutomatic) -eq 0) {
        throw "Expected manifest with unsafe dxf-safety-audit to fail."
    }

    if ((Invoke-ManifestVerifier -Root $sparse) -eq 0) {
        throw "Expected sparse projected Electrical DXF to fail."
    }

    "manifest verifier self-check passed"
}
finally {
    if (Test-Path $temp) {
        Remove-Item -LiteralPath $temp -Recurse -Force
    }
}
