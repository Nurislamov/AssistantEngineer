param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$ScenarioDirectory = "docs/equipment-diagnostics/bot-scenarios"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$scenarioRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $ScenarioDirectory))
$endpoint = "$($BaseUrl.TrimEnd('/'))/api/v1/equipment-diagnostics/bot/diagnose"
$statusValues = @("Answer", "ClarificationRequired", "NotFound", "ReferenceOnly", "Unsupported", "UnsafeOrOutOfScope")
$sideValues = @("Indoor", "Outdoor", "Chiller", "Controller", "CommissioningTool", "Unknown")
$displayValues = @("WiredController", "OduMainBoardLed", "IduDisplay", "CentralizedController", "PortableCommissioningTool", "MobileAppOrGateway", "Unknown")
$forbiddenFragments = @("bypass", "disable protection", "disable protections", "force run", "short protection", "ignore protection", "artifacts/verification", "Knowledge/staging", "Knowledge/manual-codebook", "staging-candidate-preview", ".pdf")

$failures = 0
foreach ($file in Get-ChildItem -LiteralPath $scenarioRoot -Filter "*.scenario.json" | Sort-Object Name) {
    try {
        $scenario = Get-Content -Raw -LiteralPath $file.FullName | ConvertFrom-Json
        $request = [ordered]@{
            manufacturer = $scenario.request.manufacturer
            code = $scenario.request.code
        }
        foreach ($name in @("series", "modelCode", "freeText")) {
            if ($null -ne $scenario.request.$name) { $request[$name] = $scenario.request.$name }
        }
        if ($null -ne $scenario.request.equipmentSide) { $request["equipmentSide"] = [array]::IndexOf($sideValues, [string]$scenario.request.equipmentSide) }
        if ($null -ne $scenario.request.displayContext) { $request["displayContext"] = [array]::IndexOf($displayValues, [string]$scenario.request.displayContext) }

        $response = Invoke-WebRequest -Uri $endpoint -Method Post -ContentType "application/json" -Body ($request | ConvertTo-Json) -UseBasicParsing
        if ($response.StatusCode -ne 200) { throw "Expected HTTP 200, received $($response.StatusCode)." }
        $payload = $response.Content | ConvertFrom-Json
        $actualStatus = $statusValues[[int]$payload.status]
        if ($actualStatus -ne $scenario.expected.responseStatus) { throw "Expected $($scenario.expected.responseStatus), received $actualStatus." }
        if ($scenario.expected.requiresSafetyBoundary -and [string]::IsNullOrWhiteSpace([string]$payload.safetyCard.boundary)) { throw "Safety boundary is missing." }
        if ($scenario.expected.requiresSourceOrProvenance -and $null -eq $payload.sourceCard) { throw "Source/provenance is missing." }
        foreach ($fragment in $forbiddenFragments) {
            if ($response.Content.Contains($fragment, [System.StringComparison]::OrdinalIgnoreCase)) { throw "Response contains a forbidden fragment." }
        }
        Write-Host "PASS $($scenario.scenarioId): $actualStatus"
    }
    catch {
        $failures++
        Write-Error "FAIL $($file.Name): $($_.Exception.Message)" -ErrorAction Continue
    }
}

if ($failures -gt 0) { exit 1 }
Write-Host "PASS: all bot field scenarios"
