param(
    [string]$BaseRef = "origin/master",
    [string]$OutputRoot = "artifacts/verification/equipment-diagnostics/telegram-deployment-dry-run",
    [switch]$SkipDockerComposeConfig,
    [switch]$SkipDeploymentScaffoldValidation,
    [switch]$SkipProductionEnvValidation,
    [switch]$SkipReleaseEvidenceReference
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
$summaryPath = Join-Path $outputPath "deployment-dry-run-summary.md"
$reportPath = Join-Path $outputPath "deployment-dry-run-report.json"
$checks = [System.Collections.Generic.List[object]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()
$blockers = [System.Collections.Generic.List[string]]::new()

function Convert-ToRepoRelativePath([string]$Path) {
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not $fullPath.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Dry-run evidence path must remain under the repository root."
    }
    return $fullPath.Substring($repoRoot.Length).TrimStart("\", "/").Replace("\", "/")
}

function Add-Check([string]$Name, [string]$Status, [string]$Evidence) {
    $checks.Add([ordered]@{ name = $Name; status = $Status; evidence = $Evidence })
    if ($Status -eq "fail") {
        $blockers.Add("$Name failed.")
    }
}

function Test-RequiredFiles([string]$Name, [string[]]$Paths) {
    $missing = @($Paths | Where-Object { -not (Test-Path -LiteralPath (Join-Path $repoRoot $_) -PathType Leaf) })
    if ($missing.Count -gt 0) {
        Add-Check $Name "fail" "Missing required files: $($missing -join ', ')."
    } else {
        Add-Check $Name "pass" "Required files are present."
    }
}

function Invoke-SafeValidation([string]$Name, [scriptblock]$Action) {
    try {
        & $Action
        if ($LASTEXITCODE -ne 0) {
            Add-Check $Name "fail" "Validation returned a non-zero exit code."
        } else {
            Add-Check $Name "pass" "Validation completed successfully."
        }
    } catch {
        Add-Check $Name "fail" "Validation failed without exposing environment values."
    }
}

function Invoke-GitText {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,

        [string]$Fallback = "unknown"
    )

    try {
        $output = & git @Arguments 2>$null
        if ($LASTEXITCODE -eq 0) {
            $line = $output | Select-Object -First 1
            if (-not [string]::IsNullOrWhiteSpace($line)) {
                return $line.Trim()
            }
        }
    } catch {
        return $Fallback
    }

    return $Fallback
}

$requiredDeploymentFiles = @(
    "deploy/docker-compose.yml",
    "deploy/.env.example",
    "deploy/reverse-proxy/Caddyfile.example",
    "deploy/docker/backend/Dockerfile",
    "deploy/docker/frontend/Dockerfile"
)
$requiredDeploymentScripts = @(
    "scripts/deployment/validate-production-env.ps1",
    "scripts/deployment/validate-deployment-scaffold.ps1",
    "scripts/deployment/build-production-images.ps1",
    "scripts/deployment/start-production-stack.ps1",
    "scripts/deployment/smoke-production-stack.ps1"
)
$telegramOperationScripts = @(
    "scripts/equipment-diagnostics/set-telegram-webhook.ps1",
    "scripts/equipment-diagnostics/get-telegram-webhook-info.ps1",
    "scripts/equipment-diagnostics/delete-telegram-webhook.ps1"
)
$releaseEvidenceReferences = @(
    "docs/equipment-diagnostics/telegram-closed-beta-release-evidence.md",
    "docs/equipment-diagnostics/telegram-closed-beta-release-candidate.md",
    "scripts/equipment-diagnostics/prepare-telegram-closed-beta-release-evidence.ps1"
)

Test-RequiredFiles "Deployment scaffold files" $requiredDeploymentFiles
Test-RequiredFiles "Deployment operation scripts" $requiredDeploymentScripts
Test-RequiredFiles "Telegram operation script inventory" $telegramOperationScripts

$envExamplePath = Join-Path $repoRoot "deploy/.env.example"
$envExampleLines = @(Get-Content -LiteralPath $envExamplePath)
$requiredPlaceholderLines = @(
    "TELEGRAM_IS_ENABLED=false",
    "TELEGRAM_ENABLE_CHAT_ID_DISCOVERY=false",
    "AssistantEngineer__EquipmentDiagnostics__Telegram__BotToken=",
    "AssistantEngineer__EquipmentDiagnostics__Telegram__WebhookSecret=",
    "AssistantEngineer__EquipmentDiagnostics__Telegram__AllowedChatIds__0=",
    "AssistantEngineer__EquipmentDiagnostics__Telegram__DeniedChatIds__0="
)
$missingPlaceholderLines = @($requiredPlaceholderLines | Where-Object {
    $envExampleLines -notcontains $_
})
if ($missingPlaceholderLines.Count -gt 0) {
    Add-Check "Environment example safe defaults" "fail" "One or more required safe placeholder keys are missing or non-empty."
} else {
    Add-Check "Environment example safe defaults" "pass" "Telegram and chat ID discovery are disabled; credential and chat-list placeholders are empty."
}

$compose = Get-Content -Raw -LiteralPath (Join-Path $repoRoot "deploy/docker-compose.yml")
if ($compose -match 'TELEGRAM_IS_ENABLED:-false' -and
    $compose -match 'TELEGRAM_ENABLE_CHAT_ID_DISCOVERY:-false') {
    Add-Check "Compose Telegram safe defaults" "pass" "Telegram and chat ID discovery default to false."
} else {
    Add-Check "Compose Telegram safe defaults" "fail" "Compose does not preserve both Telegram safe defaults."
}

$scanFiles = @(
    $requiredDeploymentFiles +
    $requiredDeploymentScripts +
    @(
        "docs/equipment-diagnostics/telegram-closed-beta-deployment-dry-run.md",
        "scripts/equipment-diagnostics/prepare-telegram-closed-beta-deployment-dry-run.ps1"
    )
) | Sort-Object -Unique
$tokenPattern = '\b\d{8,10}:[A-Za-z0-9_-]{30,}\b'
$unsafeTokenFiles = [System.Collections.Generic.List[string]]::new()
$unsafeDomainFiles = [System.Collections.Generic.List[string]]::new()
foreach ($relativePath in $scanFiles) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        continue
    }

    $content = Get-Content -Raw -LiteralPath $path
    if ($content -match $tokenPattern) {
        $unsafeTokenFiles.Add($relativePath)
    }

    foreach ($urlMatch in [regex]::Matches($content, '(?i)https?://(?<host>[a-z0-9.-]+)')) {
        $urlHost = $urlMatch.Groups["host"].Value.ToLowerInvariant()
        if ($urlHost -ne "localhost" -and $urlHost -ne "127.0.0.1" -and
            -not $urlHost.EndsWith(".example.com") -and $urlHost -ne "example.com" -and
            -not $urlHost.EndsWith(".example.test") -and $urlHost -ne "example.test") {
            $unsafeDomainFiles.Add($relativePath)
        }
    }
}
if ($unsafeTokenFiles.Count -eq 0) {
    Add-Check "Deployment token-pattern scan" "pass" "No Telegram token-like value was found in reviewed deployment docs or scripts."
} else {
    Add-Check "Deployment token-pattern scan" "fail" "Token-like values were found in reviewed deployment files."
}
if ($unsafeDomainFiles.Count -eq 0) {
    Add-Check "Deployment domain scan" "pass" "Only safe placeholder or local URL hosts were found."
} else {
    Add-Check "Deployment domain scan" "fail" "A non-placeholder URL host was found in reviewed deployment files."
}

$trackedArtifacts = @(& git ls-files -- "artifacts/verification")
if ($LASTEXITCODE -ne 0) {
    Add-Check "Committed generated-artifact scan" "fail" "Unable to inspect tracked verification artifacts."
} elseif ($trackedArtifacts.Count -gt 0) {
    Add-Check "Committed generated-artifact scan" "fail" "Generated verification artifacts are tracked by Git."
} else {
    Add-Check "Committed generated-artifact scan" "pass" "No generated verification artifacts are tracked by Git."
}

if ($SkipProductionEnvValidation) {
    Add-Check "Production environment example validation" "not_run" "Skipped by explicit dry-run option."
} else {
    Invoke-SafeValidation "Production environment example validation" {
        & .\scripts\deployment\validate-production-env.ps1 -EnvPath "deploy/.env.example" -AllowPlaceholders
    }
}

if ($SkipDeploymentScaffoldValidation) {
    Add-Check "Deployment scaffold validation" "not_run" "Skipped by explicit dry-run option."
} else {
    Invoke-SafeValidation "Deployment scaffold validation" {
        & .\scripts\deployment\validate-deployment-scaffold.ps1
    }
}

if ($SkipReleaseEvidenceReference) {
    Add-Check "ED-22A and ED-22B evidence references" "not_run" "Skipped by explicit dry-run option."
} else {
    Test-RequiredFiles "ED-22A and ED-22B evidence references" $releaseEvidenceReferences
}

$dockerEvidence = if ($SkipDockerComposeConfig) {
    "Skipped by explicit dry-run option."
} else {
    "Not run: ED-22C never requires or invokes Docker Compose."
}
Add-Check "Docker Compose config" "not_run" $dockerEvidence

$relativeSummaryPath = Convert-ToRepoRelativePath $summaryPath
$relativeReportPath = Convert-ToRepoRelativePath $reportPath
$branch = Invoke-GitText -Arguments @("branch", "--show-current") -Fallback ""
if ([string]::IsNullOrWhiteSpace($branch)) {
    $branch = Invoke-GitText -Arguments @("rev-parse", "--abbrev-ref", "HEAD") -Fallback ""
}
if ([string]::IsNullOrWhiteSpace($branch) -or $branch -eq "HEAD") {
    $branch = $env:GITHUB_HEAD_REF
}
if ([string]::IsNullOrWhiteSpace($branch)) {
    $branch = $env:GITHUB_REF_NAME
}
if ([string]::IsNullOrWhiteSpace($branch)) {
    $branch = "detached"
}

$head = Invoke-GitText -Arguments @("rev-parse", "HEAD") -Fallback "unknown"
$limitations = @(
    "Closed beta preparation only; not deployment, activation, production, or public release.",
    "No Telegram network call or webhook operation is executed.",
    "No Docker Compose command is executed.",
    "No real secret, domain, chat ID, deploy environment value, raw log, PDF, or manual file is collected.",
    "Runtime catalog is the only final-answer source; manual-codebook, staging, and preview are not final diagnosis.",
    "Polling is disabled by default; no database or audit persistence, external monitoring, AI, RAG, or vector search is added."
)
$report = [ordered]@{
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    baseRef = $BaseRef
    branch = $branch
    head = $head
    status = $(if ($blockers.Count -eq 0) { "PASS" } else { "FAIL" })
    blockers = @($blockers)
    warnings = @($warnings)
    checks = @($checks)
    generatedArtifacts = @($relativeSummaryPath, $relativeReportPath)
    limitations = $limitations
}
$report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $reportPath -Encoding utf8

$summary = @(
    "# Telegram Closed Beta Deployment Activation Dry Run",
    "",
    "- Status: **$($report.status)**",
    "- Base reference: $BaseRef",
    "- Branch: $branch",
    "- Head: $head",
    "- Blockers: $($blockers.Count)",
    "- Warnings: $($warnings.Count)",
    "",
    "## Boundary",
    "",
    "- Closed beta preparation only; no deployment or Telegram activation is performed.",
    "- No Telegram network call, webhook operation, Docker command, real credential, domain, or chat ID is used.",
    "- Telegram and chat ID discovery remain disabled by default.",
    "",
    "## Generated Artifacts",
    "",
    "- $relativeSummaryPath",
    "- $relativeReportPath",
    "",
    "## Manual Review",
    "",
    "- Review every check and blocker before a separately approved activation.",
    "- Confirm generated artifacts remain ignored and uncommitted.",
    "- Confirm runtime catalog remains the only final-answer source."
)
$summary -join [Environment]::NewLine | Set-Content -LiteralPath $summaryPath -Encoding utf8

Write-Host "Status: $($report.status)"
Write-Host "Blockers: $($blockers.Count)"
Write-Host "Warnings: $($warnings.Count)"
Write-Host "Report: $relativeReportPath"
Write-Host "Summary: $relativeSummaryPath"
if ($blockers.Count -gt 0) { exit 1 }
exit 0
