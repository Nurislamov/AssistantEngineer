[CmdletBinding()]
param(
    [switch]$Apply
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$greeRoot = Join-Path $repoRoot "data\equipment-diagnostics\error-knowledge\gree\gmv6"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$marker = "диагностический вывод должен оставаться"
$changed = 0

function Write-EntryJson {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [object]$Entry
    )

    if ($Apply) {
        $updated = $Entry | ConvertTo-Json -Depth 30
        [System.IO.File]::WriteAllText($Path, $updated + [Environment]::NewLine, $utf8NoBom)
    }
}

foreach ($file in Get-ChildItem -LiteralPath $greeRoot -Recurse -Filter "*.json" -File) {
    $json = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    if ($json.IndexOf($marker, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        continue
    }

    $entry = $json | ConvertFrom-Json
    foreach ($text in $entry.texts) {
        $separatorIndex = $text.title.IndexOf(" — ", [System.StringComparison]::Ordinal)
        $meaning = if ($separatorIndex -ge 0) {
            $text.title.Substring($separatorIndex + 3).Trim().TrimEnd(".")
        }
        else {
            $entry.sourceMeaning.Trim().TrimEnd(".")
        }

        $text.summary = "Код $($entry.code) означает: $meaning. Это краткое значение кода без неподтверждённых причин."
        $text.checkSteps = @(
            "Запишите код $($entry.code), модель оборудования, место отображения и условия появления."
        )
        $text.recommendedAction = "Если работа оборудования нарушена или код сохраняется, передайте код и модель квалифицированному сервисному специалисту."
    }

    Write-EntryJson -Path $file.FullName -Entry $entry

    $changed++
}

function Repair-Gmv6Aj {
    $path = Join-Path $greeRoot "status\aj.json"
    $entry = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    foreach ($text in $entry.texts) {
        $text.title = "Gree GMV6 AJ — напоминание о чистке фильтра"
        $text.summary = "AJ означает, что требуется очистить фильтр внутреннего блока. Это статусное напоминание, а не аварийная ошибка."
        $text.possibleCauses = @()
        $text.checkSteps = @(
            "Очистите фильтр внутреннего блока.",
            "Сбросьте напоминание о чистке фильтра."
        )
        if ($text.audience -ne "Consumer") {
            $text.checkSteps += "После сброса начнётся следующий сервисный цикл."
        }
        $text.recommendedAction = "Очистить фильтр и сбросить напоминание, чтобы начался следующий сервисный цикл."
        $text.safetyNote = "AJ не указывает на неисправность платы, датчика или компрессора."
        $text.sourceNote = "Раздел 2.12: Filter Clean Prompt; очистка фильтра и сброс напоминания."
    }
    Write-EntryJson -Path $path -Entry $entry
}

function Repair-Gmv6B1 {
    $path = Join-Path $greeRoot "outdoor\b1.json"
    $entry = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    foreach ($text in $entry.texts) {
        $text.title = "Gree GMV6 b1 — неисправность датчика температуры наружного воздуха"
        $text.summary = "Ошибка b1 появляется, когда цепь датчика температуры наружного воздуха получает значение вне допустимого диапазона в течение 30 секунд."
        $text.possibleCauses = @(
            "Плохой контакт между датчиком температуры и разъёмом основной платы.",
            "Неисправен датчик температуры.",
            "Неисправна цепь детекции на плате."
        )
        if ($text.audience -eq "Consumer") {
            $text.checkSteps = @(
                "Запишите код b1 и модель оборудования.",
                "Не открывайте наружный блок; передайте код квалифицированному специалисту."
            )
            $text.recommendedAction = "Обратитесь в квалифицированный сервис для последовательной проверки разъёма, датчика и цепи детекции."
        }
        else {
            $text.checkSteps = @(
                "Проверьте разъём между основной платой и датчиком температуры наружного воздуха. Если разъём ослаблен или внутри есть посторонние предметы — удалите их и подключите разъём плотно.",
                "Если разъём в норме — проверьте датчик температуры. Если датчик неисправен — замените его.",
                "Если разъём и датчик исправны — неисправна цепь детекции; замените основную плату."
            )
            $text.recommendedAction = "Выполнить проверки по порядку: разъём, датчик температуры, цепь детекции на основной плате."
        }
        $text.safetyNote = "Работы внутри наружного блока и электрического отсека выполняет квалифицированный специалист при отключённом питании."
        $text.sourceNote = "Раздел 2.17: Outdoor Ambient Temperature Sensor Fault; причины и последовательность flowchart перенесены полностью."
    }
    Write-EntryJson -Path $path -Entry $entry
}

Repair-Gmv6Aj
Repair-Gmv6B1

$mode = if ($Apply) { "updated" } else { "would update" }
Write-Output "$mode $changed generic GMV6 runtime files and 2 critical cards."
