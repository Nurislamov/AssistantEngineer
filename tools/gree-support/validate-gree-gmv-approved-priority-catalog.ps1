param(
    [string]$RepoRoot = (Resolve-Path ".").Path,
    [string[]]$PriorityCodes = @("A0", "C0", "C7", "E1", "H5", "L1", "o1", "U0", "E0", "F3", "P0", "P1", "P2", "U2", "U3", "U4", "U5")
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

function Get-PropertyValue {
    param($Object, [string]$Name)

    if ($null -eq $Object) { return $null }
    if ($Object.PSObject.Properties.Name -contains $Name) { return $Object.$Name }
    return $null
}

function Get-AsArray {
    param($Value)
    if ($null -eq $Value) { return @() }
    return @($Value)
}

function Get-BoolIsTrue {
    param($Value)

    if ($null -eq $Value) { return $false }
    if ($Value -is [bool]) { return [bool]$Value }

    return ([string]$Value).Trim().ToLowerInvariant() -eq "true"
}

function Resolve-RepoPath {
    param([string]$RepoRoot, [string]$RelativeOrFullPath)

    if ([string]::IsNullOrWhiteSpace($RelativeOrFullPath)) {
        return $null
    }

    if ([System.IO.Path]::IsPathRooted($RelativeOrFullPath)) {
        return $RelativeOrFullPath
    }

    return Join-Path $RepoRoot ($RelativeOrFullPath -replace '/', '\')
}

function Get-RepoRelativePath {
    param([string]$RepoRoot, [string]$Path)

    $base = [System.IO.Path]::GetFullPath($RepoRoot).TrimEnd('\') + '\'
    $full = [System.IO.Path]::GetFullPath($Path)

    if ($full.ToLowerInvariant().StartsWith($base.ToLowerInvariant())) {
        return ($full.Substring($base.Length) -replace '\\', '/')
    }

    return ($full -replace '\\', '/')
}

function Get-CodeFromApprovedFileName {
    param([string]$Name)
    return $Name -replace "^Gree-GMV-", "" -replace "\.approved\.json$", ""
}

function Test-RequiredText {
    param(
        [System.Collections.Generic.List[object]]$Issues,
        [object]$Node,
        [string]$FieldName,
        [string]$Path
    )

    $value = [string](Get-PropertyValue $Node $FieldName)
    if ([string]::IsNullOrWhiteSpace($value)) {
        Add-Issue $Issues "error" "empty-required-text" $Path "$FieldName is required."
    }
}

function Test-RequiredArray {
    param(
        [System.Collections.Generic.List[object]]$Issues,
        [object]$Node,
        [string]$FieldName,
        [string]$Path
    )

    $value = @(Get-AsArray (Get-PropertyValue $Node $FieldName))
    if ($value.Count -eq 0) {
        Add-Issue $Issues "error" "empty-required-array" $Path "$FieldName is required."
    }
}

Write-Host "Gree GMV approved priority catalog validator"
Write-Host "RepoRoot: $RepoRoot"

if (-not (Test-Path $RepoRoot)) {
    throw "RepoRoot not found: $RepoRoot"
}

if (-not (Test-Path (Join-Path $RepoRoot ".git"))) {
    throw "Not a git repository: $RepoRoot"
}

$catalogRoot = Join-Path $RepoRoot "data\reference\gree-official-support-error-catalog"
$approvedRoot = Join-Path $catalogRoot "approved"
$approvedIndexPath = Join-Path $approvedRoot "approved-priority-index.json"
$issues = [System.Collections.Generic.List[object]]::new()

if (-not (Test-Path $approvedRoot)) {
    Add-Issue $issues "error" "missing-approved-root" "approved" "Approved root not found."
}

if (-not (Test-Path $approvedIndexPath)) {
    Add-Issue $issues "error" "missing-approved-priority-index" "approved/approved-priority-index.json" "Approved priority index not found."
}

$approvedFiles = @()
if (Test-Path $approvedRoot) {
    $approvedFiles = @(Get-ChildItem -Path $approvedRoot -File -Filter "Gree-GMV-*.approved.json" | Sort-Object Name)
}

if ($approvedFiles.Count -ne $PriorityCodes.Count) {
    Add-Issue $issues "error" "approved-count-mismatch" "approved" "Expected $($PriorityCodes.Count) approved files, got $($approvedFiles.Count)."
}

$approvedCodes = @($approvedFiles | ForEach-Object { Get-CodeFromApprovedFileName $_.Name } | Sort-Object -Unique)

foreach ($code in $PriorityCodes) {
    if ($approvedCodes -notcontains $code) {
        Add-Issue $issues "error" "missing-approved-code" "approved/Gree-GMV-$code.approved.json" "Priority code has no approved file."
    }
}

foreach ($code in $approvedCodes) {
    if ($PriorityCodes -notcontains $code) {
        Add-Issue $issues "error" "extra-approved-code" "approved/Gree-GMV-$code.approved.json" "Approved file is not part of priority set."
    }
}

foreach ($file in $approvedFiles) {
    $relativePath = Get-RepoRelativePath $RepoRoot $file.FullName
    $expectedCode = Get-CodeFromApprovedFileName $file.Name

    try {
        $approved = Get-Content -Path $file.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        Add-Issue $issues "error" "invalid-approved-json" $relativePath $_.Exception.Message
        continue
    }

    if ([int](Get-PropertyValue $approved "schemaVersion") -ne 1) {
        Add-Issue $issues "error" "invalid-schema-version" $relativePath "schemaVersion must be 1."
    }

    if ([string](Get-PropertyValue $approved "status") -ne "approved-reference") {
        Add-Issue $issues "error" "invalid-status" $relativePath "status must be approved-reference."
    }

    if ([string](Get-PropertyValue $approved "brand") -ne "Gree") {
        Add-Issue $issues "error" "invalid-brand" $relativePath "brand must be Gree."
    }

    if ([string](Get-PropertyValue $approved "model") -ne "GMV") {
        Add-Issue $issues "error" "invalid-model" $relativePath "model must be GMV."
    }

    if ([string](Get-PropertyValue $approved "family") -ne "GMV") {
        Add-Issue $issues "error" "invalid-family" $relativePath "family must be GMV."
    }

    $code = [string](Get-PropertyValue $approved "code")
    if ($code -ne $expectedCode) {
        Add-Issue $issues "error" "code-filename-mismatch" $relativePath "code '$code' does not match file code '$expectedCode'."
    }

    if (Get-BoolIsTrue (Get-PropertyValue $approved "runtimeEnabled")) {
        Add-Issue $issues "error" "runtime-enabled" $relativePath "runtimeEnabled must be false."
    }

    $normalizedRu = Get-PropertyValue $approved "normalizedRu"
    if ($null -eq $normalizedRu) {
        Add-Issue $issues "error" "missing-normalized-ru" $relativePath "normalizedRu is required."
    }
    else {
        foreach ($field in @("titleRu", "meaningRu", "severityRu", "userSafeAnswerRu", "technicianAnswerRu")) {
            Test-RequiredText $issues $normalizedRu $field $relativePath
        }

        foreach ($field in @("possibleCausesRu", "checksRu", "serviceNotesRu")) {
            Test-RequiredArray $issues $normalizedRu $field $relativePath
        }
    }

    $source = Get-PropertyValue $approved "source"
    if ($null -eq $source) {
        Add-Issue $issues "error" "missing-source" $relativePath "source is required."
    }
    else {
        foreach ($pathField in @("reviewPath", "primaryRawCardPath")) {
            $ref = [string](Get-PropertyValue $source $pathField)
            if ([string]::IsNullOrWhiteSpace($ref)) {
                Add-Issue $issues "error" "missing-source-path" $relativePath "source.$pathField is required."
                continue
            }

            $resolved = Resolve-RepoPath $RepoRoot $ref
            if (-not (Test-Path $resolved)) {
                Add-Issue $issues "error" "missing-source-target" $relativePath "source.$pathField target not found: $ref"
            }
        }
    }

    $approval = Get-PropertyValue $approved "approval"
    if ($null -eq $approval) {
        Add-Issue $issues "error" "missing-approval" $relativePath "approval is required."
    }
    else {
        if (Get-BoolIsTrue (Get-PropertyValue $approval "runtimeEnabled")) {
            Add-Issue $issues "error" "approval-runtime-enabled" $relativePath "approval.runtimeEnabled must be false."
        }

        if ([string](Get-PropertyValue $approval "approvalStage") -ne "ED-24GEC.4A") {
            Add-Issue $issues "error" "invalid-approval-stage" $relativePath "approval.approvalStage must be ED-24GEC.4A."
        }
    }

    $importer = Get-PropertyValue $approved "importer"
    if ($null -eq $importer) {
        Add-Issue $issues "error" "missing-importer" $relativePath "importer is required."
    }
    else {
        if (-not (Get-BoolIsTrue (Get-PropertyValue $importer "diagnosticsRuntimeCandidate"))) {
            Add-Issue $issues "error" "not-runtime-candidate" $relativePath "diagnosticsRuntimeCandidate must be true for approved priority reference."
        }

        if (Get-BoolIsTrue (Get-PropertyValue $importer "diagnosticsRuntimeEnabled")) {
            Add-Issue $issues "error" "diagnostics-runtime-enabled" $relativePath "diagnosticsRuntimeEnabled must be false."
        }
    }
}

if (Test-Path $approvedIndexPath) {
    try {
        $index = Get-Content -Path $approvedIndexPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $indexRel = Get-RepoRelativePath $RepoRoot $approvedIndexPath

        if ([int](Get-PropertyValue $index "schemaVersion") -ne 1) {
            Add-Issue $issues "error" "index-invalid-schema-version" $indexRel "schemaVersion must be 1."
        }

        if ([string](Get-PropertyValue $index "status") -ne "frozen-approved-priority-index") {
            Add-Issue $issues "error" "index-invalid-status" $indexRel "status must be frozen-approved-priority-index."
        }

        if ([string](Get-PropertyValue $index "stage") -ne "ED-24GEC.4B") {
            Add-Issue $issues "error" "index-invalid-stage" $indexRel "stage must be ED-24GEC.4B."
        }

        if (Get-BoolIsTrue (Get-PropertyValue $index "runtimeEnabled")) {
            Add-Issue $issues "error" "index-runtime-enabled" $indexRel "runtimeEnabled must be false."
        }

        if (Get-BoolIsTrue (Get-PropertyValue $index "diagnosticsRuntimeEnabled")) {
            Add-Issue $issues "error" "index-diagnostics-runtime-enabled" $indexRel "diagnosticsRuntimeEnabled must be false."
        }

        if ([int](Get-PropertyValue $index "count") -ne $PriorityCodes.Count) {
            Add-Issue $issues "error" "index-count-mismatch" $indexRel "count must be $($PriorityCodes.Count)."
        }

        $indexCodes = @(Get-AsArray (Get-PropertyValue $index "codes") | ForEach-Object { [string]$_ } | Sort-Object -Unique)
        foreach ($code in $PriorityCodes) {
            if ($indexCodes -notcontains $code) {
                Add-Issue $issues "error" "index-missing-code" $indexRel "Index missing code $code."
            }
        }

        foreach ($code in $indexCodes) {
            if ($PriorityCodes -notcontains $code) {
                Add-Issue $issues "error" "index-extra-code" $indexRel "Index has non-priority code $code."
            }
        }

        $entries = @(Get-AsArray (Get-PropertyValue $index "entries"))
        if ($entries.Count -ne $PriorityCodes.Count) {
            Add-Issue $issues "error" "index-entry-count-mismatch" $indexRel "entries must contain $($PriorityCodes.Count) records."
        }

        foreach ($entry in $entries) {
            $entryCode = [string](Get-PropertyValue $entry "code")
            if ($PriorityCodes -notcontains $entryCode) {
                Add-Issue $issues "error" "index-entry-non-priority-code" $indexRel "Entry has non-priority code $entryCode."
            }

            if (Get-BoolIsTrue (Get-PropertyValue $entry "runtimeEnabled")) {
                Add-Issue $issues "error" "index-entry-runtime-enabled" $indexRel "Entry $entryCode has runtimeEnabled=true."
            }

            if (Get-BoolIsTrue (Get-PropertyValue $entry "diagnosticsRuntimeEnabled")) {
                Add-Issue $issues "error" "index-entry-diagnostics-runtime-enabled" $indexRel "Entry $entryCode has diagnosticsRuntimeEnabled=true."
            }

            foreach ($pathField in @("approvedPath", "reviewPath", "primaryRawCardPath")) {
                $ref = [string](Get-PropertyValue $entry $pathField)
                if ([string]::IsNullOrWhiteSpace($ref)) {
                    Add-Issue $issues "error" "index-entry-missing-path" $indexRel "Entry $entryCode missing $pathField."
                    continue
                }

                $resolved = Resolve-RepoPath $RepoRoot $ref
                if (-not (Test-Path $resolved)) {
                    Add-Issue $issues "error" "index-entry-missing-target" $indexRel "Entry $entryCode $pathField target not found: $ref"
                }
            }
        }
    }
    catch {
        Add-Issue $issues "error" "invalid-approved-priority-index" "approved/approved-priority-index.json" $_.Exception.Message
    }
}

$errorCount = @($issues | Where-Object { $_.severity -eq "error" }).Count
$warningCount = @($issues | Where-Object { $_.severity -eq "warning" }).Count

Write-Host ""
Write-Host "Summary:"
Write-Host "  Priority expected: $($PriorityCodes.Count)"
Write-Host "  Approved files:    $($approvedFiles.Count)"
Write-Host "  Index exists:      $(Test-Path $approvedIndexPath)"
Write-Host "  Errors:            $errorCount"
Write-Host "  Warnings:          $warningCount"

if ($issues.Count -gt 0) {
    Write-Host ""
    Write-Host "Issues:"
    $issues | Sort-Object severity, code, path | Format-Table severity, code, path, message -AutoSize
}

if ($errorCount -gt 0) {
    throw "Gree GMV approved priority catalog validation failed with $errorCount error(s)."
}

Write-Host ""
Write-Host "PASS: Gree GMV approved priority catalog validation completed."