param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

function Assert-RequiredFile {
    param([Parameter(Mandatory = $true)][string] $RelativePath)

    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required ISO52016 physical selection application integration file is missing: $RelativePath"
    }
}

function Assert-TextContains {
    param(
        [Parameter(Mandatory = $true)][string] $RelativePath,
        [Parameter(Mandatory = $true)][string] $Expected
    )

    $path = Join-Path $RepoRoot $RelativePath
    $text = Get-Content -LiteralPath $path -Raw
    if (-not $text.Contains($Expected)) {
        throw "Expected text was not found in ${RelativePath}: $Expected"
    }
}

$requiredFiles = @(
    'docs\calculations\Iso52016PhysicalSelectionApplicationIntegration.md',
    'docs\releases\Iso52016PhysicalSelectionApplicationIntegrationManifest.json',
    'scripts\iso52016\verify-iso52016-physical-selection-application-integration-hardening.ps1',
    'tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalSelectionApplicationIntegrationHardeningTests.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalModelSelectionStrategy.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalModelSelectionRequest.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalModelSelectionResult.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Abstractions\Iso52016\Physical\IIso52016PhysicalModelSelectionService.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Services\Iso52016\Physical\Iso52016PhysicalModelSelectionService.cs'
)

foreach ($relativePath in $requiredFiles) {
    Assert-RequiredFile -RelativePath $relativePath
}

Assert-TextContains -RelativePath 'docs\releases\Iso52016PhysicalSelectionApplicationIntegrationManifest.json' -Expected '"stageId": "AE-ISO52016-002-STEP-13"'
Assert-TextContains -RelativePath 'docs\releases\Iso52016PhysicalSelectionApplicationIntegrationManifest.json' -Expected '"status": "internal-engineering-gate"'
Assert-TextContains -RelativePath 'docs\releases\Iso52016PhysicalSelectionApplicationIntegrationManifest.json' -Expected '"matrixAllVerificationIntegrated": true'
Assert-TextContains -RelativePath 'docs\releases\Iso52016PhysicalSelectionApplicationIntegrationManifest.json' -Expected 'Not ASHRAE Standard 140 validation.'
Assert-TextContains -RelativePath 'docs\calculations\Iso52016PhysicalSelectionApplicationIntegration.md' -Expected 'ReducedMatrix remains the default'
Assert-TextContains -RelativePath 'docs\calculations\Iso52016PhysicalSelectionApplicationIntegration.md' -Expected 'PhysicalNodeModel is explicit opt-in'
Assert-TextContains -RelativePath 'scripts\iso52016\verify-iso52016-matrix-all.ps1' -Expected 'verify-iso52016-physical-selection-application-integration-hardening.ps1'

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter 'FullyQualifiedName~Iso52016PhysicalSelectionApplicationIntegrationHardening'
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test failed with exit code ${LASTEXITCODE} for filter: FullyQualifiedName~Iso52016PhysicalSelectionApplicationIntegrationHardening"
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host 'ISO52016 physical selection application integration hardening verification passed - validation/internal engineering anchors only.'

# Traceability literal markers:
# Iso52016PhysicalSelectionApplicationIntegrationHardeningTests
# Iso52016PhysicalSelectionApplicationIntegrationManifest.json
# ReducedMatrix remains the default application-facing path
# PhysicalNodeModel is explicit opt-in
# validation/internal engineering anchors only
# AE-ISO52016-002