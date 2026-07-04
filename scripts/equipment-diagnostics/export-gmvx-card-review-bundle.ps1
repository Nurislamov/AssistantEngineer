param(
    [string]$RepoRoot = "D:\Project\AssistantEngineer",
    [string]$OutputRoot = "",
    [switch]$RunInventory
)

$ErrorActionPreference = "Stop"

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $dir = Split-Path -Parent $Path
    if ($dir -and -not (Test-Path $dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }

    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

function ConvertTo-SafeFileName {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return "empty"
    }

    $safe = $Value -replace '[^A-Za-z0-9._-]', '_'
    if ([string]::IsNullOrWhiteSpace($safe)) {
        return "empty"
    }

    return $safe
}

function Escape-CsvField {
    param([AllowNull()][string]$Value)

    if ($null -eq $Value) {
        $Value = ""
    }

    return '"' + ($Value -replace '"', '""') + '"'
}

function Get-ObjectPropertyValue {
    param(
        [AllowNull()]$Object,
        [Parameter(Mandatory = $true)][string[]]$Names
    )

    if ($null -eq $Object) {
        return $null
    }

    foreach ($name in $Names) {
        $prop = $Object.PSObject.Properties[$name]
        if ($null -ne $prop) {
            return $prop.Value
        }
    }

    return $null
}

function ConvertTo-FlatText {
    param([AllowNull()]$Value)

    if ($null -eq $Value) {
        return ""
    }

    if ($Value -is [string]) {
        return $Value
    }

    if ($Value -is [System.Collections.IDictionary]) {
        $items = New-Object System.Collections.Generic.List[string]
        foreach ($key in $Value.Keys) {
            $text = ConvertTo-FlatText $Value[$key]
            if (-not [string]::IsNullOrWhiteSpace($text)) {
                [void]$items.Add($text)
            }
        }
        return ($items -join "`n")
    }

    if (($Value -is [System.Collections.IEnumerable]) -and -not ($Value -is [string])) {
        $items = New-Object System.Collections.Generic.List[string]
        foreach ($item in $Value) {
            $text = ConvertTo-FlatText $item
            if (-not [string]::IsNullOrWhiteSpace($text)) {
                [void]$items.Add($text)
            }
        }
        return ($items -join "`n")
    }

    return [string]$Value
}

function Add-JsonStrings {
    param(
        [AllowNull()]$Value,
        [AllowNull()]$Output
    )

    if ($null -eq $Output) {
        throw "Output list is required."
    }

    if ($null -eq $Value) {
        return
    }

    if ($Value -is [string]) {
        if (-not [string]::IsNullOrWhiteSpace($Value)) {
            [void]$Output.Add($Value)
        }
        return
    }

    if ($Value -is [System.ValueType]) {
        return
    }

    if ($Value -is [System.Collections.IDictionary]) {
        foreach ($key in $Value.Keys) {
            Add-JsonStrings -Value $Value[$key] -Output $Output
        }
        return
    }

    if (($Value -is [System.Collections.IEnumerable]) -and -not ($Value -is [string])) {
        foreach ($item in $Value) {
            Add-JsonStrings -Value $item -Output $Output
        }
        return
    }

    foreach ($prop in $Value.PSObject.Properties) {
        Add-JsonStrings -Value $prop.Value -Output $Output
    }
}

function New-TextFromCodePoints {
    param([int[]]$CodePoints)

    $chars = New-Object System.Collections.Generic.List[char]
    foreach ($codePoint in $CodePoints) {
        [void]$chars.Add([char]$codePoint)
    }

    return -join $chars
}

function New-Pattern {
    param(
        [string]$Name,
        [string]$Text
    )

    return [pscustomobject]@{
        Name = $Name
        Text = $Text
    }
}

function Find-PatternHits {
    param(
        [string]$Text,
        [object[]]$Patterns
    )

    $hits = New-Object System.Collections.Generic.List[string]

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $hits
    }

    foreach ($pattern in $Patterns) {
        if ($Text.IndexOf($pattern.Text, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            [void]$hits.Add($pattern.Name)
        }
    }

    return $hits
}

function Add-ReportLine {
    param(
        [AllowNull()]$Lines,
        [AllowNull()][string]$Text
    )

    if ($null -eq $Lines) {
        throw "Lines list is required."
    }

    if ($null -eq $Text) {
        $Text = ""
    }

    [void]$Lines.Add($Text)
}

function Get-ReviewTextEntry {
    param([AllowNull()]$Card)

    if ($null -eq $Card) {
        return $null
    }

    $texts = Get-ObjectPropertyValue -Object $Card -Names @("texts")
    if ($null -eq $texts) {
        return $null
    }

    $first = $null
    foreach ($text in $texts) {
        if ($null -eq $first) {
            $first = $text
        }

        $locale = ConvertTo-FlatText (Get-ObjectPropertyValue -Object $text -Names @("locale"))
        $audience = ConvertTo-FlatText (Get-ObjectPropertyValue -Object $text -Names @("audience"))
        if ($locale.Equals("ru", [System.StringComparison]::OrdinalIgnoreCase) -and
            $audience.Equals("Consumer", [System.StringComparison]::OrdinalIgnoreCase)) {
            return $text
        }
    }

    return $first
}

function Add-VisibleTextStrings {
    param(
        [AllowNull()]$TextEntry,
        [AllowNull()]$Output
    )

    if ($null -eq $Output) {
        throw "Output list is required."
    }

    if ($null -eq $TextEntry) {
        return
    }

    foreach ($name in @("title", "summary", "possibleCauses", "checkSteps", "recommendedAction", "safetyNote", "sourceNote", "doNotAdvise")) {
        $value = Get-ObjectPropertyValue -Object $TextEntry -Names @($name)
        Add-JsonStrings -Value $value -Output $Output
    }
}

$RepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)

if (-not (Test-Path $RepoRoot)) {
    throw "RepoRoot not found: $RepoRoot"
}

Set-Location $RepoRoot

if (-not (Test-Path (Join-Path $RepoRoot ".git"))) {
    throw "RepoRoot is not a git repository root: $RepoRoot"
}

$cardRoot = Join-Path $RepoRoot "data\equipment-diagnostics\error-knowledge\gree\gmv-x"
if (-not (Test-Path $cardRoot)) {
    throw "GMV X card root not found: $cardRoot"
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $RepoRoot "artifacts\verification\equipment-diagnostics\gmvx-card-review"
}

$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$head = (git rev-parse HEAD).Trim()
$headShort = (git rev-parse --short HEAD).Trim()
$branch = (git branch --show-current).Trim()

$workRoot = Join-Path $OutputRoot ("gmvx-card-review-" + $timestamp)
$rawRoot = Join-Path $workRoot "raw-json"
$renderedRoot = Join-Path $workRoot "rendered-cards"
$reportsRoot = Join-Path $workRoot "reports"
$inventoryRoot = Join-Path $workRoot "inventory"

New-Item -ItemType Directory -Force -Path $rawRoot, $renderedRoot, $reportsRoot, $inventoryRoot | Out-Null

$inventoryScript = Join-Path $RepoRoot "scripts\equipment-diagnostics\invoke-gmvx-manual-bound-closure-inventory.ps1"
if ($RunInventory) {
    if (Test-Path $inventoryScript) {
        Write-Host "[GMVX export] Running inventory script..."
        $inventoryLog = Join-Path $inventoryRoot "inventory-run.log"
        $oldEap = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        & powershell -NoProfile -ExecutionPolicy Bypass -File $inventoryScript *>&1 | Tee-Object -FilePath $inventoryLog
        $inventoryExit = $LASTEXITCODE
        $ErrorActionPreference = $oldEap

        if ($inventoryExit -ne 0) {
            Write-Warning ("[GMVX export] Inventory script exited with code " + $inventoryExit)
        }
    } else {
        Write-Warning ("[GMVX export] Inventory script not found: " + $inventoryScript)
    }
}

$asciiLeakPatterns = @(
    (New-Pattern -Name "manual" -Text "manual"),
    (New-Pattern -Name "source" -Text "source"),
    (New-Pattern -Name "packageId" -Text "packageId")
)

$ruPatterns = @(
    (New-Pattern -Name "ConfirmCode" -Text (New-TextFromCodePoints @(0x041F,0x043E,0x0434,0x0442,0x0432,0x0435,0x0440,0x0434,0x0438,0x0442,0x0435,0x0020,0x043A,0x043E,0x0434))),
    (New-Pattern -Name "VerifyModel" -Text (New-TextFromCodePoints @(0x0421,0x0432,0x0435,0x0440,0x044C,0x0442,0x0435,0x0020,0x043C,0x043E,0x0434,0x0435,0x043B,0x044C))),
    (New-Pattern -Name "FurtherActions" -Text (New-TextFromCodePoints @(0x0414,0x0430,0x043B,0x044C,0x043D,0x0435,0x0439,0x0448,0x0438,0x0435,0x0020,0x0434,0x0435,0x0439,0x0441,0x0442,0x0432,0x0438,0x044F))),
    (New-Pattern -Name "ExactCauseDepends" -Text (New-TextFromCodePoints @(0x0422,0x043E,0x0447,0x043D,0x0430,0x044F,0x0020,0x043F,0x0440,0x0438,0x0447,0x0438,0x043D,0x0430,0x0020,0x0437,0x0430,0x0432,0x0438,0x0441,0x0438,0x0442))),
    (New-Pattern -Name "ByTable" -Text (New-TextFromCodePoints @(0x043F,0x043E,0x0020,0x0442,0x0430,0x0431,0x043B,0x0438,0x0446,0x0435))),
    (New-Pattern -Name "ClassifiedByTable" -Text (New-TextFromCodePoints @(0x043A,0x043B,0x0430,0x0441,0x0441,0x0438,0x0444,0x0438,0x0446,0x0438,0x0440,0x043E,0x0432,0x0430,0x043D,0x0020,0x043F,0x043E,0x0020,0x0442,0x0430,0x0431,0x043B,0x0438,0x0446,0x0435))),
    (New-Pattern -Name "FaultCardPhrase" -Text (New-TextFromCodePoints @(0x043A,0x0430,0x0440,0x0442,0x043E,0x0447,0x043A,0x0430,0x0020,0x043D,0x0435,0x0438,0x0441,0x043F,0x0440,0x0430,0x0432,0x043D,0x043E,0x0441,0x0442,0x0438))),
    (New-Pattern -Name "ManualRu" -Text (New-TextFromCodePoints @(0x0440,0x0443,0x043A,0x043E,0x0432,0x043E,0x0434,0x0441,0x0442,0x0432,0x043E))),
    (New-Pattern -Name "BasisRu" -Text (New-TextFromCodePoints @(0x043E,0x0441,0x043D,0x043E,0x0432,0x0430,0x043D,0x0438,0x0435))),
    (New-Pattern -Name "SourceRu" -Text (New-TextFromCodePoints @(0x0418,0x0441,0x0442,0x043E,0x0447,0x043D,0x0438,0x043A)))
)

$allPatterns = @()
$allPatterns += $asciiLeakPatterns
$allPatterns += $ruPatterns
$allPatterns += @(
    (New-Pattern -Name "QuestionMarkRun" -Text "???"),
    (New-Pattern -Name "ReplacementChar" -Text (New-TextFromCodePoints @(0xFFFD))),
    (New-Pattern -Name "MojibakeR" -Text (New-TextFromCodePoints @(0x0420,0x045F))),
    (New-Pattern -Name "MojibakeCopyright" -Text (New-TextFromCodePoints @(0x00C2,0x00A9)))
)

$expectedCounts = @{
    "outdoor" = 121
    "indoor" = 60
    "status" = 44
    "debugging" = 38
}

$cardFiles = Get-ChildItem $cardRoot -Recurse -File -Filter "*.json" | Sort-Object FullName
$rows = New-Object System.Collections.Generic.List[object]
$parseErrors = New-Object System.Collections.Generic.List[string]

foreach ($file in $cardFiles) {
    $relativePath = $file.FullName.Substring($RepoRoot.Length).TrimStart('\', '/')
    $category = $file.Directory.Name.ToLowerInvariant()
    $fileCode = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)

    $raw = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)

    $card = $null
    try {
        $card = $raw | ConvertFrom-Json
    } catch {
        $message = $relativePath + " :: " + $_.Exception.Message
        [void]$parseErrors.Add($message)

        [void]$rows.Add([pscustomobject]@{
            Category = $category
            Code = $fileCode
            FileCode = $fileCode
            RelativePath = $relativePath
            Title = ""
            Summary = ""
            SafetyNote = ""
            RecommendedAction = ""
            CheckStepsCount = 0
            DoNotAdviseCount = 0
            PossibleCausesCount = 0
            StringCount = 0
            PatternHits = "JsonParseError"
            RenderedFile = ""
            ParseStatus = "FAIL"
        })
        continue
    }

    $codeValue = Get-ObjectPropertyValue -Object $card -Names @("code", "faultCode", "errorCode")
    $code = ConvertTo-FlatText $codeValue
    if ([string]::IsNullOrWhiteSpace($code)) {
        $code = $fileCode
    }

    $textEntry = Get-ReviewTextEntry -Card $card
    if ($null -eq $textEntry) {
        $textEntry = $card
    }

    $title = ConvertTo-FlatText (Get-ObjectPropertyValue -Object $textEntry -Names @("title", "name"))
    $summary = ConvertTo-FlatText (Get-ObjectPropertyValue -Object $textEntry -Names @("summary", "description", "consumerSummary"))
    $safetyNote = ConvertTo-FlatText (Get-ObjectPropertyValue -Object $textEntry -Names @("safetyNote"))
    $recommendedAction = ConvertTo-FlatText (Get-ObjectPropertyValue -Object $textEntry -Names @("recommendedAction"))
    $checkSteps = Get-ObjectPropertyValue -Object $textEntry -Names @("checkSteps")
    $doNotAdvise = Get-ObjectPropertyValue -Object $textEntry -Names @("doNotAdvise")
    $possibleCauses = Get-ObjectPropertyValue -Object $textEntry -Names @("possibleCauses")

    $checkStepsCount = 0
    if ($null -ne $checkSteps) {
        if (($checkSteps -is [System.Collections.IEnumerable]) -and -not ($checkSteps -is [string])) {
            foreach ($x in $checkSteps) { $checkStepsCount++ }
        } else {
            $checkStepsCount = 1
        }
    }

    $doNotAdviseCount = 0
    if ($null -ne $doNotAdvise) {
        if (($doNotAdvise -is [System.Collections.IEnumerable]) -and -not ($doNotAdvise -is [string])) {
            foreach ($x in $doNotAdvise) { $doNotAdviseCount++ }
        } else {
            $doNotAdviseCount = 1
        }
    }

    $possibleCausesCount = 0
    if ($null -ne $possibleCauses) {
        if (($possibleCauses -is [System.Collections.IEnumerable]) -and -not ($possibleCauses -is [string])) {
            foreach ($x in $possibleCauses) { $possibleCausesCount++ }
        } else {
            $possibleCausesCount = 1
        }
    }

    $strings = New-Object System.Collections.Generic.List[string]
    Add-VisibleTextStrings -TextEntry $textEntry -Output $strings
    $uniqueStrings = $strings | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique
    $allText = ($uniqueStrings -join "`n")

    $hits = Find-PatternHits -Text $allText -Patterns $allPatterns
    $hitsText = ($hits -join "; ")

    $safeCategory = ConvertTo-SafeFileName $category
    $safeCode = ConvertTo-SafeFileName $code
    $renderedName = $safeCategory + "-" + $safeCode + ".md"
    $renderedFile = "rendered-cards/" + $renderedName
    $renderedPath = Join-Path $renderedRoot $renderedName

    $md = New-Object System.Collections.Generic.List[string]
    Add-ReportLine -Lines $md -Text ("# " + $category + " / " + $code)
    Add-ReportLine -Lines $md -Text ""
    Add-ReportLine -Lines $md -Text ("Path: " + $relativePath)
    Add-ReportLine -Lines $md -Text ("ParseStatus: PASS")
    Add-ReportLine -Lines $md -Text ("PatternHits: " + $hitsText)
    Add-ReportLine -Lines $md -Text ""
    Add-ReportLine -Lines $md -Text "## Title"
    Add-ReportLine -Lines $md -Text $title
    Add-ReportLine -Lines $md -Text ""
    Add-ReportLine -Lines $md -Text "## Summary"
    Add-ReportLine -Lines $md -Text $summary
    Add-ReportLine -Lines $md -Text ""
    Add-ReportLine -Lines $md -Text "## SafetyNote"
    Add-ReportLine -Lines $md -Text $safetyNote
    Add-ReportLine -Lines $md -Text ""
    Add-ReportLine -Lines $md -Text "## RecommendedAction"
    Add-ReportLine -Lines $md -Text $recommendedAction
    Add-ReportLine -Lines $md -Text ""
    Add-ReportLine -Lines $md -Text "## All string values"
    Add-ReportLine -Lines $md -Text $allText

    Write-Utf8NoBomFile -Path $renderedPath -Content ($md -join "`r`n")

    $rawDestDir = Join-Path $rawRoot $category
    New-Item -ItemType Directory -Force -Path $rawDestDir | Out-Null
    Copy-Item -Path $file.FullName -Destination (Join-Path $rawDestDir $file.Name) -Force

    [void]$rows.Add([pscustomobject]@{
        Category = $category
        Code = $code
        FileCode = $fileCode
        RelativePath = $relativePath
        Title = $title
        Summary = $summary
        SafetyNote = $safetyNote
        RecommendedAction = $recommendedAction
        CheckStepsCount = $checkStepsCount
        DoNotAdviseCount = $doNotAdviseCount
        PossibleCausesCount = $possibleCausesCount
        StringCount = @($uniqueStrings).Count
        PatternHits = $hitsText
        RenderedFile = $renderedFile
        ParseStatus = "PASS"
    })
}

$csv = New-Object System.Collections.Generic.List[string]
Add-ReportLine -Lines $csv -Text "Category,Code,FileCode,RelativePath,Title,Summary,SafetyNote,RecommendedAction,CheckStepsCount,DoNotAdviseCount,PossibleCausesCount,StringCount,PatternHits,RenderedFile,ParseStatus"

foreach ($row in ($rows | Sort-Object Category, Code, RelativePath)) {
    $fields = @(
        (Escape-CsvField $row.Category),
        (Escape-CsvField $row.Code),
        (Escape-CsvField $row.FileCode),
        (Escape-CsvField $row.RelativePath),
        (Escape-CsvField $row.Title),
        (Escape-CsvField $row.Summary),
        (Escape-CsvField $row.SafetyNote),
        (Escape-CsvField $row.RecommendedAction),
        (Escape-CsvField ([string]$row.CheckStepsCount)),
        (Escape-CsvField ([string]$row.DoNotAdviseCount)),
        (Escape-CsvField ([string]$row.PossibleCausesCount)),
        (Escape-CsvField ([string]$row.StringCount)),
        (Escape-CsvField $row.PatternHits),
        (Escape-CsvField $row.RenderedFile),
        (Escape-CsvField $row.ParseStatus)
    )
    Add-ReportLine -Lines $csv -Text ($fields -join ",")
}

Write-Utf8NoBomFile -Path (Join-Path $reportsRoot "gmvx-card-index.csv") -Content ($csv -join "`r`n")

$parseReport = New-Object System.Collections.Generic.List[string]
if ($parseErrors.Count -eq 0) {
    Add-ReportLine -Lines $parseReport -Text "PASS"
} else {
    Add-ReportLine -Lines $parseReport -Text "FAIL"
    foreach ($err in $parseErrors) {
        Add-ReportLine -Lines $parseReport -Text $err
    }
}
Write-Utf8NoBomFile -Path (Join-Path $reportsRoot "parse-check.txt") -Content ($parseReport -join "`r`n")

$countProblems = New-Object System.Collections.Generic.List[string]
$total = $rows.Count

foreach ($key in ($expectedCounts.Keys | Sort-Object)) {
    $actual = @($rows | Where-Object { $_.Category -eq $key }).Count
    $expected = $expectedCounts[$key]
    if ($actual -ne $expected) {
        [void]$countProblems.Add($key + " expected " + $expected + " actual " + $actual)
    }
}

if ($total -ne 263) {
    [void]$countProblems.Add("total expected 263 actual " + $total)
}

$countReport = New-Object System.Collections.Generic.List[string]
if ($countProblems.Count -eq 0) {
    Add-ReportLine -Lines $countReport -Text "PASS"
} else {
    Add-ReportLine -Lines $countReport -Text "FAIL"
    foreach ($problem in $countProblems) {
        Add-ReportLine -Lines $countReport -Text $problem
    }
}
Write-Utf8NoBomFile -Path (Join-Path $reportsRoot "count-check.txt") -Content ($countReport -join "`r`n")

$flagRows = @($rows | Where-Object { -not [string]::IsNullOrWhiteSpace($_.PatternHits) })
$flagCsv = New-Object System.Collections.Generic.List[string]
Add-ReportLine -Lines $flagCsv -Text "Category,Code,RelativePath,PatternHits,RenderedFile"
foreach ($row in ($flagRows | Sort-Object Category, Code, RelativePath)) {
    $fields = @(
        (Escape-CsvField $row.Category),
        (Escape-CsvField $row.Code),
        (Escape-CsvField $row.RelativePath),
        (Escape-CsvField $row.PatternHits),
        (Escape-CsvField $row.RenderedFile)
    )
    Add-ReportLine -Lines $flagCsv -Text ($fields -join ",")
}
Write-Utf8NoBomFile -Path (Join-Path $reportsRoot "pattern-flags.csv") -Content ($flagCsv -join "`r`n")

$readme = New-Object System.Collections.Generic.List[string]
Add-ReportLine -Lines $readme -Text "# GMV X card review bundle"
Add-ReportLine -Lines $readme -Text ""
Add-ReportLine -Lines $readme -Text ("Generated: " + $timestamp)
Add-ReportLine -Lines $readme -Text ("Branch: " + $branch)
Add-ReportLine -Lines $readme -Text ("HEAD: " + $head)
Add-ReportLine -Lines $readme -Text ""
Add-ReportLine -Lines $readme -Text "## Counts"
Add-ReportLine -Lines $readme -Text ("Total JSON cards: " + $total)

foreach ($group in ($rows | Group-Object Category | Sort-Object Name)) {
    Add-ReportLine -Lines $readme -Text ("- " + $group.Name + ": " + $group.Count)
}

Add-ReportLine -Lines $readme -Text ""
Add-ReportLine -Lines $readme -Text "## Checks"
Add-ReportLine -Lines $readme -Text ("Parse errors: " + $parseErrors.Count)
Add-ReportLine -Lines $readme -Text ("Pattern flags: " + $flagRows.Count)
Add-ReportLine -Lines $readme -Text ("Count check: " + ($(if ($countProblems.Count -eq 0) { "PASS" } else { "FAIL" })))
Add-ReportLine -Lines $readme -Text ""
Add-ReportLine -Lines $readme -Text "## Files"
Add-ReportLine -Lines $readme -Text "- raw-json/: copied JSON cards"
Add-ReportLine -Lines $readme -Text "- rendered-cards/: markdown rendering per card"
Add-ReportLine -Lines $readme -Text "- reports/gmvx-card-index.csv"
Add-ReportLine -Lines $readme -Text "- reports/count-check.txt"
Add-ReportLine -Lines $readme -Text "- reports/parse-check.txt"
Add-ReportLine -Lines $readme -Text "- reports/pattern-flags.csv"

Write-Utf8NoBomFile -Path (Join-Path $reportsRoot "README.md") -Content ($readme -join "`r`n")

git status --short | Out-File -FilePath (Join-Path $workRoot "git-status.txt") -Encoding utf8
git log --oneline -20 | Out-File -FilePath (Join-Path $workRoot "git-log.txt") -Encoding utf8

$zipPath = Join-Path $OutputRoot ("gmvx-card-review-" + $timestamp + "-" + $headShort + ".zip")
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path (Join-Path $workRoot "*") -DestinationPath $zipPath -Force

Write-Host ""
Write-Host "[GMVX export] DONE"
Write-Host ("[GMVX export] Branch: " + $branch)
Write-Host ("[GMVX export] HEAD: " + $head)
Write-Host ("[GMVX export] Cards: " + $total)

foreach ($group in ($rows | Group-Object Category | Sort-Object Name)) {
    Write-Host ("[GMVX export] " + $group.Name + ": " + $group.Count)
}

if ($parseErrors.Count -eq 0) {
    Write-Host "[GMVX export] Parse check: PASS"
} else {
    Write-Host ("[GMVX export] Parse check: FAIL (" + $parseErrors.Count + ")")
}

if ($countProblems.Count -eq 0) {
    Write-Host "[GMVX export] Count check: PASS"
} else {
    Write-Host "[GMVX export] Count check: FAIL"
    foreach ($problem in $countProblems) {
        Write-Host ("[GMVX export] " + $problem)
    }
}

Write-Host ("[GMVX export] Pattern flags: " + $flagRows.Count)
Write-Host ("[GMVX export] Work folder: " + $workRoot)
Write-Host ("[GMVX export] ZIP: " + $zipPath)
