# Pull Request

## Summary

Describe the change.

## Engineering Core V1 impact

Check one:

- [ ] No Engineering Core V1 impact.
- [ ] Changes calculation formula/algorithm.
- [ ] Changes diagnostics behavior.
- [ ] Changes weather/8760 behavior.
- [ ] Changes annual energy behavior.
- [ ] Changes reports/API/frontend visibility.
- [ ] Changes EnergyPlus/ASHRAE 140 / BESTEST-style validation anchor harness.
- [ ] Changes documentation only.

## Required Engineering Core V1 checklist

If this PR touches calculation-core behavior, confirm:

- [ ] FormulaAuditMatrix is updated or confirmed unchanged.
- [ ] Units are documented for new/changed formulas.
- [ ] Diagnostics distinguish Error, Warning and Info.
- [ ] Successful calculation results do not contain CalculationDiagnosticSeverity.Error.
- [ ] Invalid mandatory inputs fail the calculation.
- [ ] Optional fallbacks/simplifications are warnings, not silent behavior.
- [ ] Known limitations are documented.
- [ ] No exact EnergyPlus comparison workflow claim is introduced.
- [ ] No exact StandardReference equivalence claim is introduced.
- [ ] No ASHRAE 140 / BESTEST-style validation anchor coverage claim is introduced.
- [ ] No full ISO/EN implementation claim is introduced unless separately validated.
- [ ] Report/API/frontend disclosures are updated if user-visible output changes.

## 8760 / annual energy checklist

If this PR touches weather or annual energy, confirm:

- [ ] EPW/PVGIS 8760 behavior remains covered.
- [ ] True annual hourly energy still requires EnergyDataSource = TrueHourlySimulation.
- [ ] True annual hourly energy still requires IsTrueHourly8760 = true.
- [ ] True annual hourly energy still requires HourlyRecordCount = 8760.
- [ ] Monthly adapter/synthetic weather are not presented as true hourly 8760 simulation.

## Validation checklist

If this PR touches EnergyPlus/ASHRAE 140 / BESTEST-style validation anchor, confirm:

- [ ] Validation is comparative with documented tolerances.
- [ ] Validation does not claim exact watt-by-watt equivalence.
- [ ] Case metadata includes source, weather, geometry, assumptions and known differences.
- [ ] Non-claims are visible in validation docs/fixtures.

## Verification

Run before requesting review:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

For a faster local pre-check:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1 -Fast

Paste relevant output or explain why the full script was not run.

## Screenshots / API examples

If UI/API output changed, include screenshots or sample JSON.
