param(
    [string]$FrontendUrl = "http://localhost:8081",
    [string]$ApiUrl = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"

function Assert-HttpSuccess([string]$Name, [string]$Url) {
    $response = Invoke-WebRequest -Uri $Url -UseBasicParsing
    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
        throw "$Name smoke failed with HTTP $($response.StatusCode)."
    }
    Write-Host "PASS: $Name"
}

Assert-HttpSuccess "Frontend" $FrontendUrl
Assert-HttpSuccess "API health" "$ApiUrl/health"

$diagnosticBody = @{
    manufacturer = "Gree"
    code = "H5"
} | ConvertTo-Json
$diagnostic = Invoke-WebRequest `
    -Method Post `
    -Uri "$ApiUrl/api/v1/equipment-diagnostics/bot/diagnose" `
    -ContentType "application/json" `
    -Body $diagnosticBody `
    -UseBasicParsing
if ($diagnostic.StatusCode -ne 200) { throw "Equipment diagnostics bot smoke failed." }
Write-Host "PASS: Equipment diagnostics bot"

try {
    Invoke-WebRequest `
        -Method Post `
        -Uri "$ApiUrl/api/v1/equipment-diagnostics/telegram/webhook" `
        -ContentType "application/json" `
        -Body '{"update_id":1}' `
        -UseBasicParsing | Out-Null
    throw "Telegram webhook must remain disabled for the default production-like scaffold."
}
catch {
    if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw }
}
Write-Host "PASS: Telegram webhook disabled by default"
