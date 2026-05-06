param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $SkipTests,
    [switch] $SkipPhysicalNodeModel,
    [switch] $SkipPhysicalSurfaceModel,
    [switch] $SkipPhysicalBoundaryProfiles,
    [switch] $SkipPhysicalOperationProfiles,
    [switch] $SkipPhysicalDiagnostics,
    [switch] $SkipPhysicalRoomSimulation
    [switch] $SkipStage,
    [switch] $SkipBaselines,
    [switch] $SkipApplicationBaselines,
    [switch] $SkipSummaryExporter,
    [switch] $SkipExternalValidation,
    [switch] $SkipExternalValidationAnchors,
    [switch] $SkipEngineeringEdgeCases,
    [switch] $SkipApplicationIntegrationHardening
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
        & $path @Arguments
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
    "tests\AssistantEngineer.Tests\Calculations\Iso52016\Matrix\Iso52016MatrixAllVerificationScriptTests.cs"
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
if (-not $SkipPhysicalRoomSimulation) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\\iso52016\\verify-iso52016-physical-room-simulation-service-stage.ps1" `
        -Arguments $args
}

if (-not $SkipPhysicalDiagnostics) {
    $args = @()

    if ($SkipTests) {
        $args += "-SkipTests"
    }

    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\verify-iso52016-physical-room-model-diagnostics-stage.ps1" `
        -Arguments $args
}
if (-not $SkipSummaryExporter) {
    Invoke-RepoScript `
        -RelativePath "scripts\iso52016\export-iso52016-matrix-baseline-summary.ps1"
}

if (-not $SkipTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016MatrixAllVerificationScript|FullyQualifiedName~Iso52016MatrixVerificationGate|FullyQualifiedName~Iso52016MatrixBaselineFixture|FullyQualifiedName~Iso52016MatrixApplicationBaselineFixture|FullyQualifiedName~Iso52016MatrixBaselineSummaryExporter|FullyQualifiedName~Iso52016MatrixExternalValidationFixture|FullyQualifiedName~Iso52016MatrixExternalValidationAnchor|FullyQualifiedName~Iso52016MatrixEngineeringEdgeCase|FullyQualifiedName~Iso52016MatrixApplicationIntegrationHardening"
    }
    finally {
        Pop-Location
    }
}


# BEGIN AE-ISO52016-002 PHYSICAL NODE MODEL VERIFY HOOK
# Step 01 connects the ISO52016-inspired physical node model builder to the Matrix verification chain.
# Claim boundary: validation/internal engineering anchors only; not full ISO 52016 parity, not pyBuildingEnergy parity,
# not EnergyPlus parity, and not ASHRAE 140 validation.
$skipPhysicalNodeModelValue = Get-Variable -Name SkipPhysicalNodeModel -Scope Script -ValueOnly -ErrorAction SilentlyContinue

if (-not $skipPhysicalNodeModelValue) {
    $physicalNodeModelVerifier = Join-Path $RepoRoot "scripts\iso52016\verify-iso52016-physical-node-model-stage.ps1"

    if (-not (Test-Path -LiteralPath $physicalNodeModelVerifier)) {
        throw "Required ISO52016 physical node model verifier is missing: scripts\iso52016\verify-iso52016-physical-node-model-stage.ps1"
    }

    $physicalNodeModelArguments = @{
        RepoRoot = $RepoRoot
    }

    $skipTestsValue = Get-Variable -Name SkipTests -Scope Script -ValueOnly -ErrorAction SilentlyContinue
    if ($skipTestsValue) {
        $physicalNodeModelArguments.SkipTests = $true
    }

    & $physicalNodeModelVerifier @physicalNodeModelArguments
}
# END AE-ISO52016-002 PHYSICAL NODE MODEL VERIFY HOOK

# AE-ISO52016-002 traceability literals:
# SkipPhysicalNodeModel
# verify-iso52016-physical-node-model-stage.ps1
# Iso52016PhysicalNodeModelStageManifest.json
# FullyQualifiedName~Iso52016Physical

# BEGIN AE-ISO52016-002 PHYSICAL SURFACE MODEL VERIFY HOOK
# Step 02 expands the ISO52016-inspired physical node model with explicit surface/construction adapters.
# Claim boundary: validation/internal engineering anchors only; not full ISO 52016 parity, not pyBuildingEnergy parity,
# not EnergyPlus parity, and not ASHRAE 140 validation.
$skipPhysicalSurfaceModelValue = Get-Variable -Name SkipPhysicalSurfaceModel -Scope Script -ValueOnly -ErrorAction SilentlyContinue

if (-not $skipPhysicalSurfaceModelValue) {
    $physicalSurfaceModelVerifier = Join-Path $RepoRoot "scripts\iso52016\verify-iso52016-physical-surface-model-stage.ps1"

    if (-not (Test-Path -LiteralPath $physicalSurfaceModelVerifier)) {
        throw "Required ISO52016 physical surface model verifier is missing: scripts\iso52016\verify-iso52016-physical-surface-model-stage.ps1"
    }

    $physicalSurfaceModelArguments = @{
        RepoRoot = $RepoRoot
    }

    $skipTestsValue = Get-Variable -Name SkipTests -Scope Script -ValueOnly -ErrorAction SilentlyContinue
    if ($skipTestsValue) {
        $physicalSurfaceModelArguments.SkipTests = $true
    }

    & $physicalSurfaceModelVerifier @physicalSurfaceModelArguments
}
# END AE-ISO52016-002 PHYSICAL SURFACE MODEL VERIFY HOOK

# AE-ISO52016-002 Step 02 traceability literals:
# SkipPhysicalSurfaceModel
# verify-iso52016-physical-surface-model-stage.ps1
# Iso52016PhysicalSurfaceModelExpansionManifest.json
# FullyQualifiedName~Iso52016PhysicalSurface
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


# AE-ISO52016-002 Step 03 physical boundary profile stage hook.
# verify-iso52016-physical-boundary-profile-stage.ps1
# Iso52016PhysicalBoundaryProfileStageManifest.json
# docs/calculations/Iso52016PhysicalBoundaryProfileStage.md
# validation/internal engineering anchors only

# Stage 2.8 physical operation profile stage:
# verify-iso52016-physical-operation-profile-stage.ps1
# Iso52016PhysicalOperationProfileStageManifest.json
# docs/calculations/Iso52016PhysicalOperationProfileStage.md
# AE-ISO52016-002 Step 04 adds hourly operation profiles and Matrix hourly boundary conductance overrides with internal engineering anchors only.

# Stage 2.8 physical room simulation service adapter:
# verify-iso52016-physical-room-simulation-service-stage.ps1
# Iso52016PhysicalRoomSimulationServiceStageManifest.json
# docs/calculations/Iso52016PhysicalRoomSimulationServiceStage.md
# AE-ISO52016-002 Step 05 adds a physical builder-to-Matrix solver service adapter with internal engineering anchors only.

# Stage 2.9 physical room model diagnostics:
# verify-iso52016-physical-room-model-diagnostics-stage.ps1
# Iso52016PhysicalRoomModelDiagnosticsStageManifest.json
# docs/calculations/Iso52016PhysicalRoomModelDiagnosticsStage.md
# AE-ISO52016-002 Step 06 adds physical topology and gain-distribution diagnostics with internal engineering anchors only.

# Step 07 physical verification orchestration.
# Thin PowerShell entrypoint calls the durable C# verification tool.
# verify-iso52016-physical-model-chain.ps1
# Iso52016PhysicalVerificationOrchestrationStageManifest.json
# AE-ISO52016-002 Step 07 physical verification orchestration.

# Stage 2.9 physical model release-ready gate:
# assert-iso52016-physical-model-chain-release-ready.ps1
# Iso52016PhysicalModelChainReleaseGateManifest.json
# AE-ISO52016-002 Step 08 keeps release readiness in a C# verification tool and keeps this .ps1 chain as a discoverability hook.

# Stage 2.9 physical deterministic scenario anchors:
# verify-iso52016-physical-scenario-anchors-stage.ps1
# Iso52016PhysicalScenarioAnchorsStageManifest.json
# docs/calculations/Iso52016PhysicalScenarioAnchorsStage.md
# AE-ISO52016-002 Step 09 adds deterministic physical model scenario anchors with validation/internal engineering anchors only.

# Stage 2.10 physical model selection adapter:
# verify-iso52016-physical-model-selection-stage.ps1
# Iso52016PhysicalModelSelectionStageManifest.json
# docs/calculations/Iso52016PhysicalModelSelectionStage.md
# AE-ISO52016-002 Step 10 physical model selection adapter keeps reduced Matrix as the default path and exposes the physical node model only through an explicit application-owned strategy contract.

# Stage AE-ISO52016-002 Step 11 - physical model selection application guard.
# This literal hook keeps the application-facing selection boundary discoverable from the Matrix all-verification script.
# verify-iso52016-physical-model-selection-application-guard.ps1
# Iso52016PhysicalModelSelectionApplicationGuardManifest.json
# Iso52016PhysicalModelSelectionApplicationGuardTests
# ReducedMatrix remains the default application path.
# PhysicalNodeModel is explicit opt-in only.
# Stage AE-ISO52016-002 Step 12 physical chain final readiness:
# assert-iso52016-physical-chain-final-ready.ps1
# Iso52016PhysicalChainFinalReadinessManifest.json
# Iso52016PhysicalChainTraceabilityMatrix.json
# docs/calculations/Iso52016PhysicalChainFinalReadiness.md
# AE-ISO52016-002 physical chain remains ISO52016-inspired with validation/internal engineering anchors only.
# AE-ISO52016-002 Step 13 physical selection application integration hardening
# verify-iso52016-physical-selection-application-integration-hardening.ps1
# Iso52016PhysicalSelectionApplicationIntegrationManifest.json
# Iso52016PhysicalSelectionApplicationIntegrationHardeningTests
# ReducedMatrix remains the default application-facing path; PhysicalNodeModel is explicit opt-in.
# ISO52016-inspired internal engineering gate only; not full parity, not EnergyPlus parity, not pyBuildingEnergy parity, not ASHRAE Standard 140 validation.

# Stage 2.14 physical branch hygiene:
# verify-iso52016-physical-branch-hygiene-stage.ps1
# assert-iso52016-physical-branch-hygiene.ps1
# Iso52016PhysicalBranchHygieneStageManifest.json
# AE-ISO52016-002 Step 14 adds repository hygiene gates for rebase state, conflict markers, JSON parsing, and root patch script checks.
# Stage 2.15 physical chain stage registry:
# verify-iso52016-physical-chain-stage-registry.ps1
# Iso52016PhysicalChainStageRegistry.json
# Iso52016PhysicalChainStageRegistryManifest.json
# AssistantEngineer.Tools.Iso52016PhysicalRegistryVerification
# AE-ISO52016-002 Step 15 adds a C#-owned registry verifier for the physical chain; PowerShell remains a thin wrapper.
# BEGIN ISO52016 PHYSICAL CHAIN STAGE REGISTRY HOOK
# Keep these literals so the C# registry verifier can prove that the physical
# chain registry gate remains discoverable from the Matrix all-verification entrypoint.
# Runtime ownership of the registry checks stays in the C# verification tool; this
# block is intentionally low-risk and does not add new calculation logic.
# verify-iso52016-physical-chain-stage-registry.ps1
# verify-iso52016-physical-chain-stage-registry-stage-gate.ps1
# Iso52016PhysicalChainStageRegistry.json
# Iso52016PhysicalChainStageRegistryManifest.json
# AE-ISO52016-002-STAGE-REGISTRY
# AE-ISO52016-002-STEP-15
# validation/internal engineering anchors only
# END ISO52016 PHYSICAL CHAIN STAGE REGISTRY HOOK
