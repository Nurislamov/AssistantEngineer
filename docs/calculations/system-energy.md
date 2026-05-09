# System Energy / EN15316-Style Foundation

This document describes the Stage 5 internal engineering implementation for the system energy chain.
It uses deterministic fixtures and validation anchors only.

## System energy chain

The chain is modeled as:

1. useful/system load intake,
2. emission stage,
3. distribution stage,
4. storage stage,
5. generation stage,
6. final energy aggregation by carrier,
7. primary energy calculation,
8. CO2 emissions summary.

The implementation is deterministic and intended as a calculation foundation for project pipelines.

## Supported uses

Supported uses include:

- SpaceHeating
- SpaceCooling
- DomesticHotWater
- Ventilation
- Auxiliary
- Generic

Heating, cooling, and DHW remain separate uses through the chain.

## Supported carriers

Supported carriers include:

- Electricity
- NaturalGas
- DistrictHeating
- DistrictCooling
- Biomass
- Oil
- LPG
- SolarThermal
- Other
- Unknown

## Emission / Distribution / Storage / Generation stages

Each stage can be defined per use with deterministic inputs:

- efficiency mode,
- loss-fraction mode,
- fixed loss profile mode,
- recovered-loss fraction,
- optional auxiliary energy profile.

Generation supports baseline kinds such as electric resistance, gas boiler, heat pump, chiller, district heating/cooling, solar thermal contribution, and generic efficiency generator.

Stage-level recovered losses reduce downstream thermal load according to the selected convention and never increase purchased final energy.

## Final energy convention

Final energy is aggregated by carrier from generation stage outputs.
Auxiliary energy is tracked as a separate lane and not merged into thermal useful load.

## Primary energy convention

Primary energy is calculated from final energy by carrier using factor catalog entries:

- primary energy = final energy x total primary factor.

Missing factors in strict mode produce validation errors.
Fallback mode can use explicit diagnostics with zero-factor fallback.

## CO2 factor convention

CO2 emissions are calculated from final energy by carrier:

- CO2 = final energy x emission factor (kgCO2/kWh).

## Recovered losses convention

Recovered losses are calculated at stage level:

- recovered losses = stage losses x recovered fraction.

Recovered losses reduce downstream load and are reported separately in diagnostics and summaries.

## Auxiliary energy convention

Auxiliary energy is tracked separately from thermal system load and useful demand.
Auxiliary lanes are summarized independently so thermal balance remains explicit.

## Ownership / NoDoubleCounting policy

Supported ownership policies:

- UpstreamOwnsLosses
- SystemEnergyOwnsLosses
- NoDoubleCounting
- ExplicitStageOwnership

NoDoubleCounting policy prioritizes safe behavior for DHW handoff and skips duplicated DHW distribution/storage loss application when upstream/system-load ownership is already present.

## Integration with ISO52016 and DHW

- ISO52016 useful heating/cooling profiles are ingested as separate uses.
- DHW handoff from Stage 4 is ingested as a separate DomesticHotWater use.
- DHW is not merged into SpaceHeating.
- Ownership diagnostics identify whether upstream/system stage losses are accepted, skipped, or owned by system energy.

## Validation rules

Validation anchors include:

- missing load profile,
- profile length mismatch,
- negative energy values,
- invalid timestep,
- missing carrier,
- invalid efficiency or COP,
- invalid recovered fraction,
- duplicate stage conflict,
- missing factor for carrier (strict mode),
- double-counting risk diagnostics for ownership conflicts.

Diagnostics are sorted deterministically by severity/code/message.

## Fixtures

Deterministic fixtures for this stage are stored in:

`tests/fixtures/system-energy/foundation/`

Coverage includes:

- electric resistance heating,
- gas boiler heating with distribution losses,
- heat pump heating,
- chiller cooling,
- DHW handoff no-double-counting,
- DHW system-energy-owned losses,
- mixed building annual summary,
- invalid system-energy input diagnostics.

## Known limitations

This is a simplified engineering implementation.

Current limitations include:

- no detailed hydraulic network,
- no dynamic plant control,
- no part-load performance curves unless explicitly implemented in code,
- no full national annex factor database,
- no complete equipment catalogue,
- no claim of full standard compliance.

