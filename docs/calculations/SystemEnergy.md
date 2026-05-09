# System Energy

This Standard-Based Calculation step converts useful heating, cooling and DHW energy into simplified final energy.

The pipeline now includes an internal useful-energy handoff path that maps building useful heating/cooling demand and optional DHW useful demand into the EN15316-style circuit-level system-energy path when explicit opt-in is enabled.

## Formula

```text
finalHeatingKWh = usefulHeatingKWh / heatingEfficiency
finalCoolingKWh = usefulCoolingKWh / coolingCOP
finalDhwKWh = usefulDhwKWh / dhwEfficiency
totalFinalKWh = finalHeatingKWh + finalCoolingKWh + finalDhwKWh + fanEnergyKWh
```

If primary energy factor is supplied:

```text
primaryEnergyKWh = totalFinalKWh * primaryEnergyFactor
```

## Diagnostics

Efficiencies and COP values must be greater than zero when supplied. If no system assumption is supplied for an end use, useful energy is carried through as final energy with diagnostics.

## Deterministic Fixtures

- `system-heating-efficiency.json`
- `system-cooling-cop.json`
- `system-total-energy.json`

## EN15316-Style Handoff Scope

- method label in handoff contracts: `Standard-Based Calculation`;
- source modules are preserved in handoff metadata;
- timestep and month indexes are preserved from true hourly (8760) or monthly fallback sources;
- energy service type is preserved per entry:
  - space heating
  - space cooling
  - domestic hot water;
- carrier selection is preserved per service entry.

This handoff is an internal engineering anchor for deterministic integration. It is not a full EN15316 compliance claim and not an external validation claim.

## Limits

This is simplified system energy, not a detailed HVAC simulation.
