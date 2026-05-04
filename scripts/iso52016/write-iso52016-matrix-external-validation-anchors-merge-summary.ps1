param(
    [string] $RepoRoot = (Get-Location).Path,
    [string] $OutputDirectory = "artifacts\iso52016\external-validation-anchors"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ValidationAnchorOnly: this generated merge summary reports independent manual engineering validation anchors only.
# It does not claim pyBuildingEnergy parity, EnergyPlus parity, ASHRAE 140 validation, or full ISO 52016 conformance.

function Get-JsonPropertyValue {
    param(
        [Parameter(Mandatory = $true)] [object] $JsonObject,
        [Parameter(Mandatory = $true)] [string] $PropertyName,
        [object] $DefaultValue = $null
    )

    if ($null -eq $JsonObject) {
        return $DefaultValue
    }

    $property = $JsonObject.PSObject.Properties[$PropertyName]
    if ($null -eq $property) {
        return $DefaultValue
    }

    if ($null -eq $property.Value) {
        return $DefaultValue
    }

    return $property.Value
}

function Get-RequiredJsonPropertyValue {
    param(
        [Parameter(Mandatory = $true)] [object] $JsonObject,
        [Parameter(Mandatory = $true)] [string] $PropertyName,
        [Parameter(Mandatory = $true)] [string] $Context
    )

    $value = Get-JsonPropertyValue -JsonObject $JsonObject -PropertyName $PropertyName
    if ($null -eq $value -or [string]::IsNullOrWhiteSpace([string]$value)) {
        throw "Required JSON property '$PropertyName' is missing in $Context."
    }

    return $value
}

$RepoRoot = (Resolve-Path $RepoRoot).Path
$manifestPath = Join-Path $RepoRoot "docs\releases\Iso52016MatrixExternalValidationAnchorsReleaseManifest.json"

if (-not (Test-Path $manifestPath)) {
    throw "ISO52016 Matrix external validation anchor release manifest is missing: $manifestPath"
}

$manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json
$outputPath = Join-Path $RepoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

$fixtureRows = @()
foreach ($fixture in $manifest.fixtures) {
    $fixtureRelativePath = [string]$fixture
    $fixturePath = Join-Path $RepoRoot $fixtureRelativePath
    if (-not (Test-Path $fixturePath)) {
        throw "Manifest fixture is missing: $fixtureRelativePath"
    }

    $fixtureJson = Get-Content -Raw -Path $fixturePath | ConvertFrom-Json
    $anchorId = Get-RequiredJsonPropertyValue -JsonObject $fixtureJson -PropertyName "anchorId" -Context $fixtureRelativePath
    $scenarioName = Get-RequiredJsonPropertyValue -JsonObject $fixtureJson -PropertyName "scenarioName" -Context $fixtureRelativePath
    $sourceType = Get-RequiredJsonPropertyValue -JsonObject $fixtureJson -PropertyName "sourceType" -Context $fixtureRelativePath
    $authoritativeReference = Get-RequiredJsonPropertyValue -JsonObject $fixtureJson -PropertyName "authoritativeReference" -Context $fixtureRelativePath
    $scope = Get-JsonPropertyValue `
        -JsonObject $fixtureJson `
        -PropertyName "validationScope" `
        -DefaultValue "Independent manual engineering validation anchor only; not a parity reference."

    $fixtureRows += [PSCustomObject]@{
        AnchorId = [string]$anchorId
        ScenarioName = [string]$scenarioName
        SourceType = [string]$sourceType
        AuthoritativeReference = [string]$authoritativeReference
        Scope = [string]$scope
    }
}

$summary = [PSCustomObject]@{
        generatedArtifactsCommitted = $false
    StageId = [string](Get-RequiredJsonPropertyValue -JsonObject $manifest -PropertyName "stageId" -Context $manifestPath)
    StageName = [string](Get-RequiredJsonPropertyValue -JsonObject $manifest -PropertyName "stageName" -Context $manifestPath)
    Status = [string](Get-RequiredJsonPropertyValue -JsonObject $manifest -PropertyName "status" -Context $manifestPath)
    ValidationStatus = [string](Get-RequiredJsonPropertyValue -JsonObject $manifest -PropertyName "validationStatus" -Context $manifestPath)
    FixtureCount = $fixtureRows.Count
    ExplicitNonClaims = (Get-JsonPropertyValue -JsonObject $manifest -PropertyName "explicitNonClaims" -DefaultValue @())
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString("O")
    Fixtures = $fixtureRows
}

$jsonPath = Join-Path $outputPath "merge-summary.json"
$markdownPath = Join-Path $outputPath "merge-summary.md"

$summary | ConvertTo-Json -Depth 8 | Set-Content -Encoding utf8 -Path $jsonPath

$markdown = New-Object System.Collections.Generic.List[string]
$markdown.Add("# ISO 52016 Matrix external validation anchors merge summary")
$markdown.Add("")
$markdown.Add("Status: ``$($summary.ValidationStatus)``")
$markdown.Add("")
$markdown.Add("This generated summary reports independent manual engineering validation anchors only. It does not claim pyBuildingEnergy parity, EnergyPlus parity, ASHRAE 140 validation, or full ISO 52016 conformance.")
$markdown.Add("")
$markdown.Add("## Fixtures")
$markdown.Add("")
$markdown.Add("| Anchor | Scenario | Reference | Scope |")
$markdown.Add("| --- | --- | --- | --- |")
foreach ($fixture in $fixtureRows) {
    $markdown.Add("| ``$($fixture.AnchorId)`` | ``$($fixture.ScenarioName)`` | ``$($fixture.AuthoritativeReference)`` | $($fixture.Scope) |")
}
$markdown.Add("")
$markdown.Add("## Explicit non-claims")
$markdown.Add("")
foreach ($nonClaim in $summary.ExplicitNonClaims) {
    $markdown.Add("- $nonClaim")
}
$markdown.Add("")
$markdown.Add("Generated artifacts under ``artifacts/iso52016/external-validation-anchors/`` are merge evidence only and must not be committed.")

$markdown | Set-Content -Encoding utf8 -Path $markdownPath

Write-Host "ISO52016 Matrix external validation anchors merge summary written: $jsonPath"
Write-Host "ISO52016 Matrix external validation anchors merge summary written: $markdownPath"

# Guard contract literal for generated merge summary artifact path.
# This generated file must stay ignored and must not be committed:
# artifacts\iso52016\external-validation-anchors\merge-summary.json
# Contract literal: Validation anchors only, not full parity.

