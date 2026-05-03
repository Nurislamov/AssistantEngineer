param(
    [switch] $SkipBuild,
    [switch] $RunAllTests
)

$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    $current = (Get-Location).Path

    while ($current) {
        if (Test-Path (Join-Path $current "AssistantEngineer.sln")) {
            return $current
        }

        $parent = Split-Path -Parent $current
        if ($parent -eq $current) {
            break
        }

        $current = $parent
    }

    throw "AssistantEngineer.sln was not found. Run this script inside the repository."
}

$repoRoot = Resolve-RepoRoot
Set-Location $repoRoot

Write-Host "Verifying ISO52016 solar chain..."

if (-not $SkipBuild) {
    dotnet build .\AssistantEngineer.sln
}

$criticalFilter = @(
    "FullyQualifiedName~Iso52016WeatherSolarContextPerezDiagnosticsTests",
    "FullyQualifiedName~Iso52016WeatherSolarWindowGainIntegrationTests",
    "FullyQualifiedName~Iso52016HourlyHeatBalanceSolarContextIntegrationTests",
    "FullyQualifiedName~Iso52016HourlySteadyStateWeatherSolarContextIntegrationTests",
    "FullyQualifiedName~Iso52016AnnualDiagnosticsVisibilityTests",
    "FullyQualifiedName~Iso52016ResponseDiagnosticsVisibilityTests",
    "FullyQualifiedName~EngineeringCoreDiagnosticsFrontendRenderingTests",
    "FullyQualifiedName~Iso52016ProductionSolarPathRegistrationTests",
    "FullyQualifiedName~Iso52016ProductionSolarRuntimeSmokeTests",
    "FullyQualifiedName~Iso52016ApiDiagnosticsContractEvidenceTests"
) -join "|"

dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --no-build --filter $criticalFilter

if ($RunAllTests) {
    dotnet test .\AssistantEngineer.sln --no-build
}

Write-Host "ISO52016 solar chain verification completed."
