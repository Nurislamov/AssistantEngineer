[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$knowledgeRoot = Join-Path $repoRoot "data\equipment-diagnostics\error-knowledge"
$gmv6Root = Join-Path $knowledgeRoot "gree\gmv6"
$packageRoot = Join-Path $knowledgeRoot "packages"
$outputRoot = Join-Path $repoRoot "artifacts\verification\equipment-diagnostics"
$jsonOutput = Join-Path $outputRoot "gmv6-manual-bound-closure-inventory.json"
$csvOutput = Join-Path $outputRoot "gmv6-manual-bound-closure-inventory.csv"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$expectedCategoryCounts = [ordered]@{
    outdoor = 121
    indoor = 60
    debugging = 38
    status = 44
}

$telegramFields = @(
    "title",
    "summary",
    "possibleCauses",
    "checkSteps",
    "recommendedAction",
    "safetyNote",
    "sourceNote"
)

function ConvertFrom-Utf8Base64 {
    param([string]$Value)

    [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($Value))
}

$forbiddenVisiblePhrases = @(
    "manual",
    "source",
    "sourceMeaning",
    "sourceReference",
    "documentCode",
    "manualId",
    "packageId",
    (ConvertFrom-Utf8Base64 "0L/QviDRgtCw0LHQu9C40YbQtSDRgNGD0LrQvtCy0L7QtNGB0YLQstCw"),
    (ConvertFrom-Utf8Base64 "0LrQu9Cw0YHRgdC40YTQuNGG0LjRgNC+0LLQsNC9INC/0L4g0YLQsNCx0LvQuNGG0LU="),
    (ConvertFrom-Utf8Base64 "0YLQtdC60YPRidCw0Y8g0LrQsNGA0YLQvtGH0LrQsA=="),
    (ConvertFrom-Utf8Base64 "0LTQuNCw0LPQvdC+0YHRgtC40YfQtdGB0LrQuNC5INCy0YvQstC+0LQg0LTQvtC70LbQtdC9INC+0YHRgtCw0LLQsNGC0YzRgdGP"),
    (ConvertFrom-Utf8Base64 "0YLQvtGH0L3QsNGPINC40YHRhdC+0LTQvdCw0Y8g0YTQvtGA0LzRg9C70LjRgNC+0LLQutCw"),
    (ConvertFrom-Utf8Base64 "0L3QtSDRgNCw0YHRiNC40YDRj9C50YLQtSDRgtGA0LDQutGC0L7QstC60YM="),
    (ConvertFrom-Utf8Base64 "0LTQsNC70YzQvdC10LnRiNC40LUg0LTQtdC50YHRgtCy0LjRjyDQstGL0L/QvtC70L3QuNGC0LUg0L/QviDRgdC10YDQstC40YHQvdC+0Lkg0L/RgNC+0YbQtdC00YPRgNC1")
)

$alreadyRepairedCodes = @(
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
    "E1",
    "E2",
    "E3",
    "E4",
    "F0",
    "F1",
    "F3",
    "F5",
    "F6",
    "F7",
    "F8",
    "F9",
    "FA",
    "FH",
    "FC",
    "FL",
    "FE",
    "FF",
    "FJ",
    "FU",
    "Fb",
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
    "HL",
    "J0",
    "J1",
    "J2",
    "J3",
    "J4",
    "J5",
    "J6",
    "J7",
    "J8",
    "J9",
    "P0",
    "P1",
    "P2",
    "P3",
    "P5",
    "P6",
    "P7",
    "P8",
    "P9",
    "PC",
    "PH",
    "PJ",
    "PL",
    "bb",
    "bd",
    "bE",
    "bF",
    "bH",
    "bJ",
    "bn",
    "bP",
    "bU",
    "E0",
    "Ed",
    "Fd",
    "Fn",
    "FP",
    "G0",
    "G1",
    "G2",
    "G3",
    "G4",
    "G5",
    "G6",
    "G7",
    "G8",
    "G9",
    "GA",
    "Gb",
    "GC",
    "Gd",
    "GE",
    "GF",
    "GH",
    "GJ",
    "GL",
    "Gn",
    "GP",
    "GU",
    "Gy",
    "H4",
    "HA",
    "HE",
    "HF",
    "HP",
    "HU",
    "JA",
    "JC",
    "JE",
    "JF",
    "JL",
    "P4",
    "PA",
    "PE",
    "PF",
    "PP",
    "PU",
    "d1",
    "d2",
    "d3",
    "d4",
    "d5",
    "d6",
    "d7",
    "d8",
    "d9",
    "dA",
    "db",
    "dC",
    "dd",
    "dE",
    "dF",
    "dH",
    "dJ",
    "dL",
    "dn",
    "dP",
    "dU",
    "dy",
    "L0",
    "L1",
    "L2",
    "L3",
    "L4",
    "L5",
    "L6",
    "L7",
    "L8",
    "L9",
    "LA",
    "Lb",
    "LC",
    "LE",
    "LF",
    "LH",
    "LJ",
    "LL",
    "LP",
    "LU",
    "o0",
    "o1",
    "o2",
    "o3",
    "o4",
    "o5",
    "o6",
    "o7",
    "o8",
    "o9",
    "oA",
    "ob",
    "oC",
    "y1",
    "y2",
    "y7",
    "y8",
    "yA",
    "A0",
    "A2",
    "A3",
    "A4",
    "A6",
    "A7",
    "A8",
    "A9",
    "Ab",
    "AC",
    "Ad",
    "AE",
    "AF",
    "AH",
    "AL",
    "An",
    "AP",
    "AU",
    "Ay",
    "n0",
    "n1",
    "n2",
    "n3",
    "n4",
    "n5",
    "n6",
    "n7",
    "n8",
    "n9",
    "nA",
    "nb",
    "nC",
    "nE",
    "nF",
    "nH",
    "nJ",
    "nn",
    "nU",
    "qA",
    "qC",
    "qH",
    "qP",
    "qU"
)
$alreadyRepairedLookup = @{}
foreach ($code in $alreadyRepairedCodes) {
    $alreadyRepairedLookup[$code] = $true
}

$noDetailedIndoorProcedureCodes = @(
    "d5",
    "d8",
    "db",
    "dE",
    "L2",
    "L6",
    "LH"
)
$noDetailedIndoorProcedureLookup = @{}
foreach ($code in $noDetailedIndoorProcedureCodes) {
    $noDetailedIndoorProcedureLookup[$code] = $true
}

function Join-TextValues {
    param([object[]]$Values)

    @($Values |
        Where-Object { $null -ne $_ -and -not [string]::IsNullOrWhiteSpace([string]$_) } |
        ForEach-Object { [string]$_ } |
        Sort-Object -Unique) -join " | "
}

function Read-JsonFile {
    param([string]$Path)

    [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8) |
        ConvertFrom-Json
}

function Get-TextArrayValues {
    param(
        [object[]]$Texts,
        [string]$PropertyName
    )

    foreach ($text in $Texts) {
        if ($null -eq $text.$PropertyName) {
            continue
        }

        foreach ($value in @($text.$PropertyName)) {
            if ($null -ne $value -and -not [string]::IsNullOrWhiteSpace([string]$value)) {
                [string]$value
            }
        }
    }
}

function Get-VisibleBlob {
    param([object[]]$Texts)

    $parts = foreach ($text in $Texts) {
        foreach ($field in $telegramFields) {
            if ($null -ne $text.$field) {
                @($text.$field) -join "`n"
            }
        }
    }

    @($parts | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join "`n"
}

function Get-RepairClass {
    param(
        [string]$Category,
        [string]$Code,
        [bool]$HasConflict,
        [bool]$HasTroubleshootingReference,
        [bool]$ManualSectionFound,
        [bool]$VisibleTextIsManualBoundSafe
    )

    if ($HasConflict) {
        return "Conflict"
    }

    if ($alreadyRepairedLookup.ContainsKey($Code) -and $VisibleTextIsManualBoundSafe) {
        return "AlreadyRepaired"
    }

    if ($Category -eq "indoor" -and $noDetailedIndoorProcedureLookup.ContainsKey($Code) -and $VisibleTextIsManualBoundSafe) {
        return "TableOnlySafe"
    }

    if ($Category -eq "status") {
        return "StatusOrPrompt"
    }

    if ($Category -eq "debugging") {
        return "DebuggingOrCommissioning"
    }

    if ($HasTroubleshootingReference) {
        return "DetailedProcedureAvailable"
    }

    if ($ManualSectionFound -and $VisibleTextIsManualBoundSafe) {
        return "TableOnlySafe"
    }

    return "NeedsManualReview"
}

$packages = @{}
foreach ($file in Get-ChildItem -LiteralPath $packageRoot -Filter "*.json" -File) {
    $package = Read-JsonFile -Path $file.FullName
    $packages[$package.packageId] = $package
}

$entries = foreach ($file in Get-ChildItem -LiteralPath $gmv6Root -Recurse -Filter "*.json" -File) {
    $entry = Read-JsonFile -Path $file.FullName
    $package = $packages[$entry.packageId]
    if ($null -eq $package) {
        throw "Missing package manifest for '$($entry.packageId)' used by '$($file.FullName)'."
    }

    $relativePath = $file.FullName.Substring($repoRoot.Length + 1).Replace("\", "/")
    $category = Split-Path (Split-Path $relativePath -Parent) -Leaf
    if (-not $expectedCategoryCounts.Contains($category)) {
        throw "Unexpected GMV6 category '$category' for '$relativePath'."
    }

    $texts = @($entry.texts)
    $visibleBlob = Get-VisibleBlob -Texts $texts
    $foundForbiddenPhrases = @($forbiddenVisiblePhrases | Where-Object {
        $visibleBlob.IndexOf($_, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
    })
    $hasVisibleText = -not [string]::IsNullOrWhiteSpace($visibleBlob)
    $visibleTextIsManualBoundSafe =
        $hasVisibleText -and
        $foundForbiddenPhrases.Count -eq 0 -and
        $entry.sourceType -eq "Manual" -and
        $entry.verificationStatus -eq "ManualVerified"

    $sourceReferences = @($entry.sourceReferences)
    $sourceReferenceNames = @($sourceReferences | ForEach-Object { $_.sourceName } | Where-Object { $_ } | Sort-Object -Unique)
    $sourceReferenceManualIds = @($sourceReferences | ForEach-Object { $_.manualId } | Where-Object { $_ } | Sort-Object -Unique)
    $sourceReferenceDocumentCodes = @($sourceReferences | ForEach-Object { $_.documentCode } | Where-Object { $_ } | Sort-Object -Unique)
    $sourceReferenceBlob = @(
        $entry.sourceReference
        $sourceReferences | ForEach-Object { $_.sourceReference }
    ) -join "`n"

    $manualSectionFound =
        -not [string]::IsNullOrWhiteSpace($entry.sourceReference) -or
        $sourceReferences.Count -gt 0
    $hasTroubleshootingReference =
        $sourceReferenceBlob.IndexOf("Troubleshooting", [System.StringComparison]::OrdinalIgnoreCase) -ge 0
    $manualHasFaultDiagnosis =
        if ($alreadyRepairedLookup.ContainsKey($entry.code)) { $true }
        elseif ($hasTroubleshootingReference) { $true }
        else { $null }
    $manualHasTroubleshooting =
        if ($alreadyRepairedLookup.ContainsKey($entry.code)) { $true }
        elseif ($hasTroubleshootingReference) { $true }
        else { $null }
    $manualHasFlowchart =
        if ($entry.code -match '^b[1-9A]$') { $true }
        elseif ($entry.code -eq "AJ") { $false }
        elseif ($hasTroubleshootingReference -and $category -in @("outdoor", "indoor")) { $null }
        else { $null }
    $manualHasPossibleCauses =
        if ($entry.code -match '^b[1-9A]$') { $true }
        elseif ($entry.code -eq "AJ") { $false }
        elseif ($category -eq "status") { $false }
        elseif ($hasTroubleshootingReference -and $category -in @("outdoor", "indoor", "debugging")) { $null }
        else { $null }

    $hasConflict = $false
    $classificationNotes = @()
    if ($sourceReferences.Count -gt 1) {
        $classificationNotes += "Multiple sourceReferences are present; treat as compatible unless a later manual review finds meaning divergence."
    }
    if ($category -eq "indoor" -and $noDetailedIndoorProcedureLookup.ContainsKey($entry.code)) {
        $classificationNotes += "Troubleshooting reference string is present, but the reviewed section is reserved, not applied, or status-only and has no detailed procedure to promote."
    }
    elseif ($hasTroubleshootingReference -and -not $alreadyRepairedLookup.ContainsKey($entry.code)) {
        $classificationNotes += "Troubleshooting section is referenced but visible procedure has not been promoted in ED-24SRC.3."
    }
    if (-not $manualSectionFound) {
        $classificationNotes += "No manual section/source reference is currently attached."
    }

    $repairClass = Get-RepairClass `
        -Category $category `
        -Code $entry.code `
        -HasConflict $hasConflict `
        -HasTroubleshootingReference $hasTroubleshootingReference `
        -ManualSectionFound $manualSectionFound `
        -VisibleTextIsManualBoundSafe $visibleTextIsManualBoundSafe

    [pscustomobject]@{
        code = $entry.code
        normalizedCode = ([string]$entry.code).ToUpperInvariant()
        category = $category
        packageId = $entry.packageId
        filePath = $relativePath
        sourceName = $entry.sourceName
        sourceReference = $entry.sourceReference
        sourceMeaning = $entry.sourceMeaning
        sourceReferenceNames = $sourceReferenceNames
        sourceReferenceManualIds = $sourceReferenceManualIds
        sourceReferenceDocumentCodes = $sourceReferenceDocumentCodes
        signalType = $entry.signalType
        equipmentType = $entry.equipmentType
        displaySource = $entry.displaySource
        verificationStatus = $entry.verificationStatus
        currentTitles = @(Get-TextArrayValues -Texts $texts -PropertyName "title")
        currentSummaries = @(Get-TextArrayValues -Texts $texts -PropertyName "summary")
        currentPossibleCauses = @(Get-TextArrayValues -Texts $texts -PropertyName "possibleCauses")
        currentCheckSteps = @(Get-TextArrayValues -Texts $texts -PropertyName "checkSteps")
        currentRecommendedActions = @(Get-TextArrayValues -Texts $texts -PropertyName "recommendedAction")
        visibleTextIsAlreadyManualBound = $visibleTextIsManualBoundSafe
        forbiddenVisiblePhrases = $foundForbiddenPhrases
        manualSectionFound = $manualSectionFound
        manualHasFaultDiagnosis = $manualHasFaultDiagnosis
        manualHasPossibleCauses = $manualHasPossibleCauses
        manualHasTroubleshooting = $manualHasTroubleshooting
        manualHasFlowchart = $manualHasFlowchart
        repairClass = $repairClass
        hasConflict = $hasConflict
        classificationNotes = $classificationNotes
    }
}

$entries = @($entries | Sort-Object category, normalizedCode, filePath)
$categoryCounts = [ordered]@{}
foreach ($category in $expectedCategoryCounts.Keys) {
    $categoryEntries = @($entries | Where-Object category -eq $category)
    $categoryCounts[$category] = $categoryEntries.Count
    if ($categoryEntries.Count -ne $expectedCategoryCounts[$category]) {
        throw "GMV6 category '$category' expected $($expectedCategoryCounts[$category]) cards but found $($categoryEntries.Count)."
    }
}
if ($entries.Count -ne 263) {
    throw "GMV6 total expected 263 cards but found $($entries.Count)."
}

$repairClassCounts = @($entries |
    Group-Object repairClass |
    Sort-Object Name |
    ForEach-Object {
        [pscustomobject]@{
            repairClass = $_.Name
            count = $_.Count
        }
    })

$categoryRepairClassCounts = foreach ($category in $expectedCategoryCounts.Keys) {
    $entries |
        Where-Object category -eq $category |
        Group-Object repairClass |
        Sort-Object Name |
        ForEach-Object {
            [pscustomobject]@{
                category = $category
                repairClass = $_.Name
                count = $_.Count
            }
        }
}

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$report = [pscustomobject]@{
    generatedAtUtc = [DateTime]::UtcNow.ToString("O")
    scope = "ED-24SRC.3 GMV6 closure inventory; no runtime card repair is performed by this script."
    totalGmv6Cards = $entries.Count
    categoryCounts = $categoryCounts
    repairClassCounts = $repairClassCounts
    categoryRepairClassCounts = @($categoryRepairClassCounts)
    entries = $entries
}
[System.IO.File]::WriteAllText(
    $jsonOutput,
    ($report | ConvertTo-Json -Depth 30) + [Environment]::NewLine,
    $utf8NoBom)

$csvRows = $entries | ForEach-Object {
    [pscustomobject]@{
        code = $_.code
        category = $_.category
        packageId = $_.packageId
        filePath = $_.filePath
        sourceName = $_.sourceName
        sourceReference = $_.sourceReference
        sourceMeaning = $_.sourceMeaning
        sourceReferenceNames = Join-TextValues -Values $_.sourceReferenceNames
        sourceReferenceManualIds = Join-TextValues -Values $_.sourceReferenceManualIds
        sourceReferenceDocumentCodes = Join-TextValues -Values $_.sourceReferenceDocumentCodes
        signalType = $_.signalType
        equipmentType = $_.equipmentType
        displaySource = $_.displaySource
        verificationStatus = $_.verificationStatus
        currentTitles = Join-TextValues -Values $_.currentTitles
        currentSummaries = Join-TextValues -Values $_.currentSummaries
        currentPossibleCauses = Join-TextValues -Values $_.currentPossibleCauses
        currentCheckSteps = Join-TextValues -Values $_.currentCheckSteps
        currentRecommendedActions = Join-TextValues -Values $_.currentRecommendedActions
        visibleTextIsAlreadyManualBound = $_.visibleTextIsAlreadyManualBound
        forbiddenVisiblePhrases = Join-TextValues -Values $_.forbiddenVisiblePhrases
        manualSectionFound = $_.manualSectionFound
        manualHasFaultDiagnosis = $_.manualHasFaultDiagnosis
        manualHasPossibleCauses = $_.manualHasPossibleCauses
        manualHasTroubleshooting = $_.manualHasTroubleshooting
        manualHasFlowchart = $_.manualHasFlowchart
        repairClass = $_.repairClass
        hasConflict = $_.hasConflict
        classificationNotes = Join-TextValues -Values $_.classificationNotes
    }
}
$csvText = $csvRows | ConvertTo-Csv -NoTypeInformation
[System.IO.File]::WriteAllLines($csvOutput, $csvText, $utf8NoBom)

Write-Output "Wrote $($entries.Count) GMV6 closure inventory rows."
Write-Output $jsonOutput
Write-Output $csvOutput
Write-Output "Category counts:"
$categoryCounts.GetEnumerator() | ForEach-Object { Write-Output "  $($_.Key): $($_.Value)" }
Write-Output "Repair class counts:"
$repairClassCounts | ForEach-Object { Write-Output "  $($_.repairClass): $($_.count)" }
