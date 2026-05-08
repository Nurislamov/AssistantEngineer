# System Energy

This Standard-Based Calculation step converts useful heating, cooling and DHW energy into simplified final energy.

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

## Limits

This is simplified system energy, not a detailed HVAC simulation.
