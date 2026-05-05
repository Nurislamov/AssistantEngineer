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
            throw "Required ISO52016 physical diagnostics stage file is missing: $relativePath"
        }
    }
}

function Invoke-StageVerifierIfPresent {
    param(
        [Parameter(Mandatory = $true)] [string] $RelativePath
    )

    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        return
    }

    $arguments = @{
        RepoRoot = $RepoRoot
    }

    if ($SkipTests) {
        $arguments.SkipTests = $true
    }

    & $path @arguments

    if ($LASTEXITCODE -ne 0) {
        throw "Dependency verifier failed with exit code ${LASTEXITCODE}: $RelativePath"
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
            throw "dotnet test failed with exit code $LASTEXITCODE for filter: $Filter"
        }
    }
    finally {
        Pop-Location
    }
}

Invoke-StageVerifierIfPresent -RelativePath 'scripts\iso52016\verify-iso52016-physical-room-simulation-service-stage.ps1'
Invoke-StageVerifierIfPresent -RelativePath 'scripts\iso52016\verify-iso52016-physical-operation-profile-stage.ps1'

$requiredFiles = @(
    'docs\calculations\Iso52016PhysicalRoomModelDiagnosticsStage.md',
    'docs\releases\Iso52016PhysicalRoomModelDiagnosticsStageManifest.json',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Abstractions\Iso52016\Physical\IIso52016PhysicalRoomModelDiagnosticsBuilder.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalRoomModelDiagnosticsProfile.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalRoomModelHourlyDiagnostics.cs',
    'src\Backend\AssistantEngineer.Modules.Calculations\Application\Services\Iso52016\Physical\Iso52016PhysicalRoomModelDiagnosticsBuilder.cs',
    'tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalRoomModelDiagnosticsBuilderTests.cs',
    'tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalRoomModelDiagnosticsStageTraceabilityTests.cs'
)

Assert-RequiredFiles -RelativePaths $requiredFiles

Invoke-StageTests -Filter 'FullyQualifiedName~Iso52016PhysicalRoomModelDiagnosticsBuilder|FullyQualifiedName~Iso52016PhysicalRoomModelDiagnosticsStageTraceability'

Write-Host 'ISO52016 physical room model diagnostics stage verification passed - validation/internal engineering anchors only.'

# Traceability literal markers:
# Iso52016PhysicalRoomModelDiagnosticsBuilderTests
# Iso52016PhysicalRoomModelDiagnosticsStageTraceabilityTests
# IIso52016PhysicalRoomModelDiagnosticsBuilder
# Iso52016PhysicalRoomModelDiagnosticsBuilder
# Iso52016PhysicalRoomModelDiagnosticsStageManifest.json
# validation/internal engineering anchors only
# AE-ISO52016-002

