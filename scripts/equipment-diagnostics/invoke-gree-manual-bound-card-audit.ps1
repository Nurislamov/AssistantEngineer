[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$knowledgeRoot = Join-Path $repoRoot "data\equipment-diagnostics\error-knowledge"
$greeRoot = Join-Path $knowledgeRoot "gree"
$packageRoot = Join-Path $knowledgeRoot "packages"
$outputRoot = Join-Path $repoRoot "artifacts\verification\equipment-diagnostics"
$jsonOutput = Join-Path $outputRoot "manual-bound-card-repair-audit.json"
$csvOutput = Join-Path $outputRoot "manual-bound-card-repair-audit.csv"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$telegramFields = @(
    "title",
    "summary",
    "possibleCauses",
    "checkSteps",
    "recommendedAction",
    "safetyNote",
    "sourceNote"
)
$badPhrases = @(
    "по таблице руководства",
    "классифицирован по таблице",
    "записи источника",
    "диагностический вывод должен оставаться",
    "точная исходная формулировка",
    "не расширяйте трактовку",
    "текущая карточка",
    "дальнейшие действия выполните по сервисной процедуре",
    "manualId",
    "sourceReference",
    "sourceMeaning",
    "packageId",
    "document code"
)
$manualReviewedCodes = @(
    "AJ",
    "b1",
    "b2",
    "b3",
    "b4",
    "b5",
    "b6",
    "b7",
    "b8",
    "b9",
    "bA",
    "F5",
    "F6",
    "F7",
    "F8",
    "F9",
    "FA",
    "H0",
    "H1",
    "H2",
    "H3",
    "H5",
    "H6",
    "H7",
    "H8",
    "H9",
    "HC",
    "HH",
    "HJ",
    "HL"
)
$manualReviewedCodesLookup = @{}
foreach ($code in $manualReviewedCodes) {
    $manualReviewedCodesLookup[$code] = $true
}

$packages = @{}
foreach ($file in Get-ChildItem -LiteralPath $packageRoot -Filter "*.json" -File) {
    $package = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8) |
        ConvertFrom-Json
    $packages[$package.packageId] = $package
}

$entries = foreach ($file in Get-ChildItem -LiteralPath $greeRoot -Recurse -Filter "*.json" -File) {
    $entry = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8) |
        ConvertFrom-Json
    $package = $packages[$entry.packageId]
    if ($null -eq $package) {
        throw "Missing package manifest for '$($entry.packageId)' used by '$($file.FullName)'."
    }

    $texts = @($entry.texts)
    $visibleText = foreach ($text in $texts) {
        foreach ($field in $telegramFields) {
            if ($null -ne $text.$field) {
                @($text.$field) -join "`n"
            }
        }
    }
    $visibleBlob = $visibleText -join "`n"
    $foundPhrases = @($badPhrases | Where-Object {
        $visibleBlob.IndexOf($_, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
    })
    $relativePath = $file.FullName.Substring($repoRoot.Length + 1).Replace("\", "/")
    $runtimeDirectory = Split-Path $relativePath -Parent
    $isReviewedCriticalCard =
        ($entry.series -eq "GMV6") -and
        ($manualReviewedCodesLookup.ContainsKey($entry.code))
    $isReviewedSensorCard =
        ($entry.series -eq "GMV6") -and
        ($entry.code -match '^b[1-9A]$')
    $isReviewedDischargeTemperatureCard =
        ($entry.series -eq "GMV6") -and
        ($entry.code -in @("F5", "F6", "F7", "F8", "F9", "FA"))
    $isReviewedFanDriveCard =
        ($entry.series -eq "GMV6") -and
        ($entry.code -in @("H0", "H1", "H2", "H3", "H5", "H6", "H7", "H8", "H9", "HC", "HH", "HJ", "HL"))
    $isReviewedFlowchartCard =
        $isReviewedSensorCard -or $isReviewedDischargeTemperatureCard -or $isReviewedFanDriveCard

    [pscustomobject]@{
        series = $entry.series
        code = $entry.code
        packageId = $entry.packageId
        filePath = $relativePath
        sourceName = $entry.sourceName
        sourceMeaning = $entry.sourceMeaning
        signalType = $entry.signalType
        equipmentType = $entry.equipmentType
        displaySource = $entry.displaySource
        currentTitle = (@($texts | ForEach-Object { $_.title }) -join " | ")
        currentSummary = (@($texts | ForEach-Object { $_.summary }) -join " | ")
        currentPossibleCausesCount = [int](@($texts | ForEach-Object { @($_.possibleCauses).Count } | Measure-Object -Sum).Sum)
        currentCheckStepsCount = [int](@($texts | ForEach-Object { @($_.checkSteps).Count } | Measure-Object -Sum).Sum)
        badGenericPhrases = $foundPhrases
        manualSectionFound = if ($isReviewedCriticalCard) { $true } else { $null }
        manualHasFaultDiagnosis = if ($isReviewedCriticalCard) { $true } else { $null }
        manualHasPossibleCauses = if ($isReviewedFlowchartCard) { $true } elseif ($entry.series -eq "GMV6" -and $entry.code -eq "AJ") { $false } else { $null }
        manualHasTroubleshooting = if ($isReviewedCriticalCard) { $true } else { $null }
        manualHasFlowchart = if ($isReviewedFlowchartCard) { $true } elseif ($entry.series -eq "GMV6" -and $entry.code -eq "AJ") { $false } else { $null }
        repairStatus = if ($isReviewedCriticalCard -and $foundPhrases.Count -eq 0) { "Repaired" } else { "NeedsManualReview" }
        runtimeDirectory = $runtimeDirectory
    }
}

$packageMap = foreach ($packageId in ($packages.Keys | Sort-Object)) {
    $packageEntries = @($entries | Where-Object packageId -eq $packageId)
    $package = $packages[$packageId]
    [pscustomobject]@{
        packageId = $packageId
        series = $package.series
        sourceName = $package.sourceName
        sourceReference = $package.sourceReference
        entryCountExpected = $package.entryCountExpected
        runtimeDirectory = @($packageEntries.runtimeDirectory | Sort-Object -Unique) -join "; "
        runtimeCount = $packageEntries.Count
    }
}

foreach ($map in $packageMap) {
    if ($map.runtimeCount -ne $map.entryCountExpected) {
        throw "Package '$($map.packageId)' expects $($map.entryCountExpected) entries but has $($map.runtimeCount)."
    }
}

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$report = [pscustomobject]@{
    generatedAtUtc = [DateTime]::UtcNow.ToString("O")
    scope = "ED-24SRC.5 audit: all Gree runtime cards; manual review completed for GMV6 AJ, outdoor sensor batch b1-bA, outdoor discharge-temperature batch F5-FA, and outdoor fan-drive batch H0-HL."
    totalEntries = $entries.Count
    packageMap = @($packageMap)
    entries = @($entries | Sort-Object series, code, filePath)
}
[System.IO.File]::WriteAllText(
    $jsonOutput,
    ($report | ConvertTo-Json -Depth 20) + [Environment]::NewLine,
    $utf8NoBom)

$csvRows = $entries | Select-Object * -ExcludeProperty badGenericPhrases |
    ForEach-Object {
        $row = $_
        $source = $entries | Where-Object filePath -eq $row.filePath | Select-Object -First 1
        $row | Add-Member -NotePropertyName badGenericPhrases -NotePropertyValue ($source.badGenericPhrases -join " | ") -PassThru
    }
$csvText = $csvRows | Sort-Object series, code, filePath | ConvertTo-Csv -NoTypeInformation
[System.IO.File]::WriteAllLines($csvOutput, $csvText, $utf8NoBom)

Write-Output "Wrote $($entries.Count) audit rows."
Write-Output $jsonOutput
Write-Output $csvOutput
