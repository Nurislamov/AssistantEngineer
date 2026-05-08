# Transmission Heat Transfer

This document describes the current Energy Calculation equivalence scope for transmission heat transfer through envelope elements.

## Scope

This stage covers envelope heat transfer for:

- walls;
- windows as conductive envelope elements;
- roof, floor, door, or generic envelope elements when such inputs are supplied to the engine;
- outdoor boundaries;
- ground boundaries using the supplied boundary temperature and correction factor;
- adjacent unheated spaces;
- adjacent conditioned zones;
- internal adiabatic boundaries.

Solar gains, ventilation, infiltration, internal gains, domestic hot water, and equipment effects are outside this stage.

## Formula

The engine uses:

```text
Q = U x A x deltaT
```

Where:

- `Q` is heat flow, W;
- `U` is thermal transmittance, W/(m2 K);
- `A` is area, m2;
- `deltaT` is `indoorTemperatureC - boundaryTemperatureC`, K or degC difference.

## Units

Input temperatures are degrees Celsius. Temperature difference is numerically equivalent in K and degC. Area is square meters. U-value is W/(m2 K). Heat flow is W.

## Boundary Types

`Outdoor`: uses `outdoorTemperatureC`, or `boundaryTemperatureC` when supplied.

`Ground`: uses `groundTemperatureC`, or `boundaryTemperatureC` when supplied. Ground transfer is currently simplified and diagnostics explicitly mark that assumption.

In the room load application pipeline, ground boundary walls receive a ground temperature from existing ground temperature/profile services when available. If ground-contact metadata is missing, diagnostics report that explicitly. If no profile is available, the configured default boundary temperature is used with a warning diagnostic; ground is not silently treated as outdoor air.

`AdjacentUnheatedSpace`: uses `adjacentTemperatureC`, or `boundaryTemperatureC` when supplied. Missing temperature is a diagnostic error.

`AdjacentConditionedZone`: uses `adjacentTemperatureC`, or `boundaryTemperatureC` when supplied. Equal room and adjacent temperatures produce zero heat flow.

`InternalAdiabatic`: returns zero heat flow and excludes the element from load totals. Diagnostics state that the element was excluded.

## Sign Convention

Positive `heatFlowW` means heat leaves the room. It is counted in `totalHeatLossW`.

Negative `heatFlowW` means heat enters the room. Its magnitude is counted in `totalHeatGainW`.

Cooling calculations can therefore receive a positive gain magnitude when outdoor temperature is higher than indoor temperature. If outdoor temperature is lower than indoor temperature, the transmission component can be negative before the upper-level cooling load clamp.

## Result Breakdown

The result includes totals and one row per element:

- `elementId`;
- `elementType`;
- `boundaryType`;
- `areaM2`;
- `uValueWPerM2K`;
- `deltaTC`;
- `heatFlowW`;
- `isIncludedInLoad`;
- diagnostics.

## Validation And Diagnostics

The engine validates:

- `areaM2 > 0`;
- `uValueWPerM2K > 0`;
- indoor temperature is within the supported range;
- required boundary temperature is present;
- correction factor is not negative.

Invalid elements are excluded from load totals and return calculation diagnostics. The engine does not silently substitute zero or arbitrary boundary temperatures.

## Deterministic Fixtures

The following deterministic fixtures cover this stage:

- `transmission-single-external-wall-winter.json`;
- `transmission-single-window-winter.json`;
- `transmission-adiabatic-internal-wall.json`;
- `transmission-adjacent-conditioned-same-temperature.json`;
- `transmission-outdoor-cooling-gain.json`.

They verify formula output, sign convention, adiabatic exclusion, adjacent-zone zero flow, and expected diagnostics.

## Limitations

This stage does not include:

- solar gains;
- ventilation;
- infiltration;
- internal gains;
- a full ground-contact heat transfer model;
- coupled multi-zone heat balance.

Ground transfer currently uses the supplied temperature and correction factor path and reports simplified-ground/profile diagnostics. It is not a full ground-contact model.
