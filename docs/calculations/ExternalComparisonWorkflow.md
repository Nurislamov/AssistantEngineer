# External Comparison Workflow

AssistantEngineer uses an external comparison workflow as an internal engineering anchor.

This workflow is not full validation.
This workflow is not a compliance claim.

## Scope

- define comparison cases and fixture metadata;
- import external comparison output with provenance;
- compare against documented tolerances;
- report case status progression.

## Claim Boundary

- no EnergyPlus validation claim;
- no ASHRAE 140 validation claim;
- no BESTEST pass claim;
- not full validation;
- not compliance claim.

## Status Model

- `Planned`
- `FixtureDefined`
- `ExternalOutputImported`
- `Compared`
- `PassedTolerance`
- `FailedTolerance`
- `NotAValidationClaim`

`PassedTolerance` requires expected output, tolerance, and provenance.
