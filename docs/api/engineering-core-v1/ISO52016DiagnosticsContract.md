# ISO 52016 Diagnostics API Contract

Stage 10 documents and guards the public API contract for ISO 52016 solar-path diagnostics.

## Contract

ISO 52016 hourly/monthly response DTOs expose:

```json
"diagnostics": [
  {
    "severity": "Info",
    "code": "Iso52016.WeatherSolarContextUsed",
    "message": "...",
    "context": "..."
  }
]
```

The field is optional for backward compatibility, but when present it must be forwarded by API/service layers and should be rendered by frontend/report UI.

## Required solar-path codes

- `Iso52016.WeatherSolarContextUsed`
- `Iso52016.SolarGainComponentPathUsed`
- `Iso52016.PerezAnisotropicModelVisibleInAnnualResult`
- `Iso52016.LegacySolarRadiationFallbackUsed`

## Consumer rule

A consumer must not infer the solar calculation path only from `solarGainsKWh`.

The source/method path must be read from `diagnostics`:

- `Iso52016.WeatherSolarContextUsed` means the ISO 52016 weather-solar context path was used.
- `Iso52016.SolarGainComponentPathUsed` means beam, diffuse sky and ground-reflected components fed window solar gains.
- `Iso52016.LegacySolarRadiationFallbackUsed` means the legacy fallback path was used and should be shown as a warning.

## Explicit non-claims

- No exact EnergyPlus numerical parity claim.
- No exact pyBuildingEnergy numerical parity claim.
- No ASHRAE 140 validation coverage claim.
- No full ISO 52016 node/matrix solver parity claim.

## Sample

See:

- `docs/api/engineering-core-v1/iso52016-diagnostics.sample.json`
