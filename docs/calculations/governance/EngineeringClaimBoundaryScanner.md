# Engineering Claim Boundary Scanner

Stage id: `AE-GOVERNANCE-002`

The claim scanner is a deterministic C# governance scanner for forbidden positive claims across:
- `docs/**/*.md`
- `docs/**/*.json`
- `tests/fixtures/**/*.json`

It checks forbidden parity/compliance/certification tokens and allows them only in explicit negative context.

Claim boundary:
- Engineering Core V2 governance and internal release readiness.
- Internal deterministic engineering governance only.
- Compatibility behavior preserved by default.
- Inspired calculation paths remain opt-in.
- No full ISO/EN compliance claim.
- No pyBuildingEnergy parity claim.
- No EnergyPlus parity claim.
- No ASHRAE 140 validation claim.
- No external certification claim.
- No automatic production data mutation.

Notes:
- controlled positive-claim test inputs live in `tests/fixtures/governance/claim-scanner-inputs`;
- production repository scans exclude that folder by default.
