param(
    [string]$BaseRef = "origin/master",
    [string]$OutputRoot = "artifacts/verification/equipment-diagnostics/telegram-activation-checklist"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
Set-Location $repoRoot

$outputPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputRoot))
$allowedRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "artifacts\verification"))
if (-not $outputPath.StartsWith($allowedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputRoot must remain under artifacts/verification."
}

New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$summaryPath = Join-Path $outputPath "activation-checklist-summary.md"
$reportPath = Join-Path $outputPath "activation-checklist-report.json"
$checks = [System.Collections.Generic.List[object]]::new()
$blockers = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

function Add-Check([string]$Name, [string]$Status, [string]$Evidence) {
    $checks.Add([ordered]@{ name = $Name; status = $Status; evidence = $Evidence })
    if ($Status -eq "fail") { $blockers.Add("$Name failed.") }
}

function Test-RequiredFiles([string]$Name, [string[]]$Paths) {
    $missing = @($Paths | Where-Object { -not (Test-Path -LiteralPath (Join-Path $repoRoot $_) -PathType Leaf) })
    if ($missing.Count -eq 0) { Add-Check $Name "pass" "Required files are present." }
    else { Add-Check $Name "fail" "Missing required files: $($missing -join ', ')." }
}

function Invoke-GitText {
    param([Parameter(Mandatory = $true)][string[]]$Arguments, [string]$Fallback = "unknown")
    try {
        $output = & git @Arguments 2>$null
        if ($LASTEXITCODE -eq 0) {
            $line = $output | Select-Object -First 1
            if (-not [string]::IsNullOrWhiteSpace($line)) { return $line.Trim() }
        }
    } catch { return $Fallback }
    return $Fallback
}

function Convert-ToRepoRelativePath([string]$Path) {
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    return $fullPath.Substring($repoRoot.Length).TrimStart("\", "/").Replace("\", "/")
}

$requiredDocs = @(
    "docs/equipment-diagnostics/telegram-closed-beta-activation-runbook.md",
    "docs/equipment-diagnostics/telegram-closed-beta-smoke-evidence-template.md",
    "docs/equipment-diagnostics/telegram-closed-beta-release-candidate.md",
    "docs/equipment-diagnostics/telegram-closed-beta-deployment-dry-run.md",
    "docs/equipment-diagnostics/telegram-closed-beta-smoke-matrix.md",
    "docs/equipment-diagnostics/telegram-operations-checklist.md"
)
$requiredScripts = @(
    "scripts/equipment-diagnostics/set-telegram-webhook.ps1",
    "scripts/equipment-diagnostics/get-telegram-webhook-info.ps1",
    "scripts/equipment-diagnostics/delete-telegram-webhook.ps1",
    "scripts/equipment-diagnostics/prepare-telegram-closed-beta-deployment-dry-run.ps1",
    "scripts/equipment-diagnostics/prepare-telegram-closed-beta-release-evidence.ps1"
)
Test-RequiredFiles "Activation documentation inventory" $requiredDocs
Test-RequiredFiles "Activation operation script inventory" $requiredScripts

$activationDocs = @(
    "docs/equipment-diagnostics/telegram-closed-beta-activation-runbook.md",
    "docs/equipment-diagnostics/telegram-closed-beta-smoke-evidence-template.md"
)
$combinedDocs = ($activationDocs | ForEach-Object { Get-Content -Raw -LiteralPath (Join-Path $repoRoot $_) }) -join "`n"
$requiredStatements = @(
    "closed beta only", "not for production or public launch", "no real secrets in Git",
    "no real domains in Git", "no real chat IDs in Git", "Telegram disabled by default",
    "chat ID discovery disabled by default", "chat ID discovery may be enabled only temporarily during setup",
    "no long polling", "no DB/audit persistence", "no external monitoring",
    "runtime catalog remains the only final-answer source", "manual-codebook/staging/preview are not final diagnosis",
    "do not claim complete vendor manual coverage", "do not bypass protections", "no hazardous electrical/refrigerant instructions"
)
$missingStatements = @($requiredStatements | Where-Object {
    $combinedDocs.IndexOf($_, [System.StringComparison]::OrdinalIgnoreCase) -lt 0
})
if ($missingStatements.Count -eq 0) { Add-Check "Activation documentation safety statements" "pass" "Required safety statements are present." }
else { Add-Check "Activation documentation safety statements" "fail" "One or more required safety statements are missing." }

$tokenPattern = '\b\d{8,10}:[A-Za-z0-9_-]{30,}\b'
$chatIdPattern = '(?i)\bchat\s*id\s*[:=]\s*-?\d+\b'
$unsafeDomain = $false
foreach ($urlMatch in [regex]::Matches($combinedDocs, '(?i)https?://(?<host>[a-z0-9.-]+)')) {
    $urlHost = $urlMatch.Groups["host"].Value.ToLowerInvariant()
    if ($urlHost -ne "localhost" -and $urlHost -ne "127.0.0.1" -and
        -not $urlHost.EndsWith(".example.com") -and $urlHost -ne "example.com" -and
        -not $urlHost.EndsWith(".example.test") -and $urlHost -ne "example.test") { $unsafeDomain = $true }
}
if ($combinedDocs -match $tokenPattern -or $combinedDocs -match $chatIdPattern -or $unsafeDomain) {
    Add-Check "Activation documentation sensitive-example scan" "fail" "A token-like value, numeric chat ID example, or non-placeholder URL host was found."
} else { Add-Check "Activation documentation sensitive-example scan" "pass" "No sensitive example values were found." }

$appSettings = Get-Content -Raw -LiteralPath (Join-Path $repoRoot "src/Backend/AssistantEngineer.Api/appsettings.json")
if ($appSettings -match '"IsEnabled"\s*:\s*false' -and $appSettings -match '"EnableChatIdDiscovery"\s*:\s*false') {
    Add-Check "Application Telegram safe defaults" "pass" "Telegram and chat ID discovery remain disabled in committed defaults."
} else { Add-Check "Application Telegram safe defaults" "fail" "Committed application defaults do not keep both Telegram flags disabled." }

$envExampleLines = @(Get-Content -LiteralPath (Join-Path $repoRoot "deploy/.env.example"))
if ($envExampleLines -contains "TELEGRAM_IS_ENABLED=false" -and
    $envExampleLines -contains "TELEGRAM_ENABLE_CHAT_ID_DISCOVERY=false") {
    Add-Check "Deployment example safe defaults" "pass" "Deployment example keeps Telegram and discovery disabled."
} else { Add-Check "Deployment example safe defaults" "fail" "Deployment example does not keep both Telegram flags disabled." }

$trackedArtifacts = @(& git ls-files -- "artifacts/verification")
$stagedArtifacts = @(& git diff --cached --name-only -- "artifacts/verification")
if ($LASTEXITCODE -ne 0 -or $trackedArtifacts.Count -gt 0 -or $stagedArtifacts.Count -gt 0) {
    Add-Check "Generated artifact Git hygiene" "fail" "Generated verification artifacts are tracked or staged."
} else { Add-Check "Generated artifact Git hygiene" "pass" "Generated verification artifacts are neither tracked nor staged." }

$branch = Invoke-GitText -Arguments @("branch", "--show-current") -Fallback ""
if ([string]::IsNullOrWhiteSpace($branch)) { $branch = Invoke-GitText -Arguments @("rev-parse", "--abbrev-ref", "HEAD") -Fallback "" }
if ([string]::IsNullOrWhiteSpace($branch) -or $branch -eq "HEAD") { $branch = $env:GITHUB_HEAD_REF }
if ([string]::IsNullOrWhiteSpace($branch)) { $branch = $env:GITHUB_REF_NAME }
if ([string]::IsNullOrWhiteSpace($branch)) { $branch = "detached" }
$head = Invoke-GitText -Arguments @("rev-parse", "HEAD") -Fallback "unknown"

$relativeSummaryPath = Convert-ToRepoRelativePath $summaryPath
$relativeReportPath = Convert-ToRepoRelativePath $reportPath
$limitations = @(
    "Closed beta checklist only; no deployment, activation, or public launch is performed.",
    "No Telegram API or webhook operation is executed.",
    "No real secret, domain, chat ID, deploy environment value, raw message body, PDF, or manual file is collected.",
    "Runtime catalog remains the only final-answer source; manual-codebook, staging, and preview are not final diagnosis.",
    "No DB/audit persistence, external monitoring, long polling, AI, RAG, or vector search is added."
)
$report = [ordered]@{
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O"); baseRef = $BaseRef; branch = $branch; head = $head
    status = $(if ($blockers.Count -eq 0) { "PASS" } else { "FAIL" })
    blockers = @($blockers); warnings = @($warnings); checks = @($checks)
    generatedArtifacts = @($relativeSummaryPath, $relativeReportPath); limitations = $limitations
}
$report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $reportPath -Encoding utf8

@(
    "# Telegram Closed Beta Activation Checklist", "",
    "- Status: **$($report.status)**", "- Base reference: $BaseRef", "- Branch: $branch", "- Head: $head",
    "- Blockers: $($blockers.Count)", "- Warnings: $($warnings.Count)", "",
    "## Boundary", "",
    "- Manual closed-beta preparation only; no Telegram network call or webhook operation is performed.",
    "- Telegram and chat ID discovery remain disabled by default.", "",
    "## Generated Artifacts", "", "- $relativeSummaryPath", "- $relativeReportPath"
) -join [Environment]::NewLine | Set-Content -LiteralPath $summaryPath -Encoding utf8

Write-Host "Status: $($report.status)"
Write-Host "Blockers: $($blockers.Count)"
Write-Host "Warnings: $($warnings.Count)"
Write-Host "Report: $relativeReportPath"
Write-Host "Summary: $relativeSummaryPath"
if ($blockers.Count -gt 0) { exit 1 }
exit 0
