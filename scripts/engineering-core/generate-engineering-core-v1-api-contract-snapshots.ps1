param(
    [string] $OutputDirectory = "docs/api/engineering-core-v1"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$manifestPath = "docs/releases/EngineeringCoreV1Manifest.json"
$diagnosticsCatalogPath = "docs/calculations/EngineeringCoreV1DiagnosticsCatalog.json"

if (-not (Test-Path $manifestPath)) {
    throw "Manifest not found: $manifestPath"
}

if (-not (Test-Path $diagnosticsCatalogPath)) {
    throw "Diagnostics catalog not found: $diagnosticsCatalogPath"
}

New-Item -ItemType Directory -Force $OutputDirectory | Out-Null

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$diagnosticsCatalog = Get-Content $diagnosticsCatalogPath -Raw | ConvertFrom-Json

$formulaGates = @($manifest.closedFormulaGates | ForEach-Object {
    [ordered]@{
        calculationId = $_
        name = $_
        status = "ClosedV1"
        priority = if ($_ -match "DHW|SYSTEM_ENERGY|EQUIPMENT_SIZING|GROUND|ADJACENT") { "P1" } else { "P0" }
        scope = "See docs/releases/EngineeringCoreV1Manifest.json and FormulaAuditMatrix for authoritative scope."
        limitation = "ClosedV1 engineering formula gate with documented limitations; no exact EnergyPlus/pyBuildingEnergy/ASHRAE 140 parity claim."
    }
})

$statusSnapshot = [ordered]@{
    coreName = $manifest.coreName
    version = $manifest.version
    status = $manifest.status
    formulaGatesClosed = [bool]$manifest.formulaGatesClosed
    weather8760GatesClosed = [bool]$manifest.weather8760GatesClosed
    annualHourly8760GateClosed = [bool]$manifest.annualHourly8760GateClosed
    successfulResultsMustNotContainErrorDiagnostics = [bool]$manifest.successfulResultsMustNotContainErrorDiagnostics
    formulaGates = $formulaGates
    explicitNonClaims = @($manifest.explicitNonClaims)
    outOfScopeV1 = @($manifest.outOfScopeV1)
    plannedValidation = @($manifest.plannedValidation)
    requiredAnnual8760Flags = @($manifest.requiredAnnual8760Flags)
    documentationFiles = @($manifest.documentationFiles)
}

$diagnosticsSnapshot = [ordered]@{
    catalogName = $diagnosticsCatalog.catalogName
    version = $diagnosticsCatalog.version
    status = $diagnosticsCatalog.status
    rules = $diagnosticsCatalog.rules
    diagnostics = @($diagnosticsCatalog.diagnostics)
}

$statusJsonPath = Join-Path $OutputDirectory "status.sample.json"
$diagnosticsJsonPath = Join-Path $OutputDirectory "diagnostics-catalog.sample.json"
$httpPath = Join-Path $OutputDirectory "engineering-core-v1.http"

$statusSnapshot |
    ConvertTo-Json -Depth 20 |
    Set-Content $statusJsonPath -Encoding utf8

$diagnosticsSnapshot |
    ConvertTo-Json -Depth 20 |
    Set-Content $diagnosticsJsonPath -Encoding utf8

$httpContent = @"
@baseUrl = https://localhost:5001

### Engineering Core V1 status
GET {{baseUrl}}/api/v1/calculations/engineering-core/v1/status
Accept: application/json

### Engineering Core V1 diagnostics catalog
GET {{baseUrl}}/api/v1/calculations/engineering-core/v1/diagnostics-catalog
Accept: application/json
"@

Set-Content $httpPath $httpContent -Encoding utf8

Write-Host "Engineering Core V1 API contract snapshots generated:" -ForegroundColor Green
Write-Host "- $statusJsonPath"
Write-Host "- $diagnosticsJsonPath"
Write-Host "- $httpPath"
Write-Host "Formula gates: $(@($formulaGates).Count)"
Write-Host "Diagnostics: $(@($diagnosticsSnapshot.diagnostics).Count)"
