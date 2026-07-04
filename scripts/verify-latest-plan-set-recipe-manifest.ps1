param(
    [string]$Root = "src\FloorplanFit.Desktop\bin\Debug\net10.0\workspace\exports\plan-sets",
    [switch]$AllowAffineOnly
)

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

$canonical = @($json.Sheets | Where-Object { (Get-JsonString $_ "SheetKind") -eq "CanonicalFloorPlan" })
$electrical = @($json.Sheets | Where-Object { (Get-JsonString $_ "SheetKind") -eq "ElectricalPlan" })
$recipeSheets = @($json.Sheets | Where-Object {
        -not [string]::IsNullOrWhiteSpace((Get-JsonString $_ "RecipeHandlingSummary"))
    })
$compressionRecipeSheets = @($recipeSheets | Where-Object {
        (Get-JsonString $_ "RecipeHandlingSummary") -match "HorizontalCompression|VerticalCompression"
    })

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
    throw "Manifest '$($manifest.FullName)' has RecipeHandlingSummary but no local compression operation. Re-export a patio/porch/living compression case, or pass -AllowAffineOnly for affine-only smoke."
}

[pscustomobject]@{
    Manifest = $manifest.FullName
    Status = $json.Status
    CanonicalSheets = $canonical.Count
    ElectricalSheets = $electrical.Count
    RecipeSheets = $recipeSheets.Count
    CompressionRecipeSheets = $compressionRecipeSheets.Count
    RecipeHandlingSummary = Get-JsonString $recipeSheets[0] "RecipeHandlingSummary"
}
