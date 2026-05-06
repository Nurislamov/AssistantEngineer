param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

function Read-Json {
    param([Parameter(Mandatory = $true)] [string] $RelativePath)

    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required JSON file is missing: $RelativePath"
    }

    return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
}

$registry = Read-Json -RelativePath 'docs\traceability\Iso52016PhysicalChainStageRegistry.json'
$manifest = Read-Json -RelativePath 'docs\releases\Iso52016PhysicalChainStageRegistryManifest.json'

if ($registry.registryId -ne 'AE-ISO52016-002-STAGE-REGISTRY') {
    throw 'Physical chain stage registry id mismatch.'
}

if ($manifest.stageId -ne 'AE-ISO52016-002-STEP-15') {
    throw 'Physical chain stage registry manifest stage id mismatch.'
}

if ($manifest.registryId -ne 'AE-ISO52016-002-STAGE-REGISTRY') {
    throw 'Physical chain stage registry manifest registry id mismatch.'
}

$requiredFiles = @(
    'docs\traceability\Iso52016PhysicalChainStageRegistry.json',
    'docs\calculations\Iso52016PhysicalChainStageRegistry.md',
    'docs\releases\Iso52016PhysicalChainStageRegistryManifest.json',
    'scripts\iso52016\verify-iso52016-physical-chain-stage-registry.ps1',
    'tools\AssistantEngineer.Tools.Iso52016PhysicalRegistryVerification\Program.cs',
    'tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalChainStageRegistryTests.cs'
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required physical chain stage registry file is missing: $relativePath"
    }
}

& (Join-Path $RepoRoot 'scripts\iso52016\verify-iso52016-physical-chain-stage-registry.ps1') -RepoRoot $RepoRoot

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016PhysicalChainStageRegistry"
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test failed with exit code ${LASTEXITCODE} for filter: FullyQualifiedName~Iso52016PhysicalChainStageRegistry"
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host 'ISO52016 physical chain stage registry stage gate passed - validation/internal engineering anchors only.'

# AE-ISO52016-002-STAGE-REGISTRY
# AE-ISO52016-002-STEP-15
# Iso52016PhysicalChainStageRegistry.json
# Iso52016PhysicalChainStageRegistryManifest.json
# verify-iso52016-physical-chain-stage-registry-stage-gate.ps1
# Validation/internal engineering anchors only.