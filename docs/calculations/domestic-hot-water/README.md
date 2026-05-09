# Domestic Hot Water Calculations

For the current DHW foundation summary (useful demand, draw-off profiles, losses, recovery, and system-energy handoff), see:

- `docs/calculations/domestic-hot-water.md`

## AE-DHW-ISO12831-001A purpose

`AE-DHW-ISO12831-001A` adds a deterministic standard-inspired useful domestic hot water demand lane at the application level.

This stage covers useful DHW demand only and does not model DHW system losses.

## Supported demand bases

- people
- dwelling/unit
- floor area
- fixture/use
- custom daily volume
- custom hourly volume

## Temperature model

- cold water temperature
- hot water setpoint temperature
- temperature rise (`deltaT = Thot - Tcold`)

## Useful energy equation

`Energy_kWh = liters * density * cp * deltaT / 3,600,000`

## Draw profile modes

- 24-hour profile
- 12-month profile
- 8760 annual hourly profile
- deterministic flat fallback when no valid profile is available

## Outputs

- daily volume
- annual volume
- monthly volume
- 8760 hourly volume
- daily useful energy
- monthly useful energy
- annual useful energy
- 8760 hourly useful energy

## Scope boundaries in this stage

- No full ISO12831-3 compliance claim.
- No protected tables copied.
- No DHW system losses in this prompt.
- No EN15316 integration in this prompt.
- No `StandardReference equivalence` claim.
- No `EnergyPlus comparison workflow` claim.
- No `ASHRAE 140 / BESTEST-style validation anchor` claim.

## Compatibility note

Existing ISO12831-inspired calculator and compatibility application service remain in place. This stage adds canonical useful-demand contracts/services additively.

## EN12831-3-style table/profile lane

For the additive table/profile-driven path, see:

- `docs/calculations/DomesticHotWaterEn12831Style.md`

## Next prompt

`AE-DHW-ISO12831-001B` - DHW storage/distribution/circulation losses and EN15316 handoff.

## AE-DHW-ISO12831-001B purpose

`AE-DHW-ISO12831-001B` adds deterministic DHW system-load preparation above useful demand by modeling thermal losses and handoff data for future EN15316 system-energy chaining.

### Storage losses

- standing-loss method (`StandingLossWatts`)
- coefficient method (`StorageLossCoefficientWPerKelvin * deltaT`)

### Distribution losses

- `PipeLengthMeters * PipeLinearLossCoefficientWPerMeterKelvin * deltaT`

### Circulation losses

- thermal loop loss:
  - `LoopLengthMeters * LoopLinearLossCoefficientWPerMeterKelvin * deltaT`
- optional pump auxiliary electricity profile from `PumpPowerWatts` and operation fractions

### Recoverable and non-recoverable split

- thermal losses are split by recoverable fraction:
  - recoverable thermal loss
  - non-recoverable thermal loss

### Hourly 8760 system heat requirement

- hourly system heat requirement includes:
  - useful DHW energy
  - storage thermal losses
  - distribution thermal losses
  - circulation thermal losses
- auxiliary electricity remains separate and is not included in thermal heat requirement

### EN15316 handoff content

- hourly useful DHW energy profile
- hourly DHW system heat requirement profile
- hourly DHW auxiliary electricity profile
- hourly recoverable loss profile
- hourly non-recoverable loss profile

### Scope boundaries in this stage

- No full ISO12831-3 compliance claim.
- No full EN15316 compliance claim.
- No protected tables copied.
- No generator efficiency/final energy/primary energy calculation in this prompt.
- No `StandardReference equivalence` claim.
- No `EnergyPlus comparison workflow` claim.
- No `ASHRAE 140 / BESTEST-style validation anchor` claim.

## Next prompt

`AE-SYS-EN15316-001A` - System energy module chain foundation.

## System energy note

DHW EN15316 handoff can now be adapted into `SystemEnergyUsefulLoadSet` for downstream system-energy module-chain calculations.
