# Ventilation and infiltration loads

This document describes the current AssistantEngineer implementation for sensible outdoor air loads in the Energy Calculation equivalence track.

## Scope

This stage covers:

- mechanical ventilation load;
- infiltration load by explicit airflow or ACH;
- natural ventilation load when an airflow is already supplied;
- heat recovery efficiency for mechanical ventilation;
- room-level result with separate mechanical, infiltration, and natural ventilation breakdown;
- integration into the Energy Calculation equivalence room load application pipeline as separate components.

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

## Application Pipeline Fallback

When room-specific ventilation parameters are missing, `EnergyCalculationPipelineService` uses the configured default ACH only if that value is valid. The room load diagnostics include:

- `Ventilation.DefaultAirChangesPerHourUsed` with the fallback ACH value; or
- `Ventilation.InvalidDefaultAirChangesPerHour` when the configured fallback is invalid.

Room heating and cooling responses expose the effective values used by the pipeline:

- `EffectiveAirChangesPerHour`
- `EffectiveMechanicalAirflowM3PerHour`
- `EffectiveInfiltrationAirChangesPerHour`
- `EffectiveInfiltrationAirflowM3PerHour`
- `VentilationAssumptionSource`

When room-specific ventilation parameters are present, diagnostics record `Ventilation.RoomParametersUsed`, response source is `RoomVentilationParameters`, and no default ACH fallback warning is emitted. When defaults are used, response source is `DefaultCalculationPreferences`.

## Natural Ventilation

The load engine accepts `naturalVentilationAirflowM3PerHour` when an existing service has already calculated or supplied natural ventilation airflow. It does not introduce a new natural ventilation physics model in this stage.

## True Hourly Component Split

The ISO52016-inspired true hourly path keeps outdoor air components separate after heat-transfer calculation:

- `MechanicalVentilationW` means mechanical ventilation magnitude.
- `NaturalVentilationW` means natural ventilation magnitude.
- `VentilationW` means `MechanicalVentilationW + NaturalVentilationW`.
- `InfiltrationW` means separate infiltration magnitude.
- `MechanicalVentilationBalanceW` preserves the sign of mechanical ventilation heat flow.
- `NaturalVentilationBalanceW` preserves the sign of natural ventilation heat flow.
- `VentilationBalanceW` means `MechanicalVentilationBalanceW + NaturalVentilationBalanceW`.
- `InfiltrationBalanceW` preserves the sign of infiltration heat flow.

The signed balance convention is:

```text
positive = heat gain to the room/building
negative = heat loss from the room/building
```

The hourly balance uses:

```text
ComponentW = ComponentHeatTransferWPerK x abs(OutdoorTemperatureC - OperativeTemperatureC)
ComponentBalanceW = ComponentHeatTransferWPerK x (OutdoorTemperatureC - OperativeTemperatureC)
```

Mechanical and natural ventilation remain ventilation contribution. Infiltration is not derived by splitting total ventilation proportionally; it is calculated from the existing infiltration heat-transfer path and remains separate.

If the natural ventilation service is absent or returns zero, `NaturalVentilationW` and `NaturalVentilationBalanceW` remain zero. If the mechanical calculator is absent, `MechanicalVentilationW` and `MechanicalVentilationBalanceW` remain zero. No proportional split is inferred from aggregate `VentilationW`.

If infiltration assumptions are explicitly zero, `InfiltrationW` can be zero without warning. If an upstream hourly source cannot expose infiltration separately, annual mapping diagnostics should report that the infiltration split is unavailable instead of faking a split. If an upstream hourly source provides aggregate ventilation but not mechanical/natural subcomponents, annual mapping diagnostics report `AnnualEnergy.VentilationSubcomponentBreakdownPartial`.

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

These fixtures verify the formula, unit conversion, heat recovery, ACH infiltration, zero airflow, and validation diagnostics. They are deterministic fixtures, not external equivalence proof.

## Limitations

- Latent and humidity loads are not included.
- Detailed natural ventilation flow physics is not expanded here.
- Wind and stack terms remain in existing services where already present; the load engine consumes normalized airflow or ACH.
- Building-level aggregation relies on existing aggregation of room heating/cooling results.
- Default ACH fallback exists only at the application assembler layer and is always diagnosed.
- This does not claim full ISO compliance or external benchmark equivalence.
