# Internal Gains

This calculation step covers deterministic internal heat gains for the Standard-Based Calculation track.

## Scope

Included:

- occupancy sensible gains;
- occupancy latent gains when supplied;
- lighting gains;
- equipment gains from internal heat sources;
- process gains;
- custom sensible and latent gains;
- schedule factors;
- room-level aggregation;
- calculation diagnostics.

Excluded:

- window solar gains;
- transmission heat transfer;
- ventilation and infiltration loads;
- HVAC equipment sizing;
- DHW demand;
- report or frontend formatting.

## Units

| Quantity | Unit |
| --- | --- |
| Area | m2 |
| Occupancy | persons |
| Sensible gain per person | W/person |
| Latent gain per person | W/person |
| Lighting power density | W/m2 |
| Equipment power density | W/m2 |
| Process/custom gain | W |
| Schedule factor | 0..1 |
| Internal gain | W |

## Formulas

```text
occupancySensibleGainW = people * sensibleGainPerPersonW * occupancyScheduleFactor
occupancyLatentGainW = people * latentGainPerPersonW * occupancyScheduleFactor
lightingGainW = areaM2 * lightingPowerDensityWPerM2 * lightingScheduleFactor
equipmentGainW = areaM2 * equipmentPowerDensityWPerM2 * equipmentScheduleFactor
processGainW = processGainW * processScheduleFactor
```

```text
totalSensibleInternalGainW =
  occupancySensibleGainW
  + lightingGainW
  + equipmentGainW
  + processSensibleGainW
  + customSensibleGainW

totalLatentInternalGainW =
  occupancyLatentGainW
  + processLatentGainW
  + customLatentGainW
```

## Schedules

Schedule factors must be between 0 and 1. A factor of 0 produces zero gain for that component. A factor of 1 uses the design gain.

In the room load application pipeline, design-point internal gains use full schedule factor `1.0`. Diagnostics report `InternalGains.DesignPointFullScheduleFactor`; if room schedules exist, diagnostics state that those schedules are reserved for hourly analysis paths.

Existing hourly analysis services continue to use schedule/profile expansion where that path is available.

## Sensible And Latent

The engine calculates sensible and latent gains separately. Current room heat-balance consumers use sensible gains; latent gains are returned in the result and reported in diagnostics when not consumed by the current path.

## Diagnostics

Invalid negative inputs and invalid schedule factors produce diagnostic errors. Missing area for W/m2 gains is an error. Defaults are not silently substituted.

## Equipment Distinction

Equipment gains here mean internal heat gains from appliances or process equipment. This is separate from HVAC equipment sizing and catalog selection.

## Deterministic Fixtures

- `internal-gains-occupancy-sensible.json`
- `internal-gains-lighting-by-area.json`
- `internal-gains-equipment-by-area.json`
- `internal-gains-process-with-schedule.json`
- `internal-gains-room-aggregation.json`
- `internal-gains-zero-schedule.json`
- `internal-gains-invalid-schedule-factor.json`
- `internal-gains-negative-power-density.json`

## Limits

This is a deterministic component model. It does not model moisture balance or dynamic heat storage. Design-point room loads do not silently expand hourly schedules.
