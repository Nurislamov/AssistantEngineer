# Engineering Core V1 Export Disclosure Template

Use this template for PDF, Excel, JSON and frontend report exports.

## Calculation scope

- Core status:
- Report type:
- Calculation scope:
- Calculation method:
- Actual method:

## Main results

- Heating load:
- Cooling load:
- Annual heating energy:
- Annual cooling energy:
- Annual total energy:
- EUI:
- Peak hour:

## Warnings

- 
- 
- 

## Assumptions

- 
- 
- 

## Explicit non-claims

- No exact EnergyPlus numerical equivalence claim.
- No exact StandardReference numerical equivalence claim.
- No ASHRAE 140 / BESTEST-style validation anchor coverage claim.
- No full ISO 52016 node/matrix solver equivalence claim.
- No latent/moisture/humidity support in v1.

## Out-of-scope v1

- HVAC.LATENT_LOAD
- HVAC.MOISTURE_BALANCE
- Detailed psychrometric supply-air treatment
- Detailed HVAC plant simulation

## Annual 8760 requirements

True hourly annual energy can be claimed only when:

    EnergyDataSource = TrueHourlySimulation
    IsTrueHourly8760 = true
    HourlyRecordCount = 8760

## Documentation references

- docs/calculations/EngineeringCoreV1Scope.md
- docs/calculations/EngineeringCoreV1ReleaseNotes.md
- docs/calculations/EnergyPlusAshrae140ValidationPlan.md

## Export checklist

- [ ] calculationDisclosure is present.
- [ ] warnings are visible.
- [ ] assumptions are visible.
- [ ] explicitNonClaims are visible.
- [ ] outOfScopeV1 is visible.
- [ ] documentationFiles are visible.
- [ ] annual 8760 requirements are visible when annual energy is exported.
- [ ] no exact EnergyPlus comparison workflow claim is introduced.
- [ ] no ASHRAE 140 / BESTEST-style validation anchor coverage claim is introduced.
- [ ] no latent/moisture/humidity support claim is introduced.
