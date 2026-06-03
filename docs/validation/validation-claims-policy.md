# Validation Claims Policy (P9-00)

## Purpose

Define allowed and forbidden validation-claim language for engineering-calculation evidence in AssistantEngineer.

## Scope

This policy applies to:

- validation docs and evidence manifests;
- validation-related tests and fixtures;
- validation-oriented tooling output language.

Canonical terminology reference:
- `docs/architecture/terminology-and-claims-vocabulary.md`

## Non-claims

- No calculation physics change claim.
- No public API route change claim.
- No DTO shape change claim.
- No ownership backfill execution claim.
- No production apply enabled claim.
- No full tenant isolation claim.
- No production security certification claim.

## Allowed claims

- allowed: reference-informed
- allowed: validation anchor
- allowed: internal invariant
- allowed: manual engineering reference fixture
- allowed: independent reference fixture (where evidence exists)
- allowed: external tool reference candidate (where formal evidence is incomplete)
- allowed: governance-ready
- allowed: write-path intentionally disabled

## Forbidden claims

- forbidden: full pyBuildingEnergy parity
- forbidden: EnergyPlus parity
- forbidden: ASHRAE 140 validated
- forbidden: ISO certified
- forbidden: fully validated
- forbidden: production certified
- forbidden: full tenant isolation
- forbidden: ownership backfill executed
- forbidden: production apply enabled
- forbidden: DB RLS enabled
- forbidden: global EF query filters enabled

## Boundary clarifications

- `validation anchor` does not mean formal validation completion.
- `reference-informed` does not mean donor-project parity.
- `EnergyPlus-style fixture naming` does not mean external simulator parity.
- `ISO implementation target` does not mean ISO certification.
- `manual engineering fixture` does not mean compliance certification.

## Usage guidance

- New validation artifacts must include explicit non-claims when discussing external references.
- Any stronger claim requires explicit evidence and governance approval stage linkage.
- Positive forbidden-claim wording remains blocked by guardrail tests.

