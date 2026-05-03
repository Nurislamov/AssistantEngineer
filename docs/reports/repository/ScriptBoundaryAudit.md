# Repository Script Boundary Audit

Generated at: 2026-01-01 00:00:00 UTC

## Status

| Field | Value |
|---|---|
| Scripts | 26 |
| Thin wrappers | 20 |
| Heavy PowerShell scripts | 4 |
| Unknown PowerShell scripts | 2 |
| Status | MigrationInProgress |

## Repository boundary

- `src/Backend` — application code.
- `src/Frontend` — frontend code.
- `tests` — test code.
- `docs` — documentation and generated evidence.
- `tools` — C# automation, validation and release tools.
- `scripts` — thin wrappers only.
- `.github/workflows` — CI entry points that call tools/scripts.

## Scripts

| Script | Classification | Target tool | Non-empty lines | Heavy patterns |
|---|---|---|---:|---|
| `scripts/engineering-core/assert-engineering-core-v1-release-ready.ps1` | UnknownPowerShellScript | AssistantEngineer.Tools.EngineeringCore | 23 |  |
| `scripts/engineering-core/assert-ep-smoke-001-real-fixture-ready.ps1` | ThinWrapper | AssistantEngineer.Tools.EnergyPlusValidation | 8 |  |
| `scripts/engineering-core/compare-energyplus-validation-fixtures.ps1` | ThinWrapper | AssistantEngineer.Tools.EnergyPlusValidation | 8 |  |
| `scripts/engineering-core/compare-ep-smoke-001-placeholder.ps1` | ThinWrapper | AssistantEngineer.Tools.EnergyPlusValidation | 8 |  |
| `scripts/engineering-core/generate-calculation-module-inventory.ps1` | ThinWrapper | AssistantEngineer.Tools.EngineeringCore | 8 |  |
| `scripts/engineering-core/generate-energyplus-validation-fixture-catalog.ps1` | ThinWrapper | AssistantEngineer.Tools.EnergyPlusValidation | 8 |  |
| `scripts/engineering-core/generate-engineering-core-v1-api-contract-snapshots.ps1` | ThinWrapper | AssistantEngineer.Tools.EngineeringCore | 12 |  |
| `scripts/engineering-core/generate-engineering-core-v1-export-disclosure-checklist.ps1` | HeavyPowerShellLogic |  | 111 | ConvertFrom-Json, New-Item, Set-Content |
| `scripts/engineering-core/generate-engineering-core-v1-release-evidence.ps1` | HeavyPowerShellLogic |  | 99 | ConvertFrom-Json, ForEach-Object, New-Item, Set-Content, Where-Object |
| `scripts/engineering-core/generate-engineering-core-v1-report-contract-snapshots.ps1` | ThinWrapper | AssistantEngineer.Tools.EngineeringCore | 12 |  |
| `scripts/engineering-core/generate-engineering-core-v1-traceability-matrix.ps1` | HeavyPowerShellLogic |  | 181 | ConvertFrom-Json, ConvertTo-Json, ForEach-Object, New-Item, Set-Content |
| `scripts/engineering-core/generate-engineering-core-v1-validation-comparison-summary.ps1` | ThinWrapper | AssistantEngineer.Tools.EnergyPlusValidation | 8 |  |
| `scripts/engineering-core/generate-engineering-core-v1-validation-evidence.ps1` | ThinWrapper | AssistantEngineer.Tools.EnergyPlusValidation | 8 |  |
| `scripts/engineering-core/generate-engineering-core-v1-validation-readiness.ps1` | ThinWrapper | AssistantEngineer.Tools.EnergyPlusValidation | 8 |  |
| `scripts/engineering-core/generate-ep-smoke-001-comparison-readiness.ps1` | ThinWrapper | AssistantEngineer.Tools.EnergyPlusValidation | 8 |  |
| `scripts/engineering-core/new-energyplus-validation-fixture.ps1` | HeavyPowerShellLogic |  | 83 | function , New-Item, Set-Content |
| `scripts/engineering-core/regenerate-engineering-core-v1-artifacts.ps1` | ThinWrapper | AssistantEngineer.Tools.EngineeringCore | 11 |  |
| `scripts/engineering-core/regenerate-engineering-core-v1-validation-artifacts.ps1` | ThinWrapper | AssistantEngineer.Tools.EnergyPlusValidation | 8 |  |
| `scripts/engineering-core/verify-calculation-module-balance-invariants.ps1` | ThinWrapper | AssistantEngineer.Tools.EngineeringCore | 8 |  |
| `scripts/engineering-core/verify-calculation-module-deepening.ps1` | ThinWrapper | AssistantEngineer.Tools.EngineeringCore | 8 |  |
| `scripts/engineering-core/verify-calculation-module-diagnostics-consistency.ps1` | ThinWrapper | AssistantEngineer.Tools.EngineeringCore | 8 |  |
| `scripts/engineering-core/verify-engineering-core-v1-contracts.ps1` | ThinWrapper | AssistantEngineer.Tools.EngineeringCore | 15 |  |
| `scripts/engineering-core/verify-engineering-core-v1-manifest.ps1` | ThinWrapper | AssistantEngineer.Tools.EngineeringCore | 11 |  |
| `scripts/engineering-core/verify-engineering-core-v1-smoke.ps1` | ThinWrapper | AssistantEngineer.Tools.EngineeringCore | 11 |  |
| `scripts/engineering-core/verify-engineering-core-v1-validation.ps1` | ThinWrapper | AssistantEngineer.Tools.EnergyPlusValidation | 8 |  |
| `scripts/engineering-core/verify-engineering-core-v1.ps1` | UnknownPowerShellScript | AssistantEngineer.Tools.EngineeringCore | 19 |  |

## Interpretation

Heavy PowerShell scripts remain. Their generation, validation or release logic must be moved into C# tools before strict mode is enabled.
