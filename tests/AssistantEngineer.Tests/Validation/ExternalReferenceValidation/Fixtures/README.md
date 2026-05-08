# External Reference Validation Fixtures

This directory stores deterministic fixture inputs and expected outputs used by AssistantEngineer validation tests.

## Required fields

Each fixture should include:

1. `fixture` name;
2. source reference;
3. source version/commit/date when available;
4. input data;
5. expected hourly output where applicable;
6. expected monthly output where applicable;
7. expected annual output where applicable;
8. tolerance;
9. known assumptions.

## Fixture plan

### P0

- `single-zone-no-solar.json`: one zone, no solar gains, constant outdoor temperature, transmission + ventilation + internal gains check.
- `single-zone-solar-south-window.json`: one zone with south-facing window, solar gain and surface-irradiance check.
- `single-zone-annual-8760.json`: full-year 8760 case for annual heating/cooling and monthly aggregation checks.

### P1

- `multi-zone-adiabatic-wall.json`: two heated zones with internal separating wall, adiabatic boundary check.
- `adjacent-unheated-zone.json`: heated zone with adjacent non-heated zone, adjusted heat-transfer check.
- `dhw-residential.json`: domestic hot water volume/energy/yearly aggregation check.
- `primary-energy-heating-system.json`: delivered/final/primary energy with carrier-factor check.

## Tolerance policy

Initial deterministic targets:

- hourly temperature: +/-0.05 C;
- hourly load: +/-1 W;
- monthly demand: +/-0.01 kWh;
- annual demand: +/-0.1 kWh.

If a reference calculation uses different rounding or assumptions, tolerance can be widened only with an explicit note.
