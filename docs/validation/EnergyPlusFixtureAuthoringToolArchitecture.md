# EnergyPlus Fixture Authoring Tool Architecture

## Purpose

EnergyPlus validation fixture authoring must live in a C# tool.

PowerShell scripts under `scripts/engineering-core` must remain thin wrappers.

## Current tool

```text
tools/AssistantEngineer.Tools.EnergyPlusFixtureAuthoring
```

## Command owned by the tool

- `new-fixture`

## Responsibilities

- CaseId validation;
The C# tool owns:

- `CaseId` validation;
- template expansion;
- fixture directory creation;
- documentation scaffold creation;
- default non-claim boundary text;
- next-step guidance.

## Wrapper rule

`new-energyplus-validation-fixture.ps1` may only:

- accept PowerShell-friendly parameters;
- resolve repository root;
- translate parameters into C# tool arguments;
- call `dotnet run --project tools/...`.

It must not contain template expansion logic.

## Non-claims

PlaceholderComparison is not real EnergyPlus validation.

Future real validation must remain tolerance-based and provenance-backed.

This tool does not claim exact EnergyPlus numerical parity.

This tool does not claim ASHRAE 140 validation coverage.

