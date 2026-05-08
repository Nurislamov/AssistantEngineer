# Engineering Core V2 Release Readiness

Stage id: `AE-RELEASE-READINESS-002`

This gate aggregates:
- engineering stage registry validation;
- claim boundary scanning;
- rollup mode coverage;
- opt-in default safety checks;
- building input validation governance stage presence;
- generated artifact policy checks;
- disclosure file presence checks.

Claim boundary:
- Engineering Core V2 governance and internal release readiness.
- Internal deterministic engineering governance only.
- Compatibility behavior preserved by default.
- Inspired calculation paths remain opt-in.
- No full ISO/EN compliance claim.
- No StandardReference equivalence claim.
- No EnergyPlus comparison workflow claim.
- No ASHRAE 140 / BESTEST-style validation anchor claim.
- No external certification claim.
- No automatic production data mutation.

Readiness result:
- `Ready`
- `ReadyWithWarnings`
- `Blocked`
- `NotEvaluated`

Tooling:
- `dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringGovernance -- list-stages --repo-root .`
- `dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringGovernance -- verify-manifests --repo-root .`
- `dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringGovernance -- verify-claims --repo-root .`
- `dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringGovernance -- verify-release-readiness --repo-root .`

Limitations:
- this is internal deterministic governance only;
- no external certification;
- no full ISO/EN compliance claim;
- no StandardReference equivalence claim;
- no EnergyPlus comparison workflow claim;
- no ASHRAE 140 / BESTEST-style validation anchor claim;
- inspired paths stay opt-in;
- external numerical validation remains incomplete.
