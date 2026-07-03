[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$knowledgeRoot = Join-Path $repoRoot "data\equipment-diagnostics\error-knowledge"
$gmvXRoot = Join-Path $knowledgeRoot "gree\gmv-x"
$packageRoot = Join-Path $knowledgeRoot "packages"
$outputRoot = Join-Path $repoRoot "artifacts\verification\equipment-diagnostics"
$jsonOutput = Join-Path $outputRoot "gmvx-manual-bound-closure-inventory.json"
$csvOutput = Join-Path $outputRoot "gmvx-manual-bound-closure-inventory.csv"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$expectedCategoryCounts = [ordered]@{
    outdoor = 121
    indoor = 60
    status = 44
    debugging = 38
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

function New-Lookup {
    param([string[]]$Values)

    $lookup = @{}
    foreach ($value in $Values) {
        $lookup[$value] = $true
    }

    $lookup
}

$detailedProcedureCodes = @(
    "b1", "b2", "b3", "b4", "b5", "b6", "b7", "b8", "b9", "bA", "bd", "bJ", "bn",
    "E1", "E2", "E3", "E4", "Ed",
    "F0", "F1", "F3", "F5", "F6", "F7", "F8", "F9", "FA", "Fb", "FC", "Fd", "FE", "FF", "FH", "FJ", "FL", "Fn", "FU",
    "H0", "H1", "H2", "H3", "H5", "H6", "H7", "H8", "H9", "HC", "HH", "HJ", "HL",
    "J0", "J1", "J2", "J3", "J4", "J5", "J6", "J7", "J8", "J9",
    "P0", "P1", "P2", "P3", "P5", "P6", "P7", "P8", "P9", "PC", "PH", "PJ", "PL",
    "d1", "d3", "d4", "d6", "d7", "d9", "dA", "dC", "dd", "dF", "dH", "dL", "dn", "dP",
    "L0", "L1", "L3", "L4", "L5", "L7", "L9", "LA", "LC", "LF", "LU",
    "o3", "o7", "o8", "o9", "y7", "y8", "yA",
    "C0", "C2", "C3", "C4", "C5", "C6", "Cb", "CC", "Cd", "CE", "CF", "CH", "CJ", "CL", "Cn", "CP", "Cy",
    "U0", "U2", "U3", "U4", "U6", "U8", "U9", "UE", "UF", "UL"
)
$statusOrPromptCodes = @(
    "A0", "A2", "A3", "A4", "A6", "A7", "A8", "Ab", "AC", "Ad", "AE", "AF", "AH", "AJ", "AP", "AU",
    "C8", "C9", "CA",
    "db",
    "n0", "n2", "n4", "n6", "n7", "n8", "n9", "nA", "nC", "nE", "nF", "nH",
    "UC"
)
$repairedDetailedCodes = @(
    "b1", "b2", "b3", "b4", "b5", "b6", "b7", "b8", "b9", "bA", "bd", "bJ", "bn",
    "E1", "E2", "E3", "E4", "Ed",
    "F0", "F1", "F3", "F5", "F6", "F7", "F8", "F9", "FA", "Fb", "FC", "Fd", "FE", "FF", "FH", "FJ", "FL", "Fn", "FU",
    "H0", "H1", "H2", "H3", "H5", "H6", "H7", "H8", "H9", "HC", "HH", "HJ", "HL",
    "J0", "J1", "J2", "J3", "J4", "J5", "J6", "J7", "J8", "J9",
    "P0", "P1", "P2", "P3", "P5", "P6", "P7", "P8", "P9", "PC", "PH", "PJ", "PL",
    "d1", "d3", "d4", "d6", "d7", "d9", "dA", "dC", "dd", "dF", "dH", "dL", "dn", "dP",
    "L0", "L1", "L3", "L4", "L5", "L7", "L9", "LA", "LC", "LF", "LU",
    "o3", "o7", "o8", "o9", "y7", "y8", "yA",
    "C0", "C2", "C3", "C4", "C5", "C6", "Cb", "CC", "Cd", "CE", "CF", "CH", "CJ", "CL", "Cn", "CP", "Cy",
    "U0", "U2", "U3", "U4", "U6", "U8", "U9", "UE", "UF", "UL"
)
$manualReviewResolutions = @{
    "d5" = [pscustomobject]@{
        disposition = "NotApplicableOrReserved"
        note = "Reserved heading only; no applicability, causes, or troubleshooting procedure is provided."
    }
    "d8" = [pscustomobject]@{
        disposition = "NotApplicableOrReserved"
        note = "Reserved heading only; no applicability, causes, or troubleshooting procedure is provided."
    }
    "dE" = [pscustomobject]@{
        disposition = "NotApplicableOrReserved"
        note = "Reserved heading only; no applicability, causes, or troubleshooting procedure is provided."
    }
    "L2" = [pscustomobject]@{
        disposition = "NotApplicableOrReserved"
        note = "Reserved code not yet applied; no causes or troubleshooting procedure is provided."
    }
    "L6" = [pscustomobject]@{
        disposition = "NonFaultSafe"
        note = "Troubleshooting heading is reserved, while Chapter 3 non-fault troubleshooting defines the mode-conflict behavior and safe mode-alignment action."
    }
    "LH" = [pscustomobject]@{
        disposition = "NotApplicableOrReserved"
        note = "Reserved code not yet applied; no causes or troubleshooting procedure is provided."
    }
}
$manualSectionNeedsReviewCodes = @()
$repairedTableOnlyCodes = @(
    "bb", "bE", "bF", "bH", "bP", "bU", "E0", "FP",
    "G0", "G1", "G2", "G3", "G4", "G5", "G6", "G7", "G8", "G9",
    "GA", "Gb", "GC", "Gd", "GE", "GF", "GH", "GJ", "GL", "Gn", "GP", "GU", "Gy",
    "H4", "HA", "HE", "HF", "HP", "HU",
    "JA", "JC", "JE", "JF", "JL",
    "P4", "PA", "PE", "PF", "PP", "PU"
)

$detailedProcedureLookup = New-Lookup -Values $detailedProcedureCodes
$statusOrPromptLookup = New-Lookup -Values $statusOrPromptCodes
$repairedDetailedLookup = New-Lookup -Values $repairedDetailedCodes
$manualSectionNeedsReviewLookup = New-Lookup -Values $manualSectionNeedsReviewCodes
$resolvedManualReviewLookup = New-Lookup -Values @($manualReviewResolutions.Keys)
$repairedTableOnlyLookup = New-Lookup -Values $repairedTableOnlyCodes

$genericVisibleTemplatePhrases = @(
    (ConvertFrom-Utf8Base64 "0J/QvtC00YLQstC10YDQtNC40YLQtSDQutC+0LQ="),
    (ConvertFrom-Utf8Base64 "0KHQstC10YDRjNGC0LUg0LzQvtC00LXQu9GM"),
    (ConvertFrom-Utf8Base64 "0JTQsNC70YzQvdC10LnRiNC40LUg0LTQtdC50YHRgtCy0LjRjw=="),
    (ConvertFrom-Utf8Base64 "0KLQvtGH0L3QsNGPINC/0YDQuNGH0LjQvdCwINC30LDQstC40YHQuNGC")
)
$sourceLeakagePhrases = @(
    "source",
    "manual",
    "packageId",
    (ConvertFrom-Utf8Base64 "0YDRg9C60L7QstC+0LTRgdGC0LLQvg=="),
    (ConvertFrom-Utf8Base64 "0L7RgdC90L7QstCw0L3QuNC1")
)
$tableProvenanceLeakagePhrases = @(
    (ConvertFrom-Utf8Base64 "0L/QviDRgtCw0LHQu9C40YbQtQ=="),
    (ConvertFrom-Utf8Base64 "0LrQu9Cw0YHRgdC40YTQuNGG0LjRgNC+0LLQsNC9INC/0L4g0YLQsNCx0LvQuNGG0LU=")
)
$faultWordingPhrases = @(
    (ConvertFrom-Utf8Base64 "0LDQstCw0YDQuNGP"),
    (ConvertFrom-Utf8Base64 "0L3QtdC40YHQv9GA0LDQstC90L7RgdGC0Yw=")
)

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

function Test-ContainsAny {
    param(
        [string]$Value,
        [string[]]$Phrases
    )

    foreach ($phrase in $Phrases) {
        if ($Value.IndexOf($phrase, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            return $true
        }
    }

    return $false
}

function Get-MatchedPhrases {
    param(
        [string]$Value,
        [string[]]$Phrases
    )

    @($Phrases | Where-Object {
        $Value.IndexOf($_, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
    })
}

function Get-RepairClass {
    param([string]$Code)

    $classes = @()
    if ($detailedProcedureLookup.ContainsKey($Code)) {
        $classes += "DetailedProcedureAvailable"
    }
    if ($statusOrPromptLookup.ContainsKey($Code)) {
        $classes += "StatusOrPrompt"
    }
    if ($manualSectionNeedsReviewLookup.ContainsKey($Code)) {
        $classes += "ManualSectionNeedsReview"
    }

    if ($classes.Count -gt 1) {
        return "Conflict"
    }

    if ($classes.Count -eq 1) {
        return $classes[0]
    }

    return "TableOnlySafe"
}

function Get-ManualSection {
    param(
        [string]$RepairClass,
        [object]$Entry
    )

    if (-not [string]::IsNullOrWhiteSpace($Entry.sourceReference)) {
        return $Entry.sourceReference
    }

    if ($RepairClass -eq "DetailedProcedureAvailable") {
        return "Chapter 3 Faults / 2 Troubleshooting"
    }

    if ($RepairClass -eq "StatusOrPrompt" -or $RepairClass -eq "TableOnlySafe") {
        return "Chapter 3 Faults / 1 Error Indication"
    }

    if ($RepairClass -eq "ManualSectionNeedsReview") {
        return "Chapter 3 Faults / 2 Troubleshooting / needs manual section review"
    }

    return ""
}

function Get-ManualSectionTitle {
    param(
        [string]$RepairClass,
        [object]$Entry
    )

    if ($RepairClass -eq "DetailedProcedureAvailable") {
        return "Troubleshooting procedure available"
    }

    if ($RepairClass -eq "StatusOrPrompt") {
        return "Status, prompt, debugging, or commissioning indication"
    }

    if ($RepairClass -eq "ManualSectionNeedsReview") {
        return "Manual section needs review before repair"
    }

    if (-not [string]::IsNullOrWhiteSpace($Entry.sourceMeaning)) {
        return $Entry.sourceMeaning
    }

    return "Error indication table row"
}

function Get-VisibleTextFlags {
    param(
        [string]$RepairClass,
        [string]$Category,
        [object[]]$Texts,
        [string]$VisibleBlob
    )

    $flags = @()
    if (Test-ContainsAny -Value $VisibleBlob -Phrases $genericVisibleTemplatePhrases) {
        $flags += "GenericVisibleTemplate"
    }
    if (Test-ContainsAny -Value $VisibleBlob -Phrases $sourceLeakagePhrases) {
        $flags += "SourceLeakage"
    }
    if (Test-ContainsAny -Value $VisibleBlob -Phrases $tableProvenanceLeakagePhrases) {
        $flags += "TableProvenanceLeakage"
    }

    $titles = @(Get-TextArrayValues -Texts $Texts -PropertyName "title")
    foreach ($title in $titles) {
        if ($title.IndexOf("Gree GMV ", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and
            $title.IndexOf("Gree GMV X", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            $flags += "GenericSeriesTitle"
            break
        }
    }

    if (($Category -eq "status" -or $Category -eq "debugging" -or $RepairClass -eq "StatusOrPrompt") -and
        (Test-ContainsAny -Value $VisibleBlob -Phrases $faultWordingPhrases)) {
        $flags += "StatusAsFault"
    }

    if ($RepairClass -eq "DetailedProcedureAvailable" -and
        (Test-ContainsAny -Value $VisibleBlob -Phrases $genericVisibleTemplatePhrases)) {
        $flags += "NeedsManualBinding"
    }

    @($flags | Sort-Object -Unique)
}

$packages = @{}
foreach ($file in Get-ChildItem -LiteralPath $packageRoot -Filter "*.json" -File) {
    $package = Read-JsonFile -Path $file.FullName
    $packages[$package.packageId] = $package
}

$entries = foreach ($file in Get-ChildItem -LiteralPath $gmvXRoot -Recurse -Filter "*.json" -File) {
    $entry = Read-JsonFile -Path $file.FullName
    $package = $packages[$entry.packageId]
    if ($null -eq $package) {
        throw "Missing package manifest for '$($entry.packageId)' used by '$($file.FullName)'."
    }

    $relativePath = $file.FullName.Substring($repoRoot.Length + 1).Replace("\", "/")
    $category = Split-Path (Split-Path $relativePath -Parent) -Leaf
    if (-not $expectedCategoryCounts.Contains($category)) {
        throw "Unexpected GMV X category '$category' for '$relativePath'."
    }

    $texts = @($entry.texts)
    $visibleBlob = Get-VisibleBlob -Texts $texts
    $repairClass = Get-RepairClass -Code $entry.code
    $preRepairClass = $repairClass
    $visibleTextFlags = @(Get-VisibleTextFlags `
        -RepairClass $repairClass `
        -Category $category `
        -Texts $texts `
        -VisibleBlob $visibleBlob)
    if ($statusOrPromptLookup.ContainsKey($entry.code) -and $visibleTextFlags.Count -eq 0) {
        $repairClass = "AlreadyRepaired"
    }
    if ($repairedDetailedLookup.ContainsKey($entry.code) -and $visibleTextFlags.Count -eq 0) {
        $repairClass = "AlreadyRepaired"
    }
    if ($resolvedManualReviewLookup.ContainsKey($entry.code) -and $visibleTextFlags.Count -eq 0) {
        $repairClass = "AlreadyRepaired"
    }
    if ($repairedTableOnlyLookup.ContainsKey($entry.code) -and $visibleTextFlags.Count -eq 0) {
        $repairClass = "AlreadyRepaired"
    }
    $sourceReferences = @($entry.sourceReferences)
    $sourceReferenceNames = @($sourceReferences | ForEach-Object { $_.sourceName } | Where-Object { $_ } | Sort-Object -Unique)
    $sourceReferenceManualIds = @($sourceReferences | ForEach-Object { $_.manualId } | Where-Object { $_ } | Sort-Object -Unique)
    $sourceReferenceDocumentCodes = @($sourceReferences | ForEach-Object { $_.documentCode } | Where-Object { $_ } | Sort-Object -Unique)
    $classificationNotes = @()
    $reviewResolution = $manualReviewResolutions[$entry.code]

    if ($null -ne $reviewResolution) {
        $classificationNotes += $reviewResolution.note
    }
    elseif ($repairClass -eq "AlreadyRepaired" -and $repairedTableOnlyLookup.ContainsKey($entry.code)) {
        $classificationNotes += "GMV X table-only visible text has been repaired without promoting a detailed troubleshooting procedure."
    }
    elseif ($repairClass -eq "AlreadyRepaired" -and $preRepairClass -eq "StatusOrPrompt") {
        $classificationNotes += "GMV X status/prompt visible text has been repaired in ED-24GMVX.2."
    }
    elseif ($repairClass -eq "AlreadyRepaired" -and $preRepairClass -eq "DetailedProcedureAvailable") {
        $classificationNotes += "GMV X detailed visible text has been repaired in a controlled GMV X batch."
    }
    elseif ($repairClass -eq "DetailedProcedureAvailable") {
        $classificationNotes += "GMV X manual inventory expects troubleshooting content; runtime visible text is not repaired in ED-24GMVX.1."
    }
    elseif ($repairClass -eq "StatusOrPrompt") {
        $classificationNotes += "Treat as status, prompt, debugging, or commissioning information rather than a normal fault repair batch."
    }
    elseif ($repairClass -eq "ManualSectionNeedsReview") {
        $classificationNotes += "Manual section boundary must be reviewed before choosing detailed or table-only repair text."
    }
    elseif ($repairClass -eq "TableOnlySafe") {
        $classificationNotes += "Current GMV X inventory boundary has a table indication but no promoted troubleshooting procedure."
    }

    [pscustomobject]@{
        packageId = $entry.packageId
        category = $category
        relativePath = $relativePath
        code = $entry.code
        normalizedCode = ([string]$entry.code).ToUpperInvariant()
        title = Join-TextValues -Values @(Get-TextArrayValues -Texts $texts -PropertyName "title")
        signalType = $entry.signalType
        textAudiencesPresent = @($texts | ForEach-Object { $_.audience } | Where-Object { $_ } | Sort-Object -Unique)
        repairClass = $repairClass
        reviewDisposition = if ($null -ne $reviewResolution) { $reviewResolution.disposition } else { "" }
        manualSection = Get-ManualSection -RepairClass $repairClass -Entry $entry
        manualSectionTitle = Get-ManualSectionTitle -RepairClass $repairClass -Entry $entry
        notes = $classificationNotes
        visibleTextFlags = $visibleTextFlags
        sourceName = $entry.sourceName
        sourceReference = $entry.sourceReference
        sourceMeaning = $entry.sourceMeaning
        sourceReferenceNames = $sourceReferenceNames
        sourceReferenceManualIds = $sourceReferenceManualIds
        sourceReferenceDocumentCodes = $sourceReferenceDocumentCodes
        equipmentType = $entry.equipmentType
        displaySource = $entry.displaySource
        verificationStatus = $entry.verificationStatus
        currentSummaries = @(Get-TextArrayValues -Texts $texts -PropertyName "summary")
        currentPossibleCauses = @(Get-TextArrayValues -Texts $texts -PropertyName "possibleCauses")
        currentCheckSteps = @(Get-TextArrayValues -Texts $texts -PropertyName "checkSteps")
        currentRecommendedActions = @(Get-TextArrayValues -Texts $texts -PropertyName "recommendedAction")
        genericVisibleTemplatePhrases = @(Get-MatchedPhrases -Value $visibleBlob -Phrases $genericVisibleTemplatePhrases)
        sourceLeakagePhrases = @(Get-MatchedPhrases -Value $visibleBlob -Phrases $sourceLeakagePhrases)
        tableProvenanceLeakagePhrases = @(Get-MatchedPhrases -Value $visibleBlob -Phrases $tableProvenanceLeakagePhrases)
    }
}

$entries = @($entries | Sort-Object category, normalizedCode, relativePath)
$categoryCounts = [ordered]@{}
foreach ($category in $expectedCategoryCounts.Keys) {
    $categoryEntries = @($entries | Where-Object category -eq $category)
    $categoryCounts[$category] = $categoryEntries.Count
    if ($categoryEntries.Count -ne $expectedCategoryCounts[$category]) {
        throw "GMV X category '$category' expected $($expectedCategoryCounts[$category]) cards but found $($categoryEntries.Count)."
    }
}
if ($entries.Count -ne 263) {
    throw "GMV X total expected 263 cards but found $($entries.Count)."
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

$visibleTextFlagCounts = @($entries |
    ForEach-Object { $_.visibleTextFlags } |
    Group-Object |
    Sort-Object Name |
    ForEach-Object {
        [pscustomobject]@{
            flag = $_.Name
            count = $_.Count
        }
    })

$categoryClosure = [ordered]@{}
foreach ($category in $expectedCategoryCounts.Keys) {
    $categoryClosure[$category] =
        @($entries | Where-Object {
            $_.category -eq $category -and $_.repairClass -ne "AlreadyRepaired"
        }).Count -eq 0
}
$gmvXClosed = @($entries | Where-Object repairClass -ne "AlreadyRepaired").Count -eq 0
$totalGreeCards = @(Get-ChildItem -LiteralPath (Join-Path $knowledgeRoot "gree") -Recurse -Filter "*.json" -File).Count

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$report = [pscustomobject]@{
    generatedAtUtc = [DateTime]::UtcNow.ToString("O")
    scope = "ED-24GMVX.1 GMV X manual-bound closure inventory; no runtime card repair is performed by this script."
    manual = "artifacts/manual-intake/sources/gree/Gree GMV X Service Manual EN.pdf"
    totalGreeCards = $totalGreeCards
    totalGmvXCards = $entries.Count
    categoryCounts = $categoryCounts
    categoryClosure = $categoryClosure
    gmvXClosed = $gmvXClosed
    repairClassCounts = $repairClassCounts
    categoryRepairClassCounts = @($categoryRepairClassCounts)
    visibleTextFlagCounts = $visibleTextFlagCounts
    entries = $entries
}
[System.IO.File]::WriteAllText(
    $jsonOutput,
    ($report | ConvertTo-Json -Depth 30) + [Environment]::NewLine,
    $utf8NoBom)

$csvRows = $entries | ForEach-Object {
    [pscustomobject]@{
        packageId = $_.packageId
        category = $_.category
        relativePath = $_.relativePath
        code = $_.code
        title = $_.title
        signalType = $_.signalType
        textAudiencesPresent = Join-TextValues -Values $_.textAudiencesPresent
        repairClass = $_.repairClass
        reviewDisposition = $_.reviewDisposition
        manualSection = $_.manualSection
        manualSectionTitle = $_.manualSectionTitle
        notes = Join-TextValues -Values $_.notes
        visibleTextFlags = Join-TextValues -Values $_.visibleTextFlags
        sourceName = $_.sourceName
        sourceReference = $_.sourceReference
        sourceMeaning = $_.sourceMeaning
        sourceReferenceNames = Join-TextValues -Values $_.sourceReferenceNames
        sourceReferenceManualIds = Join-TextValues -Values $_.sourceReferenceManualIds
        sourceReferenceDocumentCodes = Join-TextValues -Values $_.sourceReferenceDocumentCodes
        equipmentType = $_.equipmentType
        displaySource = $_.displaySource
        verificationStatus = $_.verificationStatus
        currentSummaries = Join-TextValues -Values $_.currentSummaries
        currentPossibleCauses = Join-TextValues -Values $_.currentPossibleCauses
        currentCheckSteps = Join-TextValues -Values $_.currentCheckSteps
        currentRecommendedActions = Join-TextValues -Values $_.currentRecommendedActions
        genericVisibleTemplatePhrases = Join-TextValues -Values $_.genericVisibleTemplatePhrases
        sourceLeakagePhrases = Join-TextValues -Values $_.sourceLeakagePhrases
        tableProvenanceLeakagePhrases = Join-TextValues -Values $_.tableProvenanceLeakagePhrases
    }
}
$csvText = $csvRows | ConvertTo-Csv -NoTypeInformation
[System.IO.File]::WriteAllLines($csvOutput, $csvText, $utf8NoBom)

Write-Output "Wrote $($entries.Count) GMV X closure inventory rows."
Write-Output $jsonOutput
Write-Output $csvOutput
Write-Output "Category counts:"
$categoryCounts.GetEnumerator() | ForEach-Object { Write-Output "  $($_.Key): $($_.Value)" }
Write-Output "Repair class counts:"
$repairClassCounts | ForEach-Object { Write-Output "  $($_.repairClass): $($_.count)" }
Write-Output "Visible text flag counts:"
$visibleTextFlagCounts | ForEach-Object { Write-Output "  $($_.flag): $($_.count)" }
Write-Output "GMV X CLOSED: $gmvXClosed"
