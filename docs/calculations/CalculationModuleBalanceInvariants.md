# Calculation Module Balance Invariants

## Purpose

This document defines the first deepening invariant layer for `AssistantEngineer.Modules.Calculations`.

The goal is to protect the calculation module against silent regressions while deeper formulas are added.

## Room load invariants

For design-point room loads:

- room load must equal the positive heating breakdown total for heating;
- room load must equal the positive cooling breakdown total for cooling;
- heating W/m² must equal heating load divided by room area;
- cooling W/m² must equal cooling load divided by room area;
- successful room results must not contain error diagnostics;
- negative fixed components must be clamped and reported as diagnostics.

## Aggregation invariants

For design-point aggregation:

- building/floor/thermal-zone heating load must equal the sum of selected child room heating loads;
- building/floor/thermal-zone cooling load must equal the sum of selected child room cooling loads;
- total area must equal the sum of selected child room areas;
- W/m² values must be calculated from aggregate load and aggregate area;
- component breakdown must preserve transmission, solar, ventilation, infiltration, internal gain and ground buckets.

## Hourly fallback invariant

When hourly aggregation is requested but complete hourly room profiles are unavailable, the engine must:

- fall back to design-point aggregation;
- emit an `Aggregation.HourlyUnavailable` warning;
- not claim true coincident hourly aggregation.

## Non-claims

These invariants do not claim exact EnergyPlus numerical parity.

This document does not claim exact EnergyPlus numerical parity.

These invariants do not claim ASHRAE 140 validation coverage.

These invariants do not claim full ISO 52016 node/matrix solver parity.

They only protect internal consistency of the current Engineering Core V1 calculation path.


