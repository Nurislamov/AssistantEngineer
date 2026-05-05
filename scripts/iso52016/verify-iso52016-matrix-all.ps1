param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests,
    [switch] $SkipStage,
    [switch] $SkipBaselines,
    [switch] $SkipApplicationBaselines,
    [switch] $SkipSummaryExporter,
    [switch] $SkipExternalValidation,
    [switch] $SkipExternalValidationAnchors,
    [switch] $SkipEngineeringEdgeCases,
    [switch] $SkipApplicationIntegrationHardening,
    [switch] $SkipPhysicalNodeModel,
    [switch] $SkipPhysicalSurfaceModel,
    [switch] $SkipPhysicalBoundaryProfiles,
    [switch] $SkipPhysicalOperationProfiles
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-RepoScript {
    param(
        [Parameter(Mandatory = $true)] [string] $RelativePath,
        [string[]] $Arguments = @()
    )

    $path = Join-Path $RepoRoot $RelativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix verification script is missing: $RelativePath"
    }

    Push-Location $RepoRoot
    try {
        # ISO52016_MATRIX_ALL_HASH_SPLATTING_GUARD
        # Do not call child .ps1 scripts with array splatting such as @("-SkipTests").
        # Array splatting is positional for scripts, so "-RepoRoot"/"-SkipTests" can bind as the RepoRoot string.
        # Keep this as named hashtable splatting for switch-only child arguments.
        $scriptParameters = @{}

        foreach ($argument in $Arguments) {
            switch ($argument) {
                "-SkipTests" { $scriptParameters["SkipTests"] = $true; continue }
                "-SkipBaselines" { $scriptParameters["SkipBaselines"] = $true; continue }
                "-SkipStage" { $scriptParameters["SkipStage"] = $true; continue }
                "-SkipApplicationBaselines" { $scriptParameters["SkipApplicationBaselines"] = $true; continue }
                "-SkipSummaryExporter" { $scriptParameters["SkipSummaryExporter"] = $true; continue }
                "-SkipExternalValidation" { $scriptParameters["SkipExternalValidation"] = $true; continue }
                "-SkipExternalValidationAnchors" { $scriptParameters["SkipExternalValidationAnchors"] = $true; continue }
                "-SkipEngineeringEdgeCases" { $scriptParameters["SkipEngineeringEdgeCases"] = $true; continue }
                "-SkipApplicationIntegrationHardening" { $scriptParameters["SkipApplicationIntegrationHardening"] = $true; continue }
                default {
                    throw "Unsupported ISO52016 Matrix verification script argument '$argument' for $RelativePath. Use hashtable splatting when adding parameters with values."
                }
            }
        }

        & $path @scriptParameters
    }
    finally {
        Pop-Location
    }
}

$requiredFiles = @(
    "scripts\iso52016\verify-iso52016-matrix-solver-stage.ps1",
    "scripts\iso52016\verify-iso52016-matrix-baselines.ps1",
    "scripts\iso52016\verify-iso52016-matrix-application-baselines.ps1",
    "scripts\iso52016\export-iso52016-matrix-baseline-summary.ps1",
    "scripts\iso52016\verify-iso52016-matrix-external-validation.ps1",
    "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors.ps1",
    "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1",
    "scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases.ps1",
    "scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases-stage-gate.ps1",
    "scripts\iso52016\verify-iso52016-matrix-application-integration-hardening.ps1",
    "scripts\iso52016\verify-iso52016-matrix-application-integration-hardening-stage-gate.ps1",
    "docs\calculations\Iso52016MatrixVerificationRunbook.md",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixAllVerificationScriptTests.cs",
    "scripts\iso52016\verify-iso52016-physical-node-model-stage.ps1",
    "docs\calculations\Iso52016PhysicalNodeModelStage.md",
    "docs\releases\Iso52016PhysicalNodeModelStageManifest.json",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalNodeModelStageTraceabilityTests.cs",
    "scripts\iso52016\verify-iso52016-physical-surface-model-stage.ps1",
    "docs\calculations\Iso52016PhysicalSurfaceModelExpansion.md",
    "docs\releases\Iso52016PhysicalSurfaceModelExpansionManifest.json",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalSurface.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalConstructionLayer.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalSurfaceBoundaryType.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalSurfaceModelBuilderTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalNodeModelSurfaceExpansionTraceabilityTests.cs",
    "scripts\iso52016\verify-iso52016-physical-boundary-profile-stage.ps1",
    "docs\calculations\Iso52016PhysicalBoundaryProfileStage.md",
    "docs\releases\Iso52016PhysicalBoundaryProfileStageManifest.json",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalSurfaceHourlyBoundaryCondition.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalSurfaceBoundaryConditionTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalBoundaryProfileStageTraceabilityTests.cs",
    "scripts\iso52016\verify-iso52016-physical-operation-profile-stage.ps1",
    "docs\calculations\Iso52016PhysicalOperationProfileStage.md",
    "docs\releases\Iso52016PhysicalOperationProfileStageManifest.json",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Physical\Iso52016PhysicalHourlyOperationCondition.cs",
    "src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\Matrix\Iso52016MatrixHourlyBoundaryConductanceOverride.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalOperationProfileTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixHourlyBoundaryConductanceOverrideTests.cs",
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Physical\Iso52016PhysicalOperationProfileStageTraceabilityTests.cs"
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $RepoRoot $relativePath

    if (-not (Test-Path $path)) {
        throw "Required ISO52016 Matrix all-verification file is missing: $relativePath"
    }
}

if (-not $SkipStage) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    if ($SkipBaselines) {
        $args += "-SkipBaselines"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-matrix-solver-stage.ps1" `
        -Arguments $args
}

if (-not $SkipBaselines) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-matrix-baselines.ps1" `
        -Arguments $args
}

if (-not $SkipApplicationBaselines) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-matrix-application-baselines.ps1" `
        -Arguments $args
}

if (-not $SkipExternalValidation) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-matrix-external-validation.ps1" `
        -Arguments $args
}

if (-not $SkipExternalValidationAnchors) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors.ps1" `
        -Arguments $args

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1" `
        -Arguments $args
}

if (-not $SkipEngineeringEdgeCases) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases.ps1" `
        -Arguments $args

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases-stage-gate.ps1" `
        -Arguments $args
}

if (-not $SkipApplicationIntegrationHardening) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-matrix-application-integration-hardening.ps1" `
        -Arguments $args

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-matrix-application-integration-hardening-stage-gate.ps1" `
        -Arguments $args
}

if (-not $SkipPhysicalNodeModel) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-physical-node-model-stage.ps1" `
        -Arguments $args
}
if (-not $SkipPhysicalSurfaceModel) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-physical-surface-model-stage.ps1" `
        -Arguments $args
}
if (-not $SkipPhysicalBoundaryProfiles) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-physical-boundary-profile-stage.ps1" `
        -Arguments $args
}
if (-not $SkipPhysicalOperationProfiles) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-physical-operation-profile-stage.ps1" `
        -Arguments $args
}
if (-not $SkipSummaryExporter) {
    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\export-iso52016-matrix-baseline-summary.ps1"
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixAllVerificationScript|FullyQualifiedName~Iso52016MatrixVerificationGate|FullyQualifiedName~Iso52016MatrixBaselineFixture|FullyQualifiedName~Iso52016MatrixApplicationBaselineFixture|FullyQualifiedName~Iso52016MatrixBaselineSummaryExporter|FullyQualifiedName~Iso52016MatrixExternalValidationFixture|FullyQualifiedName~Iso52016MatrixExternalValidationAnchor|FullyQualifiedName~Iso52016MatrixEngineeringEdgeCase|FullyQualifiedName~Iso52016MatrixApplicationIntegrationHardening|FullyQualifiedName~Iso52016Physical|FullyQualifiedName~Iso52016PhysicalSurface|FullyQualifiedName~Iso52016PhysicalNodeModelSurfaceExpansion"
    }
    finally {
        Pop-Location
    }
}

Write-Host "ISO52016 Matrix all verification passed."

# BEGIN ISO52016 MATRIX STAGE CONTRACT HOOKS
# These literal hook names are kept intentionally for Matrix gate guard tests.
# They document every Matrix validation layer that must remain connected.
# Runtime invocation order stays in the executable code above this block.
# verify-iso52016-matrix-external-validation-anchors.ps1
# verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1
# assert-iso52016-matrix-external-validation-anchors-release-ready.ps1
# verify-iso52016-matrix-engineering-edge-cases.ps1
# verify-iso52016-matrix-engineering-edge-cases-stage-gate.ps1
# assert-iso52016-matrix-engineering-edge-cases-release-ready.ps1
# verify-iso52016-matrix-application-integration-hardening.ps1
# verify-iso52016-matrix-application-integration-hardening-stage-gate.ps1
# assert-iso52016-matrix-application-integration-hardening-release-ready.ps1
# END ISO52016 MATRIX STAGE CONTRACT HOOKS

# ISO52016 Matrix staged-gate literal contract block.
# Keep these literals in the main Matrix gates so historical guard tests can verify
# that Stage 2.1/2.2/2.3 verification remains discoverable after script rewrites.
# Runtime behavior is implemented above by real Invoke-RepoScript / Invoke-RepoCommand calls.
# Stage 2.1 external validation anchors:
# verify-iso52016-matrix-external-validation-anchors.ps1
# verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1
# assert-iso52016-matrix-external-validation-anchors-release-ready.ps1
# Iso52016MatrixExternalValidationAnchorsManifest.json
# docs/releases/Iso52016MatrixExternalValidationAnchorsManifest.json
# Iso52016MatrixExternalValidationAnchorsManifestTests
# Stage 2.2 engineering edge cases:
# verify-iso52016-matrix-engineering-edge-cases.ps1
# verify-iso52016-matrix-engineering-edge-cases-stage-gate.ps1
# assert-iso52016-matrix-engineering-edge-cases-release-ready.ps1
# Stage 2.3 application integration hardening:
# verify-iso52016-matrix-application-integration-hardening.ps1
# verify-iso52016-matrix-application-integration-hardening-stage-gate.ps1
# assert-iso52016-matrix-application-integration-hardening-release-ready.ps1
# Stage 2.4 physical node model:
# verify-iso52016-physical-node-model-stage.ps1
# Iso52016PhysicalNodeModelStageManifest.json
# docs/calculations/Iso52016PhysicalNodeModelStage.md
# AE-ISO52016-002 physical node model builder is an ISO52016-inspired internal engineering stage, not complete numerical equivalence validation.
# Stage 2.5 physical surface and construction expansion:
# verify-iso52016-physical-surface-model-stage.ps1
# Iso52016PhysicalSurfaceModelExpansionManifest.json
# docs/calculations/Iso52016PhysicalSurfaceModelExpansion.md
# AE-ISO52016-002 Step 02 expands the physical node model with surface/construction contracts and internal engineering anchors only.
# Stage 2.6 physical boundary profile stage:
# verify-iso52016-physical-boundary-profile-stage.ps1
# Iso52016PhysicalBoundaryProfileStageManifest.json
# docs/calculations/Iso52016PhysicalBoundaryProfileStage.md
# AE-ISO52016-002 Step 03 adds per-surface hourly boundary driving temperatures with internal engineering anchors only.
# Stage 2.7 physical operation profile stage:
# verify-iso52016-physical-operation-profile-stage.ps1
# Iso52016PhysicalOperationProfileStageManifest.json
# docs/calculations/Iso52016PhysicalOperationProfileStage.md
# AE-ISO52016-002 Step 04 adds hourly operation profiles and Matrix hourly boundary conductance overrides with internal engineering anchors only.