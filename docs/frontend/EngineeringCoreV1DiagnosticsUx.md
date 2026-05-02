# Engineering Core V1 Diagnostics UX

## Purpose

This document defines how the frontend should display Engineering Core V1 diagnostics.

Diagnostics should help users understand whether a result is valid, adapted, simplified or blocked.

## Severity display

| Severity | Visual treatment | User expectation |
|---|---|---|
| Error | Blocking alert / red error state. | User must correct input before trusting result. |
| Warning | Visible warning panel near results. | Result may be usable, but limitations/fallbacks must be reviewed. |
| Info | Details or metadata area. | Explains method/source/status. |

## Do not hide warnings

Warnings must not be hidden only in:

- raw JSON;
- browser console;
- debug-only panels;
- collapsed developer-only sections.

Warnings should be visible in normal result/report UI.

## User action text

Every user-facing diagnostic should have a clear action.

Examples:

- Enter a positive building area before calculating annual EUI.
- Provide cooling COP for final energy conversion.
- Provide hourly profiles for coincident hourly aggregation.
- Use EPW or PVGIS normalized weather for weather-driven annual analysis.
- Do not present this result as true hourly annual 8760 simulation unless HourlyRecordCount is 8760.

## Annual 8760 diagnostics

For annual energy, the frontend should highlight whether the result satisfies:

    EnergyDataSource = TrueHourlySimulation
    IsTrueHourly8760 = true
    HourlyRecordCount = 8760

If the result contains AnnualEnergy.Not8760, AnnualEnergy.MonthlyBalanceAdapter, AnnualEnergy.SyntheticWeather or SolarWeather.SyntheticWeatherUsed, the UI must avoid labeling it as true hourly 8760 annual simulation.

## Report disclosure relationship

Report calculationDisclosure and diagnostics are complementary.

calculationDisclosure explains:

- core status;
- scope;
- assumptions;
- explicit non-claims;
- out-of-scope v1 items.

Diagnostics explain:

- specific calculation warnings;
- invalid inputs;
- source/method metadata;
- fallback behavior.

Both should be visible.
