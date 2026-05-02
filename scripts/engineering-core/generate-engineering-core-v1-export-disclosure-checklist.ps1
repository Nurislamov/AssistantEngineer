param(
    [string] $OutputPath = "docs/reports/engineering-core-v1/ExportDisclosureChecklist.md"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repoRoot

$snapshotDirectory = "docs/reports/engineering-core-v1"
$requiredSnapshots = @(
    "heating-report.sample.json",
    "cooling-report.sample.json",
    "annual-energy-disclosure.sample.json"
)

$requiredDisclosureFields = @(
    "coreStatus",
    "calculationScope",
    "calculationMethod",
    "actualMethod",
    "warnings",
    "assumptions",
    "explicitNonClaims",
    "outOfScopeV1",
    "documentationFiles"
)

$lines = @()
$lines += "# Engineering Core V1 Export Disclosure Checklist"
$lines += ""
$lines += "Generated from report contract snapshots."
$lines += ""
$lines += "## Snapshot status"
$lines += ""
$lines += "| Snapshot | Exists | Has calculationDisclosure | Missing disclosure fields |"
$lines += "|---|---|---|---|"

foreach ($snapshot in $requiredSnapshots) {
    $path = Join-Path $snapshotDirectory $snapshot
    $exists = Test-Path $path
    $hasDisclosure = $false
    $missingFields = @()

    if ($exists) {
        $json = Get-Content $path -Raw | ConvertFrom-Json

        if ($null -ne $json.calculationDisclosure) {
            $hasDisclosure = $true

            foreach ($field in $requiredDisclosureFields) {
                if (-not ($json.calculationDisclosure.PSObject.Properties.Name -contains $field)) {
                    $missingFields += $field
                }
            }
        }
        else {
            $missingFields = $requiredDisclosureFields
        }
    }
    else {
        $missingFields = $requiredDisclosureFields
    }

    $missingText = if ($missingFields.Count -eq 0) { "none" } else { $missingFields -join ", " }
    $lines += "| $snapshot | $exists | $hasDisclosure | $missingText |"
}

$lines += ""
$lines += "## Required export surfaces"
$lines += ""
$lines += "- Frontend report UI"
$lines += "- JSON exports"
$lines += "- PDF exports"
$lines += "- Excel exports"
$lines += "- Future report templates"
$lines += "- Support/debug report packages"
$lines += ""
$lines += "## Required disclosure fields"
$lines += ""

foreach ($field in $requiredDisclosureFields) {
    $lines += "- calculationDisclosure.$field"
}

$lines += ""
$lines += "## Required visible sections"
$lines += ""
$lines += "- Calculation scope"
$lines += "- Calculation method and actual method"
$lines += "- Warnings"
$lines += "- Assumptions"
$lines += "- Explicit non-claims"
$lines += "- Out-of-scope v1"
$lines += "- Documentation references"
$lines += ""
$lines += "## Annual 8760 requirements"
$lines += ""
$lines += "- EnergyDataSource = TrueHourlySimulation"
$lines += "- IsTrueHourly8760 = true"
$lines += "- HourlyRecordCount = 8760"
$lines += ""
$lines += "## Required non-claims"
$lines += ""
$lines += "- No exact EnergyPlus numerical parity claim."
$lines += "- No exact pyBuildingEnergy numerical parity claim."
$lines += "- No ASHRAE 140 validation coverage claim."
$lines += "- No full ISO 52016 node/matrix solver parity claim."
$lines += "- No latent/moisture/humidity support in v1."
$lines += ""
$lines += "## Export approval checklist"
$lines += ""
$lines += "- [ ] PDF exports show warnings and non-claims near report totals."
$lines += "- [ ] Excel exports include a visible disclosure sheet/table."
$lines += "- [ ] JSON exports preserve structured calculationDisclosure."
$lines += "- [ ] Frontend report UI shows disclosure before raw JSON."
$lines += "- [ ] Annual energy exports do not misuse true hourly 8760 wording."
$lines += "- [ ] No external-simulator parity claim is introduced."

$directory = Split-Path $OutputPath -Parent
if (-not [string]::IsNullOrWhiteSpace($directory)) {
    New-Item -ItemType Directory -Force $directory | Out-Null
}

Set-Content $OutputPath $lines -Encoding utf8

Write-Host "Engineering Core V1 export disclosure checklist generated: $OutputPath" -ForegroundColor Green
