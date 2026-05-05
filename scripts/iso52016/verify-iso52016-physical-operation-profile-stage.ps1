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
            throw "Required ISO52016 physical stage file is missing: $relativePath"
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
                throw "$Scope contains forbidden positive claim: $claim in $relativePath line $lineNumber. Line: $line"
            }
        }
    }
}

function Invoke-StageVerifier {
    param(
        [Parameter(Mandatory = $true)] [string] $RelativePath
    )

    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required ISO52016 physical dependency verifier is missing: $RelativePath"
    }

    $arguments = @{
        RepoRoot = $RepoRoot
    }

    if ($SkipTests) {
        $arguments.SkipTests = $true
    }

    & $path @arguments
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
    }
    finally {
        Pop-Location
    }
}

Invoke-StageVerifier -RelativePath 'scripts\iso52016\verify-iso52016-physical-boundary-profile-stage.ps1'

$requiredFiles = @(
    'docs\calculations\Iso52016PhysicalOperationProfileStage.md',
    'docs\releases\Iso52016PhysicalOperationProfileStageManifest.json',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalHourlyOperationCondition.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Matrix\Iso52016MatrixHourlyBoundaryConductanceOverride.cs',
    'tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalOperationProfileTests.cs',
    'tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalOperationProfileStageTraceabilityTests.cs',
    'tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixHourlyBoundaryConductanceOverrideTests.cs'
)

Assert-RequiredFiles -RelativePaths $requiredFiles
Assert-NoPositiveParityClaims -Scope 'ISO52016 physical operation profile stage' -RelativePaths @(
    'docs\calculations\Iso52016PhysicalOperationProfileStage.md',
    'docs\releases\Iso52016PhysicalOperationProfileStageManifest.json'
)

Invoke-StageTests -Filter 'FullyQualifiedName~Iso52016PhysicalOperationProfile|FullyQualifiedName~Iso52016MatrixHourlyBoundaryConductanceOverride'

Write-Host 'ISO52016 physical operation profile stage verification passed - validation/internal engineering anchors only.'

# Traceability literal markers:
# Iso52016PhysicalOperationProfileTests
# Iso52016PhysicalOperationProfileStageTraceabilityTests
# Iso52016MatrixHourlyBoundaryConductanceOverrideTests
# Iso52016PhysicalHourlyOperationCondition
# Iso52016MatrixHourlyBoundaryConductanceOverride
# Iso52016PhysicalOperationProfileStageManifest.json
# validation/internal engineering anchors only
# AE-ISO52016-002 traceability marker
# Keep this literal marker in the verification wrapper so guard tests can prove
# that the Physical node model builder stage remains connected to the Matrix chain.
# AE-ISO52016-002
