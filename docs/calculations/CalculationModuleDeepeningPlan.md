# Calculation Module Deepening Plan

## Purpose

This document defines the next deepening layer for `AssistantEngineer.Modules.Calculations`.

The goal is not to add random formulas one by one.

The goal is to make the calculation module harder to break, easier to extend and clearer about what each engine is responsible for.

## Current core layers

The calculation module is expected to keep these layers visible:

- room load orchestration;
- envelope transmission;
- ventilation and infiltration sensible loads;
- internal sensible gains;
- window solar gains;
- weather/solar context;
- room/floor/building aggregation;
- annual energy balance;
- system useful/final/primary energy;
- equipment capacity sizing;
- application pipeline coordination.

## Deepening axes

### 1. Input normalization and units policy

All calculation engines must make units explicit.

Future input factories should normalize application/read-model data into engine contracts before formulas run.

### 2. Scenario fixtures

The next calculation module fixtures should cover:

- room heating design point;
- room cooling design point;
- floor aggregation;
- building aggregation;
- annual true hourly 8760 path;
- monthly fallback path;
- useful/final/primary system energy path;
- equipment sizing path.

### 3. Diagnostics consistency

Invalid mandatory inputs should produce errors.

Optional assumptions and simplifications should produce warnings.

Successful calculation results should not contain error diagnostics.

### 4. Balance invariants

Guard tests should verify:

- room load components sum to total load;
- floor/building aggregation equals sum of child loads;
- annual energy components preserve heating/cooling/DHW/fan separation;
- final energy and primary energy remain separate from useful energy;
- monthly fallback is never presented as true hourly 8760 simulation.

### 5. Method strategy isolation

Simplified methods, ISO-inspired methods and future external validation paths must remain explicit.

No code path should silently imply full ISO 52016, full ASHRAE 140 or exact EnergyPlus comparison workflow.

## Non-claims

This plan does not claim exact EnergyPlus numerical equivalence.

This plan does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.

This plan does not claim full ISO 52016 node/matrix solver equivalence.

This plan does not claim full ISO 13370 implementation.

This plan does not claim full EN 15316 system-chain implementation.

## Next implementation milestones

1. Calculation module inventory and boundary guard.
2. Scenario fixture catalog for internal calculation paths.
3. Cross-engine balance invariant tests.
4. Diagnostics consistency tests.
5. Method strategy and disclosure guard tests.
6. Real application pipeline scenario tests.
