param(
    [string] $RepoRoot = (Get-Location).Path,
    [string] $OutputDirectory = "artifacts\iso52016\engineering-edge-cases"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Contract literal: artifacts\iso52016\engineering-edge-cases\merge-summary.json
# Contract literal: generatedArtifactsCommitted
# Contract literal: Engineering edge-case hardening only.
# Contract literal: Validation anchors only, not full parity.

function Get-JsonPropertyValue {
    param(
        [Parameter(Mandatory = $true)] [object] $JsonObject,
        [Parameter(Mandatory = $true)] [string] $PropertyName,
        [object] $DefaultValue = $null
    )

    $property = $JsonObject.PSObject.Properties[$PropertyName]

    if ($null -eq $property) {
        return $DefaultValue
    }

    return $property.Value
}

$manifestPath = Join-Path $RepoRoot "docs\releases\Iso52016MatrixEngineeringEdgeCasesReleaseManifest.json"

if (-not (Test-Path $manifestPath)) {
    throw "Engineering edge-case release manifest was not found: $manifestPath"
}

$manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json
$outputDirPath = Join-Path $RepoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputDirPath | Out-Null

$summary = [PSCustomObject]@{
    stageId = Get-JsonPropertyValue -JsonObject $manifest -PropertyName "stageId" -DefaultValue "ISO52016-MATRIX-ENGINEERING-EDGE-CASES-RELEASE"
    baseStageId = Get-JsonPropertyValue -JsonObject $manifest -PropertyName "baseStageId" -DefaultValue "ISO52016-MATRIX-ENGINEERING-EDGE-CASES"
    scope = Get-JsonPropertyValue -JsonObject $manifest -PropertyName "scope" -DefaultValue "EngineeringHardeningOnly"
    status = Get-JsonPropertyValue -JsonObject $manifest -PropertyName "status" -DefaultValue "ClosedCandidate"
    hardeningType = "Engineering edge-case hardening only."
    generatedArtifactsCommitted = $false
    explicitNonClaims = Get-JsonPropertyValue -JsonObject $manifest -PropertyName "explicitNonClaims" -DefaultValue @(
        "Validation anchors only, not full parity.",
        "No pyBuildingEnergy parity claim.",
        "No EnergyPlus parity claim.",
        "No ASHRAE 140 validation coverage claim.",
        "No full ISO 52016 parity claim."
    )
    verificationScripts = Get-JsonPropertyValue -JsonObject $manifest -PropertyName "verificationScripts" -DefaultValue @()
    testGuards = Get-JsonPropertyValue -JsonObject $manifest -PropertyName "testGuards" -DefaultValue @()
}

$jsonPath = Join-Path $outputDirPath "merge-summary.json"
$markdownPath = Join-Path $outputDirPath "merge-summary.md"

$summary |
    ConvertTo-Json -Depth 10 |
    Set-Content -Path $jsonPath -Encoding utf8

$markdown = @"
# ISO 52016 Matrix engineering edge cases merge summary

Stage: $($summary.stageId)

Scope: $($summary.scope)

Status: $($summary.status)

Engineering edge-case hardening only.

Validation anchors only, not full parity.

Generated artifacts committed: $($summary.generatedArtifactsCommitted)

## Non-claims

$($summary.explicitNonClaims -join "
")
"@

Set-Content -Path $markdownPath -Value $markdown -Encoding utf8

Write-Host "ISO52016 Matrix engineering edge-case merge summary written:"
Write-Host " - $jsonPath"
Write-Host " - $markdownPath"