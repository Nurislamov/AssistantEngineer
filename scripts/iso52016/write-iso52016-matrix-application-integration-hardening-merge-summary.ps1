param(
    [string] $RepoRoot = (Get-Location).Path,
    [string] $OutputJsonPath = "artifacts\iso52016\application-integration-hardening\merge-summary.json",
    [string] $OutputMarkdownPath = "artifacts\iso52016\application-integration-hardening\merge-summary.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Contract literal: artifacts\iso52016\application-integration-hardening\merge-summary.json
# Contract literal: generatedArtifactsCommitted
# Contract literal: ApplicationIntegrationHardeningOnly
# Contract literal: Validation anchors only, not full parity.
# Contract literal: No pyBuildingEnergy parity claim.
# Contract literal: No EnergyPlus parity claim.
# Contract literal: No ASHRAE 140 validation coverage claim.
# Contract literal: No full ISO 52016 parity claim.

function Read-JsonFile {
    param([Parameter(Mandatory = $true)] [string] $RelativePath)

    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path $path)) {
        throw "Required JSON file was not found: $RelativePath"
    }

    return Get-Content $path -Raw | ConvertFrom-Json
}

function Get-OptionalProperty {
    param(
        [Parameter(Mandatory = $true)] [object] $Object,
        [Parameter(Mandatory = $true)] [string] $Name,
        [object] $DefaultValue = $null
    )

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $DefaultValue
    }

    return $property.Value
}

$stageManifest = Read-JsonFile "docs\releases\Iso52016MatrixApplicationIntegrationHardeningManifest.json"
$releaseManifest = Read-JsonFile "docs\releases\Iso52016MatrixApplicationIntegrationHardeningReleaseManifest.json"

$fixtureRows = @()
foreach ($fixturePath in $stageManifest.fixtures) {
    $path = Join-Path $RepoRoot $fixturePath
    if (-not (Test-Path $path)) {
        throw "Application integration fixture referenced by manifest was not found: $fixturePath"
    }

    $fixtureJson = Get-Content $path -Raw | ConvertFrom-Json
    $fixtureRows += [PSCustomObject]@{
        id = Get-OptionalProperty -Object $fixtureJson -Name "id" -DefaultValue ([System.IO.Path]::GetFileNameWithoutExtension($fixturePath))
        scenarioName = Get-OptionalProperty -Object $fixtureJson -Name "scenarioName" -DefaultValue ([System.IO.Path]::GetFileNameWithoutExtension($fixturePath))
        fixture = $fixturePath
        scope = "Application integration hardening only; not a parity reference."
    }
}

$summary = [PSCustomObject]@{
    stageId = $releaseManifest.stageId
    baseStageId = $releaseManifest.baseStageId
    scope = $releaseManifest.scope
    status = $releaseManifest.status
    generatedArtifactsCommitted = $false
    fixtureCount = $fixtureRows.Count
    fixtures = $fixtureRows
    nonClaims = $releaseManifest.explicitNonClaims
    verificationScripts = $releaseManifest.verificationScripts
    testGuards = $releaseManifest.testGuards
}

$outputJson = Join-Path $RepoRoot $OutputJsonPath
$outputMarkdown = Join-Path $RepoRoot $OutputMarkdownPath

New-Item -ItemType Directory -Path (Split-Path -Parent $outputJson) -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $outputMarkdown) -Force | Out-Null

$summary | ConvertTo-Json -Depth 10 | Set-Content -Path $outputJson -Encoding UTF8

$markdown = @()
$markdown += "# ISO 52016 Matrix application integration hardening merge summary"
$markdown += ""
$markdown += "Stage: $($summary.stageId)"
$markdown += "Scope: $($summary.scope)"
$markdown += "Generated artifacts committed: $($summary.generatedArtifactsCommitted)"
$markdown += "Fixture count: $($summary.fixtureCount)"
$markdown += ""
$markdown += "## Non-claims"
foreach ($claim in $summary.nonClaims) {
    $markdown += "- $claim"
}
$markdown += ""
$markdown += "## Fixtures"
foreach ($fixture in $summary.fixtures) {
    $markdown += "- $($fixture.id): $($fixture.fixture)"
}

$markdown -join [Environment]::NewLine | Set-Content -Path $outputMarkdown -Encoding UTF8

Write-Host "ISO52016 Matrix application integration hardening merge summary written to $OutputJsonPath and $OutputMarkdownPath."