Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$verifier = Join-Path $PSScriptRoot "verify-latest-plan-set-recipe-manifest.ps1"
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("ff-manifest-check-" + [guid]::NewGuid().ToString("N"))

function Write-Manifest($Directory, $RecipeSummary) {
    New-Item -ItemType Directory -Path $Directory | Out-Null
    @"
{
  "Status": "RequiresManualConfirmation",
  "Sheets": [
    { "SheetKind": "CanonicalFloorPlan" },
    { "SheetKind": "ElectricalPlan", "RecipeHandlingSummary": "$RecipeSummary" }
  ]
}
"@ | Set-Content -Path (Join-Path $Directory "manifest.json") -Encoding UTF8
}

function Invoke-ManifestVerifier {
    param(
        [string]$Root,
        [switch]$AllowAffineOnly
    )

    try {
        if ($AllowAffineOnly) {
            & $verifier -Root $Root -AllowAffineOnly *> $null
        }
        else {
            & $verifier -Root $Root *> $null
        }
        return 0
    }
    catch {
        return 1
    }
}

try {
    $compression = Join-Path $temp "compression"
    $affine = Join-Path $temp "affine"

    Write-Manifest `
        $compression `
        "ElectricalPlan: affine placement applied; local recipe requires review before DXF deformation: HorizontalCompression Right @50 delta 2."
    Write-Manifest `
        $affine `
        "ElectricalPlan: affine placement applied; no local compression operations."

    if ((Invoke-ManifestVerifier -Root $compression) -ne 0) {
        throw "Expected compression manifest to pass."
    }

    if ((Invoke-ManifestVerifier -Root $affine) -eq 0) {
        throw "Expected affine-only manifest to fail without -AllowAffineOnly."
    }

    if ((Invoke-ManifestVerifier -Root $affine -AllowAffineOnly) -ne 0) {
        throw "Expected affine-only manifest to pass with -AllowAffineOnly."
    }

    "manifest verifier self-check passed"
}
finally {
    if (Test-Path $temp) {
        Remove-Item -LiteralPath $temp -Recurse -Force
    }
}
