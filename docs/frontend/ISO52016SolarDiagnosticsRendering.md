# ISO 52016 Solar Diagnostics Rendering

Stage 7 makes ISO 52016 solar-path diagnostics visible in frontend/report UI.

## Closed scope

- `EngineeringCoreDisclosurePanel` renders `diagnostics` from API/report objects.
- The panel can render diagnostics even when `calculationDisclosure` is absent.
- The panel highlights the solar path:
  - `Iso52016.WeatherSolarContextUsed`;
  - `Iso52016.SolarGainComponentPathUsed`;
  - `Iso52016.MatrixSolarRadiationFallbackUsed`.
- Matrix fallback is displayed as a warning path, not hidden in raw JSON.

## Explicit non-claims

- This does not claim exact EnergyPlus numerical parity.
- This does not claim ASHRAE 140 validation coverage.
- This does not remove matrix fallback support.
- This does not add new calculation formulas.

## Guard tests

- `EngineeringCoreDiagnosticsFrontendRenderingTests.EngineeringCoreDisclosurePanelRendersCalculationDiagnostics`
- `EngineeringCoreDiagnosticsFrontendRenderingTests.EngineeringCoreDisclosurePanelCanRenderDiagnosticsWithoutCalculationDisclosure`
- `EngineeringCoreDiagnosticsFrontendRenderingTests.FrontendDiagnosticsDocumentationMentionsIso52016SolarPathCodes`

## User-facing expectation

A user viewing ISO 52016 hourly/monthly/API/report output should be able to see whether the result used the ISO 52016 weather-solar context or the legacy solar-radiation fallback before opening the raw JSON inspector.
