# Calculation Module Diagnostics Consistency

## Purpose

This document defines the diagnostics consistency layer for `AssistantEngineer.Modules.Calculations`.

The goal is to prevent hidden calculation assumptions while deeper calculation methods are added.

## Rules

### Error diagnostics

Invalid mandatory inputs must be visible as error diagnostics or validation failures.

Examples:

- invalid room area;
- invalid aggregation room area;
- invalid annual energy building area;
- missing required hourly records.

### Warning diagnostics

Warnings are required when a result is still produced, but the calculation used a fallback, simplification or non-ideal input.

Examples:

- hourly aggregation requested but hourly profiles are unavailable;
- hourly aggregation uses profiles with mismatched lengths;
- AnnualEnergy.MonthlyBalanceAdapter warning must be emitted when monthly balance adapter data is used instead of true hourly 8760 simulation.
- synthetic weather is used;
- monthly balance adapter is used instead of true hourly 8760;
- non-8760 record count is used;
- annual-energy hourly inputs do not cover all 12 calendar months;
- negative values are clamped.
- contradictory system performance assumptions are supplied;

- SystemEnergy.HeatingDualPerformanceAssumption and SystemEnergy.DhwDualPerformanceAssumption warnings must be emitted when both efficiency and COP are supplied for the same useful-energy end use.

- Aggregation.HourlyProfileLengthMismatch warning must be emitted when hourly room profiles have different lengths and the shortest common profile length is used.

- AnnualEnergy.MonthlyCoverageIncomplete warning must be emitted when supplied hourly annual-energy inputs do not cover all 12 calendar months.

### Info diagnostics

Info diagnostics may be used for traceability when the result is valid and no fallback was needed.

Examples:

- true hourly simulation source used;
- annual-energy hourly inputs cover all 12 calendar months;
- signed component balance is available;
- external weather source was identified.

## Successful result invariant

A successful calculation result should not hide fatal invalid state.

Where engines currently return successful results with error diagnostics, those diagnostics must remain visible and covered by tests until the contract is intentionally tightened.

## Annual energy source disclosure

A result must not be presented as true hourly 8760 unless all of these are true:

- `EnergyDataSource = TrueHourlySimulation`;
- `IsTrueHourly8760 = true`;
- `HourlyRecordCount = 8760`.

Partial hourly simulation, synthetic weather and monthly adapters must stay visible as warnings.

## Non-claims

Diagnostics consistency does not claim exact EnergyPlus numerical parity.

Diagnostics consistency does not claim ASHRAE 140 validation coverage.

Diagnostics consistency does not claim full ISO 52016 node/matrix solver parity.

Diagnostics consistency only protects internal transparency of current calculation behavior.




