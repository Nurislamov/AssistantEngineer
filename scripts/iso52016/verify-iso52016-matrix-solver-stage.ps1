param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$requiredFiles = @(
    "docs\calculations\Iso52016MatrixSolverStage.md",
    "docs\releases\Iso52016MatrixSolverStageManifest.json",
    "docs\traceability\Iso52016MatrixSolverTraceabilityMatrix.json",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Iso52016BuildingEnergySimulationCommand.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Matrix\Iso52016MatrixHourlySolverRequest.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Services\Iso52016\Matrix\Iso52016MatrixHourlySolver.cs",
    "src\Backend\AssistantEngineer.Api\Controllers\Analysis\BuildingEnergyAnalysisController.cs"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath
    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix stage file is missing: $relativePath"
    }
}

$manifestPath = Join-Path $RepoRoot "docs\releases\Iso52016MatrixSolverStageManifest.json"
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json

foreach ($workItem in @("AE-ISO52016-001", "AE-GAINS-001", "AE-ZONES-001")) {
    if ($manifest.closedWorkItems -notcontains $workItem) {
        throw "Manifest does not contain closed work item: $workItem"
    }
}

$sourceRoot = Join-Path $RepoRoot "src\Backend"
$sourceText = Get-ChildItem $sourceRoot -Recurse -File -Include *.cs |
    ForEach-Object { Get-Content $_.FullName -Raw } |
    Out-String

foreach ($guard in @(
    "Iso52016MatrixHourlySolver",
    "Iso52016InternalGainReferenceDataProvider",
    "AdjacentUnconditioned",
    "Iso52016MatrixRoomEnergySimulationService",
        "Iso52016BuildingEnergySimulationCommand",
    "Iso52016MatrixRoomEnergySimulationResultMapper",
    "Iso52016MatrixReducedRoomModelBuilder",
    "SimulateIso52016"
)) {
    if (-not $sourceText.Contains($guard)) {
        throw "Required ISO52016 Matrix implementation guard was not found in source: $guard"
    }
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixSolverStageTraceability"
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix solver stage verification passed."