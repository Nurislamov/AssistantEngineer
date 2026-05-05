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
            throw "Required ISO52016 physical model selection stage file is missing: ${relativePath}"
        }
    }
}

function Assert-NoPositiveParityClaims {
    param(
        [Parameter(Mandatory = $true)] [string[]] $RelativePaths
    )

    $forbiddenPositiveClaims = @(
        "full ISO 52016 parity",
        "ISO52016 parity",
        "complete ISO 52016 numerical equivalence achieved",
        "pyBuildingEnergy parity",
        "pyBuildingEnergy numerical equivalence achieved",
        "EnergyPlus parity",
        "EnergyPlus numerical equivalence achieved",
        "ASHRAE 140 validation passed",
        "ASHRAE Standard 140 benchmark-grade claim passed"
    )

    foreach ($relativePath in $RelativePaths) {
        $path = Join-Path $RepoRoot $relativePath
        if (-not (Test-Path -LiteralPath $path)) {
            continue
        }

        $content = Get-Content -LiteralPath $path -Raw
        foreach ($claim in $forbiddenPositiveClaims) {
            if ($content.IndexOf($claim, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "Forbidden positive parity/validation claim found in ${relativePath}: ${claim}"
            }
        }
    }
}

function Invoke-RepoScript {
    param(
        [Parameter(Mandatory = $true)] [string] $RelativePath,
        [switch] $PassSkipTests
    )

    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required dependency verifier is missing: ${RelativePath}"
    }

    $arguments = @{
        RepoRoot = $RepoRoot
    }

    if ($PassSkipTests) {
        $arguments.SkipTests = $true
    }

    & $path @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Dependency verifier failed with exit code ${LASTEXITCODE}: ${RelativePath}"
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
            throw "dotnet test failed with exit code ${LASTEXITCODE} for filter: ${Filter}"
        }
    }
    finally {
        Pop-Location
    }
}

Invoke-RepoScript `
    -RelativePath "scripts\iso52016\assert-iso52016-physical-model-chain-release-ready.ps1" `
    -PassSkipTests:$SkipTests

$requiredFiles = @(
    "docs\calculations\Iso52016PhysicalModelSelectionStage.md",
    "docs\releases\Iso52016PhysicalModelSelectionStageManifest.json",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalModelSelectionStrategy.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalModelSelectionRequest.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalModelSelectionResult.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Abstractions\Iso52016\Physical\IIso52016PhysicalModelSelectionService.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Services\Iso52016\Physical\Iso52016PhysicalModelSelectionService.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalModelSelectionServiceTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalModelSelectionStageTraceabilityTests.cs"
)

Assert-RequiredFiles -RelativePaths $requiredFiles
Assert-NoPositiveParityClaims -RelativePaths @(
    "docs\calculations\Iso52016PhysicalModelSelectionStage.md",
    "docs\releases\Iso52016PhysicalModelSelectionStageManifest.json"
)

Invoke-StageTests -Filter "FullyQualifiedName~Iso52016PhysicalModelSelection"

Write-Host "ISO52016 physical model selection adapter stage verification passed - validation/internal engineering anchors only."

# Traceability literal markers:
# Iso52016PhysicalModelSelectionServiceTests
# Iso52016PhysicalModelSelectionStageTraceabilityTests
# IIso52016PhysicalModelSelectionService.cs
# Iso52016PhysicalModelSelectionStageManifest.json
# validation/internal engineering anchors only
# AE-ISO52016-002 Step 10 physical model selection adapter