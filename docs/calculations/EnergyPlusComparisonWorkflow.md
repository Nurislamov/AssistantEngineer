# EnergyPlus External Comparison Workflow

This document defines the external comparison workflow foundation for EnergyPlus outputs in AssistantEngineer.

The workflow supports fixture-defined cases and deterministic metadata handling.

## Supported Foundation

- case metadata and status tracking;
- optional external output import;
- provenance requirement for imported output;
- tolerance-ready schema for future comparison runs.

## Current Boundary

- not full validation;
- not compliance claim;
- no fabricated comparison numbers;
- no pass status without imported external output and provenance.

## Fixtures

- `tests/fixtures/external-comparison/energyplus/ep-smoke-foundation.case.json`
