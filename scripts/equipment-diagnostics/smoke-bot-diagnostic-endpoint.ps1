param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

$endpoint = "$($BaseUrl.TrimEnd('/'))/api/v1/equipment-diagnostics/bot/diagnose"
$body = @{
    manufacturer = "Gree"
    code = "H5"
    series = "GMV"
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest `
        -Uri $endpoint `
        -Method Post `
        -ContentType "application/json" `
        -Body $body `
        -UseBasicParsing

    if ($response.StatusCode -ne 200) {
        throw "Expected HTTP 200 but received $($response.StatusCode)."
    }

    $payload = $response.Content | ConvertFrom-Json
    if ($null -eq $payload.status -or $null -eq $payload.verificationRequired) {
        throw "Response JSON does not contain status and verificationRequired."
    }

    Write-Host "HTTP status: $($response.StatusCode)"
    Write-Host "Response status: $($payload.status)"
    Write-Host "Verification required: $($payload.verificationRequired)"
}
catch {
    Write-Error "EquipmentDiagnostics bot endpoint smoke failed: $($_.Exception.Message)"
    exit 1
}
