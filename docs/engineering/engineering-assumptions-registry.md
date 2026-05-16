# Engineering Assumptions Registry

## Purpose

This registry captures engineering assumptions, defaults, and fallbacks used across the calculation core, validation fixtures, and workflow orchestration.
It provides a governed reference for what is active, what is validation-only, and what still requires audit.
Assumption units must follow `docs/engineering/units-governance.md`.

## Scope

This registry covers:

- heating and cooling design assumptions;
- ventilation and infiltration assumptions;
- internal gains assumptions;
- schedules and profile assumptions;
- weather and solar assumptions;
- ground boundary assumptions;
- domestic hot water assumptions;
- system energy assumptions;
- equipment sizing assumptions;
- validation fixture assumptions.

## Non-claims

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No pyBuilding\u0045nergy parity claim.
- No full ISO/EN compliance claim.
- No certified/certification claim.

## Registry status model

- ActiveDefault: currently used runtime default or fallback.
- ValidationOnly: used only in validation fixtures, not runtime default logic.
- Candidate: proposed future assumption, not used in runtime.
- Deprecated: historical assumption, do not use in new code.
- UnknownNeedsAudit: discovered assumption not yet confirmed from runtime source.

## Required fields per assumption

Each registry entry must include:

- assumptionId
- category
- name
- value
- unit
- status
- source
- usageArea
- codeReference or fixtureReference
- rationale
- risk
- owner = "Engineering"
- lastReviewedDate

## Initial registry entries

| assumptionId | category | name | value | unit | status | source | usageArea | codeReference | fixtureReference | rationale | risk | owner | lastReviewedDate |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ASSUMP-HVAC-INDOOR-HEATING-TEMP-001 | HeatingCoolingDesign | Default indoor heating design temperature | 20.0 | °C | UnknownNeedsAudit | Project defaults and calculation preferences (needs code confirmation) | Room and building heating load | UnknownNeedsAudit | n/a | Common heating design indoor baseline | Wrong default shifts design heating load | Engineering | 2026-05-14 |
| ASSUMP-HVAC-INDOOR-COOLING-TEMP-001 | HeatingCoolingDesign | Default indoor cooling design temperature | 24.0 | °C | UnknownNeedsAudit | Project defaults and calculation preferences (needs code confirmation) | Room and building cooling load | UnknownNeedsAudit | n/a | Common cooling indoor design target baseline | Wrong default shifts cooling load | Engineering | 2026-05-14 |
| ASSUMP-VENT-SENSIBLE-COEFFICIENT-001 | Ventilation | Simplified sensible air heat coefficient | 0.33 | Wh/(m³·K) | ValidationOnly | Manual arithmetic fixture convention | Manual ventilation and heating fixture arithmetic | n/a | MAN-ENG-HEAT-001, MAN-ENG-VENT-001 | Simple independent arithmetic for Tier 1 checks | Misuse in runtime would overstate certainty of simplified method | Engineering | 2026-05-14 |
| ASSUMP-VENT-DEFAULT-ACH-001 | Ventilation | Default air changes per hour | UnknownNeedsAudit | ACH | UnknownNeedsAudit | Runtime default/fallback not yet confirmed | Ventilation and infiltration load paths | UnknownNeedsAudit | n/a | Track pending audit for any ACH fallback | Unverified ACH could distort ventilation load outcomes | Engineering | 2026-05-14 |
| ASSUMP-GAINS-PEOPLE-001 | InternalGains | Default sensible heat gain per person | UnknownNeedsAudit | W/person | UnknownNeedsAudit | Runtime default/fallback not yet confirmed | Internal gains calculation | UnknownNeedsAudit | n/a | Track pending audit for occupant sensible default | Unverified occupant gain default impacts heating/cooling balance | Engineering | 2026-05-14 |
| ASSUMP-GAINS-LIGHTING-001 | InternalGains | Default lighting power density | UnknownNeedsAudit | W/m² | UnknownNeedsAudit | Runtime default/fallback not yet confirmed | Internal gains and cooling loads | UnknownNeedsAudit | n/a | Track pending audit for lighting default | Unverified lighting default biases cooling demand | Engineering | 2026-05-14 |
| ASSUMP-SOLAR-SHGC-001 | SolarShading | Solar heat gain coefficient | fixture-specific for MAN-ENG-SOLAR-001 | dimensionless | ValidationOnly | Tier 1 manual fixture input | Manual solar-gain arithmetic | n/a | MAN-ENG-SOLAR-001 | Isolated fixture assumption for transparent hand derivation | Treating fixture value as global default would be incorrect | Engineering | 2026-05-14 |
| ASSUMP-SOLAR-SHADING-FACTOR-001 | SolarShading | Simple shading factor | 0.80 | dimensionless | ValidationOnly | Tier 1 manual fixture input | Manual solar-gain arithmetic | n/a | MAN-ENG-SOLAR-001 | Independent simple shading reduction in fixture | Not representative of all facade/control conditions | Engineering | 2026-05-14 |
| ASSUMP-GROUND-EQUIVALENT-U-001 | GroundBoundary | Equivalent ground U-value | 0.30 | W/(m²·K) | ValidationOnly | Tier 1 manual fixture input | Manual ground boundary arithmetic | n/a | MAN-ENG-GROUND-001 | Simple steady-state fixture assumption | Not a detailed ISO 13370 coupling representation | Engineering | 2026-05-14 |
| ASSUMP-GROUND-EFFECTIVE-TEMP-001 | GroundBoundary | Effective ground temperature | 10.0 | °C | ValidationOnly | Tier 1 manual fixture input | Manual ground boundary arithmetic | n/a | MAN-ENG-GROUND-001 | Simplified fixed ground temperature for hand check | Fixed value can differ from climate and depth conditions | Engineering | 2026-05-14 |
| ASSUMP-DHW-WATER-DENSITY-001 | DomesticHotWater | Water density | 1.0 | kg/L | ValidationOnly | Tier 1 manual fixture input | Manual DHW arithmetic | n/a | MAN-ENG-DHW-001 | Simplifies mass conversion for independent derivation | Temperature-dependent density effects ignored | Engineering | 2026-05-14 |
| ASSUMP-DHW-SPECIFIC-HEAT-001 | DomesticHotWater | Water specific heat capacity | 0.001163 | kWh/(kg·K) | ValidationOnly | Tier 1 manual fixture input | Manual DHW arithmetic | n/a | MAN-ENG-DHW-001 | Fixed specific heat supports transparent arithmetic | Real-world variation with temperature is ignored | Engineering | 2026-05-14 |
| ASSUMP-SYS-DISTRIBUTION-EFF-001 | SystemEnergy | Distribution efficiency | 0.95 | dimensionless | ValidationOnly | Tier 1 manual fixture input | Manual system-energy chain arithmetic | n/a | MAN-ENG-SYS-001 | Isolates useful-to-final chain math in fixture | Not a runtime default and not seasonal/system specific | Engineering | 2026-05-14 |
| ASSUMP-SYS-GENERATION-EFF-001 | SystemEnergy | Generation efficiency | 0.90 | dimensionless | ValidationOnly | Tier 1 manual fixture input | Manual system-energy chain arithmetic | n/a | MAN-ENG-SYS-001 | Isolates generator conversion math in fixture | Does not encode part-load or seasonal behavior | Engineering | 2026-05-14 |
| ASSUMP-SYS-PRIMARY-FACTOR-FUEL-001 | SystemEnergy | Fuel primary energy factor | 1.10 | dimensionless | ValidationOnly | Tier 1 manual fixture input | Manual primary energy arithmetic | n/a | MAN-ENG-SYS-001 | Demonstrates transparent primary conversion path | Regulatory factors vary by jurisdiction and period | Engineering | 2026-05-14 |
| ASSUMP-SYS-PRIMARY-FACTOR-ELECTRICITY-001 | SystemEnergy | Electricity primary energy factor | 2.50 | dimensionless | ValidationOnly | Tier 1 manual fixture input | Manual primary energy arithmetic | n/a | MAN-ENG-SYS-001 | Demonstrates transparent auxiliary-primary conversion | Regulatory factors vary by grid and methodology | Engineering | 2026-05-14 |
