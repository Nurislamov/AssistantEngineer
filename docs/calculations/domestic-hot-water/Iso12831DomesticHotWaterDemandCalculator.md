# AE-DHW-001 ISO12831-3-inspired DHW demand depth

## Stage

- Stage id: `AE-DHW-001`
- Scope: pure C# domestic hot water demand calculator with deterministic reference modes and draw profiles.

## Claim boundary

- ISO12831-3-inspired domestic hot water engineering calculator.
- Internal deterministic engineering anchors only.
- No full ISO 12831-3 compliance claim.
- No StandardReference equivalence claim.
- No EnergyPlus comparison workflow claim.
- No ASHRAE 140 / BESTEST-style validation anchor claim.
- No external certification claim.

## Formula and assumptions

1. Equivalent occupants:
   - explicit input if provided;
   - otherwise category factor x people count.
2. Daily reference volume:
   - people-based, area-based, unit-based, or custom volume mode.
3. Daily draw energy:
   - `VolumeLiters x 1 kg/l x 4.186 kJ/(kg K) x (Thot - Tcold) / 3600`.
4. Daily total energy:
   - draw energy with distribution loss factor, plus storage and circulation losses.
5. Monthly and annual totals:
   - non-leap calendar (365 days).
6. Optional hourly profile:
   - weekday/weekend or category profile;
   - holiday dates can be treated as weekend draw behavior;
   - 8760 hourly distribution for non-leap year shape.

## Usage categories and reference modes

- Usage categories:
  - `ResidentialApartment`, `SingleFamilyHouse`, `Office`, `Hotel`, `School`, `Healthcare`, `Restaurant`, `Custom`
- Reference modes:
  - `PeopleBased`, `AreaBased`, `UnitBased`, `CustomVolume`

Reference data defaults are internal deterministic table-inspired anchors and are not a normative table reproduction.

## Draw profiles

- Profile kinds:
  - `ResidentialWeekdayWeekend`, `OfficeDaytime`, `HotelMorningEvening`, `SchoolDaytime`, `Flat`, `Custom`
- Profiles are validated for:
  - 24 values
  - finite non-negative entries
  - positive sum
- Profiles are normalized internally.

## Deterministic fixtures

- `tests/fixtures/domestic-hot-water/iso12831/residential-people-based-basic.json`
- `tests/fixtures/domestic-hot-water/iso12831/office-area-based-daytime.json`
- `tests/fixtures/domestic-hot-water/iso12831/hotel-unit-based-morning-evening.json`
- `tests/fixtures/domestic-hot-water/iso12831/custom-volume-flat-profile.json`
- `tests/fixtures/domestic-hot-water/iso12831/zero-occupants-zero-demand.json`

## Limitations

- internal engineering anchor model only;
- no full compliance output;
- no external certification output;
- no equivalence claims with external tools.

## Migration strategy

- existing `DomesticHotWaterDemandService` remains the compatibility production path;
- this stage introduces the pure calculator foundation consumed by controlled integration;
- controlled opt-in application integration is tracked in `AE-DHW-002`.
