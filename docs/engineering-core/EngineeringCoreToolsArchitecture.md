# Engineering Core Tools Architecture

## Purpose

Engineering Core automation must be implemented in C# tools, not in large PowerShell scripts.

## Repository structure

- `src/Backend` — application code.
- `src/Frontend` — frontend code.
- `tests` — test projects.
- `docs` — documentation and generated evidence.
- `tools` — C# automation, validation and release tools.
- `scripts` — thin wrappers only.
- `.github/workflows` — CI entry points that call tools/scripts.

## Current C# tool

The current Engineering Core tool is:

```text
tools/AssistantEngineer.Tools.EngineeringCore
```

It owns calculation-module inventory generation and calculation-module verification commands.

## Wrapper rule

A script in `scripts/engineering-core` may:

- set `$ErrorActionPreference`;
- resolve repository root;
- call `dotnet run --project tools/... -- <command>`;
- pass through arguments.

A script in `scripts/engineering-core` must not contain heavy generation, validation or release logic.

## Why

This keeps project behavior testable, refactorable and close to the application language.

PowerShell remains useful for local/CI entry points, but C# owns the actual automation.
