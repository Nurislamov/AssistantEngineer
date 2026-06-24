param(
    [string]$RepoRoot = "D:\Project\AssistantEngineer",

    # Comma/semicolon separated first symbols. Examples:
    # -FirstSymbols "C,U,L"
    # -FirstSymbols "A,C,E,H,L,U"
    [string]$FirstSymbols = "C,U,L",

    # Optional exact codes. If set, script skips first/second symbol expansion.
    # Example: -OnlyCodes "C0,U0,L1,E1,H5,A0,C7,o1,01"
    [string]$OnlyCodes = "",

    # Optional model filter for reporting only. searchModel still returns all models.
    # Example: -PreferredModels "GMV,GMV6,GMV Mini"
    [string]$PreferredModels = "GMV",

    [int]$MaxSecondSymbolsPerFirst = 20,
    [int]$MaxCodes = 60,

    # Safety limits.
    [int]$MaxRequests = 100,
    [int]$DelaySeconds = 12,
    [int]$JitterSeconds = 8,

    # No network calls. Prints planned calls and writes plan files.
    [switch]$PlanOnly
)

$ErrorActionPreference = "Stop"

$ApiBase = "https://support.gree.com:9877/api"
$script:ClientUuid = [guid]::NewGuid().ToString()
$script:RequestCount = 0
$script:LastRequestUtc = $null

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message"
}

function Split-List {
    param([string]$Value)

    $result = @()

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return @()
    }

    foreach ($part in ($Value -split '[,;]')) {
        $clean = $part.Trim()
        if (-not [string]::IsNullOrWhiteSpace($clean)) {
            $result += $clean
        }
    }

    return @($result | Select-Object -Unique)
}

function Save-Json {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Value
    )

    $Value | ConvertTo-Json -Depth 100 | Set-Content -Path $Path -Encoding UTF8
}

function ConvertTo-JsonBody {
    param([hashtable]$Body)
    return ($Body | ConvertTo-Json -Depth 20 -Compress)
}

function Wait-GreeThrottle {
    param([string]$Reason)

    if ($script:LastRequestUtc -ne $null) {
        $jitter = 0
        if ($JitterSeconds -gt 0) {
            $jitter = Get-Random -Minimum 0 -Maximum ($JitterSeconds + 1)
        }

        $wait = [Math]::Max(0, $DelaySeconds + $jitter)
        Write-Host ("[safe throttle] wait {0}s before {1}" -f $wait, $Reason)
        Start-Sleep -Seconds $wait
    }

    $script:LastRequestUtc = [DateTime]::UtcNow
}

function Invoke-GreeApi {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [hashtable]$Body = @{}
    )

    if ($script:RequestCount -ge $MaxRequests) {
        throw "Safety stop: MaxRequests reached ($MaxRequests)."
    }

    $script:RequestCount += 1
    $jsonBody = ConvertTo-JsonBody -Body $Body

    Write-Host ("[{0}/{1}] POST {2} {3}" -f $script:RequestCount, $MaxRequests, $Path, $jsonBody)

    if ($PlanOnly) {
        return [ordered]@{
            planOnly = $true
            code = 200
            data = @()
            path = $Path
            body = $Body
        }
    }

    Wait-GreeThrottle -Reason $Path

    $headers = @{
        "Accept" = "application/json, text/plain, */*"
        "Content-Type" = "application/json; charset=UTF-8"
        "Origin" = "https://support.gree.com"
        "Referer" = "https://support.gree.com/"
        "Front-End" = "1"
        "Page-Name" = "errorCodeSearch"
        "requestType" = "client"
        "uuId" = $script:ClientUuid
        "idempotentId" = [guid]::NewGuid().ToString()
        "User-Agent" = "AssistantEngineer-GreeIndexProbe/0.1 safe-research; single-thread; low-rate"
    }

    try {
        return Invoke-RestMethod `
            -Uri "$ApiBase$Path" `
            -Method Post `
            -Headers $headers `
            -Body $jsonBody `
            -TimeoutSec 60
    }
    catch {
        $statusCode = $null

        try {
            $statusCode = [int]$_.Exception.Response.StatusCode
        } catch {}

        if ($statusCode -eq 403 -or $statusCode -eq 429) {
            throw "Safety stop: server returned HTTP $statusCode for $Path."
        }

        throw
    }
}

function Get-DataArray {
    param($Response)

    if ($null -eq $Response) {
        return @()
    }

    if ($Response.planOnly) {
        return @()
    }

    if ($Response.code -ne 200) {
        Write-Warning ("Unexpected API code: {0}; msg={1}" -f $Response.code, $Response.msg)
        return @()
    }

    if ($null -eq $Response.data) {
        return @()
    }

    if ($Response.data -is [System.Array]) {
        return @($Response.data)
    }

    return @($Response.data)
}

Write-Step "Gree support index safe collector"

if (-not (Test-Path $RepoRoot)) {
    throw "RepoRoot not found: $RepoRoot"
}

if (-not (Test-Path (Join-Path $RepoRoot ".git"))) {
    throw "Not a git repository: $RepoRoot"
}

$selectedFirstSymbols = Split-List $FirstSymbols
$onlyCodeList = Split-List $OnlyCodes
$preferredModelList = Split-List $PreferredModels

$stamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH-mm-ss-fffZ")
$outRoot = Join-Path $RepoRoot ".ae-tools\gree-support-index"
$runRoot = Join-Path $outRoot ("run-" + $stamp)
$rawRoot = Join-Path $runRoot "raw"

New-Item -ItemType Directory -Force -Path $rawRoot | Out-Null

$config = [ordered]@{
    repoRoot = $RepoRoot
    apiBase = $ApiBase
    clientUuid = $script:ClientUuid
    firstSymbols = $selectedFirstSymbols
    onlyCodes = $onlyCodeList
    preferredModels = $preferredModelList
    maxSecondSymbolsPerFirst = $MaxSecondSymbolsPerFirst
    maxCodes = $MaxCodes
    maxRequests = $MaxRequests
    delaySeconds = $DelaySeconds
    jitterSeconds = $JitterSeconds
    planOnly = [bool]$PlanOnly
    startedUtc = (Get-Date).ToUniversalTime().ToString("o")
}

Save-Json -Path (Join-Path $runRoot "run-config.json") -Value $config

$summary = [ordered]@{
    startedUtc = $config.startedUtc
    finishedUtc = $null
    requestCount = 0
    source = [ordered]@{
        name = "Gree Global After-Sales Service Center"
        website = "https://support.gree.com/#/errorCode"
        apiBase = $ApiBase
    }
    safety = [ordered]@{
        mode = "index-only"
        downloads = 0
        presignCalls = 0
        searchCalls = 0
        note = "This collector calls only getErrorcode and searchModel. It does not call search, presign, or download card files."
    }
    firstSymbolsDiscovered = @()
    firstSymbolsProcessed = @()
    codes = @()
    codeModels = @()
    preferredModelMatches = @()
}

try {
    Write-Host ("RepoRoot: {0}" -f $RepoRoot)
    Write-Host ("PlanOnly: {0}" -f [bool]$PlanOnly)
    Write-Host ("FirstSymbols: {0}" -f ($selectedFirstSymbols -join ", "))
    Write-Host ("OnlyCodes: {0}" -f ($onlyCodeList -join ", "))
    Write-Host ("DelaySeconds: {0}; JitterSeconds: {1}; MaxRequests: {2}" -f $DelaySeconds, $JitterSeconds, $MaxRequests)

    $firstResp = Invoke-GreeApi -Path "/docu/client/errorcode/getErrorcode" -Body @{ errorcode = $null }
    Save-Json -Path (Join-Path $rawRoot "first-symbols.json") -Value $firstResp

    $firstAvailable = @(Get-DataArray $firstResp)
    $summary.firstSymbolsDiscovered = $firstAvailable

    $codesToProcess = @()

    if ($onlyCodeList.Count -gt 0) {
        $codesToProcess = @($onlyCodeList | Select-Object -First $MaxCodes)
        Write-Host ("[mode] OnlyCodes: {0}" -f ($codesToProcess -join ", "))
    }
    else {
        $firstToProcess = @()

        foreach ($symbol in $selectedFirstSymbols) {
            if ($firstAvailable -contains $symbol) {
                $firstToProcess += $symbol
            }
            else {
                Write-Warning "First symbol '$symbol' not found in site options. Skip."
            }
        }

        $summary.firstSymbolsProcessed = $firstToProcess

        foreach ($first in $firstToProcess) {
            if ($script:RequestCount -ge $MaxRequests) { break }

            $secondResp = Invoke-GreeApi -Path "/docu/client/errorcode/getErrorcode" -Body @{ errorcode = $first }
            Save-Json -Path (Join-Path $rawRoot ("second-symbols-" + $first + ".json")) -Value $secondResp

            $seconds = @(Get-DataArray $secondResp | Select-Object -First $MaxSecondSymbolsPerFirst)

            foreach ($second in $seconds) {
                $codesToProcess += "$first$second"

                if ($codesToProcess.Count -ge $MaxCodes) {
                    break
                }
            }

            if ($codesToProcess.Count -ge $MaxCodes) {
                break
            }
        }

        $codesToProcess = @($codesToProcess | Select-Object -Unique | Select-Object -First $MaxCodes)
    }

    $summary.codes = $codesToProcess

    Write-Host ("Codes to process: {0}" -f ($codesToProcess -join ", "))

    foreach ($code in $codesToProcess) {
        if ($script:RequestCount -ge $MaxRequests) { break }

        $modelResp = Invoke-GreeApi -Path "/docu/client/errorcode/searchModel" -Body @{ errorcode = $code }
        Save-Json -Path (Join-Path $rawRoot ("models-" + $code + ".json")) -Value $modelResp

        $models = @(Get-DataArray $modelResp)

        $entry = [ordered]@{
            code = $code
            models = $models
            modelCount = $models.Count
            preferredMatches = @()
        }

        foreach ($preferred in $preferredModelList) {
            if ($models -contains $preferred) {
                $entry.preferredMatches += $preferred
                $summary.preferredModelMatches += [ordered]@{
                    code = $code
                    model = $preferred
                }
            }
        }

        $summary.codeModels += $entry
    }
}
finally {
    $summary.requestCount = $script:RequestCount
    $summary.finishedUtc = (Get-Date).ToUniversalTime().ToString("o")

    Save-Json -Path (Join-Path $runRoot "gree-support-index-summary.json") -Value $summary

    $csvPath = Join-Path $runRoot "gree-support-index-code-models.csv"
    $csvRows = @()

    foreach ($entry in @($summary.codeModels)) {
        $csvRows += [pscustomobject]@{
            Code = $entry.code
            ModelCount = $entry.modelCount
            Models = (@($entry.models) -join ";")
            PreferredMatches = (@($entry.preferredMatches) -join ";")
        }
    }

    $csvRows | Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8

    $zipPath = Join-Path $outRoot ("gree-support-index-" + (Split-Path $runRoot -Leaf) + ".zip")

    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    if (-not $PlanOnly) {
        Compress-Archive -Path (Join-Path $runRoot "*") -DestinationPath $zipPath -Force
    }

    Write-Host ""
    Write-Host "Done."
    Write-Host ("Requests used: {0} / {1}" -f $script:RequestCount, $MaxRequests)
    Write-Host "Run folder:"
    Write-Host $runRoot

    if (-not $PlanOnly) {
        Write-Host "Zip:"
        Write-Host $zipPath
    }

    Write-Host ""
    Write-Host "Key files:"
    Write-Host "- gree-support-index-summary.json"
    Write-Host "- gree-support-index-code-models.csv"
    Write-Host "- raw/*.json"
}
