param(
    [string]$BaseRef = "origin/master",
    [string]$OutputRoot = "artifacts/verification/equipment-diagnostics/telegram-final-go-no-go",
    [switch]$SkipBranchReadiness,
    [switch]$SkipReleaseEvidence,
    [switch]$SkipDeploymentDryRun,
    [switch]$SkipActivationChecklist,
    [switch]$SkipBackendTests,
    [switch]$SkipEquipmentDiagnosticsTests,
    [switch]$SkipGoalRunReportValidation
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
$summaryPath = Join-Path $outputPath "final-go-no-go-summary.md"
$reportPath = Join-Path $outputPath "final-go-no-go-report.json"
$checks = [System.Collections.Generic.List[object]]::new()
$blockers = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

function Convert-ToRepoRelativePath([string]$Path) {
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not $fullPath.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Evidence path must remain under the repository root."
    }
    return $fullPath.Substring($repoRoot.Length).TrimStart("\", "/").Replace("\", "/")
}

function Add-Check([string]$Name, [string]$Command, [string]$Status, [string]$Evidence) {
    $checks.Add([ordered]@{ name = $Name; command = $Command; status = $Status; evidence = $Evidence })
    if ($Status -eq "fail") { $blockers.Add("$Name failed.") }
}

function Test-RequiredFiles([string]$Name, [string[]]$Paths) {
    $missing = @($Paths | Where-Object { -not (Test-Path -LiteralPath (Join-Path $repoRoot $_) -PathType Leaf) })
    if ($missing.Count -eq 0) {
        Add-Check $Name "repository file inventory" "pass" "Required files are present."
    } else {
        Add-Check $Name "repository file inventory" "fail" "Missing required files: $($missing -join ', ')."
    }
}

function Invoke-Check([string]$Name, [string]$CommandText, [scriptblock]$Action) {
    Write-Host "RUN: $Name"
    try {
        & $Action
        if ($LASTEXITCODE -eq 0) {
            Add-Check $Name $CommandText "pass" "Command completed successfully."
        } else {
            Add-Check $Name $CommandText "fail" "Command returned a non-zero exit code."
        }
    } catch {
        Add-Check $Name $CommandText "fail" "Command failed without exposing environment values."
    }
}

function Skip-Check([string]$Name, [string]$CommandText) {
    Add-Check $Name $CommandText "not_run" "Skipped by explicit final go/no-go option."
    $warnings.Add("$Name was skipped and requires manual review.")
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

$requiredDocs = @(
    "docs/equipment-diagnostics/telegram-closed-beta-release-evidence.md",
    "docs/equipment-diagnostics/telegram-closed-beta-release-candidate.md",
    "docs/equipment-diagnostics/telegram-closed-beta-deployment-dry-run.md",
    "docs/equipment-diagnostics/telegram-closed-beta-activation-runbook.md",
    "docs/equipment-diagnostics/telegram-closed-beta-smoke-evidence-template.md",
    "docs/equipment-diagnostics/telegram-closed-beta-smoke-matrix.md"
)
$requiredEvidenceScripts = @(
    "scripts/equipment-diagnostics/prepare-telegram-closed-beta-release-evidence.ps1",
    "scripts/equipment-diagnostics/prepare-telegram-closed-beta-deployment-dry-run.ps1",
    "scripts/equipment-diagnostics/prepare-telegram-closed-beta-activation-checklist.ps1"
)
$manualActivationTools = @(
    "scripts/equipment-diagnostics/set-telegram-webhook.ps1",
    "scripts/equipment-diagnostics/get-telegram-webhook-info.ps1",
    "scripts/equipment-diagnostics/delete-telegram-webhook.ps1"
)
Test-RequiredFiles "ED-22 documentation inventory" $requiredDocs
Test-RequiredFiles "ED-22 evidence script inventory" $requiredEvidenceScripts
Test-RequiredFiles "Manual activation tool inventory" $manualActivationTools

if ($SkipBranchReadiness) {
    Skip-Check "Branch readiness" ".\scripts\dev\verify-branch-readiness.ps1 -BaseRef <base-ref> -Scope EquipmentDiagnostics"
} else {
    Invoke-Check "Branch readiness" ".\scripts\dev\verify-branch-readiness.ps1 -BaseRef <base-ref> -Scope EquipmentDiagnostics" {
        & .\scripts\dev\verify-branch-readiness.ps1 -BaseRef $BaseRef -Scope EquipmentDiagnostics
    }
}

if ($SkipReleaseEvidence) {
    Skip-Check "ED-22A release evidence pack" "prepare-telegram-closed-beta-release-evidence.ps1 -BaseRef <base-ref> -SkipFrontend"
} else {
    Invoke-Check "ED-22A release evidence pack" "prepare-telegram-closed-beta-release-evidence.ps1 -BaseRef <base-ref> -SkipFrontend" {
        & .\scripts\equipment-diagnostics\prepare-telegram-closed-beta-release-evidence.ps1 -BaseRef $BaseRef -SkipFrontend
    }
}

if ($SkipDeploymentDryRun) {
    Skip-Check "ED-22C deployment dry-run" "prepare-telegram-closed-beta-deployment-dry-run.ps1 -BaseRef <base-ref> -SkipDockerComposeConfig"
} else {
    Invoke-Check "ED-22C deployment dry-run" "prepare-telegram-closed-beta-deployment-dry-run.ps1 -BaseRef <base-ref> -SkipDockerComposeConfig" {
        & .\scripts\equipment-diagnostics\prepare-telegram-closed-beta-deployment-dry-run.ps1 -BaseRef $BaseRef -SkipDockerComposeConfig
    }
}

if ($SkipActivationChecklist) {
    Skip-Check "ED-22D activation checklist" "prepare-telegram-closed-beta-activation-checklist.ps1 -BaseRef <base-ref>"
} else {
    Invoke-Check "ED-22D activation checklist" "prepare-telegram-closed-beta-activation-checklist.ps1 -BaseRef <base-ref>" {
        & .\scripts\equipment-diagnostics\prepare-telegram-closed-beta-activation-checklist.ps1 -BaseRef $BaseRef
    }
}

$targetedBackendTests = @(
    "EquipmentDiagnosticsTelegramActivationRunbookTests",
    "EquipmentDiagnosticsTelegramDeploymentDryRunTests",
    "EquipmentDiagnosticsTelegramReleaseCandidateDocsTests",
    "EquipmentDiagnosticsTelegramReleaseEvidenceTests"
)
foreach ($filter in $targetedBackendTests) {
    if ($SkipBackendTests) {
        Skip-Check "Targeted backend test: $filter" "dotnet test AssistantEngineer.sln --filter $filter"
    } else {
        Invoke-Check "Targeted backend test: $filter" "dotnet test AssistantEngineer.sln --filter $filter" {
            dotnet test AssistantEngineer.sln --filter $filter
        }
    }
}

if ($SkipGoalRunReportValidation) {
    Skip-Check "Goal-run report validator tests" "dotnet test AssistantEngineer.sln --filter EquipmentDiagnosticsGoalRunReportValidatorTests"
} else {
    Invoke-Check "Goal-run report validator tests" "dotnet test AssistantEngineer.sln --filter EquipmentDiagnosticsGoalRunReportValidatorTests" {
        dotnet test AssistantEngineer.sln --filter EquipmentDiagnosticsGoalRunReportValidatorTests
    }
}

if ($SkipEquipmentDiagnosticsTests) {
    Skip-Check "EquipmentDiagnostics test filter" "dotnet test AssistantEngineer.sln --filter EquipmentDiagnostics --no-build"
} else {
    Invoke-Check "EquipmentDiagnostics test filter" "dotnet test AssistantEngineer.sln --filter EquipmentDiagnostics --no-build" {
        dotnet test AssistantEngineer.sln --filter EquipmentDiagnostics --no-build
    }
}

$branch = Invoke-GitText -Arguments @("branch", "--show-current") -Fallback ""
if ([string]::IsNullOrWhiteSpace($branch)) { $branch = Invoke-GitText -Arguments @("rev-parse", "--abbrev-ref", "HEAD") -Fallback "" }
if ([string]::IsNullOrWhiteSpace($branch) -or $branch -eq "HEAD") { $branch = $env:GITHUB_HEAD_REF }
if ([string]::IsNullOrWhiteSpace($branch)) { $branch = $env:GITHUB_REF_NAME }
if ([string]::IsNullOrWhiteSpace($branch)) { $branch = "detached" }
$head = Invoke-GitText -Arguments @("rev-parse", "HEAD") -Fallback "unknown"
$resolvedBase = Invoke-GitText -Arguments @("rev-parse", $BaseRef) -Fallback $BaseRef

$relativeSummaryPath = Convert-ToRepoRelativePath $summaryPath
$relativeReportPath = Convert-ToRepoRelativePath $reportPath
$decision = if ($blockers.Count -gt 0) {
    "NO_GO_BLOCKED"
} elseif ($warnings.Count -gt 0) {
    "MANUAL_REVIEW_REQUIRED"
} else {
    "GO_FOR_MANUAL_ACTIVATION"
}
$manualReviewRequired = @(
    "A named operator and reviewer must approve the final decision before any real activation.",
    "Review all generated evidence sources, warnings, access policy, smoke plan, and rollback ownership.",
    "GO_WITH_WARNINGS_REVIEWED may be recorded only in the final handoff after every warning is explicitly accepted."
)
$evidenceSources = @(
    "ED-22A release evidence pack",
    "ED-22C deployment dry-run",
    "ED-22D activation checklist",
    "EquipmentDiagnostics branch readiness",
    "Focused backend and EquipmentDiagnostics tests"
)
$limitations = @(
    "Closed beta only; not for production or public launch.",
    "No Telegram network call or webhook operation is executed.",
    "No real secret, domain, chat ID, deploy environment value, raw log, PDF, or manual file is collected.",
    "Telegram and chat ID discovery remain disabled by default.",
    "Runtime catalog remains the only final-answer source; manual-codebook, staging, and preview are not final diagnosis.",
    "Polling is disabled by default; no DB/audit persistence, external monitoring, AI, RAG, or vector search is added."
)
$outOfScope = @(
    "Real activation or deployment",
    "Telegram API calls",
    "setWebhook, getWebhookInfo, or deleteWebhook execution",
    "Calculation physics or public API route changes",
    "Public launch or complete vendor-manual claims"
)
$nextStep = if ($blockers.Count -gt 0) {
    "Resolve blockers before activation."
} elseif ($warnings.Count -gt 0) {
    "Complete manual warning review, then proceed to ED-22F release tag/handoff or manual activation planning."
} else {
    "Proceed to ED-22F release tag/handoff or manual activation planning."
}

$report = [ordered]@{
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    goalId = "ED-22E"
    title = "Telegram Closed Beta Final Go/No-Go Evidence"
    baseRef = $BaseRef
    resolvedBase = $resolvedBase
    branch = $branch
    head = $head
    status = $(if ($blockers.Count -eq 0) { "PASS" } else { "FAIL" })
    decision = $decision
    blockers = @($blockers)
    warnings = @($warnings)
    checks = @($checks)
    evidenceSources = $evidenceSources
    generatedArtifacts = @($relativeSummaryPath, $relativeReportPath)
    limitations = $limitations
    manualReviewRequired = $manualReviewRequired
    nextStep = $nextStep
    outOfScope = $outOfScope
}
$report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $reportPath -Encoding utf8

$checkLines = @($checks | ForEach-Object {
    $marker = if ($_.status -eq "pass") { "x" } else { " " }
    "- [$marker] $($_.name): $($_.status) - $($_.evidence)"
})
$summary = @(
    "# Telegram Closed Beta Final Go/No-Go: $($report.status)",
    "",
    "- Decision: **$decision**",
    "- Goal: ED-22E",
    "- Base reference: $BaseRef",
    "- Branch: $branch",
    "- Head: $head",
    "- Blockers: $($blockers.Count)",
    "- Warnings: $($warnings.Count)",
    "",
    "## Checks",
    "",
    $checkLines,
    "",
    "## Generated Artifacts",
    "",
    "- $relativeSummaryPath",
    "- $relativeReportPath",
    "",
    "## Manual Review Points",
    "",
    ($manualReviewRequired | ForEach-Object { "- $_" }),
    "",
    "## Next Step",
    "",
    $nextStep
)
$summary -join [Environment]::NewLine | Set-Content -LiteralPath $summaryPath -Encoding utf8

Write-Host "Status: $($report.status)"
Write-Host "Decision: $decision"
Write-Host "Blockers: $($blockers.Count)"
Write-Host "Warnings: $($warnings.Count)"
Write-Host "Report: $relativeReportPath"
Write-Host "Summary: $relativeSummaryPath"
if ($blockers.Count -gt 0) { exit 1 }
exit 0
