# Engineering Core V1 Operational Runbook

## Purpose

This runbook describes how to operate, verify and support Engineering Core V1 after closure.

Engineering Core V1 is closed as an engineering formula gate. It is not an exact external simulator parity release.

## Daily verification

For normal development:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1 -Fast

Before merging calculation, reporting, diagnostics or frontend visibility changes:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

Before release handoff:

    .\scripts\engineering-core\verify-engineering-core-v1-manifest.ps1
    .\scripts\engineering-core\generate-engineering-core-v1-release-evidence.ps1

## Health indicators

Healthy state means:

- FormulaAuditMatrix contains no Partial formula items;
- all closed formula gates are ClosedV1;
- status endpoint returns ClosedV1;
- report disclosure exists in heating/cooling reports;
- dashboard shows Engineering Core V1 status;
- report UI shows calculationDisclosure before raw JSON;
- diagnostics catalog exists and exposes Error/Warning/Info rules;
- verification script passes;
- CI workflow exists.

## Status endpoint

Use:

    GET /api/v1/calculations/engineering-core/v1/status

Expected:

- status = ClosedV1;
- formulaGatesClosed = true;
- weather8760GatesClosed = true;
- annualHourly8760GateClosed = true;
- explicitNonClaims includes EnergyPlus, pyBuildingEnergy and ASHRAE 140 non-claims.

## Diagnostics catalog endpoint

Use:

    GET /api/v1/calculations/engineering-core/v1/diagnostics-catalog

Expected:

- Error means calculation must fail;
- Warning means fallback/simplification/partial source;
- Info means metadata;
- userMessage and userAction are non-empty;
- annual 8760 warning diagnostics tell the UI not to present non-8760 results as true annual 8760.

## Release evidence generation

Generate release evidence:

    .\scripts\engineering-core\generate-engineering-core-v1-release-evidence.ps1

Output:

    docs/reports/EngineeringCoreV1ReleaseEvidence.md

The evidence report summarizes:

- closed gates;
- out-of-scope items;
- planned validation;
- annual 8760 flags;
- diagnostics counts;
- diagnostics categories;
- documentation inventory;
- non-claims.

## Support procedure

When a calculation issue is reported:

1. identify calculation area and FormulaAuditMatrix gate;
2. check whether the issue is invalid input, fallback behavior, simplified scope or a true bug;
3. inspect diagnostics severity;
4. confirm successful result does not contain Error diagnostics;
5. check report calculationDisclosure;
6. check frontend visibility;
7. run focused tests;
8. run Engineering Core V1 verification script.

## Escalation

Escalate as future work when issue requires:

- no exact EnergyPlus parity claim;
- ASHRAE 140 certification;
- full ISO 52016 node/matrix solver;
- latent/moisture balance;
- detailed HVAC plant simulation;
- equipment part-load performance curves.

These are not Engineering Core V1 support regressions unless a current ClosedV1 claim is broken.
