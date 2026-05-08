# Engineering Core Release Tool Architecture

## Purpose

Engineering Core release/profile orchestration must live in C# tools.

PowerShell scripts under `scripts/engineering-core` must remain thin wrappers.

## Current tool

```text
tools/AssistantEngineer.Tools.EngineeringCoreRelease
```

## Commands owned by the tool

- `regenerate-artifacts`
- `verify-smoke`
- `verify-contracts`
- `verify-manifest`
- `assert-release-ready`

## Wrapper rule

The corresponding PowerShell scripts may only:

- resolve the repository root;
- translate PowerShell switches into tool arguments;
- call `dotnet run --project tools/...`.

They must not contain the release readiness file list, verification step list or artifact regeneration sequence.

## Non-claims

The release gate does not claim exact EnergyPlus numerical equivalence.

The release gate does not claim exact StandardReference numerical equivalence.

The release gate does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.
