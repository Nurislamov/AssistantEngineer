param(
    [string]$BaseUrl = "http://localhost:8081",
    [string]$ApiBaseUrl = "http://localhost:8080",
    [switch]$TelegramExpectedEnabled,
    [switch]$ExplicitlyExpectTelegramEnabled
)

$ErrorActionPreference = "Stop"
$results = [System.Collections.Generic.List[string]]::new()
$failures = [System.Collections.Generic.List[string]]::new()
$correlationId = "deployment-smoke-$([Guid]::NewGuid().ToString('N'))"
$correlationHeaders = @{ "X-Correlation-ID" = $correlationId }

function Add-SmokeResult([string]$Name, [scriptblock]$Check) {
    try {
        & $Check
        $results.Add("PASS: $Name")
    } catch {
        $failures.Add("FAIL: $Name - $($_.Exception.Message)")
    }
}

function Assert-HttpSuccess([string]$Url, [bool]$VerifyCorrelation = $false) {
    $response = Invoke-WebRequest -Uri $Url -Headers $correlationHeaders -UseBasicParsing
    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
        throw "HTTP $($response.StatusCode)"
    }
    if ($VerifyCorrelation -and $response.Headers["X-Correlation-ID"] -ne $correlationId) {
        throw "X-Correlation-ID response header did not match the safe smoke request ID."
    }
}

Add-SmokeResult "Frontend reachable" {
    Assert-HttpSuccess $BaseUrl
}
Add-SmokeResult "API health reachable" {
    Assert-HttpSuccess "$ApiBaseUrl/health" $true
}
Add-SmokeResult "API readiness reachable" {
    Assert-HttpSuccess "$ApiBaseUrl/ready" $true
}
Add-SmokeResult "Equipment diagnostics bot deterministic response" {
    $diagnosticBody = @{
        manufacturer = "Gree"
        code = "H5"
    } | ConvertTo-Json
    $diagnostic = Invoke-WebRequest `
        -Method Post `
        -Uri "$ApiBaseUrl/api/v1/equipment-diagnostics/bot/diagnose" `
        -Headers $correlationHeaders `
        -ContentType "application/json" `
        -Body $diagnosticBody `
        -UseBasicParsing
    if ($diagnostic.StatusCode -ne 200) {
        throw "HTTP $($diagnostic.StatusCode)"
    }
    $payload = $diagnostic.Content | ConvertFrom-Json
    if ([string]::IsNullOrWhiteSpace($payload.responseStatus)) {
        throw "Response status is missing."
    }
    if ($diagnostic.Headers["X-Correlation-ID"] -ne $correlationId) {
        throw "X-Correlation-ID response header did not match the safe smoke request ID."
    }
}

$expectTelegramEnabled = $TelegramExpectedEnabled -or $ExplicitlyExpectTelegramEnabled
if (-not $expectTelegramEnabled) {
    Add-SmokeResult "Telegram webhook disabled by default" {
        try {
            Invoke-WebRequest `
                -Method Post `
                -Uri "$ApiBaseUrl/api/v1/equipment-diagnostics/telegram/webhook" `
                -ContentType "application/json" `
                -Body '{"update_id":1}' `
                -UseBasicParsing | Out-Null
            throw "Webhook returned success while expected disabled."
        } catch {
            $statusCode = $_.Exception.Response.StatusCode.value__
            if ($statusCode -ne 404) {
                throw "Expected HTTP 404, received $statusCode."
            }
        }
    }
} else {
    $results.Add("SKIP: Telegram disabled-default check (explicitly expected enabled)")
}

$results | ForEach-Object { Write-Host $_ }
$failures | ForEach-Object { Write-Host $_ }
Write-Host "Summary: $($results.Count) passed/skipped; $($failures.Count) failed"
Write-Host "Correlation ID: $correlationId"
if ($failures.Count -gt 0) {
    exit 1
}
