param(
    [string]$RepoRoot = (Resolve-Path ".").Path,
    [string]$InventoryRoot = "",
    [string]$StagingRoot = "",
    [switch]$Apply
)

$ErrorActionPreference = "Stop"

function Repo-Path([string]$RelativePath) {
    return Join-Path $RepoRoot ($RelativePath -replace '/', '\')
}

function Rel-Path([string]$FullPath) {
    $root = (Resolve-Path $RepoRoot).Path.TrimEnd('\', '/')
    $full = (Resolve-Path $FullPath).Path
    if ($full.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $full.Substring($root.Length).TrimStart('\', '/') -replace '\\', '/'
    }
    return $full -replace '\\', '/'
}

function Read-JsonFile([string]$Path) {
    return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
}

function Write-Utf8Json([string]$Path, $Value) {
    if ($Value -is [array] -and $Value.Count -eq 0) {
        $json = "[]"
    } else {
        $json = $Value | ConvertTo-Json -Depth 100
    }
    [System.IO.File]::WriteAllText($Path, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
}

function Write-Utf8Text([string]$Path, [string]$Text) {
    [System.IO.File]::WriteAllText($Path, $Text, [System.Text.UTF8Encoding]::new($false))
}

function Get-CodeKey([string]$Code) {
    if ($null -eq $Code) { return "" }
    return $Code.Trim()
}

function Get-ProposedCategory([string]$Code) {
    if ([string]::IsNullOrWhiteSpace($Code)) { return "manual-review" }
    if ($Code -match '^[AaNn]') { return "status" }
    if ($Code -match '^[CcUu]') { return "debugging" }
    if ($Code -match '^[DdLlOoYy]') { return "indoor" }
    return "outdoor"
}

function Get-ProposedPackage([string]$Category) {
    switch ($Category) {
        "status" { return "gree-gmv6-status-codes" }
        "debugging" { return "gree-gmv6-debugging-codes" }
        "indoor" { return "gree-gmv6-indoor-fault-codes" }
        "outdoor" { return "gree-gmv6-outdoor-fault-protection-codes" }
        default { return "" }
    }
}

function Get-ProposedRuntimePath([string]$Code) {
    $category = Get-ProposedCategory $Code
    if ($category -eq "manual-review") { return "" }
    $fileName = ($Code.ToLowerInvariant()) + ".json"
    return "data/equipment-diagnostics/error-knowledge/gree/gmv6/$category/$fileName"
}

function Has-MeaningfulReviewText($Review) {
    $title = [string]$Review.normalizedRu.titleRu
    $meaning = [string]$Review.normalizedRu.meaningRu
    $safe = [string]$Review.normalizedRu.userSafeAnswerRu
    return -not [string]::IsNullOrWhiteSpace($title) -and
        -not [string]::IsNullOrWhiteSpace($meaning) -and
        -not [string]::IsNullOrWhiteSpace($safe)
}

function Get-BlockReason($Review, [bool]$RuntimeExists, [bool]$MiniExists) {
    if ($RuntimeExists) { return "already-runtime" }

    $reasons = New-Object System.Collections.Generic.List[string]
    $status = [string]$Review.status
    $extractionStatus = [string]$Review.review.extractionStatus
    $code = [string]$Review.code

    if ($status -eq "review-template" -or $extractionStatus -eq "not-started") {
        $reasons.Add("review-template-not-extracted")
    }

    if (-not (Has-MeaningfulReviewText $Review)) {
        $reasons.Add("missing-normalized-runtime-text")
    }

    if ($Review.review.approved -ne $true) {
        $reasons.Add("not-approved")
    }

    if ($Review.botCandidate.readyForBotKnowledge -ne $true) {
        $reasons.Add("not-ready-for-bot-knowledge")
    }

    if ($code -in @("o1", "O1", "01", "Ho", "No")) {
        $reasons.Add("visual-ambiguity-requires-manual-review")
    }

    if ($MiniExists) {
        $reasons.Add("gmv-mini-code-conflict")
    }

    if ($reasons.Count -eq 0) {
        return ""
    }

    return ($reasons | Sort-Object -Unique) -join ";"
}

if ([string]::IsNullOrWhiteSpace($InventoryRoot)) {
    $InventoryRoot = Repo-Path ".ae-tools/ED-24GEC.8A-remaining-gmv-inventory"
}

if ([string]::IsNullOrWhiteSpace($StagingRoot)) {
    $StagingRoot = Repo-Path "data/reference/gree-official-support-error-catalog/staging/remaining-runtime-candidates"
}

if (-not (Test-Path $RepoRoot)) {
    throw "RepoRoot not found: $RepoRoot"
}

New-Item -ItemType Directory -Force -Path $InventoryRoot | Out-Null
New-Item -ItemType Directory -Force -Path $StagingRoot | Out-Null

$reviewRoot = Repo-Path "data/reference/gree-official-support-error-catalog/review"
$runtimeRoot = Repo-Path "data/equipment-diagnostics/error-knowledge/gree/gmv6"
$miniRoot = Repo-Path "data/equipment-diagnostics/error-knowledge/gree/gmv-mini"

$reviewCards = Get-ChildItem -LiteralPath $reviewRoot -Filter "*.review.json" -File |
    Sort-Object Name |
    ForEach-Object {
        $json = Read-JsonFile $_.FullName
        [pscustomobject]@{
            Code = Get-CodeKey $json.code
            ReviewId = if ($json.PSObject.Properties.Name -contains "reviewId") { [string]$json.reviewId } else { "Gree-GMV-$($json.code)" }
            Status = [string]$json.status
            ExtractionStatus = [string]$json.review.extractionStatus
            Approved = ($json.review.approved -eq $true)
            ReadyForBotKnowledge = ($json.botCandidate.readyForBotKnowledge -eq $true)
            RuntimeEnabled = ($json.runtimeEnabled -eq $true)
            SourcePath = Rel-Path $_.FullName
            RawCardPath = [string]$json.source.primaryRawCardPath
            Json = $json
        }
    }

$runtimeEntries = Get-ChildItem -LiteralPath $runtimeRoot -Filter "*.json" -Recurse -File |
    Sort-Object FullName |
    ForEach-Object {
        $json = Read-JsonFile $_.FullName
        [pscustomobject]@{
            Code = Get-CodeKey $json.code
            Id = [string]$json.id
            Path = Rel-Path $_.FullName
            Category = Split-Path (Split-Path $_.FullName -Parent) -Leaf
            PackageId = [string]$json.packageId
            EquipmentType = [string]$json.equipmentType
            SignalType = [string]$json.signalType
            DisplaySource = [string]$json.displaySource
            HasOfficialSupportReference = @($json.sourceReferences | Where-Object { $_.manualId -eq "gree-official-support-error-catalog" }).Count -gt 0
        }
    }

$miniEntries = @()
if (Test-Path $miniRoot) {
    $miniEntries = Get-ChildItem -LiteralPath $miniRoot -Filter "*.json" -Recurse -File |
        Sort-Object FullName |
        ForEach-Object {
            $json = Read-JsonFile $_.FullName
            [pscustomobject]@{
                Code = Get-CodeKey $json.code
                Id = [string]$json.id
                Path = Rel-Path $_.FullName
                PackageId = [string]$json.packageId
            }
        }
}

$runtimeCodes = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($entry in $runtimeEntries) { [void]$runtimeCodes.Add($entry.Code) }

$miniCodes = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($entry in $miniEntries) { [void]$miniCodes.Add($entry.Code) }

$rows = foreach ($card in $reviewCards) {
    $runtimeMatches = @($runtimeEntries | Where-Object { $_.Code -eq $card.Code })
    $miniMatches = @($miniEntries | Where-Object { $_.Code -eq $card.Code })
    $runtimeExists = $runtimeMatches.Count -gt 0
    $miniExists = $miniMatches.Count -gt 0
    $proposedCategory = Get-ProposedCategory $card.Code
    $proposedPackage = Get-ProposedPackage $proposedCategory
    $proposedRuntimePath = Get-ProposedRuntimePath $card.Code
    $blockReason = Get-BlockReason $card.Json $runtimeExists $miniExists
    $safe = -not $runtimeExists -and [string]::IsNullOrWhiteSpace($blockReason)

    [pscustomobject]@{
        code = $card.Code
        reviewId = $card.ReviewId
        sourcePath = $card.SourcePath
        rawCardPath = $card.RawCardPath
        reviewStatus = $card.Status
        extractionStatus = $card.ExtractionStatus
        approved = $card.Approved
        readyForBotKnowledge = $card.ReadyForBotKnowledge
        alreadyRuntime = $runtimeExists
        runtimePath = if ($runtimeExists) { ($runtimeMatches | Select-Object -First 1).Path } else { "" }
        runtimeCategory = if ($runtimeExists) { ($runtimeMatches | Select-Object -First 1).Category } else { "" }
        runtimePackageId = if ($runtimeExists) { ($runtimeMatches | Select-Object -First 1).PackageId } else { "" }
        hasOfficialSupportReference = if ($runtimeExists) { ($runtimeMatches | Where-Object HasOfficialSupportReference).Count -gt 0 } else { $false }
        miniConflict = $miniExists
        miniRuntimePath = if ($miniExists) { ($miniMatches | Select-Object -First 1).Path } else { "" }
        visualAmbiguity = ($card.Code -in @("o1", "O1", "01", "Ho", "No"))
        proposedRuntimePath = $proposedRuntimePath
        proposedCategory = $proposedCategory
        proposedPackageId = $proposedPackage
        decision = if ($runtimeExists) { "already-runtime" } elseif ($safe) { "safe-to-generate" } else { "blocked-manual-review" }
        blockReason = $blockReason
    }
}

$rows = @($rows | Sort-Object code)
$safeRows = @($rows | Where-Object { $_.decision -eq "safe-to-generate" })
$blockedRows = @($rows | Where-Object { $_.decision -eq "blocked-manual-review" })
$alreadyRows = @($rows | Where-Object { $_.decision -eq "already-runtime" })

if ($Apply -and $safeRows.Count -gt 0) {
    throw "Apply mode is intentionally not implemented for non-empty candidates. Review staging output first."
}

$duplicateReviewCodes = @(
    $reviewCards |
        Group-Object Code |
        Where-Object Count -gt 1 |
        Sort-Object Name |
        ForEach-Object { [pscustomobject]@{ code = $_.Name; count = $_.Count } }
)

$duplicateRuntimeCodes = @(
    $runtimeEntries |
        Group-Object Code |
        Where-Object Count -gt 1 |
        Sort-Object Name |
        ForEach-Object { [pscustomobject]@{ code = $_.Name; count = $_.Count } }
)

$categoryConflicts = @(
    $runtimeEntries |
        Group-Object Code |
        Where-Object { @($_.Group | Select-Object -ExpandProperty Category -Unique).Count -gt 1 } |
        Sort-Object Name |
        ForEach-Object {
            [pscustomobject]@{
                code = $_.Name
                categories = (@($_.Group | Select-Object -ExpandProperty Category -Unique | Sort-Object) -join ";")
                paths = (@($_.Group | Select-Object -ExpandProperty Path | Sort-Object) -join ";")
            }
        }
)

$miniConflicts = @($rows | Where-Object miniConflict | Select-Object code, reviewId, miniRuntimePath, proposedRuntimePath, decision, blockReason)
$visualAmbiguities = @($rows | Where-Object visualAmbiguity | Select-Object code, reviewId, alreadyRuntime, runtimePath, decision, blockReason)

$summary = [ordered]@{
    schemaVersion = 1
    stage = "ED-24GEC.8"
    generatedUtc = "2026-06-26T00:00:00Z"
    reviewCardCount = $reviewCards.Count
    distinctReviewCodeCount = @($reviewCards | Select-Object -ExpandProperty Code -Unique).Count
    runtimeGmv6EntryCount = $runtimeEntries.Count
    distinctRuntimeGmv6CodeCount = @($runtimeEntries | Select-Object -ExpandProperty Code -Unique).Count
    alreadyRuntimeCount = $alreadyRows.Count
    remainingNotRuntimeCount = ($safeRows.Count + $blockedRows.Count)
    safeToGenerateCount = $safeRows.Count
    blockedManualReviewCount = $blockedRows.Count
    officialSupportRuntimeReferenceCount = @($runtimeEntries | Where-Object HasOfficialSupportReference).Count
    duplicateReviewCodes = $duplicateReviewCodes
    duplicateRuntimeCodes = $duplicateRuntimeCodes
    categoryConflicts = $categoryConflicts
    miniVsGmv6Conflicts = $miniConflicts
    visualAmbiguities = $visualAmbiguities
    rows = $rows
}

$inventoryJsonPath = Join-Path $InventoryRoot "inventory.json"
$inventoryCsvPath = Join-Path $InventoryRoot "inventory.csv"
$blockedJsonPath = Join-Path $InventoryRoot "blocked-manual-review.json"
$blockedCsvPath = Join-Path $InventoryRoot "blocked-manual-review.csv"
$safeJsonPath = Join-Path $InventoryRoot "safe-to-generate.json"
$summaryPath = Join-Path $InventoryRoot "summary.md"

Write-Utf8Json $inventoryJsonPath $summary
$rows | Export-Csv -LiteralPath $inventoryCsvPath -NoTypeInformation -Encoding UTF8
Write-Utf8Json $blockedJsonPath $blockedRows
$blockedRows | Export-Csv -LiteralPath $blockedCsvPath -NoTypeInformation -Encoding UTF8
Write-Utf8Json $safeJsonPath $safeRows

$stagePreview = [ordered]@{
    schemaVersion = 1
    stage = "ED-24GEC.8"
    status = "remaining-runtime-candidates-preview"
    generatedUtc = $summary.generatedUtc
    runtimeEnabled = $false
    diagnosticsRuntimeEnabled = $false
    candidates = $safeRows
    blockedManualReview = $blockedRows
    sourceToRuntimeMapping = $rows | Select-Object code, reviewId, sourcePath, rawCardPath, decision, runtimePath, proposedRuntimePath, proposedPackageId, blockReason
}

Write-Utf8Json (Join-Path $StagingRoot "remaining-runtime-candidates-preview.json") $stagePreview
Write-Utf8Json (Join-Path $StagingRoot "candidate-runtime-json.json") @()
$stagePreview.sourceToRuntimeMapping | Export-Csv -LiteralPath (Join-Path $StagingRoot "source-to-runtime-mapping.csv") -NoTypeInformation -Encoding UTF8

$blockedList = if ($blockedRows.Count -eq 0) {
    "None."
} else {
    ($blockedRows | ForEach-Object { "- $($_.code): $($_.blockReason)" }) -join [Environment]::NewLine
}

$summaryText = @"
# ED-24GEC.8A Remaining GMV Inventory

Generated UTC: $($summary.generatedUtc)

## Counts

- Official support review cards: $($summary.reviewCardCount)
- Distinct review codes: $($summary.distinctReviewCodeCount)
- GMV6 runtime entries: $($summary.runtimeGmv6EntryCount)
- Distinct GMV6 runtime codes: $($summary.distinctRuntimeGmv6CodeCount)
- Already present in GMV6 runtime: $($summary.alreadyRuntimeCount)
- Remaining not in GMV6 runtime: $($summary.remainingNotRuntimeCount)
- Safe to generate now: $($summary.safeToGenerateCount)
- Blocked/manual-review: $($summary.blockedManualReviewCount)
- Runtime entries with official support references: $($summary.officialSupportRuntimeReferenceCount)

## Decision

No runtime files are generated or applied when `safeToGenerateCount` is 0. Remaining not-runtime cards are templates or otherwise not approved for bot knowledge.

## Blocked / Manual Review

$blockedList
"@

Write-Utf8Text $summaryPath ($summaryText + [Environment]::NewLine)

$stagingReadme = @"
# ED-24GEC.8 Remaining Runtime Candidates

This folder is generated by `tools/gree-support/generate-gree-gmv-remaining-runtime-candidates.ps1`.

The preview is deterministic and does not enable diagnostics runtime entries. Runtime JSON files are not written from this folder automatically.

## Current Result

- Safe runtime candidates: $($summary.safeToGenerateCount)
- Blocked/manual-review cards: $($summary.blockedManualReviewCount)
- Already present in GMV6 runtime: $($summary.alreadyRuntimeCount)

Files:

- `remaining-runtime-candidates-preview.json` - candidate and blocked/manual-review preview.
- `candidate-runtime-json.json` - generated runtime JSON candidates; currently empty when no card is safe.
- `source-to-runtime-mapping.csv` - deterministic source-to-runtime mapping and decisions.

Cards remain blocked when their review file is still a template, lacks normalized runtime text, is not approved, is not marked ready for bot knowledge, has visual ambiguity requiring manual review, or conflicts with GMV Mini.
"@

Write-Utf8Text (Join-Path $StagingRoot "README.md") ($stagingReadme + [Environment]::NewLine)

Write-Host "ED-24GEC.8 remaining GMV inventory generated."
Write-Host "Inventory: $InventoryRoot"
Write-Host "Staging:   $StagingRoot"
Write-Host "Review cards: $($summary.reviewCardCount)"
Write-Host "Already runtime: $($summary.alreadyRuntimeCount)"
Write-Host "Safe candidates: $($summary.safeToGenerateCount)"
Write-Host "Blocked/manual-review: $($summary.blockedManualReviewCount)"
