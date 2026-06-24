param(
    [string]$RepoRoot = "D:\Project\AssistantEngineer",

    # Максимально безопасный режим по умолчанию:
    # только маленький индекс, без скачивания файлов.
    [string[]]$FirstSymbols = @("C"),
    [int]$MaxSecondSymbolsPerFirst = 3,
    [int]$MaxCodes = 5,
    [int]$MaxModelsPerCode = 3,

    # Скачивание файлов выключено по умолчанию.
    [switch]$DownloadFiles,
    [int]$MaxDownloads = 2,

    # В v3.2 по умолчанию скачиваем только по временной ссылке,
    # которую сайт уже вернул в search response.
    # /docu/file/presign может требовать login, поэтому без явного ключа его не трогаем.
    [switch]$AllowPresignFallback,

    # Жёсткие лимиты безопасности.
    [int]$MaxRequests = 30,
    [int]$DelaySeconds = 8,
    [int]$JitterSeconds = 4,

    # Если нужно проверить один конкретный код/модель, можно задать:
    # -OnlyCodes "C0","H5" -OnlyModels "GMV"
    [string[]]$OnlyCodes = @(),
    [string[]]$OnlyModels = @(),

    # Полностью без сетевых вызовов: только показать план.
    [switch]$PlanOnly
)

$ErrorActionPreference = "Stop"

$ApiBase = "https://support.gree.com:9877/api"
$script:ClientUuid = [guid]::NewGuid().ToString()

function New-SafeFileName {
    param([Parameter(Mandatory=$true)][string]$Value)

    $name = $Value -replace '[\\/:*?"<>|]+', '_'
    $name = $name -replace '\s+', ' '
    $name = $name.Trim()

    if ($name.Length -gt 120) {
        $name = $name.Substring(0, 120)
    }

    if ([string]::IsNullOrWhiteSpace($name)) {
        return "unnamed"
    }

    return $name
}

function ConvertTo-JsonBody {
    param([hashtable]$Body)

    return ($Body | ConvertTo-Json -Depth 20 -Compress)
}

$script:RequestCount = 0
$script:LastRequestUtc = $null

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
        [Parameter(Mandatory=$true)][string]$Path,
        [hashtable]$Body = @{},
        [string]$Method = "POST"
    )

    if ($script:RequestCount -ge $MaxRequests) {
        throw "Safety stop: MaxRequests reached ($MaxRequests)."
    }

    $script:RequestCount += 1

    $url = "$ApiBase$Path"
    $jsonBody = ConvertTo-JsonBody -Body $Body

    Write-Host ("[{0}/{1}] {2} {3} {4}" -f $script:RequestCount, $MaxRequests, $Method, $Path, $jsonBody)

    if ($PlanOnly) {
        return [ordered]@{
            planOnly = $true
            path = $Path
            method = $Method
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
        "User-Agent" = "AssistantEngineer-GreeCatalogProbe/0.3.3 safe-research; single-thread; low-rate"
    }

    try {
        if ($Method -eq "GET") {
            $response = Invoke-RestMethod -Uri $url -Method Get -Headers $headers -TimeoutSec 60
        }
        else {
            $response = Invoke-RestMethod -Uri $url -Method Post -Headers $headers -Body $jsonBody -TimeoutSec 60
        }

        return $response
    }
    catch {
        $statusCode = $null
        try {
            $statusCode = [int]$_.Exception.Response.StatusCode
        } catch {}

        if ($statusCode -eq 403 -or $statusCode -eq 429) {
            throw "Safety stop: server returned HTTP $statusCode for $Path. Stop immediately to avoid abuse."
        }

        throw
    }
}

function Save-Json {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)]$Value
    )

    $json = $Value | ConvertTo-Json -Depth 100
    $json | Set-Content -Path $Path -Encoding UTF8
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


function Normalize-ListArgument {
    param(
        [string[]]$Values
    )

    $result = @()

    foreach ($value in @($Values)) {
        if ([string]::IsNullOrWhiteSpace($value)) {
            continue
        }

        foreach ($part in ($value -split '[,;]')) {
            $clean = $part.Trim()
            if (-not [string]::IsNullOrWhiteSpace($clean)) {
                $result += $clean
            }
        }
    }

    return @($result | Select-Object -Unique)
}

$FirstSymbols = @(Normalize-ListArgument -Values $FirstSymbols)
$OnlyCodes = @(Normalize-ListArgument -Values $OnlyCodes)
$OnlyModels = @(Normalize-ListArgument -Values $OnlyModels)

Write-Host ""
Write-Host "[Gree catalog v3.3 safe probe]"
Write-Host "RepoRoot: $RepoRoot"
Write-Host "DownloadFiles: $DownloadFiles"
Write-Host "PlanOnly: $PlanOnly"
Write-Host "DelaySeconds: $DelaySeconds; JitterSeconds: $JitterSeconds; MaxRequests: $MaxRequests"
Write-Host "MaxCodes: $MaxCodes; MaxModelsPerCode: $MaxModelsPerCode; MaxDownloads: $MaxDownloads"
Write-Host ""

if (-not (Test-Path $RepoRoot)) {
    throw "RepoRoot not found: $RepoRoot"
}

$runStamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH-mm-ss-fffZ")
$outRoot = Join-Path $RepoRoot ".ae-tools\gree-support-catalog-v3"
$runRoot = Join-Path $outRoot ("run-" + $runStamp)
$rawRoot = Join-Path $runRoot "raw"
$downloadRoot = Join-Path $runRoot "downloads"

New-Item -ItemType Directory -Force -Path $rawRoot | Out-Null
New-Item -ItemType Directory -Force -Path $downloadRoot | Out-Null

$config = [ordered]@{
    repoRoot = $RepoRoot
    apiBase = $ApiBase
    clientUuid = $script:ClientUuid
    firstSymbols = $FirstSymbols
    maxSecondSymbolsPerFirst = $MaxSecondSymbolsPerFirst
    maxCodes = $MaxCodes
    maxModelsPerCode = $MaxModelsPerCode
    downloadFiles = [bool]$DownloadFiles
    maxDownloads = $MaxDownloads
    allowPresignFallback = [bool]$AllowPresignFallback
    maxRequests = $MaxRequests
    delaySeconds = $DelaySeconds
    jitterSeconds = $JitterSeconds
    onlyCodes = $OnlyCodes
    onlyModels = $OnlyModels
    planOnly = [bool]$PlanOnly
    startedUtc = (Get-Date).ToUniversalTime().ToString("o")
}

Save-Json -Path (Join-Path $runRoot "run-config.json") -Value $config

$summary = [ordered]@{
    startedUtc = $config.startedUtc
    finishedUtc = $null
    requestCount = 0
    firstSymbolsDiscovered = @()
    firstSymbolsProcessed = @()
    codeCandidates = @()
    codeModelIndex = @()
    searchResults = @()
    fileCandidates = @()
    downloads = @()
    safety = [ordered]@{
        stoppedByLimit = $false
        note = "Single-threaded low-rate probe. Downloads disabled unless -DownloadFiles is passed. v3.3 sends the same client headers as the web app. Downloads use presign first, then direct temporary URLs only as fallback."
    }
}

try {
    # 1. Получаем первые символы только один раз.
    $firstResp = Invoke-GreeApi -Path "/docu/client/errorcode/getErrorcode" -Body @{ errorcode = $null }
    Save-Json -Path (Join-Path $rawRoot "first-symbols.json") -Value $firstResp

    $firstAvailable = @(Get-DataArray $firstResp)
    $summary.firstSymbolsDiscovered = $firstAvailable

    if ($OnlyCodes.Count -gt 0) {
        $codesToProcess = @($OnlyCodes | Select-Object -Unique)
        Write-Host ("[mode] OnlyCodes mode: {0}" -f ($codesToProcess -join ", "))
    }
    else {
        $selectedFirst = @()
        foreach ($f in $FirstSymbols) {
            if ($firstAvailable -contains $f) {
                $selectedFirst += $f
            }
            else {
                Write-Warning "First symbol '$f' not found in site options; skip."
            }
        }

        if ($selectedFirst.Count -eq 0) {
            throw "No valid first symbols selected."
        }

        $summary.firstSymbolsProcessed = $selectedFirst

        $codesToProcess = @()

        foreach ($first in $selectedFirst) {
            if ($script:RequestCount -ge $MaxRequests) { break }

            $secondResp = Invoke-GreeApi -Path "/docu/client/errorcode/getErrorcode" -Body @{ errorcode = $first }
            Save-Json -Path (Join-Path $rawRoot ("second-symbols-" + (New-SafeFileName $first) + ".json")) -Value $secondResp

            $seconds = @(Get-DataArray $secondResp | Select-Object -First $MaxSecondSymbolsPerFirst)

            foreach ($second in $seconds) {
                $code = "$first$second"
                $codesToProcess += $code
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

    $summary.codeCandidates = $codesToProcess

    Write-Host ("[codes] {0}" -f ($codesToProcess -join ", "))

    # 2. Для каждого кода получаем список серий.
    $allCodeModels = @()

    foreach ($code in $codesToProcess) {
        if ($script:RequestCount -ge $MaxRequests) { break }

        $modelsResp = Invoke-GreeApi -Path "/docu/client/errorcode/searchModel" -Body @{ errorcode = $code }
        Save-Json -Path (Join-Path $rawRoot ("models-" + (New-SafeFileName $code) + ".json")) -Value $modelsResp

        $models = @(Get-DataArray $modelsResp)

        if ($OnlyModels.Count -gt 0) {
            $models = @($models | Where-Object { $OnlyModels -contains $_ })
        }

        $models = @($models | Select-Object -First $MaxModelsPerCode)

        $entry = [ordered]@{
            code = $code
            models = $models
        }

        $allCodeModels += $entry
        $summary.codeModelIndex += $entry
    }

    # 3. Для ограниченной выборки code + model делаем search.
    foreach ($codeModel in $allCodeModels) {
        $code = [string]$codeModel.code
        foreach ($model in @($codeModel.models)) {
            if ($script:RequestCount -ge $MaxRequests) { break }

            $searchBody = @{
                model = [string]$model
                errorcode = $code
                current = 1
                size = 20
            }

            $searchResp = Invoke-GreeApi -Path "/docu/client/errorcode/search" -Body $searchBody
            $searchFile = Join-Path $rawRoot ("search-" + (New-SafeFileName "$code-$model") + ".json")
            Save-Json -Path $searchFile -Value $searchResp

            $rows = @()
            if (-not $PlanOnly -and $searchResp.code -eq 200 -and $null -ne $searchResp.data -and $null -ne $searchResp.data.data) {
                $rows = @($searchResp.data.data)
            }

            $searchEntry = [ordered]@{
                code = $code
                model = $model
                total = if ($PlanOnly) { $null } else { $searchResp.data.total }
                rows = @()
            }

            foreach ($row in $rows) {
                $rowSummary = [ordered]@{
                    id = $row.id
                    nameCn = $row.nameCn
                    nameEn = $row.nameEn
                    gmtCreated = $row.gmtCreated
                    gmtModified = $row.gmtModified
                    images = @()
                    files = @()
                }

                foreach ($img in @($row.imageList)) {
                    if ($null -eq $img) { continue }

                    $fileEntry = [ordered]@{
                        code = $code
                        model = $model
                        rowId = $row.id
                        sourceId = $img.id
                        type = "image"
                        filename = $img.filename
                        documentSize = $img.documentSize
                        fileKey = $img.fileKey
                        url = $img.url
                        urlAvailableInSearch = -not [string]::IsNullOrWhiteSpace($img.url)
                    }

                    $rowSummary.images += $fileEntry
                    $summary.fileCandidates += $fileEntry
                }

                foreach ($file in @($row.fileList)) {
                    if ($null -eq $file) { continue }

                    $fileEntry = [ordered]@{
                        code = $code
                        model = $model
                        rowId = $row.id
                        sourceId = $file.id
                        type = "file"
                        filename = $file.filename
                        documentSize = $file.documentSize
                        fileKey = $file.fileKey
                        url = $file.url
                        urlAvailableInSearch = -not [string]::IsNullOrWhiteSpace($file.url)
                    }

                    $rowSummary.files += $fileEntry
                    $summary.fileCandidates += $fileEntry
                }

                $searchEntry.rows += $rowSummary
            }

            $summary.searchResults += $searchEntry
        }
    }

    # 4. Очень ограниченные скачивания, только если явно включено.
    $downloaded = 0

    if ($DownloadFiles) {
        foreach ($candidate in @($summary.fileCandidates)) {
            if ($downloaded -ge $MaxDownloads) { break }
            if ($script:RequestCount -ge $MaxRequests) { break }

            if ([string]::IsNullOrWhiteSpace($candidate.filename)) {
                continue
            }

            $downloadUrl = $null
            $downloadSource = $null

            # v3.3: для скачивания сначала используем /docu/file/presign,
            # потому что search response иногда возвращает битую S3-ссылку вида ".../null<fileKey>".
            # С правильными client headers presign работает без логина, как в браузере.
            if (-not [string]::IsNullOrWhiteSpace($candidate.fileKey)) {
                $presignBody = @{
                    fileKey = [string]$candidate.fileKey
                    filename = [string]$candidate.filename
                }

                $presignResp = Invoke-GreeApi -Path "/docu/file/presign" -Body $presignBody
                Save-Json -Path (Join-Path $rawRoot ("presign-" + (New-SafeFileName "$($candidate.code)-$($candidate.model)-$($candidate.filename)") + ".json")) -Value $presignResp

                if ($PlanOnly) {
                    continue
                }

                if ($presignResp.code -eq 200 -and -not [string]::IsNullOrWhiteSpace($presignResp.data)) {
                    $downloadUrl = [string]$presignResp.data
                    $downloadSource = "presign"
                }
                else {
                    Write-Warning ("Presign did not return URL for {0}; code={1}; msg={2}" -f $candidate.filename, $presignResp.code, $presignResp.msg)
                }
            }

            if ([string]::IsNullOrWhiteSpace($downloadUrl) -and -not [string]::IsNullOrWhiteSpace($candidate.url)) {
                $downloadUrl = [string]$candidate.url
                $downloadSource = "search-url-fallback"
            }

            if ([string]::IsNullOrWhiteSpace($downloadUrl)) {
                Write-Warning ("No downloadable URL for {0}; skip." -f $candidate.filename)
                continue
            }

            if ($PlanOnly) {
                continue
            }

            $safeCode = New-SafeFileName "$($candidate.code)-$($candidate.model)"
            $targetDir = Join-Path $downloadRoot $safeCode
            New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

            $targetPath = Join-Path $targetDir (New-SafeFileName $candidate.filename)

            if ($script:RequestCount -ge $MaxRequests) { break }
            $script:RequestCount += 1
            Write-Host ("[{0}/{1}] DOWNLOAD {2} via {3}" -f $script:RequestCount, $MaxRequests, $candidate.filename, $downloadSource)
            Wait-GreeThrottle -Reason "download"

            try {
                Invoke-WebRequest -Uri $downloadUrl -OutFile $targetPath -TimeoutSec 120 -Headers @{
                    "User-Agent" = "AssistantEngineer-GreeCatalogProbe/0.3 safe-research; single-thread; low-rate"
                }

                $item = Get-Item $targetPath

                $downloadEntry = [ordered]@{
                    code = $candidate.code
                    model = $candidate.model
                    filename = $candidate.filename
                    bytes = $item.Length
                    path = $targetPath
                    source = $downloadSource
                }

                $summary.downloads += $downloadEntry
                $downloaded += 1
            }
            catch {
                $statusCode = $null
                try {
                    $statusCode = [int]$_.Exception.Response.StatusCode
                } catch {}

                if ($statusCode -eq 403 -or $statusCode -eq 429) {
                    throw "Safety stop: download returned HTTP $statusCode. Stop immediately."
                }

                Write-Warning ("Download failed for {0}: {1}" -f $candidate.filename, $_.Exception.Message)
                continue
            }
        }
    }

    if ($script:RequestCount -ge $MaxRequests) {
        $summary.safety.stoppedByLimit = $true
    }
}
finally {
    $summary.requestCount = $script:RequestCount
    $summary.finishedUtc = (Get-Date).ToUniversalTime().ToString("o")
    Save-Json -Path (Join-Path $runRoot "catalog-summary.json") -Value $summary

    $zipPath = Join-Path $outRoot ("gree-support-catalog-v3-" + (Split-Path $runRoot -Leaf) + ".zip")
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    if (-not $PlanOnly) {
        Compress-Archive -Path (Join-Path $runRoot "*") -DestinationPath $zipPath -Force
    }

    Write-Host ""
    Write-Host "[Gree catalog v3.3 safe probe] Done."
    Write-Host "Requests used: $script:RequestCount / $MaxRequests"
    Write-Host "Run folder:"
    Write-Host $runRoot

    if (-not $PlanOnly) {
        Write-Host ""
        Write-Host "Zip:"
        Write-Host $zipPath
    }

    Write-Host ""
    Write-Host "Key files:"
    Write-Host "- run-config.json"
    Write-Host "- catalog-summary.json"
    Write-Host "- raw\*.json"
    if ($DownloadFiles) {
        Write-Host "- downloads\*"
    }
}
