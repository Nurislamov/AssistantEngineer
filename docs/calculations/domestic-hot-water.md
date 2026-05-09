# Domestic Hot Water Foundation

## Scope

This module is an internal engineering implementation for domestic hot water (DHW) useful demand and DHW system-load preparation.

It provides deterministic fixtures and validation anchors only. It does not claim full ISO12831-3 or EN15316 compliance.

## Supported demand bases

- `PerPerson`
- `PerFloorArea`
- `PerDwelling`
- `PerFixture`
- `ScheduledVolume`
- `ScheduledEnergy`
- `Custom`

## Volume to energy convention

Useful DHW thermal energy is calculated with:

`Energy_kWh = Volume_liters * rho_kg_per_liter * cp_J_per_kgK * deltaT_K / 3,600,000`

Default internal constants:

- `rho = 0.997 kg/liter`
- `cp = 4186 J/(kg.K)`

`deltaT = T_hot_setpoint - T_cold`.

## Draw-off profile convention

Supported profile resolutions:

- hourly (`8760`)
- monthly (`12`)

Supported profile modes:

- custom schedule with normalization
- deterministic fallback profile by building use kind

If schedule data is missing or invalid, fallback profile is used with diagnostics.

## Losses convention

The simplified DHW technical-loss lane includes:

- storage losses
- distribution losses
- circulation losses
- recovered loss fraction
- auxiliary energy profile

Thermal system-load convention:

`SystemLoad = Useful + Storage + Distribution + Circulation - Recovered`

Recovered losses reduce DHW thermal system load and do not increase auxiliary electricity.

Auxiliary energy is tracked separately from thermal system load.

## Loss ownership and no-double-counting

Supported ownership policy:

- `DhwOwnLosses`
- `SystemEnergyOwnLosses`
- `NoDoubleCounting`

When `SystemEnergyOwnLosses` is selected, DHW-side technical losses are excluded from DHW thermal handoff lane and the adapter forwards useful DHW only.

## Integration with system energy chain

DHW handoff remains a separate end use and is not merged into space heating/cooling loads.

Downstream handoff preserves:

- useful DHW profile
- DHW thermal system-load profile
- DHW auxiliary electricity profile
- recoverable/non-recoverable loss lanes

## Validation rules

Validation covers:

- negative occupants/floor area/volumes
- non-positive temperature rise (`T_hot <= T_cold`)
- invalid timestep
- schedule length mismatch
- negative schedule values
- demand basis with missing required inputs
- negative loss coefficients or lengths
- recovered fraction outside `[0, 1]`
- auxiliary energy below zero
- duplicate-loss ownership risk diagnostics

Diagnostics are deterministic and sorted by severity/code/message in foundation lanes.

## Deterministic fixtures

Fixtures are stored in:

- `tests/fixtures/domestic-hot-water/foundation`

Included anchors:

- residential per-person useful demand
- office per-floor-area demand
- scheduled hourly volume
- storage losses
- distribution/circulation losses
- recovered losses
- EN15316 ownership handoff (no-double-counting policy)
- invalid DHW input diagnostics

## Known limitations

- simplified engineering implementation
- no full fixture-level plumbing network
- no detailed draw-off stochastic model
- no legionella/control model
- no detailed recirculation hydraulic model
- no full national annex defaults
- no detailed plant control optimization
- no claim of full standard compliance
