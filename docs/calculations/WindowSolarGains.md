# Window Solar Gains

This document describes the current Energy Calculation equivalence scope for solar gains through windows.

## Scope

This stage covers:

- solar irradiance already mapped to the window plane;
- centralized solar position and isotropic sky surface irradiance calculations for the weather-solar path;
- window orientation azimuth;
- window tilt;
- SHGC;
- frame factor;
- internal shading factor;
- external shading factor;
- fixed shading factor;
- total window solar heat gain;
- room-level aggregation across windows.

Surface irradiance can come from the existing weather-solar path, or it can be supplied directly as deterministic input. Window solar gains are not mixed with window transmission heat transfer.

## Application Pipeline Source Priority

For room cooling load endpoints, `EnergyCalculationPipelineService` resolves incident irradiance in this order:

1. hourly weather/solar context when available to the hourly path;
2. annual climate direct/diffuse solar data through the centralized surface irradiance path;
3. orientation reference irradiance fallback.

The result diagnostics expose the source as `AnnualClimateData` or `ReferenceByOrientationFallback`. When the fallback is used, diagnostics warn that hourly weather/solar context was not available.

Related diagnostics include:

- `SolarWeather.HourlyWeatherSourceUsed`;
- `SolarWeather.AnnualClimateSolarDataUsed`;
- `SolarWeather.ReferenceByOrientationFallbackUsed`;
- `SolarWeather.SyntheticWeatherUsed`;
- `SolarWeather.MissingDirectDiffuseSolarData`;
- `SolarWeather.NightSolarClampedToZero`;
- `SolarWeather.SurfaceIrradianceCalculated`.

Existing `SolarGains.IrradianceSource` and `SolarGains.ReferenceByOrientationFallback` diagnostics remain for compatibility.

## Surface Irradiance

The weather-solar path calculates surface irradiance from:

- latitude, timestamp and longitude for solar position;
- surface azimuth and tilt;
- direct normal irradiance;
- diffuse horizontal irradiance;
- global horizontal irradiance, supplied or derived from projected direct plus diffuse;
- ground reflectance.

The surface result contains direct, diffuse sky, ground-reflected and total incident irradiance. Surface tilt must be `0..180`; azimuth is normalized to `0..360`.

When solar altitude is below or equal to the horizon, direct, diffuse sky, ground-reflected and total surface irradiance are clamped to zero. This prevents nighttime diffuse/global source noise from creating window solar gains.

## Formula

The simplified deterministic formula is:

```text
SolarGainW =
    AreaM2
    x IncidentIrradianceWPerM2
    x SHGC
    x FrameFactor
    x InternalShadingFactor
    x ExternalShadingFactor
    x FixedShadingFactor
```

When direct, diffuse, and ground-reflected irradiance components are supplied, the engine also returns component gains. The total gain is still based on the same effective solar factor.

## Units

- `areaM2`: m2;
- `incidentIrradianceWPerM2`: W/m2;
- `solarGainW`: W;
- `SHGC`: 0..1;
- shading factors: 0..1;
- `orientationAzimuthDeg`: 0..360 degrees;
- `tiltDeg`: 0..180 degrees.

## Factors

`SHGC` is the solar heat gain coefficient of the glazing system.

`FrameFactor` is the transparent area multiplier. A value of `1.0` means no frame reduction in the deterministic engine.

`InternalShadingFactor`, `ExternalShadingFactor`, and `FixedShadingFactor` are multiplicative reduction factors. They must be between `0` and `1`.

The engine returns `effectiveSolarFactor`, which is:

```text
SHGC x FrameFactor x InternalShadingFactor x ExternalShadingFactor x FixedShadingFactor
```

## Orientation And Tilt

Orientation and tilt are preserved in the input and result. When the existing weather-solar profile is used, orientation selects the matching surface irradiance record, so south/north/east/west vertical surfaces can receive different incident irradiance under the same weather hour. When irradiance is supplied directly, orientation and tilt are diagnostics context and result metadata; the window gain engine does not recalculate solar position.

## Sign Convention

Window solar gains are positive heat gains into the room. Night or zero incident irradiance returns zero solar gain.

## Diagnostics

The engine validates:

- `areaM2 > 0`;
- `SHGC` is present and within `0..1`;
- frame and shading factors are within `0..1`;
- irradiance values are non-negative;
- orientation azimuth is within `0..360`;
- tilt is within `0..180`.

Missing frame factor uses a documented default of `1.0` and returns a warning diagnostic. Missing SHGC is an error; it is not silently defaulted.

Diagnostics also state whether incident irradiance or component irradiance was provided, include the effective solar factor, and report night clamping when applicable.

## Deterministic Fixtures

The following deterministic fixtures cover this stage:

- `window-solar-single-window-no-shading.json`;
- `window-solar-single-window-with-shading.json`;
- `window-solar-night-is-zero.json`;
- `window-solar-invalid-shgc-diagnostics.json`;
- `window-solar-room-aggregation.json`.

They verify no-shading gain, combined factors, night/zero irradiance, validation diagnostics, and room-level aggregation.

Active benchmark comparison fixtures also cover deterministic window and surface cases:

- `solar-night-zero.json`;
- `window-solar-gain-basic.json`;
- `window-solar-gain-with-shading.json`;
- `surface-irradiance-night-zero.json`.

These are deterministic benchmark references. They do not mark the feature `ExternalReferenceCovered`.

## Limitations

This stage does not include:

- window transmission heat transfer;
- ventilation;
- infiltration;
- internal gains;
- domestic hot water;
- equipment;
- a full rewrite of room cooling load;
- a full ISO 52010 clone.

The engine consumes provided surface irradiance. The existing weather-solar path remains responsible for mapping weather and sun position to surface irradiance. The application fallback is documented and diagnosed; it is not a full solar-context replacement.
