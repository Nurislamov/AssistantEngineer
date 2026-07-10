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
$lines = $content -split "`r?`n"

function Select-MatchingLines {
    param(
        [string[]]$SourceLines,
        [string]$Pattern
    )

    return @($SourceLines | Where-Object { $_ -match $Pattern } | Select-Object -First 200)
}

function Write-EvidenceFile {
    param(
        [string]$Name,
        [string[]]$Lines
    )

    $path = Join-Path $outputFullPath $Name
    $normalizedLines = @($Lines)
    if ($normalizedLines.Count -eq 0) {
        Set-Content -LiteralPath $path -Value "No matching redacted evidence found." -NoNewline
        return
    }

    Set-Content -LiteralPath $path -Value ($normalizedLines -join [Environment]::NewLine) -NoNewline
}

$statusMarkerPattern = '(?:"t"|\\"t\\"|\bt)\s*[:=]\s*(?:"status"|\\"status\\"|status)'
$commandMarkerPattern = '(?:"t"|\\"t\\"|\bt)\s*[:=]\s*(?:"cmd"|\\"cmd\\"|cmd)'
$statusPattern = 'fullstatueJson|cordova\.callbackFromNative|getInfo|setMqttStatusCallback|' + $statusMarkerPattern + '|Pow|Mod|SetTem|TemUn|WdSpd|AllErr|SwUpDn|SwingLfRig|deviceState|status|hkgrih\.gree\.com|hk\.dis\.gree\.com'
$riskCandidatePattern = 'sendDataToDevice|cmd|action|control|publish|PUBLISH|control_order|dev_control|' + $commandMarkerPattern + '|write/control/action request|command payload'
$strongControlPattern = $commandMarkerPattern + '|control_order|dev_control|MQTT publish|PUBLISH|write/control/action request|command fields in a transport payload'

$statusEvidence = @(Select-MatchingLines -SourceLines $lines -Pattern $statusPattern)
$riskCandidates = @(Select-MatchingLines -SourceLines $lines -Pattern $riskCandidatePattern)
$strongControl = @(Select-MatchingLines -SourceLines $lines -Pattern $strongControlPattern)

Write-EvidenceFile -Name "status-evidence.txt" -Lines $statusEvidence
Write-EvidenceFile -Name "control-risk-evidence.txt" -Lines $riskCandidates

$negativeProof = New-Object System.Collections.Generic.List[string]
if ($strongControl.Count -eq 0) {
    $negativeProof.Add("No strong command/control markers found in the redacted input.")
}
else {
    $negativeProof.Add("Strong command/control markers require manual review:")
    foreach ($line in $strongControl) {
        $negativeProof.Add($line)
    }
}

$analyticsOnly = @($riskCandidates | Where-Object { $_ -match 'sendDataToDevice' -and $_ -notmatch $strongControlPattern })
if ($analyticsOnly.Count -gt 0) {
    $negativeProof.Add("sendDataToDevice appeared only as a risk candidate without a nearby strong command payload marker in extractor rules.")
}

Write-EvidenceFile -Name "negative-control-proof.txt" -Lines $negativeProof.ToArray()

$contractGaps = @(
    "Exact HTTP live read endpoint/method remains unresolved unless captured in masked evidence.",
    "Required auth/session headers and refresh behavior remain unresolved unless captured in masked evidence.",
    "Homes/devices response envelopes remain unresolved unless captured in masked evidence.",
    "Status callback shape may be evidenced, but direct HTTP live read contract still requires masked endpoint/method/body/response proof."
)
Write-EvidenceFile -Name "contract-gaps.txt" -Lines $contractGaps

$leakPatterns = @(
    "[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}",
    "\b(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}\b",
    "(?i)(access_token|refresh_token|Authorization|Cookie|Session|homeId|deviceId|deviceid|user_id|userId|uid|mac)\s*[:=]\s*(?!<)[^,\r\n;\s}]+"
)

$leaks = New-Object System.Collections.Generic.List[string]
foreach ($pattern in $leakPatterns) {
    foreach ($line in ($lines | Where-Object { $_ -match $pattern } | Select-Object -First 50)) {
        $leaks.Add($line)
    }
}

if ($leaks.Count -eq 0) {
    Write-EvidenceFile -Name "leak-check.txt" -Lines @("No obvious unredacted email, MAC-like value, or sensitive key value found.")
}
else {
    Write-EvidenceFile -Name "leak-check.txt" -Lines (@("Potential leak candidates require manual review:") + $leaks.ToArray())
}

$summary = @(
    "# Gree Plus Live Evidence Extract",
    "",
    "Input: $inputFullPath",
    "Status evidence lines: $($statusEvidence.Count)",
    "Control risk candidate lines: $($riskCandidates.Count)",
    "Strong command/control marker lines: $($strongControl.Count)",
    "Leak candidate lines: $($leaks.Count)",
    "",
    "Interpretation:",
    "- Status fields inside status callback or fullstatueJson are status evidence, not control proof.",
    "- sendDataToDevice in analytics or click traces is a risk candidate, not command proof by itself.",
    "- Confirmed read-only status client work still requires masked endpoint, method, headers, body, and response envelope evidence."
)
Write-EvidenceFile -Name "summary.md" -Lines $summary
