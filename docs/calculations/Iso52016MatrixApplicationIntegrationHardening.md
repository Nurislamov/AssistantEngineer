# ISO 52016 Matrix application integration hardening

This stage adds application-level integration hardening anchors for the ISO 52016 Matrix calculation path.

## Scope

Application integration hardening only.

The anchors verify integration contracts between:

- building/domain room objects;
- the ISO 52016 building simulation facade;
- room envelope input mapping;
- hourly building result aggregation;
- monthly and annual report-path aggregation.

The fixtures use `sourceType = ManualEngineeringIntegrationAnchor`.

## Added anchors

| Anchor | Contract |
| --- | --- |
| `APPLICATION-ISO52016-MATRIX-INTEGRATION-001` | Building facade room/hour/month/annual aggregation invariant. |
| `APPLICATION-ISO52016-MATRIX-INTEGRATION-002` | Duplicate room names are rejected before simulation. |
| `APPLICATION-ISO52016-MATRIX-INTEGRATION-003` | AdjacentUnconditioned envelope boundary contributes to transmission; AdjacentConditioned and Adiabatic do not. |
| `APPLICATION-ISO52016-MATRIX-INTEGRATION-004` | Building result `GetHour` preserves hour-of-year indexing and guards out-of-range access. |
| `APPLICATION-ISO52016-MATRIX-INTEGRATION-005` | Monthly report summaries reconcile with annual hourly totals. |

## Non-claims

Validation anchors only, not full parity.

No pyBuildingEnergy parity claim.
No EnergyPlus parity claim.
No ASHRAE 140 validation coverage claim.
No full ISO 52016 parity claim.
No external application parity claim.

These anchors are not pyBuildingEnergy outputs and are not EnergyPlus outputs. They are independent application integration contract checks.