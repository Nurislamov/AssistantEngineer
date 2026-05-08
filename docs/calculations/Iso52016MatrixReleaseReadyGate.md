# ISO 52016 Matrix Release-Ready Gate

This gate is the pre-merge/pre-release command for the ISO52016 Matrix and Physical-chain verification lane.

Durable release-readiness orchestration is owned by:

```text
tools/AssistantEngineer.Tools.Iso52016Verification
docs/verification/Iso52016VerificationRegistry.json
```

The PowerShell entrypoint is a thin wrapper only:

```powershell
.\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1
```

Equivalent direct command:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- assert-release-ready
```

## Fast Local Check

```powershell
.\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1 -SkipTests
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- assert-release-ready --skip-tests
```

## Optional Strict Mode

Require a clean working tree:

```powershell
.\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1 -RequireCleanGit
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- assert-release-ready --require-clean-git
```

## What The Gate Checks

- The ISO52016 verification registry parses.
- Required docs, source files, test files, and manifests exist.
- Release-ready manifests parse.
- Claim boundaries and non-claims are present.
- Forbidden positive equivalence claims are absent.
- Generated artifact paths are not tracked by git.
- PowerShell scripts listed in the registry remain thin wrappers.
- Registry-owned test filters pass unless `--skip-tests` is used.

## Generated Artifacts

Generated artifact paths are listed in `docs/verification/Iso52016VerificationRegistry.json`.

Generated artifacts must not be committed, including Matrix baseline summaries, external-validation anchor outputs, engineering edge-case outputs, application integration hardening outputs, and physical-chain generated outputs.

## Claim Boundary

Validation/internal engineering anchors only.

No full ISO 52016 equivalence claim, no StandardReference equivalence claim, no EnergyPlus comparison workflow claim, no ASHRAE 140 / BESTEST-style validation anchor claim, and no complete ISO52010/ISO52016 compliance claim is made by this gate.

