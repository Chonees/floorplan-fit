param(
    [string]$Root = "src\FloorplanFit.Desktop\bin\Debug\net10.0\workspace\exports\plan-sets",
    [switch]$AllowAffineOnly,
    [switch]$RequireAutomatic
)

$manifest = Get-ChildItem -Path $Root -Recurse -Filter manifest.json -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $manifest) {
    throw "No manifest.json found under '$Root'. Export a HousePlanSet package first."
}

$json = Get-Content -Raw -Path $manifest.FullName | ConvertFrom-Json

if ($json.schemaVersion -ne 2) {
    throw "Manifest '$($manifest.FullName)' has unsupported or missing schemaVersion '$($json.schemaVersion)'."
}

$verification = $json.Verification
if ($null -eq $verification) {
    throw "Manifest '$($manifest.FullName)' is missing the typed Verification report."
}

if ($verification.schemaVersion -ne 1) {
    throw "Manifest '$($manifest.FullName)' has unsupported or missing Verification schemaVersion '$($verification.schemaVersion)'."
}

$verificationDecision = [string]$verification.Decision
$verificationReasonCodes = @($verification.Reasons | ForEach-Object { [string]$_.Code })
if ([string]$json.Status -eq "ReadyForExport" -and $verificationDecision -ne "ReadyForExport") {
    throw "Manifest '$($manifest.FullName)' claims ReadyForExport but typed Verification is '$verificationDecision': $($verificationReasonCodes -join ', ')."
}

if ($RequireAutomatic -and $verificationDecision -ne "ReadyForExport") {
    throw "Automatic proof requires a green typed Verification report, but latest decision is '$verificationDecision': $($verificationReasonCodes -join ', ')."
}
$auditDirectory = Join-Path $manifest.DirectoryName "audit"
$requiredAuditFiles = @(
    "input-audit.json",
    "canonical-recipe-audit.json",
    "floorplan-impact-audit.json",
    "electrical-registration-audit.json",
    "electrical-projection-audit.json",
    "outline-congruence-audit.json",
    "outline-segment-congruence-audit.json",
    "final-output-congruence-audit.json",
    "dxf-safety-audit.json"
)
foreach ($auditFile in $requiredAuditFiles) {
    $auditPath = Join-Path $auditDirectory $auditFile
    if (!(Test-Path -LiteralPath $auditPath)) {
        throw "Manifest '$($manifest.FullName)' is missing observability artifact '$auditFile'. Re-export after the audit writer changes."
    }
}

$electricalProjectionAuditPath = Join-Path $auditDirectory "electrical-projection-audit.json"
$electricalProjectionAudit = Get-Content -Raw -Path $electricalProjectionAuditPath | ConvertFrom-Json
$canonicalRecipeAuditPath = Join-Path $auditDirectory "canonical-recipe-audit.json"
$canonicalRecipeAudit = Get-Content -Raw -Path $canonicalRecipeAuditPath | ConvertFrom-Json
$floorPlanImpactAuditPath = Join-Path $auditDirectory "floorplan-impact-audit.json"
$floorPlanImpactAudit = Get-Content -Raw -Path $floorPlanImpactAuditPath | ConvertFrom-Json
$inputAuditPath = Join-Path $auditDirectory "input-audit.json"
$inputAudit = Get-Content -Raw -Path $inputAuditPath | ConvertFrom-Json
$dxfSafetyAuditPath = Join-Path $auditDirectory "dxf-safety-audit.json"
$dxfSafetyAudit = Get-Content -Raw -Path $dxfSafetyAuditPath | ConvertFrom-Json
$outlineCongruenceAuditPath = Join-Path $auditDirectory "outline-congruence-audit.json"
$outlineCongruenceAudit = Get-Content -Raw -Path $outlineCongruenceAuditPath | ConvertFrom-Json
$outlineSegmentCongruenceAuditPath = Join-Path $auditDirectory "outline-segment-congruence-audit.json"
$outlineSegmentCongruenceAudit = Get-Content -Raw -Path $outlineSegmentCongruenceAuditPath | ConvertFrom-Json
$finalOutputCongruenceAuditPath = Join-Path $auditDirectory "final-output-congruence-audit.json"
$finalOutputCongruenceAudit = Get-Content -Raw -Path $finalOutputCongruenceAuditPath | ConvertFrom-Json

function Get-JsonString($Object, $Name) {
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property -or $null -eq $property.Value) {
        return ""
    }

    return [string]$property.Value
}

function Get-JsonValue($Object, $Name) {
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

$finalOutputFloorPath = $null
$finalOutputElectricalPath = $null
$finalOutputFloorWidth = $null
$finalOutputFloorHeight = $null
$finalOutputElectricalWidth = $null
$finalOutputElectricalHeight = $null
$finalOutputRawWidthMismatch = $null
$finalOutputRawHeightMismatch = $null
$finalOutputReason = $null
$finalOutputTolerance = $null
$finalOutputSizeResidualsWithinTolerance = $null
$finalOutputNativeEdgeResidualsWithinTolerance = $null

$canonicalAdjustmentId = Get-JsonString $json "CanonicalAdjustmentId"
if ([string]::IsNullOrWhiteSpace($canonicalAdjustmentId)) {
    throw "Manifest '$($manifest.FullName)' has no CanonicalAdjustmentId."
}

$parsedCanonicalAdjustmentId = [guid]::Empty
if (![guid]::TryParse($canonicalAdjustmentId, [ref]$parsedCanonicalAdjustmentId) -or
    $parsedCanonicalAdjustmentId -eq [guid]::Empty) {
    throw "Manifest '$($manifest.FullName)' has invalid CanonicalAdjustmentId '$canonicalAdjustmentId'."
}

$inputDimensionsProperty = $inputAudit.PSObject.Properties["dimensionsInches"]
$inputDimensions = if ($null -eq $inputDimensionsProperty) { $null } else { $inputDimensionsProperty.Value }
if ($null -eq $inputDimensions) {
    throw "Manifest '$($manifest.FullName)' input-audit.json has no captured dimensionsInches. Re-export after input audit changes."
}

foreach ($dimensionName in @(
    "originalWidthInches",
    "originalHeightInches",
    "requestedWidthInches",
    "requestedHeightInches",
    "requiredWidthDeltaInches",
    "requiredHeightDeltaInches")) {
    $dimensionProperty = $inputDimensions.PSObject.Properties[$dimensionName]
    $value = if ($null -eq $dimensionProperty) { $null } else { $dimensionProperty.Value }
    if ($null -eq $value) {
        throw "Manifest '$($manifest.FullName)' input-audit.json is missing '$dimensionName'."
    }
}

if ([decimal]$inputDimensions.originalWidthInches -le 0 -or
    [decimal]$inputDimensions.originalHeightInches -le 0 -or
    [decimal]$inputDimensions.requestedWidthInches -le 0 -or
    [decimal]$inputDimensions.requestedHeightInches -le 0) {
    throw "Manifest '$($manifest.FullName)' input-audit.json has non-positive original/requested dimensions."
}

$canonical = @($json.Sheets | Where-Object { (Get-JsonString $_ "SheetKind") -eq "CanonicalFloorPlan" })
$electrical = @($json.Sheets | Where-Object { (Get-JsonString $_ "SheetKind") -eq "ElectricalPlan" })
$recipeSheets = @($json.Sheets | Where-Object {
        -not [string]::IsNullOrWhiteSpace((Get-JsonString $_ "RecipeHandlingSummary"))
    })
$compressionRecipeSheets = @($recipeSheets | Where-Object {
        (Get-JsonString $_ "RecipeHandlingSummary") -match "HorizontalCompression|VerticalCompression"
    })
$electricalSheet = if ($electrical.Count -gt 0) { $electrical[0] } else { $null }

if ($canonical.Count -eq 0) {
    throw "Manifest '$($manifest.FullName)' has no CanonicalFloorPlan sheet."
}

if ($electrical.Count -eq 0) {
    throw "Manifest '$($manifest.FullName)' has no ElectricalPlan sheet."
}

if ($recipeSheets.Count -eq 0) {
    throw "Manifest '$($manifest.FullName)' has no RecipeHandlingSummary. Re-export after the latest recipe changes."
}

if (!$AllowAffineOnly -and $compressionRecipeSheets.Count -eq 0) {
    throw "Manifest '$($manifest.FullName)' has RecipeHandlingSummary but no local compression operation. Re-export a local compression case, or pass -AllowAffineOnly for affine-only smoke."
}

$canonicalOperationCountProperty = $canonicalRecipeAudit.PSObject.Properties["operationCount"]
$canonicalOperationCount = if ($null -eq $canonicalOperationCountProperty) { 0 } else { [int]$canonicalOperationCountProperty.Value }
if (!$AllowAffineOnly -and $canonicalOperationCount -le 0) {
    throw "Manifest '$($manifest.FullName)' canonical-recipe-audit.json has no compression operation count."
}

$canonicalRecipeProperty = $canonicalRecipeAudit.PSObject.Properties["recipe"]
$canonicalRecipe = if ($null -eq $canonicalRecipeProperty) { $null } else { $canonicalRecipeProperty.Value }
$canonicalRecipeOperationsProperty = if ($null -eq $canonicalRecipe) { $null } else { $canonicalRecipe.PSObject.Properties["Operations"] }
[object[]]$canonicalRecipeOperations = if ($null -eq $canonicalRecipeOperationsProperty -or $null -eq $canonicalRecipeOperationsProperty.Value) {
    @()
}
else {
    @($canonicalRecipeOperationsProperty.Value)
}

if (!$AllowAffineOnly -and $canonicalRecipeOperations.Count -ne $canonicalOperationCount) {
    throw "canonical-recipe-audit.json has operationCount $canonicalOperationCount but lists $($canonicalRecipeOperations.Count) recipe operation(s). Inspect '$canonicalRecipeAuditPath'."
}

foreach ($operation in $canonicalRecipeOperations) {
    foreach ($propertyName in @("Kind", "AxisTag", "Edge", "Coordinate", "DeltaSourceUnits")) {
        if ($null -eq $operation.PSObject.Properties[$propertyName]) {
            throw "Canonical recipe operation in '$canonicalRecipeAuditPath' is missing '$propertyName'."
        }
    }
}

$floorPlanImpactOperations = @($floorPlanImpactAudit.operations)
if (!$AllowAffineOnly -and $floorPlanImpactOperations.Count -lt $canonicalOperationCount) {
    throw "FloorPlan observability audit has $($floorPlanImpactOperations.Count) operation(s), but canonical recipe has $canonicalOperationCount. Inspect '$floorPlanImpactAuditPath'."
}

foreach ($operation in $floorPlanImpactOperations) {
    foreach ($propertyName in @(
            "operationId",
            "operationIndex",
            "Kind",
            "AxisTag",
            "Edge",
            "Coordinate",
            "expectedDeltaSourceUnits",
            "affectedEntities",
            "affectedVertices",
            "measuredMinDeltaSourceUnits",
            "measuredMaxDeltaSourceUnits",
            "Status")) {
        if ($null -eq $operation.PSObject.Properties[$propertyName]) {
            throw "FloorPlan impact operation in '$floorPlanImpactAuditPath' is missing '$propertyName'."
        }
    }

    $operationStatus = Get-JsonString $operation "Status"
    if (@("Applied", "NoGeometryAffected") -notcontains $operationStatus) {
        throw "FloorPlan impact operation in '$floorPlanImpactAuditPath' has unexpected status '$operationStatus'."
    }

    if ([int]$operation.affectedVertices -le 0 -and [string]::IsNullOrWhiteSpace((Get-JsonString $operation "Warning"))) {
        throw "FloorPlan impact operation in '$floorPlanImpactAuditPath' affected no vertices but has no warning."
    }
}

if (!$AllowAffineOnly) {
    $floorPlanOperationIndexes = @($floorPlanImpactOperations | ForEach-Object { [int]$_.operationIndex })
    for ($index = 0; $index -lt $canonicalOperationCount; $index++) {
        if (@($floorPlanOperationIndexes | Where-Object { $_ -eq $index }).Count -ne 1) {
            throw "FloorPlan impact audit must contain exactly one operation with operationIndex $index. Inspect '$floorPlanImpactAuditPath'."
        }
    }
}

$electricalProjectionOperations = @($electricalProjectionAudit.sheets | ForEach-Object { $_.operations })
if (!$AllowAffineOnly -and $electricalProjectionOperations.Count -lt $canonicalOperationCount) {
    throw "ElectricalPlan observability audit has $($electricalProjectionOperations.Count) operation(s), but canonical recipe has $canonicalOperationCount. Inspect '$electricalProjectionAuditPath'."
}

foreach ($operation in $electricalProjectionOperations) {
    foreach ($propertyName in @(
            "OperationId",
            "OperationIndex",
            "Kind",
            "AxisTag",
            "Edge",
            "Coordinate",
            "ExpectedDeltaSourceUnits",
            "AffectedEntities",
            "AffectedVertices",
            "MeasuredMinDeltaSourceUnits",
            "MeasuredMaxDeltaSourceUnits",
            "Status")) {
        if ($null -eq $operation.PSObject.Properties[$propertyName]) {
            throw "ElectricalPlan projection operation in '$electricalProjectionAuditPath' is missing '$propertyName'."
        }
    }

    $operationStatus = Get-JsonString $operation "Status"
    if (@("Applied", "NoGeometryAffected", "RequiresManualReview", "Failed") -notcontains $operationStatus) {
        throw "ElectricalPlan projection operation in '$electricalProjectionAuditPath' has unexpected status '$operationStatus'."
    }

    $operationReason = Get-JsonString $operation "Reason"
    if ($operationStatus -ne "Applied" -and [string]::IsNullOrWhiteSpace($operationReason)) {
        throw "ElectricalPlan projection operation in '$electricalProjectionAuditPath' is '$operationStatus' but has no reason."
    }

    if ($RequireAutomatic -and
        $operationStatus -eq "NoGeometryAffected" -and
        $operationReason -eq "No electrical geometry matched this canonical operation.") {
        throw "ElectricalPlan projection operation in '$electricalProjectionAuditPath' still uses the old ambiguous NoGeometryAffected reason. Re-export with edge-anchor observability or report a specific empty-zone reason."
    }
}

if (!$AllowAffineOnly) {
    $electricalOperationIndexes = @($electricalProjectionOperations | ForEach-Object { [int]$_.OperationIndex })
    for ($index = 0; $index -lt $canonicalOperationCount; $index++) {
        if (@($electricalOperationIndexes | Where-Object { $_ -eq $index }).Count -ne 1) {
            throw "ElectricalPlan projection audit must contain exactly one operation with OperationIndex $index. Inspect '$electricalProjectionAuditPath'."
        }
    }
}

$electricalStatus = Get-JsonString $electricalSheet "Status"
$electricalWarning = Get-JsonString $electricalSheet "Warning"
$electricalStoragePath = Get-JsonString $electricalSheet "StoragePath"
$electricalRecipeSummary = Get-JsonString $electricalSheet "RecipeHandlingSummary"
$electricalDxfEntityCount = $null
$electricalInsertCount = $null
$electricalDimensionCount = $null
$electricalEllipseCount = $null
$electricalWireOrCurveCount = $null
$electricalDxfSafety = $null
$outlineStatus = $null
$outlineSourceWidthMismatch = $null
$outlineSourceHeightMismatch = $null
$outlineExportWidthMismatch = $null
$outlineExportHeightMismatch = $null
$outlineNormalizationApplied = $null
$outlineSegmentStatus = $null
$outlineSegmentComparisonMode = $null
$outlineSegmentMissingCount = $null
$outlineSegmentExtraCount = $null
$finalOutputStatus = $null
$finalOutputComparisonMode = $null
$finalOutputWidthMismatch = $null
$finalOutputHeightMismatch = $null

if ($RequireAutomatic -and $electricalStatus -ne "ProjectedAutomatically") {
    throw "Expected ElectricalPlan to be ProjectedAutomatically for final proof, but latest manifest '$($manifest.FullName)' has '$electricalStatus'. Confirm/re-export or create a fresh adjusted export."
}

if ($electricalStatus -eq "ProjectedAutomatically") {
    if ($RequireAutomatic -and $electricalRecipeSummary -notmatch "recipe-aware DXF export") {
        throw "ElectricalPlan final proof requires a recipe-aware DXF export summary, but latest manifest '$($manifest.FullName)' says '$electricalRecipeSummary'."
    }

    if ($electricalRecipeSummary -match "requires review before DXF deformation") {
        throw "ElectricalPlan in '$($manifest.FullName)' is ProjectedAutomatically but still says recipe requires review before DXF deformation. Re-export after the recipe-aware export changes."
    }

    if ($electricalRecipeSummary -match "manual review required") {
        throw "ElectricalPlan in '$($manifest.FullName)' is ProjectedAutomatically but still says manual review is required. Confirm review and re-export."
    }

    if ([string]::IsNullOrWhiteSpace($electricalStoragePath)) {
        throw "ElectricalPlan in '$($manifest.FullName)' is ProjectedAutomatically but has no StoragePath."
    }

    if (!(Test-Path -LiteralPath $electricalStoragePath)) {
        throw "ElectricalPlan StoragePath '$electricalStoragePath' does not exist."
    }

    $dxfSafetySheet = @($dxfSafetyAudit.dependentSheets | Where-Object {
            (Get-JsonString $_ "SheetKind") -eq "ElectricalPlan"
        } | Select-Object -First 1)
    if ($dxfSafetySheet.Count -eq 0) {
        throw "dxf-safety-audit.json has no ElectricalPlan dependent sheet. Inspect '$dxfSafetyAuditPath'."
    }

    $electricalDxfSafety = $dxfSafetySheet[0].dxfSafety
    if ($null -eq $electricalDxfSafety) {
        throw "dxf-safety-audit.json ElectricalPlan row has no dxfSafety object. Inspect '$dxfSafetyAuditPath'."
    }

    foreach ($propertyName in @(
            "OutputFileExists",
            "OutputFileBytes",
            "EntityCountBefore",
            "EntityCountAfter",
            "InsertCountAfter",
            "DimensionCountAfter",
            "EllipseCountAfter",
            "WireOrCurveCountAfter",
            "MissingHandleCountAfter",
            "MissingOwnerCountAfter",
            "UnsupportedCrossingEntityCount")) {
        if ($null -eq $electricalDxfSafety.PSObject.Properties[$propertyName]) {
            throw "dxf-safety-audit.json ElectricalPlan dxfSafety is missing '$propertyName'. Inspect '$dxfSafetyAuditPath'."
        }
    }

    if ($electricalDxfSafety.OutputFileExists -ne $true -or
        [long]$electricalDxfSafety.OutputFileBytes -le 0) {
        throw "dxf-safety-audit.json says ElectricalPlan output file is missing/empty. Inspect '$dxfSafetyAuditPath'."
    }

    if ([int]$electricalDxfSafety.EntityCountBefore -le 0 -or
        [int]$electricalDxfSafety.EntityCountAfter -le 0) {
        throw "dxf-safety-audit.json has no before/after ElectricalPlan entity counts. Inspect '$dxfSafetyAuditPath'."
    }

    if ([int]$electricalDxfSafety.InsertCountAfter -lt 1 -or
        [int]$electricalDxfSafety.DimensionCountAfter -lt 1 -or
        [int]$electricalDxfSafety.EllipseCountAfter -lt 1 -or
        [int]$electricalDxfSafety.WireOrCurveCountAfter -lt 1) {
        throw "dxf-safety-audit.json says ElectricalPlan lost key entity classes. Inspect '$dxfSafetyAuditPath'."
    }

    if ([int]$electricalDxfSafety.MissingHandleCountAfter -ne 0 -or
        [int]$electricalDxfSafety.MissingOwnerCountAfter -ne 0 -or
        [int]$electricalDxfSafety.UnsupportedCrossingEntityCount -ne 0) {
        throw "dxf-safety-audit.json reports unsafe ElectricalPlan DXF output. Inspect '$dxfSafetyAuditPath'."
    }

    $bytes = [System.IO.File]::ReadAllBytes($electricalStoragePath)
    if ($bytes.Length -lt 64) {
        throw "ElectricalPlan DXF '$electricalStoragePath' is too small to be valid."
    }

    $dxfText = [System.Text.Encoding]::GetEncoding(28591).GetString($bytes)
    $isBinaryDxf = $dxfText.StartsWith("AutoCAD Binary DXF", [System.StringComparison]::Ordinal)
    if ($isBinaryDxf) {
        $nul = [string][char]0
        if (!$dxfText.Contains($nul + "SECTION" + $nul) -or
            !$dxfText.Contains($nul + "ENTITIES" + $nul) -or
            !$dxfText.Contains($nul + "EOF" + $nul)) {
            throw "ElectricalPlan DXF '$electricalStoragePath' does not look like a readable binary DXF with ENTITIES and EOF."
        }

        $countBinaryNames = {
            param([string[]]$Names)
            $count = 0
            foreach ($name in $Names) {
                $count += ([regex]::Matches(
                        $dxfText,
                        [regex]::Escape($nul + $name + $nul),
                        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)).Count
            }

            return $count
        }

        $electricalDxfEntityCount = & $countBinaryNames @("LINE", "LWPOLYLINE", "POLYLINE", "SPLINE", "INSERT", "TEXT", "MTEXT", "ARC", "CIRCLE", "ELLIPSE", "DIMENSION")
        $electricalInsertCount = & $countBinaryNames @("INSERT")
        $electricalDimensionCount = & $countBinaryNames @("DIMENSION")
        $electricalEllipseCount = & $countBinaryNames @("ELLIPSE")
        $electricalWireOrCurveCount = & $countBinaryNames @("LWPOLYLINE", "POLYLINE", "SPLINE", "ARC")
    }
    else {
        if ($dxfText -notmatch "(?m)^\s*0\s*\r?\n\s*SECTION\s*$" -or
            $dxfText -notmatch "(?m)^\s*2\s*\r?\n\s*ENTITIES\s*$" -or
            $dxfText -notmatch "(?m)^\s*0\s*\r?\n\s*EOF\s*$") {
            throw "ElectricalPlan DXF '$electricalStoragePath' does not look like a readable text DXF with ENTITIES and EOF."
        }

        $electricalDxfEntityCount = ([regex]::Matches($dxfText, "(?m)^\s*0\s*\r?\n\s*(LINE|LWPOLYLINE|POLYLINE|SPLINE|INSERT|TEXT|MTEXT|ARC|CIRCLE|ELLIPSE|DIMENSION)\s*$")).Count
        $electricalInsertCount = ([regex]::Matches($dxfText, "(?m)^\s*0\s*\r?\n\s*INSERT\s*$")).Count
        $electricalDimensionCount = ([regex]::Matches($dxfText, "(?m)^\s*0\s*\r?\n\s*DIMENSION\s*$")).Count
        $electricalEllipseCount = ([regex]::Matches($dxfText, "(?m)^\s*0\s*\r?\n\s*ELLIPSE\s*$")).Count
        $electricalWireOrCurveCount = ([regex]::Matches($dxfText, "(?m)^\s*0\s*\r?\n\s*(LWPOLYLINE|POLYLINE|SPLINE|ARC)\s*$")).Count
    }

    if ($electricalDxfEntityCount -lt 10) {
        throw "ElectricalPlan DXF '$electricalStoragePath' has too few drawable entities ($electricalDxfEntityCount)."
    }

    if ($electricalInsertCount -lt 1) {
        throw "ElectricalPlan DXF '$electricalStoragePath' has no INSERT entities; electrical symbols may have disappeared."
    }

    if ($electricalDimensionCount -lt 1) {
        throw "ElectricalPlan DXF '$electricalStoragePath' has no DIMENSION entities; dimensions may have disappeared."
    }

    if ($electricalEllipseCount -lt 1) {
        throw "ElectricalPlan DXF '$electricalStoragePath' has no ELLIPSE entities; electrical wall ellipse geometry may have disappeared."
    }

    if ($electricalWireOrCurveCount -lt 1) {
        throw "ElectricalPlan DXF '$electricalStoragePath' has no LWPOLYLINE/POLYLINE/SPLINE/ARC entities; wiring/door curves may have disappeared."
    }

    $operationStatuses = @($electricalProjectionOperations | ForEach-Object { Get-JsonString $_ "Status" })
    if ($RequireAutomatic -and @($operationStatuses | Where-Object { $_ -eq "Failed" }).Count -gt 0) {
        throw "ElectricalPlan observability audit has Failed operation(s). Inspect '$electricalProjectionAuditPath'."
    }

    $outlineSheet = @($outlineCongruenceAudit.sheets | Select-Object -First 1)
    if ($outlineSheet.Count -eq 0) {
        throw "outline-congruence-audit.json has no ElectricalPlan sheet. Inspect '$outlineCongruenceAuditPath'."
    }

    $outline = $outlineSheet[0].outlineCongruence
    if ($null -eq $outline) {
        throw "outline-congruence-audit.json ElectricalPlan row has no outlineCongruence object. Inspect '$outlineCongruenceAuditPath'."
    }

    foreach ($propertyName in @(
            "Status",
            "Reason",
            "ToleranceInches",
            "NormalizationApplied",
            "ElectricalSourceOutline",
            "ElectricalExportOutline")) {
        if ($null -eq $outline.PSObject.Properties[$propertyName]) {
            throw "outline-congruence-audit.json ElectricalPlan outlineCongruence is missing '$propertyName'. Inspect '$outlineCongruenceAuditPath'."
        }
    }

    $outlineStatus = Get-JsonString $outline "Status"
    if (@("Congruent", "RegistrationProofAuthorized", "MismatchRequiresNormalization", "MismatchRequiresManualReview", "InsufficientData") -notcontains $outlineStatus) {
        throw "outline-congruence-audit.json has unexpected status '$outlineStatus'."
    }

    $outlineNormalizationApplied = [bool]$outline.NormalizationApplied
    if ($RequireAutomatic -and $outlineStatus -eq "InsufficientData") {
        throw "Automatic ElectricalPlan proof requires outline congruence data. Inspect '$outlineCongruenceAuditPath'."
    }

    if ($RequireAutomatic -and $outlineStatus -eq "MismatchRequiresManualReview") {
        throw "Automatic ElectricalPlan proof cannot pass with outline mismatch requiring manual review. Inspect '$outlineCongruenceAuditPath'."
    }

    if ($RequireAutomatic -and $outlineStatus -eq "MismatchRequiresNormalization" -and !$outlineNormalizationApplied) {
        throw "Electrical outline mismatch was detected but no normalization was applied. Inspect '$outlineCongruenceAuditPath'."
    }

    if ($outlineStatus -eq "RegistrationProofAuthorized") {
        if ($outlineNormalizationApplied) {
            throw "RegistrationProofAuthorized cannot report outline normalization. Inspect '$outlineCongruenceAuditPath'."
        }

        foreach ($propertyName in @(
                "CanonicalSourceOutline",
                "ElectricalSourceOutline",
                "ElectricalNormalizedSourceOutline",
                "ElectricalExportOutline",
                "SourceWidthMismatchInches",
                "SourceHeightMismatchInches",
                "ExportWidthMismatchInches",
                "ExportHeightMismatchInches",
                "AnchorX",
                "AnchorY",
                "ScaleX",
                "ScaleY")) {
            $property = $outline.PSObject.Properties[$propertyName]
            if ($null -eq $property) {
                throw "RegistrationProofAuthorized is missing unmeasured field '$propertyName'. Inspect '$outlineCongruenceAuditPath'."
            }

            if ($null -ne $property.Value) {
                throw "RegistrationProofAuthorized must leave unmeasured field '$propertyName' null. Inspect '$outlineCongruenceAuditPath'."
            }
        }
    }

    $outlineTolerance = [decimal]$outline.ToleranceInches
    $outlineSourceWidthMismatch = $outline.SourceWidthMismatchInches
    $outlineSourceHeightMismatch = $outline.SourceHeightMismatchInches
    $outlineExportWidthMismatch = $outline.ExportWidthMismatchInches
    $outlineExportHeightMismatch = $outline.ExportHeightMismatchInches
    if ($RequireAutomatic -and $outlineNormalizationApplied) {
        if ($null -eq $outlineExportWidthMismatch -or $null -eq $outlineExportHeightMismatch) {
            throw "Electrical outline normalization was applied but export mismatch values are missing. Inspect '$outlineCongruenceAuditPath'."
        }

        if ([math]::Abs([decimal]$outlineExportWidthMismatch) -gt $outlineTolerance -or
            [math]::Abs([decimal]$outlineExportHeightMismatch) -gt $outlineTolerance) {
            throw "Electrical outline normalization did not produce a congruent exported outline. Width mismatch $outlineExportWidthMismatch, height mismatch $outlineExportHeightMismatch, tolerance $outlineTolerance. Inspect '$outlineCongruenceAuditPath'."
        }
    }

    $segmentSheet = @($outlineSegmentCongruenceAudit.sheets | Select-Object -First 1)
    if ($segmentSheet.Count -eq 0) {
        throw "outline-segment-congruence-audit.json has no ElectricalPlan sheet. Inspect '$outlineSegmentCongruenceAuditPath'."
    }

    $segment = $segmentSheet[0].segmentCongruence
    if ($null -eq $segment) {
        throw "outline-segment-congruence-audit.json ElectricalPlan row has no segmentCongruence object. Inspect '$outlineSegmentCongruenceAuditPath'."
    }

    foreach ($propertyName in @(
            "Status",
            "Reason",
            "ToleranceInches",
            "ComparisonMode",
            "FloorSegmentCount",
            "ElectricalSegmentCount",
            "RequiredOutlineSegmentCount",
            "OutlineEdges",
            "CornerCoverage",
            "MissingInElectricalCount",
            "ExtraInElectricalCount",
            "AdvisoryMissingInternalWallRunCount",
            "AdvisoryExtraElectricalWallRunCount",
            "Mismatches")) {
        if ($null -eq $segment.PSObject.Properties[$propertyName]) {
            throw "outline-segment-congruence-audit.json segmentCongruence is missing '$propertyName'. Inspect '$outlineSegmentCongruenceAuditPath'."
        }
    }

    $outlineSegmentStatus = Get-JsonString $segment "Status"
    if (@("SegmentCongruent", "SegmentMismatchRequiresRecipeRemap", "SegmentMismatchRequiresManualReview", "InsufficientData") -notcontains $outlineSegmentStatus) {
        throw "outline-segment-congruence-audit.json has unexpected status '$outlineSegmentStatus'."
    }

    $outlineSegmentComparisonMode = Get-JsonString $segment "ComparisonMode"
    if ($outlineSegmentComparisonMode -ne "StructuralOutlineCoverageWithWallRunAdvisory") {
        throw "outline-segment-congruence-audit.json has stale or unsupported ComparisonMode '$outlineSegmentComparisonMode'. Re-export after the outline-coverage gate changes."
    }

    if ([int]$segment.RequiredOutlineSegmentCount -le 0) {
        throw "outline-segment-congruence-audit.json did not detect required FloorPlan outline segments. Inspect '$outlineSegmentCongruenceAuditPath'."
    }

    $outlineEdges = @($segment.OutlineEdges)
    if ($outlineEdges.Count -ne 4) {
        throw "outline-segment-congruence-audit.json must report Left/Right/Bottom/Top outline edge coverage. Inspect '$outlineSegmentCongruenceAuditPath'."
    }

    foreach ($edgeName in @("Left", "Right", "Bottom", "Top")) {
        $edge = @($outlineEdges | Where-Object { (Get-JsonString $_ "Edge") -eq $edgeName })
        if ($edge.Count -ne 1) {
            throw "outline-segment-congruence-audit.json must report exactly one '$edgeName' outline edge row. Inspect '$outlineSegmentCongruenceAuditPath'."
        }

        foreach ($propertyName in @("RequiredCount", "MissingCount", "Mismatches")) {
            if ($null -eq $edge[0].PSObject.Properties[$propertyName]) {
                throw "outline-segment-congruence-audit.json '$edgeName' outline edge row is missing '$propertyName'. Inspect '$outlineSegmentCongruenceAuditPath'."
            }
        }
    }

    $cornerCoverage = @($segment.CornerCoverage)
    if ($cornerCoverage.Count -ne 4) {
        throw "outline-segment-congruence-audit.json must report four principal corner coverage rows. Inspect '$outlineSegmentCongruenceAuditPath'."
    }

    foreach ($cornerName in @("BottomLeft", "BottomRight", "TopLeft", "TopRight")) {
        $corner = @($cornerCoverage | Where-Object { (Get-JsonString $_ "Corner") -eq $cornerName })
        if ($corner.Count -ne 1) {
            throw "outline-segment-congruence-audit.json must report exactly one '$cornerName' corner coverage row. Inspect '$outlineSegmentCongruenceAuditPath'."
        }

        foreach ($propertyName in @("DeltaX", "DeltaY", "IsCovered")) {
            if ($null -eq $corner[0].PSObject.Properties[$propertyName]) {
                throw "outline-segment-congruence-audit.json '$cornerName' corner coverage row is missing '$propertyName'. Inspect '$outlineSegmentCongruenceAuditPath'."
            }
        }
    }

    $outlineSegmentMissingCount = [int]$segment.MissingInElectricalCount
    $outlineSegmentExtraCount = [int]$segment.ExtraInElectricalCount
    if ($RequireAutomatic -and $outlineSegmentStatus -ne "SegmentCongruent") {
        throw "Automatic ElectricalPlan proof requires structural segment congruence, but latest audit is '$outlineSegmentStatus' with missing=$outlineSegmentMissingCount extra=$outlineSegmentExtraCount. Inspect '$outlineSegmentCongruenceAuditPath'."
    }

    $finalOutputSheet = @($finalOutputCongruenceAudit.sheets | Select-Object -First 1)
    if ($finalOutputSheet.Count -eq 0) {
        throw "final-output-congruence-audit.json has no ElectricalPlan sheet. Inspect '$finalOutputCongruenceAuditPath'."
    }

    $finalOutput = $finalOutputSheet[0].finalOutputCongruence
    if ($null -eq $finalOutput) {
        throw "final-output-congruence-audit.json ElectricalPlan row has no finalOutputCongruence object. Inspect '$finalOutputCongruenceAuditPath'."
    }

    foreach ($propertyName in @(
            "Status",
            "Reason",
            "ToleranceInches",
            "ComparisonMode",
            "FloorOutputPath",
            "ElectricalOutputPath",
            "FloorStructuralBounds",
            "ElectricalStructuralBounds",
            "WidthMismatchInches",
            "HeightMismatchInches",
            "SizeResidualsWithinTolerance",
            "NativeEdgeResidualsWithinTolerance",
            "EdgeDeltas",
            "MissingInElectricalCount",
            "ExtraInElectricalCount",
            "Mismatches")) {
        if ($null -eq $finalOutput.PSObject.Properties[$propertyName]) {
            throw "final-output-congruence-audit.json finalOutputCongruence is missing '$propertyName'. Inspect '$finalOutputCongruenceAuditPath'."
        }
    }

    $finalOutputStatus = Get-JsonString $finalOutput "Status"
    if (@("FinalOutputCongruent", "FinalOutputMismatchRequiresReview", "InsufficientFinalOutputCongruenceData") -notcontains $finalOutputStatus) {
        throw "final-output-congruence-audit.json field 'Status' has unsupported value '$finalOutputStatus'."
    }

    $finalOutputComparisonMode = Get-JsonString $finalOutput "ComparisonMode"
    if ($finalOutputComparisonMode -ne "FinalNativeDominantStructuralOutlineEdgesAndSize") {
        throw "final-output-congruence-audit.json field 'ComparisonMode' must be 'FinalNativeDominantStructuralOutlineEdgesAndSize', but found '$finalOutputComparisonMode'. Re-export after the final-output gate changes."
    }

    foreach ($boundsName in @("FloorStructuralBounds", "ElectricalStructuralBounds")) {
        $bounds = $finalOutput.PSObject.Properties[$boundsName].Value
        if ($null -eq $bounds) {
            throw "final-output-congruence-audit.json has no '$boundsName'. Inspect '$finalOutputCongruenceAuditPath'."
        }

        foreach ($propertyName in @("MinX", "MinY", "MaxX", "MaxY", "Width", "Height")) {
            if ($null -eq $bounds.PSObject.Properties[$propertyName]) {
                throw "final-output-congruence-audit.json '$boundsName' is missing '$propertyName'. Inspect '$finalOutputCongruenceAuditPath'."
            }
        }
    }

    $finalOutputFloorPath = Get-JsonString $finalOutput "FloorOutputPath"
    $finalOutputElectricalPath = Get-JsonString $finalOutput "ElectricalOutputPath"
    $finalOutputFloorBounds = $finalOutput.FloorStructuralBounds
    $finalOutputElectricalBounds = $finalOutput.ElectricalStructuralBounds
    $finalOutputFloorWidth = $finalOutputFloorBounds.Width
    $finalOutputFloorHeight = $finalOutputFloorBounds.Height
    $finalOutputElectricalWidth = $finalOutputElectricalBounds.Width
    $finalOutputElectricalHeight = $finalOutputElectricalBounds.Height
    $finalOutputReason = Get-JsonString $finalOutput "Reason"
    $finalOutputTolerance = $finalOutput.ToleranceInches
    $finalOutputWidthMismatch = $finalOutput.WidthMismatchInches
    $finalOutputHeightMismatch = $finalOutput.HeightMismatchInches
    $finalOutputSizeResidualsWithinTolerance = Get-JsonValue $finalOutput "SizeResidualsWithinTolerance"
    $finalOutputNativeEdgeResidualsWithinTolerance = Get-JsonValue $finalOutput "NativeEdgeResidualsWithinTolerance"
    $finalOutputRawWidthMismatch = Get-JsonValue $finalOutput "RawWidthMismatchInches"
    $finalOutputRawHeightMismatch = Get-JsonValue $finalOutput "RawHeightMismatchInches"
    if ($RequireAutomatic -and $finalOutputStatus -ne "FinalOutputCongruent") {
        throw "Automatic ElectricalPlan proof requires field 'Status' to be 'FinalOutputCongruent', but found '$finalOutputStatus'. Inspect '$finalOutputCongruenceAuditPath'."
    }

    if ($RequireAutomatic) {
        if ($finalOutputSizeResidualsWithinTolerance -isnot [bool] -or
            !$finalOutputSizeResidualsWithinTolerance) {
            throw "Automatic ElectricalPlan proof requires field 'SizeResidualsWithinTolerance' to be boolean true, but found '$finalOutputSizeResidualsWithinTolerance'. Inspect '$finalOutputCongruenceAuditPath'."
        }

        if ($finalOutputNativeEdgeResidualsWithinTolerance -isnot [bool] -or
            !$finalOutputNativeEdgeResidualsWithinTolerance) {
            throw "Automatic ElectricalPlan proof requires field 'NativeEdgeResidualsWithinTolerance' to be boolean true, but found '$finalOutputNativeEdgeResidualsWithinTolerance'. Inspect '$finalOutputCongruenceAuditPath'."
        }

        $finalOutputToleranceDecimal = [decimal]$finalOutputTolerance
        foreach ($mismatchField in @("WidthMismatchInches", "HeightMismatchInches")) {
            $mismatchValue = Get-JsonValue $finalOutput $mismatchField
            if ($null -eq $mismatchValue) {
                throw "Automatic ElectricalPlan proof requires non-null field '$mismatchField'. Inspect '$finalOutputCongruenceAuditPath'."
            }

            if ([math]::Abs([decimal]$mismatchValue) -gt $finalOutputToleranceDecimal) {
                throw "Automatic ElectricalPlan proof requires field '$mismatchField' within ToleranceInches=$finalOutputToleranceDecimal, but found '$mismatchValue'. Inspect '$finalOutputCongruenceAuditPath'."
            }
        }

        $finalOutputEdges = @($finalOutput.EdgeDeltas)
        if ($finalOutputEdges.Count -ne 4) {
            throw "Automatic ElectricalPlan proof requires field 'EdgeDeltas' to contain exactly Left, Right, Bottom, and Top rows. Inspect '$finalOutputCongruenceAuditPath'."
        }

        foreach ($edgeName in @("Left", "Right", "Bottom", "Top")) {
            $edgeRows = @($finalOutputEdges | Where-Object { (Get-JsonString $_ "Edge") -eq $edgeName })
            if ($edgeRows.Count -ne 1) {
                throw "Automatic ElectricalPlan proof requires field 'EdgeDeltas[$edgeName]' exactly once. Inspect '$finalOutputCongruenceAuditPath'."
            }

            $edgeRow = $edgeRows[0]
            foreach ($edgeField in @("FloorCoordinate", "ElectricalCoordinate", "Delta", "IsCovered")) {
                $edgeProperty = $edgeRow.PSObject.Properties[$edgeField]
                if ($null -eq $edgeProperty -or $null -eq $edgeProperty.Value) {
                    throw "Automatic ElectricalPlan proof requires non-null field 'EdgeDeltas[$edgeName].$edgeField'. Inspect '$finalOutputCongruenceAuditPath'."
                }
            }

            if ([math]::Abs([decimal]$edgeRow.Delta) -gt $finalOutputToleranceDecimal) {
                throw "Automatic ElectricalPlan proof requires field 'EdgeDeltas[$edgeName].Delta' within ToleranceInches=$finalOutputToleranceDecimal, but found '$($edgeRow.Delta)'. Inspect '$finalOutputCongruenceAuditPath'."
            }

            if ($edgeRow.IsCovered -isnot [bool] -or !$edgeRow.IsCovered) {
                throw "Automatic ElectricalPlan proof requires field 'EdgeDeltas[$edgeName].IsCovered' to be boolean true, but found '$($edgeRow.IsCovered)'. Inspect '$finalOutputCongruenceAuditPath'."
            }
        }
    }
}
elseif ($electricalStatus -eq "RequiresManualConfirmation") {
    if ([string]::IsNullOrWhiteSpace($electricalWarning) -and
        $electricalRecipeSummary -notmatch "manual|review|required") {
        throw "ElectricalPlan requires manual confirmation but has no warning or manual-review recipe summary."
    }
}
else {
    throw "ElectricalPlan has unexpected status '$electricalStatus'."
}

[pscustomobject]@{
    Manifest = $manifest.FullName
    Status = $json.Status
    ManifestSchemaVersion = $json.schemaVersion
    VerificationSchemaVersion = $verification.schemaVersion
    VerificationDecision = $verificationDecision
    VerificationReasons = $verificationReasonCodes -join ", "
    CanonicalAdjustmentId = $canonicalAdjustmentId
    CanonicalSheets = $canonical.Count
    ElectricalSheets = $electrical.Count
    RecipeSheets = $recipeSheets.Count
    CompressionRecipeSheets = $compressionRecipeSheets.Count
    RecipeHandlingSummary = Get-JsonString $recipeSheets[0] "RecipeHandlingSummary"
    ElectricalStatus = $electricalStatus
    ElectricalStoragePath = $electricalStoragePath
    ElectricalDxfEntityCount = $electricalDxfEntityCount
    ElectricalInsertCount = $electricalInsertCount
    ElectricalDimensionCount = $electricalDimensionCount
    ElectricalEllipseCount = $electricalEllipseCount
    ElectricalWireOrCurveCount = $electricalWireOrCurveCount
    ObservabilityAuditFiles = $requiredAuditFiles.Count
    OutlineCongruenceStatus = $outlineStatus
    OutlineNormalizationApplied = $outlineNormalizationApplied
    OutlineSourceWidthMismatch = $outlineSourceWidthMismatch
    OutlineSourceHeightMismatch = $outlineSourceHeightMismatch
    OutlineExportWidthMismatch = $outlineExportWidthMismatch
    OutlineExportHeightMismatch = $outlineExportHeightMismatch
    OutlineSegmentCongruenceStatus = $outlineSegmentStatus
    OutlineSegmentComparisonMode = $outlineSegmentComparisonMode
    OutlineSegmentMissingInElectrical = $outlineSegmentMissingCount
    OutlineSegmentExtraInElectrical = $outlineSegmentExtraCount
    FinalOutputCongruenceStatus = $finalOutputStatus
    FinalOutputComparisonMode = $finalOutputComparisonMode
    FinalOutputReason = $finalOutputReason
    FinalOutputTolerance = $finalOutputTolerance
    FinalOutputFloorPath = $finalOutputFloorPath
    FinalOutputElectricalPath = $finalOutputElectricalPath
    FinalOutputFloorWidth = $finalOutputFloorWidth
    FinalOutputFloorHeight = $finalOutputFloorHeight
    FinalOutputElectricalWidth = $finalOutputElectricalWidth
    FinalOutputElectricalHeight = $finalOutputElectricalHeight
    FinalOutputWidthMismatch = $finalOutputWidthMismatch
    FinalOutputHeightMismatch = $finalOutputHeightMismatch
    FinalOutputSizeResidualsWithinTolerance = $finalOutputSizeResidualsWithinTolerance
    FinalOutputNativeEdgeResidualsWithinTolerance = $finalOutputNativeEdgeResidualsWithinTolerance
    FinalOutputRawWidthMismatch = $finalOutputRawWidthMismatch
    FinalOutputRawHeightMismatch = $finalOutputRawHeightMismatch
}
