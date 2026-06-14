param(
    [string]$BaseRef = "origin/master",
    [string]$OutputRoot = "artifacts/verification/equipment-diagnostics/telegram-closed-beta",
    [switch]$SkipRestore,
    [switch]$SkipBuild,
    [switch]$SkipBackendTests,
    [switch]$SkipFrontend,
    [switch]$SkipReadinessScripts,
    [switch]$SkipGoalRunValidation
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
$summaryPath = Join-Path $outputPath "release-evidence-summary.md"
$reportPath = Join-Path $outputPath "release-evidence-report.json"
$goalRunPath = Join-Path $outputPath "telegram-closed-beta-goal-run-report.json"

function Convert-ToRepoRelativePath([string]$Path) {
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not $fullPath.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Evidence path must remain under the repository root."
    }
    return $fullPath.Substring($repoRoot.Length).TrimStart("\", "/").Replace("\", "/")
}

$relativeSummaryPath = Convert-ToRepoRelativePath $summaryPath
$relativeReportPath = Convert-ToRepoRelativePath $reportPath
$relativeGoalRunPath = Convert-ToRepoRelativePath $goalRunPath

$branch = (& git branch --show-current).Trim()
$head = (& git rev-parse HEAD).Trim()
$checks = [System.Collections.Generic.List[object]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()
$blockers = [System.Collections.Generic.List[string]]::new()

function Add-Check([string]$Name, [string]$Command, [string]$Status, [string]$Evidence) {
    $checks.Add([ordered]@{
        name = $Name
        command = $Command
        status = $Status
        evidence = $Evidence
    })
}

function Invoke-Check([string]$Name, [string]$CommandText, [scriptblock]$Action) {
    Write-Host "RUN: $Name"
    & $Action
    if ($LASTEXITCODE -ne 0) {
        Add-Check $Name $CommandText "fail" "Command returned a non-zero exit code."
        $blockers.Add("$Name failed.")
        return
    }

    Add-Check $Name $CommandText "pass" "Command completed successfully."
}

function Skip-Check([string]$Name, [string]$CommandText) {
    Add-Check $Name $CommandText "not_run" "Skipped by explicit runner option."
    $warnings.Add("$Name was skipped.")
}

if ($SkipRestore) {
    Skip-Check "Restore" "dotnet restore AssistantEngineer.sln"
} else {
    Invoke-Check "Restore" "dotnet restore AssistantEngineer.sln" { dotnet restore AssistantEngineer.sln }
}

if ($SkipBuild) {
    Skip-Check "Build" "dotnet build AssistantEngineer.sln --no-restore"
} else {
    Invoke-Check "Build" "dotnet build AssistantEngineer.sln --no-restore" { dotnet build AssistantEngineer.sln --no-restore }
}

if ($SkipBackendTests) {
    Skip-Check "Goal-run validator tests" "dotnet test AssistantEngineer.sln --filter EquipmentDiagnosticsGoalRunReportValidatorTests"
    Skip-Check "Goal protocol tests" "dotnet test AssistantEngineer.sln --filter EquipmentDiagnosticsGoalProtocolTests"
    Skip-Check "Telegram deterministic tests" "dotnet test AssistantEngineer.sln --filter EquipmentDiagnosticTelegram"
} else {
    Invoke-Check "Goal-run validator tests" "dotnet test AssistantEngineer.sln --filter EquipmentDiagnosticsGoalRunReportValidatorTests --no-build" {
        dotnet test AssistantEngineer.sln --filter EquipmentDiagnosticsGoalRunReportValidatorTests --no-build
    }
    Invoke-Check "Goal protocol tests" "dotnet test AssistantEngineer.sln --filter EquipmentDiagnosticsGoalProtocolTests --no-build" {
        dotnet test AssistantEngineer.sln --filter EquipmentDiagnosticsGoalProtocolTests --no-build
    }
    Invoke-Check "Telegram deterministic tests" "dotnet test AssistantEngineer.sln --filter EquipmentDiagnosticTelegram --no-build" {
        dotnet test AssistantEngineer.sln --filter EquipmentDiagnosticTelegram --no-build
    }
}

if ($SkipFrontend) {
    Skip-Check "Frontend tests" "npm test"
} else {
    Invoke-Check "Frontend tests" "npm test" {
        Push-Location (Join-Path $repoRoot "src\Frontend")
        try { npm test } finally { Pop-Location }
    }
}

if ($SkipReadinessScripts) {
    Skip-Check "Beta readiness evidence" "scripts/equipment-diagnostics/prepare-beta-readiness-report.ps1"
} else {
    Invoke-Check "Beta readiness evidence" "scripts/equipment-diagnostics/prepare-beta-readiness-report.ps1 -BaseRef <base-ref>" {
        & .\scripts\equipment-diagnostics\prepare-beta-readiness-report.ps1 -BaseRef $BaseRef
    }
}

$phases = @(
    [ordered]@{ number = 1; title = "Repository baseline"; status = "pass"; deliverables = @("Branch, head, and base reference captured."); acceptanceCriteria = @("Repository identity is available without secret values."); mandatoryCommands = @("git branch --show-current", "git rev-parse HEAD"); evidence = @("Branch and head metadata captured.") },
    [ordered]@{ number = 2; title = "Build and backend validation"; status = $(if ($checks | Where-Object { $_.name -in @("Restore", "Build", "Goal-run validator tests", "Goal protocol tests") -and $_.status -eq "fail" }) { "fail" } elseif ($checks | Where-Object { $_.name -in @("Restore", "Build", "Goal-run validator tests", "Goal protocol tests") -and $_.status -eq "not_run" }) { "not_run" } else { "pass" }); deliverables = @("Build and focused backend validation evidence."); acceptanceCriteria = @("Required deterministic checks do not fail."); mandatoryCommands = @("dotnet restore", "dotnet build", "focused dotnet test"); evidence = @("See preflight command evidence.") },
    [ordered]@{ number = 3; title = "Telegram deterministic tests"; status = $(if ($checks | Where-Object { $_.name -eq "Telegram deterministic tests" -and $_.status -eq "fail" }) { "fail" } elseif ($checks | Where-Object { $_.name -eq "Telegram deterministic tests" -and $_.status -eq "not_run" }) { "not_run" } else { "pass" }); deliverables = @("Telegram deterministic test evidence."); acceptanceCriteria = @("Telegram deterministic tests do not fail."); mandatoryCommands = @("dotnet test --filter EquipmentDiagnosticTelegram"); evidence = @("See preflight command evidence.") },
    [ordered]@{ number = 4; title = "Beta readiness evidence"; status = $(if ($checks | Where-Object { $_.name -eq "Beta readiness evidence" -and $_.status -eq "fail" }) { "fail" } elseif ($checks | Where-Object { $_.name -eq "Beta readiness evidence" -and $_.status -eq "not_run" }) { "not_run" } else { "pass" }); deliverables = @("Existing beta readiness report invocation evidence."); acceptanceCriteria = @("Beta readiness evidence does not fail."); mandatoryCommands = @("prepare-beta-readiness-report.ps1"); evidence = @("See preflight command evidence.") },
    [ordered]@{ number = 5; title = "Goal-run-report validation"; status = "not_run"; deliverables = @("ED-21B-compatible goal-run report."); acceptanceCriteria = @("Goal-run validator returns PASS."); mandatoryCommands = @("goal-run-report --input <generated-report>"); evidence = @() },
    [ordered]@{ number = 6; title = "Generated artifact review"; status = "pass"; deliverables = @("Safe local evidence paths."); acceptanceCriteria = @("Evidence remains under artifacts/verification."); mandatoryCommands = @("Output-root boundary check"); evidence = @("Output root boundary check passed.") }
)

$goalRun = [ordered]@{
    goalId = "ED-22A"
    title = "Telegram Closed Beta Release Evidence Pack"
    sourceBranch = $branch
    targetBranch = $BaseRef
    scope = @("Local deterministic Telegram closed-beta release evidence.")
    outOfScope = @("Telegram activation", "Real deployment", "Production or public release", "Network calls", "Runtime feature changes")
    constraints = @("No real secrets or domains.", "Telegram and chat ID discovery remain disabled by default.", "No raw logs or chat IDs.", "Runtime catalog remains the only final-answer source.")
    preflight = [ordered]@{ commands = @($checks) }
    phases = $phases
    finalAudit = [ordered]@{
        status = "not_run"
        roadmapCoverage = "ED-22A evidence-pack roadmap reviewed."
        phaseCompletion = "Goal-run validation is pending."
        changedFilesReview = "Runner does not modify runtime product files."
        forbiddenFilesReview = "Generated outputs remain under artifacts/verification."
        forbiddenClaimsReview = "No unsupported release or autonomy claims are made."
        secretsScan = "No deploy environment values, chat IDs, raw logs, or real credentials are included."
        generatedArtifacts = "Only the declared local evidence files are generated."
        mergeReadiness = "Pending goal-run validation and human review."
    }
    warnings = @($warnings)
    blockers = @($blockers)
    generatedArtifacts = @($relativeSummaryPath, $relativeReportPath, $relativeGoalRunPath)
}

$goalRun | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $goalRunPath -Encoding utf8

if ($SkipGoalRunValidation) {
    $warnings.Add("Goal-run report validation was skipped.")
} else {
    & dotnet run --project tools/AssistantEngineer.Tools.EquipmentDiagnosticsVerification -- goal-run-report --repo-root . --input $relativeGoalRunPath
    if ($LASTEXITCODE -ne 0) {
        $blockers.Add("Goal-run report validation failed.")
    } else {
        $goalRun.phases[4].status = "pass"
        $goalRun.phases[4].evidence = @("ED-21B goal-run report validator returned PASS.")
        $goalRun.finalAudit.status = "pass"
        $goalRun.finalAudit.phaseCompletion = "All non-skipped phases passed; skipped checks remain warnings."
        $goalRun.finalAudit.mergeReadiness = "Evidence pack is ready for required human review."
        $goalRun.warnings = @($warnings)
        $goalRun.blockers = @($blockers)
        $goalRun | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $goalRunPath -Encoding utf8
        & dotnet run --project tools/AssistantEngineer.Tools.EquipmentDiagnosticsVerification -- goal-run-report --repo-root . --input $relativeGoalRunPath
        if ($LASTEXITCODE -ne 0) { $blockers.Add("Final goal-run report validation failed.") }
    }
}

$report = [ordered]@{
    goalId = "ED-22A"
    title = "Telegram Closed Beta Release Evidence Pack"
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    sourceBranch = $branch
    targetBranch = $BaseRef
    head = $head
    status = $(if ($blockers.Count -eq 0) { "PASS" } else { "FAIL" })
    blockerCount = $blockers.Count
    warningCount = $warnings.Count
    checks = @($checks)
    warnings = @($warnings)
    blockers = @($blockers)
    generatedArtifacts = @($relativeSummaryPath, $relativeReportPath, $relativeGoalRunPath)
}
$report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $reportPath -Encoding utf8

$summary = @(
    "# Telegram Closed Beta Release Evidence",
    "",
    "- Status: **$($report.status)**",
    "- Goal: ED-22A",
    "- Branch: $branch",
    "- Target/base: $BaseRef",
    "- Blockers: $($blockers.Count)",
    "- Warnings: $($warnings.Count)",
    "",
    "## Review Boundary",
    "",
    "- Closed beta evidence only; not activation, deployment, production, or public release.",
    "- No Telegram network call, Docker requirement, real credential, domain, chat ID, or raw log is included.",
    "- Runtime catalog is the only final-answer source.",
    "",
    "## Generated Artifacts",
    "",
    "- $relativeSummaryPath",
    "- $relativeReportPath",
    "- $relativeGoalRunPath"
)
$summary -join [Environment]::NewLine | Set-Content -LiteralPath $summaryPath -Encoding utf8

Write-Host "Status: $($report.status)"
Write-Host "Blockers: $($blockers.Count)"
Write-Host "Warnings: $($warnings.Count)"
Write-Host "Report: $relativeReportPath"
Write-Host "Summary: $relativeSummaryPath"
Write-Host "Goal run: $relativeGoalRunPath"
if ($blockers.Count -gt 0) { exit 1 }
exit 0
