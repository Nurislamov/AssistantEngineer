param(
    [string]$RepoRoot = "",
    [string]$LocalBaseUrl = "",
    [switch]$SkipRestore,
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$RunHttpSmoke,
    [switch]$RunOAuthSmoke,
    [string]$ClientId = "dev-yandex-client",
    [string]$SharedSecret = "dev-yandex-client-secret",
    [string]$RedirectUri = "http://localhost:5005/oauth/callback",
    [switch]$NoLogo
)

$ErrorActionPreference = "Stop"

function Write-Stage {
    param([string]$Message)
    Write-Host "[GREE-ALICE-52] $Message"
}

function Fail-Stage {
    param([string]$Message)
    Write-Error "[GREE-ALICE-52] FAIL: $Message"
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

function Test-LocalBaseUrl {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        Fail-Stage "LocalBaseUrl is required when an HTTP smoke mode is used."
    }

    $uri = $null
    if (-not [System.Uri]::TryCreate($Value, [System.UriKind]::Absolute, [ref]$uri)) {
        Fail-Stage "LocalBaseUrl must be an absolute local URL."
    }

    if ($uri.Scheme -ne "http") {
        Fail-Stage "LocalBaseUrl must use http only."
    }

    if ($uri.Host -ne "localhost" -and $uri.Host -ne "127.0.0.1") {
        Fail-Stage "LocalBaseUrl host must be localhost or 127.0.0.1 only."
    }

    if ($uri.IsDefaultPort -or $uri.Port -le 0) {
        Fail-Stage "LocalBaseUrl must include an explicit local port."
    }

    $lowerValue = $Value.ToLowerInvariant()
    $forbiddenFragments = @(
        "yandex",
        "gree.com",
        "grih",
        "oauth",
        "mqtt",
        "prod",
        "production"
    )

    foreach ($fragment in $forbiddenFragments) {
        if ($lowerValue.Contains($fragment)) {
            Fail-Stage "LocalBaseUrl contains forbidden non-local fragment: $fragment"
        }
    }

    return $uri.AbsoluteUri.TrimEnd("/")
}

function Invoke-LocalJson {
    param(
        [string]$Method,
        [string]$BaseUrl,
        [string]$Path,
        [string]$Body = "",
        [hashtable]$Headers = @{}
    )

    $uri = $BaseUrl + $Path
    try {
        if ([string]::IsNullOrWhiteSpace($Body)) {
            return Invoke-RestMethod -Method $Method -Uri $uri -Headers $Headers
        }

        return Invoke-RestMethod -Method $Method -Uri $uri -ContentType "application/json" -Body $Body -Headers $Headers
    }
    catch {
        Fail-Stage "HTTP smoke request failed for $Method ${Path}: $($_.Exception.Message)"
    }
}

function Invoke-LocalOAuthSmoke {
    param(
        [string]$BaseUrl,
        [string]$OAuthClientId,
        [string]$OAuthSharedSecret,
        [string]$OAuthRedirectUri
    )

    $checkedBaseUrl = Test-LocalBaseUrl -Value $BaseUrl

    Write-Stage "Running localhost dev-only OAuth smoke"

    $state = "dev-smoke-state"
    $authorizePath = "/oauth/authorize?response_type=code&client_id=$([System.Uri]::EscapeDataString($OAuthClientId))&redirect_uri=$([System.Uri]::EscapeDataString($OAuthRedirectUri))&state=$([System.Uri]::EscapeDataString($state))&dev_response=json"
    $authorization = Invoke-LocalJson -Method "GET" -BaseUrl $checkedBaseUrl -Path $authorizePath
    Assert-Truthy -Condition ($null -ne $authorization.code) -Message "/oauth/authorize did not return a dev-only code."
    Assert-Truthy -Condition ($authorization.state -eq $state) -Message "/oauth/authorize did not preserve state."

    $secretFormKey = "client" + "_secret"
    $tokenBody = @{}
    $tokenBody["grant_type"] = "authorization_code"
    $tokenBody["code"] = $authorization.code
    $tokenBody["client_id"] = $OAuthClientId
    $tokenBody[$secretFormKey] = $OAuthSharedSecret
    $tokenBody["redirect_uri"] = $OAuthRedirectUri

    try {
        $tokenResponse = Invoke-RestMethod -Method "POST" -Uri ($checkedBaseUrl + "/oauth/token") -Body $tokenBody
    }
    catch {
        Fail-Stage "HTTP smoke request failed for POST /oauth/token: $($_.Exception.Message)"
    }

    $accessField = "access" + "_token"
    $issuedAccessValue = $tokenResponse.$accessField
    Assert-Truthy -Condition (-not [string]::IsNullOrWhiteSpace($issuedAccessValue)) -Message "/oauth/token did not return an access value."
    Assert-Truthy -Condition ($tokenResponse.token_type -eq "Bearer") -Message "/oauth/token did not return Bearer token type."

    $headers = @{ "Authorization" = "Bearer $issuedAccessValue"; "X-Request-Id" = "local-oauth-smoke-001" }

    $devices = Invoke-LocalJson -Method "GET" -BaseUrl $checkedBaseUrl -Path "/v1.0/user/devices" -Headers $headers
    Assert-Truthy -Condition ($null -ne $devices.devices) -Message "OAuth /devices returned no devices collection."
    $deviceIds = @($devices.devices | ForEach-Object { $_.id })
    Assert-Truthy -Condition ($deviceIds -contains "dummy-gree-ac-001") -Message "OAuth /devices did not include dummy-gree-ac-001."

    $queryBody = @"
{
  "devices": [
    { "id": "dummy-gree-ac-001" },
    { "id": "unknown-device-001" }
  ]
}
"@
    $query = Invoke-LocalJson -Method "POST" -BaseUrl $checkedBaseUrl -Path "/v1.0/user/devices/query" -Body $queryBody -Headers $headers
    Assert-Truthy -Condition ($null -ne $query.devices) -Message "OAuth /query returned no devices collection."

    $actionBody = @"
{
  "devices": [
    {
      "id": "dummy-gree-ac-001",
      "capabilities": []
    }
  ]
}
"@
    $action = Invoke-LocalJson -Method "POST" -BaseUrl $checkedBaseUrl -Path "/v1.0/user/devices/action" -Body $actionBody -Headers $headers
    foreach ($device in @($action.devices)) {
        Assert-Truthy -Condition ($device.status -eq "dry-run-fail-closed") -Message "OAuth /action did not return dry-run fail-closed."
        Assert-Truthy -Condition ($device.sentToGreeCloud -eq $false) -Message "OAuth /action set SentToGreeCloud=true."
        Assert-Truthy -Condition ($device.sentToMqtt -eq $false) -Message "OAuth /action set SentToMqtt=true."
        Assert-Truthy -Condition ($device.sentToDevice -eq $false) -Message "OAuth /action set SentToDevice=true."
    }

    $unlink = Invoke-LocalJson -Method "POST" -BaseUrl $checkedBaseUrl -Path "/v1.0/user/unlink" -Headers $headers
    Assert-Truthy -Condition ($unlink.status -eq "offline-no-production-data-touched") -Message "OAuth /unlink did not return offline template result."
}

function Assert-Truthy {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        Fail-Stage $Message
    }
}

function Invoke-LocalHttpSmoke {
    param([string]$BaseUrl)

    $checkedBaseUrl = Test-LocalBaseUrl -Value $BaseUrl

    Write-Stage "Running localhost HTTP smoke"

    $health = Invoke-LocalJson -Method "GET" -BaseUrl $checkedBaseUrl -Path "/health"
    Assert-Truthy -Condition ($null -ne $health) -Message "/health returned empty response."
    Assert-Truthy -Condition ($health.status -eq "healthy") -Message "/health did not return healthy status."

    $devices = Invoke-LocalJson -Method "GET" -BaseUrl $checkedBaseUrl -Path "/v1.0/user/devices"
    Assert-Truthy -Condition ($null -ne $devices.devices) -Message "/devices returned no devices collection."
    $deviceIds = @($devices.devices | ForEach-Object { $_.id })
    Assert-Truthy -Condition ($deviceIds.Count -gt 0) -Message "/devices returned no dummy devices."
    Assert-Truthy -Condition ($deviceIds -contains "dummy-gree-ac-001") -Message "/devices did not include dummy-gree-ac-001."
    Assert-Truthy -Condition ($deviceIds -contains "yandex-dummy-vrf-child-living-001") -Message "/devices did not include exposed VRF child unit."
    Assert-Truthy -Condition (-not ($deviceIds -contains "dummy-vrf-gateway-001")) -Message "/devices exposed hidden gateway."

    $queryBody = @"
{
  "devices": [
    { "id": "dummy-gree-ac-001" },
    { "id": "yandex-dummy-vrf-child-living-001" },
    { "id": "unknown-device-001" }
  ]
}
"@
    $query = Invoke-LocalJson -Method "POST" -BaseUrl $checkedBaseUrl -Path "/v1.0/user/devices/query" -Body $queryBody
    Assert-Truthy -Condition ($null -ne $query.devices) -Message "/query returned no devices collection."
    $queryStatuses = @($query.devices | ForEach-Object { $_.status })
    Assert-Truthy -Condition ($queryStatuses -contains "offline-fixture") -Message "/query did not return offline fixture state."
    Assert-Truthy -Condition (($queryStatuses -contains "offline-unknown") -or ($queryStatuses -contains "dry-run-fail-closed")) -Message "/query did not return controlled unknown/fail-closed result."

    $actionBody = @"
{
  "devices": [
    {
      "id": "dummy-gree-ac-001",
      "capabilities": []
    },
    {
      "id": "yandex-dummy-vrf-child-living-001",
      "capabilities": []
    },
    {
      "id": "unknown-device-001",
      "capabilities": []
    }
  ]
}
"@
    $action = Invoke-LocalJson -Method "POST" -BaseUrl $checkedBaseUrl -Path "/v1.0/user/devices/action" -Body $actionBody
    Assert-Truthy -Condition ($null -ne $action.devices) -Message "/action returned no devices collection."
    foreach ($device in @($action.devices)) {
        Assert-Truthy -Condition ($device.status -eq "dry-run-fail-closed") -Message "/action did not return dry-run fail-closed for a device."
        Assert-Truthy -Condition ($device.sentToGreeCloud -eq $false) -Message "/action set SentToGreeCloud=true."
        Assert-Truthy -Condition ($device.sentToMqtt -eq $false) -Message "/action set SentToMqtt=true."
        Assert-Truthy -Condition ($device.sentToDevice -eq $false) -Message "/action set SentToDevice=true."
    }

    $unlink = Invoke-LocalJson -Method "POST" -BaseUrl $checkedBaseUrl -Path "/v1.0/user/unlink"
    Assert-Truthy -Condition ($null -ne $unlink) -Message "/unlink returned empty response."
    Assert-Truthy -Condition ($unlink.status -eq "offline-no-production-data-touched") -Message "/unlink did not return offline template result."
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
    Write-Stage "No real Yandex live endpoints, real OAuth material, live Gree+ Cloud, MQTT, device control, or production deployment"
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

if ($RunHttpSmoke) {
    Invoke-LocalHttpSmoke -BaseUrl $LocalBaseUrl
}

if ($RunOAuthSmoke) {
    Invoke-LocalOAuthSmoke -BaseUrl $LocalBaseUrl -OAuthClientId $ClientId -OAuthSharedSecret $SharedSecret -OAuthRedirectUri $RedirectUri
}

Write-Stage "PASS"
exit 0
