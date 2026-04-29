# Ventilation and infiltration loads

This document describes the current AssistantEngineer implementation for sensible outdoor air loads in the Energy Calculation Parity track.

## Scope

This stage covers:

- mechanical ventilation load;
- infiltration load by explicit airflow or ACH;
- natural ventilation load when an airflow is already supplied;
- heat recovery efficiency for mechanical ventilation;
- room-level result with separate mechanical, infiltration, and natural ventilation breakdown;
- integration into existing room heating and ISO cooling calculations as separate components.

This stage does not cover:

- latent or moisture load;
- internal gains;
- transmission heat transfer;
- solar gains;
- domestic hot water;
- equipment energy;
- ventilation controls or detailed natural ventilation physics beyond existing airflow inputs.

## Formula

The dry sensible outdoor air load uses:

```text
Q = rho * cp * Vdot * deltaT
```

Where:

- `Q` is heat load in W;
- `rho` is air density in kg/m3;
- `cp` is air specific heat in J/(kg K);
- `Vdot` is airflow in m3/s;
- `deltaT` is the temperature difference in K or degC difference.

The centralized constants are:

- `airDensityKgPerM3 = 1.2`;
- `airSpecificHeatJPerKgK = 1005`.

The equivalent volumetric heat capacity is `1.2 * 1005 / 3600 = 0.335 Wh/(m3 K)`.

## Sign Convention

The result exposes both heating and cooling loads:

- if outdoor temperature is below indoor temperature, outdoor air creates a positive heating load;
- if outdoor temperature is above indoor temperature, outdoor air creates a positive cooling load;
- if temperatures are equal, both loads are zero.

`signedHeatFlowW` is `totalHeatingLoadW - totalCoolingLoadW`.

## Airflow Normalization

Airflow conversions are centralized in `AirflowNormalizer`:

- `m3/h -> m3/s`: divide by 3600;
- `l/s -> m3/s`: divide by 1000;
- `ACH -> m3/h`: `roomVolumeM3 * airChangesPerHour`;
- per-person airflow: `airflowPerPersonLps * occupancyPeople`;
- per-area airflow: `airflowPerAreaLpsM2 * areaM2`.

Negative airflow values are diagnostics errors. ACH conversion requires valid room volume. Per-area conversion requires valid area.

## Mechanical Ventilation

Mechanical ventilation airflow can be supplied as:

- explicit `mechanicalAirflowM3PerHour`;
- `airflowLitersPerSecond`;
- `airflowPerPersonLps`;
- `airflowPerAreaLpsM2`;
- `airChangesPerHour`.

Multiple supplied mechanical airflow inputs are additive.

If `heatRecoveryEfficiency` is supplied, it must be between `0` and `1`:

```text
effectiveLoad = rawLoad * (1 - heatRecoveryEfficiency)
```

Heat recovery applies to mechanical ventilation only in this implementation.

## Infiltration

Infiltration can be supplied as:

- explicit `infiltrationAirflowM3PerHour`;
- `infiltrationAirChangesPerHour`.

For ACH:

```text
infiltrationAirflowM3PerHour = roomVolumeM3 * infiltrationAirChangesPerHour
```

If no infiltration input is supplied, the engine returns zero infiltration load and emits a diagnostic that no infiltration airflow was assumed.

## Natural Ventilation

The load engine accepts `naturalVentilationAirflowM3PerHour` when an existing service has already calculated or supplied natural ventilation airflow. It does not introduce a new natural ventilation physics model in this stage.

## Diagnostics

The engine returns calculation diagnostics instead of silently accepting invalid input.

Diagnostics cover:

- invalid area or volume;
- invalid temperatures;
- negative airflow;
- negative ACH;
- invalid heat recovery efficiency;
- missing mechanical, infiltration, or natural ventilation inputs;
- default air physical constants used.

Critical validation issues are returned as diagnostics errors and exclude the affected load from totals.

## Deterministic Fixtures

The following deterministic fixtures were added:

- `ventilation-mechanical-heating-load.json`;
- `ventilation-mechanical-cooling-load.json`;
- `ventilation-with-heat-recovery.json`;
- `ventilation-infiltration-by-ach.json`;
- `ventilation-zero-airflow.json`;
- `ventilation-invalid-heat-recovery-efficiency.json`.

These fixtures verify the formula, unit conversion, heat recovery, ACH infiltration, zero airflow, and validation diagnostics. They are deterministic fixtures, not external parity proof.

## Limitations

- Latent and humidity loads are not included.
- Detailed natural ventilation flow physics is not expanded here.
- Wind and stack terms remain in existing services where already present; the load engine consumes normalized airflow or ACH.
- Building-level aggregation relies on existing aggregation of room heating/cooling results.
