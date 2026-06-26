param(
    [string]$RepoRoot = (Resolve-Path ".").Path,
    [switch]$StrictDraftText
)

$ErrorActionPreference = "Stop"

function Add-Issue {
    param(
        [System.Collections.Generic.List[object]]$Issues,
        [string]$Severity,
        [string]$Code,
        [string]$Path,
        [string]$Message
    )

    $Issues.Add([pscustomobject]@{
        severity = $Severity
        code = $Code
        path = $Path
        message = $Message
    }) | Out-Null
}

function Get-RepoRelativePath {
    param(
        [string]$RepoRoot,
        [string]$Path
    )

    $base = [System.IO.Path]::GetFullPath($RepoRoot).TrimEnd('\') + '\'
    $full = [System.IO.Path]::GetFullPath($Path)

    if ($full.ToLowerInvariant().StartsWith($base.ToLowerInvariant())) {
        return ($full.Substring($base.Length) -replace '\\', '/')
    }

    return ($full -replace '\\', '/')
}

function Resolve-RepoPath {
    param(
        [string]$RepoRoot,
        [string]$RelativeOrFullPath
    )

    if ([string]::IsNullOrWhiteSpace($RelativeOrFullPath)) {
        return $null
    }

    if ([System.IO.Path]::IsPathRooted($RelativeOrFullPath)) {
        return $RelativeOrFullPath
    }

    return Join-Path $RepoRoot ($RelativeOrFullPath -replace '/', '\')
}

function Get-PropertyValue {
    param(
        $Object,
        [string]$Name
    )

    if ($null -eq $Object) {
        return $null
    }

    if ($Object.PSObject.Properties.Name -contains $Name) {
        return $Object.$Name
    }

    return $null
}

function Get-AsArray {
    param($Value)

    if ($null -eq $Value) {
        return @()
    }

    return @($Value)
}

function Get-BoolIsTrue {
    param($Value)

    if ($null -eq $Value) {
        return $false
    }

    if ($Value -is [bool]) {
        return [bool]$Value
    }

    return ([string]$Value).Trim().ToLowerInvariant() -eq "true"
}

function Test-ImageFileName {
    param([string]$Filename)

    $ext = [System.IO.Path]::GetExtension($Filename).ToLowerInvariant()
    return $ext -in @(".png", ".jpg", ".jpeg", ".webp")
}

Write-Host "Gree GMV review catalog validator"
Write-Host "RepoRoot: $RepoRoot"

if (-not (Test-Path $RepoRoot)) {
    throw "RepoRoot not found: $RepoRoot"
}

if (-not (Test-Path (Join-Path $RepoRoot ".git"))) {
    throw "Not a git repository: $RepoRoot"
}

$catalogRoot = Join-Path $RepoRoot "data\reference\gree-official-support-error-catalog"
$catalogPath = Join-Path $catalogRoot "catalog-index.json"
$reviewRoot = Join-Path $catalogRoot "review"
$rawCardsRoot = Join-Path $catalogRoot "raw\cards"
$approvedRoot = Join-Path $catalogRoot "approved"

$issues = [System.Collections.Generic.List[object]]::new()

foreach ($requiredPath in @($catalogPath, $reviewRoot, $rawCardsRoot)) {
    if (-not (Test-Path $requiredPath)) {
        Add-Issue $issues "error" "missing-required-path" (Get-RepoRelativePath $RepoRoot $requiredPath) "Required path not found."
    }
}

if ($issues.Count -gt 0) {
    $issues | Format-Table severity, code, path, message -AutoSize
    throw "Validation failed before catalog parsing."
}

try {
    $catalog = Get-Content -Path $catalogPath -Raw -Encoding UTF8 | ConvertFrom-Json
}
catch {
    Add-Issue $issues "error" "invalid-catalog-json" (Get-RepoRelativePath $RepoRoot $catalogPath) $_.Exception.Message
    $issues | Format-Table severity, code, path, message -AutoSize
    throw "Validation failed."
}

$gmvCatalogCards = @($catalog.cards | Where-Object {
    [string]$_.brand -eq "Gree" -and [string]$_.model -eq "GMV"
})

$catalogCodes = @($gmvCatalogCards | ForEach-Object { [string]$_.code } | Sort-Object -Unique)

$reviewFiles = @(Get-ChildItem -Path $reviewRoot -File -Filter "Gree-GMV-*.review.json" | Sort-Object Name)
$reviewCodes = @($reviewFiles | ForEach-Object {
    $_.BaseName -replace "^Gree-GMV-", "" -replace "\.review$", ""
} | Sort-Object -Unique)

$rawFolders = @(Get-ChildItem -Path $rawCardsRoot -Directory -Filter "Gree-GMV-*" | Sort-Object Name)
$rawCodes = @($rawFolders | ForEach-Object {
    $_.Name -replace "^Gree-GMV-", ""
} | Sort-Object -Unique)

if ($catalogCodes.Count -ne 256) {
    Add-Issue $issues "warning" "unexpected-gmv-catalog-count" "catalog-index.json" "Expected 256 GMV catalog cards, got $($catalogCodes.Count)."
}

if ($reviewCodes.Count -ne $catalogCodes.Count) {
    Add-Issue $issues "error" "review-count-mismatch" "review" "GMV catalog cards: $($catalogCodes.Count); review files: $($reviewCodes.Count)."
}

$missingReview = @($catalogCodes | Where-Object { $reviewCodes -notcontains $_ })
foreach ($code in $missingReview) {
    Add-Issue $issues "error" "missing-review-file" "review/Gree-GMV-$code.review.json" "Catalog code has no review file."
}

$extraReview = @($reviewCodes | Where-Object { $catalogCodes -notcontains $_ })
foreach ($code in $extraReview) {
    Add-Issue $issues "error" "extra-review-file" "review/Gree-GMV-$code.review.json" "Review file does not exist in GMV catalog index."
}

$missingRawFolder = @($catalogCodes | Where-Object { $rawCodes -notcontains $_ })
foreach ($code in $missingRawFolder) {
    Add-Issue $issues "error" "missing-raw-folder" "raw/cards/Gree-GMV-$code" "Catalog code has no raw folder."
}

$duplicateReviewNames = @($reviewFiles | Group-Object Name | Where-Object { $_.Count -gt 1 })
foreach ($dup in $duplicateReviewNames) {
    Add-Issue $issues "error" "duplicate-review-file-name" "review/$($dup.Name)" "Duplicate review file name."
}

foreach ($reviewFile in $reviewFiles) {
    $relativeReviewPath = Get-RepoRelativePath $RepoRoot $reviewFile.FullName
    $expectedCode = $reviewFile.BaseName -replace "^Gree-GMV-", "" -replace "\.review$", ""

    try {
        $review = Get-Content -Path $reviewFile.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        Add-Issue $issues "error" "invalid-review-json" $relativeReviewPath $_.Exception.Message
        continue
    }

    $schemaVersion = Get-PropertyValue $review "schemaVersion"
    if ([int]$schemaVersion -ne 1) {
        Add-Issue $issues "error" "invalid-schema-version" $relativeReviewPath "schemaVersion must be 1."
    }

    if ([string](Get-PropertyValue $review "brand") -ne "Gree") {
        Add-Issue $issues "error" "invalid-brand" $relativeReviewPath "brand must be Gree."
    }

    if ([string](Get-PropertyValue $review "model") -ne "GMV") {
        Add-Issue $issues "error" "invalid-model" $relativeReviewPath "model must be GMV."
    }

    $code = [string](Get-PropertyValue $review "code")
    if ($code -ne $expectedCode) {
        Add-Issue $issues "error" "code-filename-mismatch" $relativeReviewPath "Review code '$code' does not match filename code '$expectedCode'."
    }

    if ($catalogCodes -notcontains $code) {
        Add-Issue $issues "error" "review-code-not-in-catalog" $relativeReviewPath "Code '$code' is not present in catalog-index.json."
    }

    $status = [string](Get-PropertyValue $review "status")
    if ([string]::IsNullOrWhiteSpace($status)) {
        Add-Issue $issues "error" "missing-status" $relativeReviewPath "status is required."
    }
    elseif ($status -notin @("review-template", "review-draft", "approved")) {
        Add-Issue $issues "error" "invalid-status" $relativeReviewPath "Unsupported status '$status'."
    }

    if (Get-BoolIsTrue (Get-PropertyValue $review "runtimeEnabled")) {
        Add-Issue $issues "error" "runtime-enabled-in-review" $relativeReviewPath "runtimeEnabled must not be true in review catalog."
    }

    $source = Get-PropertyValue $review "source"
    if ($null -eq $source) {
        Add-Issue $issues "error" "missing-source" $relativeReviewPath "source object is required."
        continue
    }

    $primaryRawCardPath = [string](Get-PropertyValue $source "primaryRawCardPath")
    if ([string]::IsNullOrWhiteSpace($primaryRawCardPath)) {
        Add-Issue $issues "error" "missing-primary-raw-card-path" $relativeReviewPath "source.primaryRawCardPath is required."
    }
    else {
        $resolvedPrimary = Resolve-RepoPath $RepoRoot $primaryRawCardPath
        if (-not (Test-Path $resolvedPrimary)) {
            Add-Issue $issues "error" "missing-primary-raw-card-file" $relativeReviewPath "source.primaryRawCardPath target not found: $primaryRawCardPath"
        }
        elseif (-not (Test-ImageFileName (Split-Path $resolvedPrimary -Leaf))) {
            Add-Issue $issues "error" "primary-raw-card-not-image" $relativeReviewPath "source.primaryRawCardPath must point to an image file."
        }
    }

    $rawFolderPath = [string](Get-PropertyValue $source "rawFolderPath")
    if (-not [string]::IsNullOrWhiteSpace($rawFolderPath)) {
        $resolvedRawFolder = Resolve-RepoPath $RepoRoot $rawFolderPath
        if (-not (Test-Path $resolvedRawFolder)) {
            Add-Issue $issues "error" "missing-raw-folder-path" $relativeReviewPath "source.rawFolderPath target not found: $rawFolderPath"
        }
    }

    foreach ($rawCardPath in Get-AsArray (Get-PropertyValue $source "rawCardPaths")) {
        $resolvedRawCardPath = Resolve-RepoPath $RepoRoot ([string]$rawCardPath)
        if (-not (Test-Path $resolvedRawCardPath)) {
            Add-Issue $issues "error" "missing-raw-card-path" $relativeReviewPath "source.rawCardPaths target not found: $rawCardPath"
        }
    }

    foreach ($supplementalRawCardPath in Get-AsArray (Get-PropertyValue $source "supplementalRawCardPaths")) {
        $resolvedSupplementalRawCardPath = Resolve-RepoPath $RepoRoot ([string]$supplementalRawCardPath)
        if (-not (Test-Path $resolvedSupplementalRawCardPath)) {
            Add-Issue $issues "error" "missing-supplemental-raw-card-path" $relativeReviewPath "source.supplementalRawCardPaths target not found: $supplementalRawCardPath"
        }
    }

    $reviewNode = Get-PropertyValue $review "review"
    if ($null -ne $reviewNode) {
        if (Get-BoolIsTrue (Get-PropertyValue $reviewNode "runtimeEnabled")) {
            Add-Issue $issues "error" "nested-review-runtime-enabled" $relativeReviewPath "review.runtimeEnabled must not be true."
        }
    }

    $importerNode = Get-PropertyValue $review "importer"
    if ($null -ne $importerNode) {
        if (Get-BoolIsTrue (Get-PropertyValue $importerNode "diagnosticsRuntimeEnabled")) {
            Add-Issue $issues "error" "diagnostics-runtime-enabled" $relativeReviewPath "importer.diagnosticsRuntimeEnabled must not be true."
        }
    }

    if ($StrictDraftText) {
        $normalizedRu = Get-PropertyValue $review "normalizedRu"
        if ($null -eq $normalizedRu) {
            Add-Issue $issues "warning" "missing-normalized-ru" $relativeReviewPath "normalizedRu object is missing."
        }
        else {
            foreach ($field in @("titleRu", "meaningRu", "userSafeAnswerRu", "technicianAnswerRu")) {
                $value = [string](Get-PropertyValue $normalizedRu $field)
                if ([string]::IsNullOrWhiteSpace($value) -and $status -ne "review-template") {
                    Add-Issue $issues "warning" "empty-draft-field" $relativeReviewPath "$field is empty for non-template review."
                }
            }
        }
    }
}

$approvedFiles = @()
if (Test-Path $approvedRoot) {
    $approvedFiles = @(Get-ChildItem -Path $approvedRoot -File -Filter "Gree-GMV-*.approved.json" -ErrorAction SilentlyContinue)
}

foreach ($approvedFile in $approvedFiles) {
    $relativeApprovedPath = Get-RepoRelativePath $RepoRoot $approvedFile.FullName
    try {
        $approved = Get-Content -Path $approvedFile.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        Add-Issue $issues "error" "invalid-approved-json" $relativeApprovedPath $_.Exception.Message
        continue
    }

    if (Get-BoolIsTrue (Get-PropertyValue $approved "runtimeEnabled")) {
        Add-Issue $issues "error" "runtime-enabled-in-approved-reference" $relativeApprovedPath "approved reference data must not enable runtime directly."
    }
}

$errorCount = @($issues | Where-Object { $_.severity -eq "error" }).Count
$warningCount = @($issues | Where-Object { $_.severity -eq "warning" }).Count

Write-Host ""
Write-Host "Summary:"
Write-Host "  GMV catalog cards: $($catalogCodes.Count)"
Write-Host "  GMV raw folders:   $($rawCodes.Count)"
Write-Host "  GMV review files:  $($reviewCodes.Count)"
Write-Host "  Approved JSON:     $($approvedFiles.Count)"
Write-Host "  Errors:            $errorCount"
Write-Host "  Warnings:          $warningCount"

if ($issues.Count -gt 0) {
    Write-Host ""
    Write-Host "Issues:"
    $issues | Sort-Object severity, code, path | Format-Table severity, code, path, message -AutoSize
}

if ($errorCount -gt 0) {
    throw "Gree GMV review catalog validation failed with $errorCount error(s)."
}

Write-Host ""
Write-Host "PASS: Gree GMV review catalog validation completed."