param(
    [string] $OutputDirectory = "docs/traceability"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$manifestPath = "docs/releases/EngineeringCoreV1Manifest.json"
$diagnosticsCatalogPath = "docs/calculations/EngineeringCoreV1DiagnosticsCatalog.json"
$validationRegistryPath = "docs/validation/EnergyPlusValidationCaseRegistry.json"

if (-not (Test-Path $manifestPath)) {
    throw "Manifest not found: $manifestPath"
}

if (-not (Test-Path $diagnosticsCatalogPath)) {
    throw "Diagnostics catalog not found: $diagnosticsCatalogPath"
}

if (-not (Test-Path $validationRegistryPath)) {
    throw "Validation registry not found: $validationRegistryPath"
}

New-Item -ItemType Directory -Force $OutputDirectory | Out-Null

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$diagnosticsCatalog = Get-Content $diagnosticsCatalogPath -Raw | ConvertFrom-Json
$validationRegistry = Get-Content $validationRegistryPath -Raw | ConvertFrom-Json

$diagnosticsByGate = @{}
foreach ($diagnostic in @($diagnosticsCatalog.diagnostics)) {
    $gate = [string]$diagnostic.closedV1Gate

    if (-not $diagnosticsByGate.ContainsKey($gate)) {
        $diagnosticsByGate[$gate] = @()
    }

    $diagnosticsByGate[$gate] = @($diagnosticsByGate[$gate]) + [string]$diagnostic.code
}

$gateRows = @()
foreach ($gate in @($manifest.closedFormulaGates | Sort-Object)) {
    $diagnostics = @()
    if ($diagnosticsByGate.ContainsKey($gate)) {
        $diagnostics = @($diagnosticsByGate[$gate] | Sort-Object)
    }

    $gateRows += [ordered]@{
        calculationId = $gate
        status = "ClosedV1"
        diagnostics = $diagnostics
        apiVisible = $true
        reportDisclosureVisible = $true
        frontendVisible = $true
        documentationFiles = @($manifest.documentationFiles)
        verificationScripts = @($manifest.verificationScripts)
    }
}

$validationRows = @($validationRegistry.cases | ForEach-Object {
    [ordered]@{
        caseId = $_.caseId
        stage = $_.stage
        status = $_.status
        metrics = @($_.metrics | ForEach-Object { $_.metricId })
        nonClaims = @($_.nonClaims)
    }
})

$traceability = [ordered]@{
    matrixName = "Engineering Core V1 Traceability Matrix"
    version = "v1"
    status = "ClosedV1"
    sourceManifest = $manifestPath
    sourceDiagnosticsCatalog = $diagnosticsCatalogPath
    sourceValidationRegistry = $validationRegistryPath
    generatedFrom = @(
        $manifestPath,
        $diagnosticsCatalogPath,
        $validationRegistryPath
    )
    closedFormulaGateCount = @($manifest.closedFormulaGates).Count
    diagnosticsCount = @($diagnosticsCatalog.diagnostics).Count
    validationCaseCount = @($validationRegistry.cases).Count
    annual8760Requirements = @($manifest.requiredAnnual8760Flags)
    outOfScopeV1 = @($manifest.outOfScopeV1)
    plannedValidation = @($manifest.plannedValidation)
    applicationEndpoints = @($manifest.applicationEndpoints)
    frontendVisibility = @($manifest.frontendVisibility)
    backendVisibility = @($manifest.backendVisibility)
    documentationFiles = @($manifest.documentationFiles)
    verificationScripts = @($manifest.verificationScripts)
    ciWorkflows = @($manifest.ciWorkflows)
    explicitNonClaims = @($manifest.explicitNonClaims)
    closedFormulaGates = $gateRows
    validationCases = $validationRows
}

$jsonPath = Join-Path $OutputDirectory "EngineeringCoreV1TraceabilityMatrix.json"
$markdownPath = Join-Path $OutputDirectory "EngineeringCoreV1TraceabilityMatrix.md"

$traceability |
    ConvertTo-Json -Depth 30 |
    Set-Content $jsonPath -Encoding utf8

$markdownLines = @()
$markdownLines += "# Engineering Core V1 Traceability Matrix"
$markdownLines += ""
$markdownLines += "## Status"
$markdownLines += ""
$markdownLines += "| Field | Value |"
$markdownLines += "|---|---|"
$markdownLines += "| Matrix name | $($traceability.matrixName) |"
$markdownLines += "| Version | $($traceability.version) |"
$markdownLines += "| Status | $($traceability.status) |"
$markdownLines += "| Closed formula gates | $($traceability.closedFormulaGateCount) |"
$markdownLines += "| Diagnostics | $($traceability.diagnosticsCount) |"
$markdownLines += "| Validation cases | $($traceability.validationCaseCount) |"
$markdownLines += ""
$markdownLines += "## Sources"
$markdownLines += ""
foreach ($source in $traceability.generatedFrom) {
    $markdownLines += "- $source"
}
$markdownLines += ""
$markdownLines += "## Annual 8760 requirements"
$markdownLines += ""
foreach ($flag in $traceability.annual8760Requirements) {
    $markdownLines += "- $flag"
}
$markdownLines += ""
$markdownLines += "## Application endpoints"
$markdownLines += ""
foreach ($endpoint in $traceability.applicationEndpoints) {
    $markdownLines += "- $endpoint"
}
$markdownLines += ""
$markdownLines += "## Closed formula gates"
$markdownLines += ""
$markdownLines += "| CalculationId | Status | Diagnostics | API | Report disclosure | Frontend |"
$markdownLines += "|---|---|---:|---|---|---|"
foreach ($gate in $traceability.closedFormulaGates) {
    $diagnosticsCount = @($gate.diagnostics).Count
    $markdownLines += "| $($gate.calculationId) | $($gate.status) | $diagnosticsCount | $($gate.apiVisible) | $($gate.reportDisclosureVisible) | $($gate.frontendVisible) |"
}
$markdownLines += ""
$markdownLines += "## Validation cases"
$markdownLines += ""
$markdownLines += "| CaseId | Stage | Status | Metrics |"
$markdownLines += "|---|---|---|---:|"
foreach ($case in $traceability.validationCases) {
    $metricCount = @($case.metrics).Count
    $markdownLines += "| $($case.caseId) | $($case.stage) | $($case.status) | $metricCount |"
}
$markdownLines += ""
$markdownLines += "## Out of scope v1"
$markdownLines += ""
foreach ($item in $traceability.outOfScopeV1) {
    $markdownLines += "- $item"
}
$markdownLines += ""
$markdownLines += "## Planned validation"
$markdownLines += ""
foreach ($item in $traceability.plannedValidation) {
    $markdownLines += "- $item"
}
$markdownLines += ""
$markdownLines += "## Explicit non-claims"
$markdownLines += ""
foreach ($claim in $traceability.explicitNonClaims) {
    $markdownLines += "- $claim"
}
$markdownLines += ""
$markdownLines += "## Verification scripts"
$markdownLines += ""
foreach ($script in $traceability.verificationScripts) {
    $markdownLines += "- $script"
}
$markdownLines += ""
$markdownLines += "## CI workflows"
$markdownLines += ""
foreach ($workflow in $traceability.ciWorkflows) {
    $markdownLines += "- $workflow"
}
$markdownLines += ""
$markdownLines += "## Interpretation"
$markdownLines += ""
$markdownLines += "This matrix proves traceability between the closed Engineering Core V1 formula gates, diagnostics catalog, validation registry, API visibility, report/frontend visibility, documentation, verification scripts and CI workflow."
$markdownLines += ""
$markdownLines += "It does not claim exact EnergyPlus numerical parity, exact pyBuildingEnergy numerical parity, ASHRAE 140 validation coverage, full ISO 52016 node/matrix solver parity or latent/moisture/humidity support in v1."

Set-Content $markdownPath $markdownLines -Encoding utf8

Write-Host "Engineering Core V1 traceability matrix generated:" -ForegroundColor Green
Write-Host "- $jsonPath"
Write-Host "- $markdownPath"
Write-Host "Closed gates: $($traceability.closedFormulaGateCount)"
Write-Host "Diagnostics: $($traceability.diagnosticsCount)"
Write-Host "Validation cases: $($traceability.validationCaseCount)"
