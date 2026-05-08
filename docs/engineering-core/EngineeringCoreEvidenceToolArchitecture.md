# Engineering Core Evidence Tool Architecture

## Purpose

Engineering Core evidence generation must live in C# tools.

PowerShell scripts under `scripts/engineering-core` must remain thin wrappers.

## Current tool

```text
tools/AssistantEngineer.Tools.EngineeringCoreEvidence
```

## Commands owned by the tool

- `generate-release-evidence`
- `generate-export-disclosure-checklist`
- `generate-traceability-matrix`
- `generate-all-evidence`

## Wrapper rule

The corresponding PowerShell scripts may only resolve repository root and call the C# tool.

They must not contain Markdown generation, JSON parsing, JSON construction, traceability matrix logic or export checklist logic.

## Non-claims

This tool does not claim exact EnergyPlus numerical equivalence.

This tool does not claim exact StandardReference numerical equivalence.

This tool does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.
