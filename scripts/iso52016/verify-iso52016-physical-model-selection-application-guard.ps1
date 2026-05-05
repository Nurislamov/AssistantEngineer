param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

function Assert-RequiredFiles {
    param(
        [Parameter(Mandatory = $true)] [string[]] $RelativePaths
    )

    foreach ($relativePath in $RelativePaths) {
        $path = Join-Path $RepoRoot $relativePath
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Required ISO52016 physical model selection application guard file is missing: ${relativePath}"
        }
    }
}

function Test-LineNegatesClaim {
    param(
        [Parameter(Mandatory = $true)] [string] $Line,
        [Parameter(Mandatory = $true)] [string] $Claim
    )

    $lineLower = $Line.ToLowerInvariant()
    $claimLower = $Claim.ToLowerInvariant()
    $index = $lineLower.IndexOf($claimLower)

    if ($index -lt 0) {
        return $false
    }

    $prefix = $lineLower.Substring(0, $index)

    if ($prefix -match '(^|[^a-z])(not|no|without|does not|doesn''t)\s+([^.;:,\|\)]*\s+){0,8}$') {
        return $true
    }

    return $false
}

function Assert-NoPositiveParityClaims {
    param(
        [Parameter(Mandatory = $true)] [string] $Scope,
        [Parameter(Mandatory = $true)] [string[]] $RelativePaths
    )

    $claims = @(
        'full ISO 52016 parity',
        'ISO52016 parity',
        'complete ISO 52016 numerical equivalence',
        'complete ISO52016 numerical equivalence',
        'pyBuildingEnergy parity',
        'pyBuildingEnergy numerical equivalence',
        'EnergyPlus parity',
        'EnergyPlus numerical equivalence',
        'ASHRAE 140 validation',
        'ASHRAE Standard 140 benchmark-grade claim'
    )

    foreach ($relativePath in $RelativePaths) {
        $path = Join-Path $RepoRoot $relativePath
        if (-not (Test-Path -LiteralPath $path)) {
            continue
        }

        $lines = Get-Content -LiteralPath $path
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = [string]$lines[$i]

            foreach ($claim in $claims) {
                if ($line.IndexOf($claim, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
                    continue
                }

                if (Test-LineNegatesClaim -Line $line -Claim $claim) {
                    continue
                }

                $lineNumber = $i + 1
                throw "${Scope} contains forbidden positive claim: $claim in ${relativePath} line $lineNumber. Line: $line"
            }
        }
    }
}

function Invoke-StageTests {
    param(
        [Parameter(Mandatory = $true)] [string] $Filter
    )

    if ($SkipTests) {
        return
    }

    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter $Filter
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test failed with exit code ${LASTEXITCODE} for filter: $Filter"
        }
    }
    finally {
        Pop-Location
    }
}

$requiredFiles = @(
    'docs\calculations\Iso52016PhysicalModelSelectionApplicationGuard.md',
    'docs\releases\Iso52016PhysicalModelSelectionApplicationGuardManifest.json',
    'scripts\iso52016\verify-iso52016-physical-model-selection-application-guard.ps1',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalModelSelectionStrategy.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalModelSelectionRequest.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalModelSelectionResult.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Abstractions\Iso52016\Physical\IIso52016PhysicalModelSelectionService.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Services\Iso52016\Physical\Iso52016PhysicalModelSelectionService.cs',
    'tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalModelSelectionApplicationGuardTests.cs'
)

Assert-RequiredFiles -RelativePaths $requiredFiles
Assert-NoPositiveParityClaims -Scope 'ISO52016 physical model selection application guard' -RelativePaths @(
    'docs\calculations\Iso52016PhysicalModelSelectionApplicationGuard.md',
    'docs\releases\Iso52016PhysicalModelSelectionApplicationGuardManifest.json'
)

Invoke-StageTests -Filter 'FullyQualifiedName~Iso52016PhysicalModelSelectionApplicationGuard'

Write-Host 'ISO52016 physical model selection application guard verification passed - validation/internal engineering anchors only.'

# Traceability literal markers:
# Iso52016PhysicalModelSelectionApplicationGuardTests
# Iso52016PhysicalModelSelectionApplicationGuardManifest.json
# verify-iso52016-physical-model-selection-application-guard.ps1
# ReducedMatrix remains the default
# PhysicalNodeModel is explicit opt-in
# validation/internal engineering anchors only
# AE-ISO52016-002
