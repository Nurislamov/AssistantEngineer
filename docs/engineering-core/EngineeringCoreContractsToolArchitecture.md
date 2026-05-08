# Engineering Core Contracts Tool Architecture

## Purpose

Engineering Core API/report contract snapshot generation must live in C# tools.

PowerShell scripts under `scripts/engineering-core` must remain thin wrappers.

## Current tool

```text
tools/AssistantEngineer.Tools.EngineeringCoreContracts
```

## Commands owned by the tool

- `generate-api-contract-snapshots`
- `generate-report-contract-snapshots`
- `generate-all-contract-snapshots`

## Wrapper rule

The corresponding PowerShell scripts may only resolve repository root and call the C# tool.

They must not contain JSON construction, manifest parsing or report sample construction logic.

## Non-claims

This tool does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.

This tool does not claim exact EnergyPlus numerical equivalence.

Generated contract snapshots do not claim exact EnergyPlus numerical equivalence.

Generated contract snapshots do not claim ASHRAE 140 / BESTEST-style validation anchor coverage.


