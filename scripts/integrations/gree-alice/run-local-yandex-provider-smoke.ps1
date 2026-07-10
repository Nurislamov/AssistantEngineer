param(
    [string]$RepoRoot = "",
    [switch]$SkipRestore,
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$NoLogo
)

$ErrorActionPreference = "Stop"

function Write-Stage {
    param([string]$Message)
    Write-Host "[GREE-ALICE-51] $Message"
}

function Fail-Stage {
    param([string]$Message)
    Write-Error "[GREE-ALICE-51] FAIL: $Message"
    exit 1
}

function Invoke-Checked {
    param(
        [string]$DisplayName,
        [string]$FileName,
        [string[]]$Arguments
    )

    Write-Stage $DisplayName
    & $FileName @Arguments
    if ($LASTEXITCODE -ne 0) {
        Fail-Stage "$DisplayName failed."
    }
}

function Resolve-RepositoryRoot {
    param([string]$Candidate)

    if (-not [string]::IsNullOrWhiteSpace($Candidate)) {
        return (Resolve-Path -LiteralPath $Candidate).Path
    }

    $current = (Get-Location).Path
    while ($null -ne $current -and $current.Length -gt 0) {
        if ((Test-Path -LiteralPath (Join-Path $current "AssistantEngineer.sln")) -and
            (Test-Path -LiteralPath (Join-Path $current ".git"))) {
            return $current
        }

        $parent = Split-Path -Parent $current
        if ($parent -eq $current) {
            break
        }

        $current = $parent
    }

    Fail-Stage "AssistantEngineer.sln not found."
}

function Get-TextFiles {
    param([string[]]$Roots)

    $extensions = @(".cs", ".csproj", ".props", ".targets", ".json", ".md", ".ps1", ".psm1", ".txt")
    foreach ($root in $Roots) {
        if (-not (Test-Path -LiteralPath $root)) {
            continue
        }

        Get-ChildItem -LiteralPath $root -File -Recurse |
            Where-Object { $extensions -contains $_.Extension.ToLowerInvariant() }
    }
}

function Convert-ToRelativePath {
    param(
        [string]$Root,
        [string]$Path
    )

    $rootFull = (Resolve-Path -LiteralPath $Root).Path.TrimEnd('\', '/')
    $pathFull = (Resolve-Path -LiteralPath $Path).Path

    if ($pathFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $pathFull.Substring($rootFull.Length).TrimStart('\', '/')
    }

    return $pathFull
}

function Assert-NoForbiddenText {
    param([string]$Root)

    Write-Stage "Running static safety scans"

    $docsRoot = Join-Path $Root "docs\integrations\gree-alice"
    $scriptRoot = Join-Path $Root "scripts\integrations\gree-alice"
    $sourceRoot = Join-Path $Root "src\Integrations\GreeAliceBridge"

    $assignmentNames = @(
        ("client_" + "secret"),
        ("access_" + "token"),
        ("refresh_" + "token"),
        "password",
        "token"
    )
    $assignmentPattern = "(?i)\b(" + (($assignmentNames | ForEach-Object { [regex]::Escape($_) }) -join "|") + ")\s*=\s*\S+"
    $realUrlPattern = "(?i)https?://(?!localhost\b|127\.0\.0\.1\b|\[::1\])\S+"

    foreach ($file in Get-TextFiles -Roots @($docsRoot, $scriptRoot, $sourceRoot)) {
        $relative = Convert-ToRelativePath -Root $Root -Path $file.FullName
        $content = Get-Content -LiteralPath $file.FullName -Raw

        if ($content -match $assignmentPattern) {
            Fail-Stage "Forbidden assignment-like secret pattern found: $relative"
        }

        $isLocalBridgeDoc = $file.Name.StartsWith("local-bridge-", [System.StringComparison]::OrdinalIgnoreCase) -or
            $file.Name.Equals("README.md", [System.StringComparison]::OrdinalIgnoreCase)

        if ($file.FullName.StartsWith($docsRoot, [System.StringComparison]::OrdinalIgnoreCase) -and
            $isLocalBridgeDoc -and
            $content -match $realUrlPattern) {
            Fail-Stage "Forbidden real URL pattern found: $relative"
        }
    }

    $strictForbiddenPatterns = @(
        (("mqtt") + "\.connect"),
        (("mqtt") + "\.subscribe"),
        (("mqtt") + "\.publish"),
        (("mos") + "quitto"),
        (("api") + "\.iot\.yandex"),
        (("grih") + "\.gree\.com"),
        (("gree") + "\.com/oauth")
    )

    foreach ($file in Get-TextFiles -Roots @($sourceRoot, $scriptRoot)) {
        $relative = Convert-ToRelativePath -Root $Root -Path $file.FullName
        $content = Get-Content -LiteralPath $file.FullName -Raw

        foreach ($pattern in $strictForbiddenPatterns) {
            if ($content -match $pattern) {
                Fail-Stage "Forbidden live endpoint pattern found: $relative"
            }
        }
    }
}

$resolvedRoot = Resolve-RepositoryRoot -Candidate $RepoRoot

if (-not (Test-Path -LiteralPath (Join-Path $resolvedRoot ".git"))) {
    Fail-Stage ".git not found."
}

if (-not (Test-Path -LiteralPath (Join-Path $resolvedRoot "AssistantEngineer.sln"))) {
    Fail-Stage "AssistantEngineer.sln not found."
}

if ((Split-Path -Leaf $resolvedRoot) -ne "AssistantEngineer") {
    Fail-Stage "Script must be run inside the AssistantEngineer repository."
}

Set-Location -LiteralPath $resolvedRoot

if (-not $NoLogo) {
    Write-Stage "Using repository root: $resolvedRoot"
    Write-Stage "Safety boundary: offline/local only"
    Write-Stage "No real Yandex, OAuth, credentials, live Gree+ Cloud, MQTT, device control, or production deployment"
}

Write-Stage "Checking worktree"
& git status --short
if ($LASTEXITCODE -ne 0) {
    Fail-Stage "git status failed."
}

if (-not $SkipRestore) {
    Invoke-Checked -DisplayName "Running restore" -FileName "dotnet" -Arguments @("restore", ".\AssistantEngineer.sln")
}

if (-not $SkipBuild) {
    Invoke-Checked -DisplayName "Running build" -FileName "dotnet" -Arguments @("build", ".\AssistantEngineer.sln", "--no-restore")
}

if (-not $SkipTests) {
    Invoke-Checked -DisplayName "Running GreeAlice tests" -FileName "dotnet" -Arguments @("test", ".\AssistantEngineer.sln", "--no-build", "--filter", "FullyQualifiedName~GreeAlice")
}

Invoke-Checked -DisplayName "Running smoke harness tests" -FileName "dotnet" -Arguments @("test", ".\AssistantEngineer.sln", "--no-build", "--filter", "FullyQualifiedName~GreeAliceLocalYandexProviderSmokeHarnessTests")
Invoke-Checked -DisplayName "Running git diff --check" -FileName "git" -Arguments @("diff", "--check")
Assert-NoForbiddenText -Root $resolvedRoot

Write-Stage "PASS"
exit 0
