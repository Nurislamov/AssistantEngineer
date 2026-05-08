# ISO 52016 Matrix engineering edge cases release notes

## Status

Closed candidate.

## Scope

Engineering edge-case hardening only.

Validation anchors only, not full equivalence claim.

## Included edge-case anchors

- Two-node free-floating implicit thermal response.
- Adjacent/unconditioned boundary heating load.
- Timestep energy scaling for steady controlled load.
- Positive internal gain sign conventions.
- Monthly and annual aggregation edge case.

## Verification

```powershell
.\scripts\iso52016\assert-iso52016-matrix-engineering-edge-cases-release-ready.ps1
.\scripts\iso52016\verify-iso52016-matrix-all.ps1
dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj
```

## Explicit non-claims

Engineering edge-case hardening only.

Validation anchors only, not full equivalence claim.

No StandardReference equivalence claim.

No EnergyPlus comparison workflow claim.

No ASHRAE 140 / BESTEST-style validation anchor coverage claim.

No full ISO 52016 equivalence claim.

No ExternalReferenceCovered claim.

No FullReferenceCovered claim.