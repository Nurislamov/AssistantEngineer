param(
    [string]$Text,
    [string]$InputPath,
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Redact-GreePlusLiveEvidence {
    param([Parameter(Mandatory = $true)][string]$Value)

    $redacted = $Value

    $redacted = [regex]::Replace($redacted, "[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", "<EMAIL>")
    $redacted = [regex]::Replace($redacted, "(?i)(Authorization\s*[:=]\s*)(Bearer\s+)?[^,\r\n;]+", '${1}<AUTHORIZATION>')
    $redacted = [regex]::Replace($redacted, "(?i)(Cookie|Session)(\s*[:=]\s*)[^,\r\n;]+", '${1}${2}<SESSION>')
    $redacted = [regex]::Replace($redacted, "\b(?:10|172\.(?:1[6-9]|2\d|3[01])|192\.168)\.\d{1,3}\.\d{1,3}\b", "<LOCAL_IP>")
    $redacted = [regex]::Replace($redacted, "(?i)\b(?:\+?\d[\d\s().-]{8,}\d)\b", "<PHONE>")
    $redacted = [regex]::Replace($redacted, "\b(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}\b", "<DEVICE_MAC>")

    $keyMarkers = @{
        "access_token" = "<ACCESS_TOKEN>"
        "refresh_token" = "<REFRESH_TOKEN>"
        "token" = "<ACCESS_TOKEN>"
        "uid" = "<UID>"
        "userId" = "<UID>"
        "homeId" = "<HOME_ID>"
        "deviceId" = "<DEVICE_ID>"
        "mac" = "<DEVICE_MAC>"
        "account" = "<ACCOUNT>"
        "accountName" = "<ACCOUNT>"
        "password" = "<PASSWORD>"
        "credential" = "<CREDENTIAL>"
    }

    foreach ($entry in $keyMarkers.GetEnumerator()) {
        $key = [regex]::Escape($entry.Key)
        $marker = $entry.Value
        $redacted = [regex]::Replace(
            $redacted,
            "(?i)([""']?$key[""']?\s*[:=]\s*)([""']?)[^,""'\s;}\]]+(\2)",
            "`$1`$2$marker`$3")
    }

    return $redacted
}

if ([string]::IsNullOrWhiteSpace($Text) -and [string]::IsNullOrWhiteSpace($InputPath)) {
    throw "Provide -Text or an explicit -InputPath. This helper has no default input path."
}

if (-not [string]::IsNullOrWhiteSpace($Text) -and -not [string]::IsNullOrWhiteSpace($InputPath)) {
    throw "Provide either -Text or -InputPath, not both."
}

if (-not [string]::IsNullOrWhiteSpace($InputPath)) {
    $sourceText = Get-Content -LiteralPath $InputPath -Raw
}
else {
    $sourceText = $Text
}

$result = Redact-GreePlusLiveEvidence -Value $sourceText

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    Set-Content -LiteralPath $OutputPath -Value $result -NoNewline
}
else {
    $result
}
