# Engineering Stage Manifest Registry

Stage id: `AE-GOVERNANCE-001`

This registry is the Engineering Core V2 governance index for deterministic stage manifests under `docs/releases/*.json`.

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

What it checks:
- required stage manifests exist;
- stage ids and manifest paths are unique;
- manifest fields are complete;
- referenced files exist;
- dependencies resolve or are explicitly legacy/external;
- generated artifacts policy remains deterministic and non-committed.

Limitations:
- this is an internal deterministic governance anchor only;
- it is not external validation and not certification.
