# Domestic Hot Water Calculations

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
- No `pyBuildingEnergy parity` claim.
- No `EnergyPlus parity` claim.
- No `ASHRAE 140 validation` claim.

## Compatibility note

Existing ISO12831-inspired calculator and compatibility application service remain in place. This stage adds canonical useful-demand contracts/services additively.

## Next prompt

`AE-DHW-ISO12831-001B` - DHW storage/distribution/circulation losses and EN15316 handoff.
