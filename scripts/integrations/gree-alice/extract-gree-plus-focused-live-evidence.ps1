param(
    [Parameter(Mandatory = $true)][string]$InputPath,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($InputPath) -or [string]::IsNullOrWhiteSpace($OutputDirectory)) {
    throw "Provide explicit -InputPath and -OutputDirectory. This helper has no default paths."
}

$inputFullPath = [System.IO.Path]::GetFullPath($InputPath)
$outputFullPath = [System.IO.Path]::GetFullPath($OutputDirectory)

if (-not (Test-Path -LiteralPath $inputFullPath -PathType Leaf)) {
    throw "InputPath does not exist: $inputFullPath"
}

New-Item -ItemType Directory -Force -Path $outputFullPath | Out-Null

$content = Get-Content -LiteralPath $inputFullPath -Raw
$lines = @($content -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

function Write-FocusedFile {
    param(
        [string]$Name,
        [string[]]$Lines
    )

    $path = Join-Path $outputFullPath $Name
    $normalizedLines = @($Lines)
    if ($normalizedLines.Count -eq 0) {
        Set-Content -LiteralPath $path -Value "No matching focused redacted evidence found." -NoNewline
        return
    }

    Set-Content -LiteralPath $path -Value ($normalizedLines -join [Environment]::NewLine) -NoNewline
}

$statusMarkerPattern = '(?:"t"|\\"t\\"|\bt)\s*[:=]\s*(?:"status"|\\"status\\"|status)'
$commandMarkerPattern = '(?:"t"|\\"t\\"|\bt)\s*[:=]\s*(?:"cmd"|\\"cmd\\"|cmd)'
$focusedIncludePattern = 'com\.gree\.greeplus|GREE\+|greeplus|ApiAddress|getEnvApiAddress|GreeAccess|access/action|actionBytes|hkgrih\.gree\.com|hk\.dis\.gree\.com|cordova|callbackFromNative|PluginInterface|getInfo|getSystemInfo|getUserInfo|getHomeId|setMqttStatusCallback|fullstatueJson|deviceState|AllErr|SetTem|Pow|Mod|WdSpd|TemUn|TemRec|' + $statusMarkerPattern + '|sendDataToDevice|' + $commandMarkerPattern + '|dev_control|control_order|control_Agtype|publish|PUBLISH'
$noisePattern = 'PowerManager|SourcePower|TvStatus|StatusBar|WindowManager|wifirtt|No service published for|oneconnect|ScanController|BLE|Bluetooth|binder|location|media|InputMethod|ActivityTaskManager|SurfaceFlinger'
$apiPattern = 'ApiAddress|getEnvApiAddress|GreeAccess|access/action|actionBytes|hkgrih\.gree\.com|hk\.dis\.gree\.com'
$statusPattern = 'cordova|callbackFromNative|PluginInterface|getInfo|setMqttStatusCallback|fullstatueJson|deviceState|AllErr|SetTem|Pow|Mod|WdSpd|TemUn|TemRec|' + $statusMarkerPattern
$riskCandidatePattern = 'sendDataToDevice|' + $commandMarkerPattern + '|dev_control|control_order|control_Agtype|access/action|actionBytes|publish|PUBLISH'
$publishContextPattern = '(?i)(publish|PUBLISH).*(gree|mqtt|topic)|(?:gree|mqtt|topic).*(publish|PUBLISH)'

$rejectedNoise = @($lines | Where-Object { $_ -match $noisePattern -and $_ -notmatch 'com\.gree\.greeplus|GREE\+|greeplus|fullstatueJson|' + $statusMarkerPattern })
$focusedLines = @($lines | Where-Object { $_ -match $focusedIncludePattern -and $_ -notmatch $noisePattern })
$greeAppLines = @($focusedLines | Where-Object { $_ -match 'com\.gree\.greeplus|GREE\+|greeplus|PluginInterface|cordova' })
$apiEvidence = @($focusedLines | Where-Object { $_ -match $apiPattern })
$statusEvidence = @($focusedLines | Where-Object { $_ -match $statusPattern })
$riskCandidates = @($focusedLines | Where-Object { $_ -match $riskCandidatePattern })

$strongControl = @($riskCandidates | Where-Object {
        ($_ -match $commandMarkerPattern) -or
        ($_ -match 'dev_control|control_order|control_Agtype') -or
        ($_ -match 'sendDataToDevice' -and $_ -match $commandMarkerPattern) -or
        ($_ -match $publishContextPattern)
    })

Write-FocusedFile -Name "focused-status-evidence.txt" -Lines $statusEvidence
Write-FocusedFile -Name "focused-api-evidence.txt" -Lines $apiEvidence
Write-FocusedFile -Name "focused-control-risk-evidence.txt" -Lines $riskCandidates
Write-FocusedFile -Name "focused-noise-rejected.txt" -Lines $rejectedNoise

$negativeProof = New-Object System.Collections.Generic.List[string]
if ($strongControl.Count -eq 0) {
    $negativeProof.Add("No strong command/control markers found in focused redacted Gree+ evidence.")
}
else {
    $negativeProof.Add("Strong command/control markers require manual review:")
    foreach ($line in $strongControl) {
        $negativeProof.Add($line)
    }
}

$sendOnly = @($riskCandidates | Where-Object { $_ -match 'sendDataToDevice' -and $_ -notmatch $commandMarkerPattern })
if ($sendOnly.Count -gt 0) {
    $negativeProof.Add("sendDataToDevice appeared as a focused risk candidate without a same-line command payload marker.")
}

Write-FocusedFile -Name "focused-negative-control-proof.txt" -Lines $negativeProof.ToArray()

$summary = @(
    "# Focused Gree Plus Live Evidence Extract",
    "",
    "Input: $inputFullPath",
    "Gree app lines: $($greeAppLines.Count)",
    "API endpoint lines: $($apiEvidence.Count)",
    "Callback/status lines: $($statusEvidence.Count)",
    "Command/control risk candidate lines: $($riskCandidates.Count)",
    "Strong command/control marker lines: $($strongControl.Count)",
    "Rejected noise lines: $($rejectedNoise.Count)",
    "",
    "Interpretation:",
    "- Status callback evidence and fullstatueJson fields are useful Gree+ status evidence.",
    "- Status callback evidence is not yet direct HTTP live-read endpoint/method/header/body/response proof.",
    "- sendDataToDevice in analytics or bridge logs is a risk candidate, not command proof unless a command payload marker is present.",
    "- Android/Samsung service, window, BLE, TV, and power-manager noise is excluded from focused status/control evidence."
)

Write-FocusedFile -Name "focused-summary.md" -Lines $summary
