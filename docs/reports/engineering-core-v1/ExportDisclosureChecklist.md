# Engineering Core V1 Export Disclosure Checklist

Generated from report contract snapshots.

## Snapshot status

| Snapshot | Exists | Has calculationDisclosure | Missing disclosure fields |
|---|---|---|---|
| heating-report.sample.json | True | True | none |
| cooling-report.sample.json | True | True | none |
| annual-energy-disclosure.sample.json | True | True | none |

## Required export surfaces

- Frontend report UI
- JSON exports
- PDF exports
- Excel exports
- Future report templates
- Support/debug report packages

## Required disclosure fields

- calculationDisclosure.coreStatus
- calculationDisclosure.calculationScope
- calculationDisclosure.calculationMethod
- calculationDisclosure.actualMethod
- calculationDisclosure.warnings
- calculationDisclosure.assumptions
- calculationDisclosure.explicitNonClaims
- calculationDisclosure.outOfScopeV1
- calculationDisclosure.documentationFiles

## Required visible sections

- Calculation scope
- Calculation method and actual method
- Warnings
- Assumptions
- Explicit non-claims
- Out-of-scope v1
- Documentation references

## Annual 8760 requirements

- EnergyDataSource = TrueHourlySimulation
- IsTrueHourly8760 = true
- HourlyRecordCount = 8760

## Required non-claims

- No exact EnergyPlus numerical parity claim.
- No exact pyBuildingEnergy numerical parity claim.
- No ASHRAE 140 validation coverage claim.
- No full ISO 52016 node/matrix solver parity claim.
- No latent/moisture/humidity support in v1.

## Export approval checklist

- [ ] PDF exports show warnings and non-claims near report totals.
- [ ] Excel exports include a visible disclosure sheet/table.
- [ ] JSON exports preserve structured calculationDisclosure.
- [ ] Frontend report UI shows disclosure before raw JSON.
- [ ] Annual energy exports do not misuse true hourly 8760 wording.
- [ ] No external-simulator parity claim is introduced.
