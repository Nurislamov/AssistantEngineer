param(
    [string]$BaseRef = "origin/master",
    [string]$OutputRoot = "artifacts/verification/equipment-diagnostics/telegram-release-tag-handoff",
    [string]$ReleaseTag = "equipment-diagnostics-telegram-closed-beta-v0.1.0",
    [switch]$SkipFinalGoNoGoReference,
    [switch]$SkipBranchReadiness,
    [switch]$SkipBackendTests,
    [switch]$SkipEquipmentDiagnosticsTests
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
$summaryPath = Join-Path $outputPath "release-tag-handoff-summary.md"
$reportPath = Join-Path $outputPath "release-tag-handoff-report.json"
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
    Add-Check $Name $CommandText "not_run" "Skipped by explicit release tag/handoff option."
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
    "docs/equipment-diagnostics/telegram-closed-beta-final-go-no-go.md",
    "docs/equipment-diagnostics/telegram-closed-beta-final-handoff-template.md",
    "docs/equipment-diagnostics/telegram-closed-beta-release-tag-and-handoff.md",
    "docs/equipment-diagnostics/telegram-closed-beta-release-notes-template.md",
    "docs/equipment-diagnostics/telegram-closed-beta-activation-runbook.md",
    "docs/equipment-diagnostics/telegram-closed-beta-smoke-evidence-template.md"
)
$requiredEvidenceScripts = @(
    "scripts/equipment-diagnostics/prepare-telegram-closed-beta-final-go-no-go.ps1",
    "scripts/equipment-diagnostics/prepare-telegram-closed-beta-activation-checklist.ps1",
    "scripts/equipment-diagnostics/prepare-telegram-closed-beta-deployment-dry-run.ps1",
    "scripts/equipment-diagnostics/prepare-telegram-closed-beta-release-evidence.ps1"
)
$manualActivationTools = @(
    "scripts/equipment-diagnostics/set-telegram-webhook.ps1",
    "scripts/equipment-diagnostics/get-telegram-webhook-info.ps1",
    "scripts/equipment-diagnostics/delete-telegram-webhook.ps1"
)
Test-RequiredFiles "Release tag and handoff documentation inventory" $requiredDocs
Test-RequiredFiles "ED-22 evidence script inventory" $requiredEvidenceScripts
Test-RequiredFiles "Manual activation tool inventory" $manualActivationTools

if ($ReleaseTag -match '^equipment-diagnostics-telegram-closed-beta-v\d+\.\d+\.\d+$') {
    Add-Check "Release tag format" "validate release tag format" "pass" "Release tag matches the closed-beta semantic-version naming policy."
} else {
    Add-Check "Release tag format" "validate release tag format" "fail" "Release tag does not match equipment-diagnostics-telegram-closed-beta-v<major>.<minor>.<patch>."
}

$branch = Invoke-GitText -Arguments @("branch", "--show-current") -Fallback ""
if ([string]::IsNullOrWhiteSpace($branch)) { $branch = Invoke-GitText -Arguments @("rev-parse", "--abbrev-ref", "HEAD") -Fallback "" }
if ([string]::IsNullOrWhiteSpace($branch) -or $branch -eq "HEAD") { $branch = $env:GITHUB_HEAD_REF }
if ([string]::IsNullOrWhiteSpace($branch)) { $branch = $env:GITHUB_REF_NAME }
if ([string]::IsNullOrWhiteSpace($branch)) { $branch = "detached" }
$head = Invoke-GitText -Arguments @("rev-parse", "HEAD") -Fallback "unknown"
$resolvedBase = Invoke-GitText -Arguments @("rev-parse", $BaseRef) -Fallback $BaseRef

if ($branch -ne "master") {
    $warnings.Add("Current branch is '$branch'; create and push the annotated tag only after merge into master.")
}
$warnings.Add("Full backend test evidence must be reviewed separately; this runner executes only the declared focused and EquipmentDiagnostics filters.")

$tagRef = "refs/tags/$ReleaseTag"
$tagCommit = Invoke-GitText -Arguments @("rev-list", "-n", "1", $tagRef) -Fallback ""
if ([string]::IsNullOrWhiteSpace($tagCommit)) {
    Add-Check "Local release tag collision" "git show-ref --verify <tag-ref>" "pass" "Release tag does not exist locally."
} elseif ($tagCommit -eq $head) {
    Add-Check "Local release tag collision" "git show-ref --verify <tag-ref>" "warning" "Release tag already exists at current HEAD."
    $warnings.Add("Release tag already exists at current HEAD and requires manual review.")
} else {
    Add-Check "Local release tag collision" "git show-ref --verify <tag-ref>" "fail" "Release tag already exists at a different commit."
}

if ($SkipBranchReadiness) {
    Skip-Check "Branch readiness" ".\scripts\dev\verify-branch-readiness.ps1 -BaseRef <base-ref> -Scope EquipmentDiagnostics"
} else {
    Invoke-Check "Branch readiness" ".\scripts\dev\verify-branch-readiness.ps1 -BaseRef <base-ref> -Scope EquipmentDiagnostics" {
        & .\scripts\dev\verify-branch-readiness.ps1 -BaseRef $BaseRef -Scope EquipmentDiagnostics
    }
}

if ($SkipFinalGoNoGoReference) {
    Skip-Check "ED-22E final go/no-go reference" "prepare-telegram-closed-beta-final-go-no-go.ps1 -BaseRef <base-ref> -SkipBackendTests -SkipEquipmentDiagnosticsTests"
} else {
    Invoke-Check "ED-22E final go/no-go reference" "prepare-telegram-closed-beta-final-go-no-go.ps1 -BaseRef <base-ref> -SkipBackendTests -SkipEquipmentDiagnosticsTests" {
        & .\scripts\equipment-diagnostics\prepare-telegram-closed-beta-final-go-no-go.ps1 -BaseRef $BaseRef -SkipBackendTests -SkipEquipmentDiagnosticsTests
    }
}

$targetedBackendTests = @(
    "EquipmentDiagnosticsTelegramFinalGoNoGoTests",
    "EquipmentDiagnosticsTelegramActivationRunbookTests"
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

if ($SkipEquipmentDiagnosticsTests) {
    Skip-Check "EquipmentDiagnostics test filter" "dotnet test AssistantEngineer.sln --filter EquipmentDiagnostics --no-build"
} else {
    Invoke-Check "EquipmentDiagnostics test filter" "dotnet test AssistantEngineer.sln --filter EquipmentDiagnostics --no-build" {
        dotnet test AssistantEngineer.sln --filter EquipmentDiagnostics --no-build
    }
}

$relativeSummaryPath = Convert-ToRepoRelativePath $summaryPath
$relativeReportPath = Convert-ToRepoRelativePath $reportPath
$decision = if ($blockers.Count -gt 0) {
    "NO_GO_BLOCKED"
} elseif ($warnings.Count -gt 0) {
    "READY_WITH_MANUAL_REVIEW"
} else {
    "READY_TO_TAG_AFTER_MASTER_MERGE"
}
$tagCommands = @(
    "git switch master",
    "git pull",
    "git status --short",
    "git rev-parse HEAD",
    "git tag -a $ReleaseTag -m `"EquipmentDiagnostics Telegram closed beta v0.1.0`"",
    "git push origin $ReleaseTag"
)
$manualReviewRequired = @(
    "Confirm the selected commit is the merged master commit covered by reviewed ED-22 evidence.",
    "Review every warning, final go/no-go decision, release notes placeholder, and evidence archive path.",
    "Create and push the annotated tag manually only after merge into master.",
    "Assign the ED-23A activation operator, approved window, and rollback owner separately."
)
$evidenceSources = @(
    "ED-22A release evidence pack",
    "ED-22C deployment dry-run",
    "ED-22D activation checklist",
    "ED-22E final go/no-go",
    "EquipmentDiagnostics branch readiness",
    "Focused backend and EquipmentDiagnostics tests; separately reviewed full backend test evidence"
)
$limitations = @(
    "Closed beta only; not for production or public launch.",
    "No Git tag is created or pushed automatically.",
    "No Telegram network call or webhook operation is executed.",
    "No real secret, domain, chat ID, deploy environment value, raw log, PDF, or manual file is collected.",
    "Telegram and chat ID discovery remain disabled by default.",
    "Runtime catalog remains the only final-answer source; manual-codebook, staging, and preview are not final diagnosis.",
    "No long polling, DB/audit persistence, external monitoring, AI, RAG, or vector search is added."
)
$outOfScope = @(
    "Automatic Git tag creation or push",
    "Real activation or deployment",
    "Telegram API calls",
    "setWebhook, getWebhookInfo, or deleteWebhook execution",
    "Runtime, calculation, API route, appsettings, or Docker Compose changes",
    "Public launch or complete vendor-manual claims"
)
$nextStep = if ($blockers.Count -gt 0) {
    "Resolve blockers before creating a release tag."
} else {
    "After merge into master and manual evidence review, create the annotated tag manually, then proceed to ED-23A real server activation planning."
}

$report = [ordered]@{
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    goalId = "ED-22F"
    title = "Telegram Closed Beta Release Tag and Handoff"
    baseRef = $BaseRef
    resolvedBase = $resolvedBase
    branch = $branch
    head = $head
    releaseTag = $ReleaseTag
    status = $(if ($blockers.Count -eq 0) { "PASS" } else { "FAIL" })
    decision = $decision
    blockers = @($blockers)
    warnings = @($warnings)
    checks = @($checks)
    evidenceSources = $evidenceSources
    generatedArtifacts = @($relativeSummaryPath, $relativeReportPath)
    limitations = $limitations
    manualReviewRequired = $manualReviewRequired
    tagCommands = $tagCommands
    nextStep = $nextStep
    outOfScope = $outOfScope
}
$report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $reportPath -Encoding utf8

$checkLines = @($checks | ForEach-Object {
    $marker = if ($_.status -eq "pass") { "x" } else { " " }
    "- [$marker] $($_.name): $($_.status) - $($_.evidence)"
})
$summary = @(
    "# Telegram Closed Beta Release Tag And Handoff: $($report.status)",
    "",
    "- Release tag: **$ReleaseTag**",
    "- Decision: **$decision**",
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
    "## Manual Tag Commands",
    "",
    ($tagCommands | ForEach-Object { "- ``$_``" }),
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
Write-Host "Release tag: $ReleaseTag"
Write-Host "Decision: $decision"
Write-Host "Blockers: $($blockers.Count)"
Write-Host "Warnings: $($warnings.Count)"
Write-Host "Report: $relativeReportPath"
Write-Host "Summary: $relativeSummaryPath"
if ($blockers.Count -gt 0) { exit 1 }
exit 0
