# AE-EN15316-001 EN15316-inspired modular system energy chain

## Stage

- Stage id: `AE-EN15316-001`
- Scope: pure C# modular system energy chain calculator for deterministic engineering anchors.

## Claim boundary

- EN15316-inspired modular system energy engineering calculator.
- Internal deterministic engineering anchors only.
- No full EN 15316 compliance claim.
- No pyBuildingEnergy parity claim.
- No EnergyPlus parity claim.
- No ASHRAE 140 validation claim.
- No external certification claim.

## Formula chain

For each end use (`Heating`, `Cooling`, `DomesticHotWater`, `VentilationFans`, `Auxiliary`):

1. useful energy input;
2. emission module:
   - efficiency path: `upstream = downstream / efficiency`;
   - loss factor path: `upstream = downstream * (1 + lossFactor)`;
3. distribution module (same rule);
4. storage module (same rule);
5. generation module:
   - COP path: `finalGeneration = storageInput / COP`;
   - efficiency path: `finalGeneration = storageInput / generationEfficiency`;
   - fallback path: pass-through with diagnostics;
6. auxiliary energy add;
7. recovered losses subtract:
   - `recovered = (emissionLoss + distributionLoss + storageLoss + generationLoss) * recoveredLossFraction`;
8. final energy and primary energy aggregation:
   - by end use;
   - by carrier;
   - total final and total primary.

When renewable and non-renewable primary factors are supplied, the calculator also returns split primary totals.

## Module boundaries

- Emission
- Distribution
- Storage
- Generation
- Auxiliary
- Primary energy

Each module returns deterministic per-module upstream/downstream/loss values with diagnostics.

## Energy carriers

- `Electricity`
- `NaturalGas`
- `DistrictHeat`
- `DistrictCooling`
- `Biomass`
- `FuelOil`
- `Unknown`

## Generation technologies

- `Boiler`
- `CondensingBoiler`
- `HeatPump`
- `Chiller`
- `DistrictHeatingSubstation`
- `DistrictCoolingSubstation`
- `ElectricResistance`
- `DirectElectric`
- `Custom`

## Reference defaults

`En15316SystemEnergyReferenceDataProvider` provides table-inspired internal deterministic defaults by generation technology (efficiency, COP, typical auxiliary fraction). These defaults are not a normative table reproduction.

## Deterministic fixtures

- `tests/fixtures/system-energy/en15316/boiler-heating-emission-distribution-generation.json`
- `tests/fixtures/system-energy/en15316/condensing-boiler-heating-with-recovered-losses.json`
- `tests/fixtures/system-energy/en15316/heat-pump-heating-electricity-primary.json`
- `tests/fixtures/system-energy/en15316/chiller-cooling-electricity-primary.json`
- `tests/fixtures/system-energy/en15316/dhw-storage-distribution-generation-chain.json`

## Limitations

- internal engineering anchor model only;
- no full standard compliance output;
- no external certification output;
- no parity claims with pyBuildingEnergy or EnergyPlus.

## Migration strategy

- current production path remains `SystemEnergyEngine` for compatibility;
- this stage adds a pure EN15316-inspired calculator and optional mapping adapter only;
- controlled application integration is deferred to `AE-EN15316-002`.
