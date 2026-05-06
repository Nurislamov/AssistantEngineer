# ISO52016 Verification Tool

`tools/AssistantEngineer.Tools.Iso52016Verification` owns ISO52016 Matrix and Physical-chain verification orchestration.

PowerShell files under `scripts/iso52016` are developer and CI entrypoints only. They must delegate to the C# tool and must not contain durable orchestration rules, required-file arrays, test filters, manifest parsing, generated-artifact policy, claim scanning, stage dependency logic, or engineering formulas.

## Registry

The source of truth is:

```text
docs/verification/Iso52016VerificationRegistry.json
```

The registry describes:

- verification stages;
- stage ids, names, and scopes;
- related release manifests;
- required documentation, source, and test files;
- `dotnet test` filters;
- generated artifact paths that must not be tracked;
- claim boundaries and required non-claims;
- entrypoint wrappers and deprecated stage wrapper aliases.

## Commands

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- list-stages
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- verify-stage --stage-id AE-ISO52016-002-STEP-01 --skip-tests
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- verify-all --skip-tests
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- assert-release-ready --skip-tests
```

Use `--require-clean-git` with `assert-release-ready` when the release gate must also prove a clean working tree.

## Claim Boundary

The ISO52016 verification chain is limited to validation and internal engineering anchors.

Required non-claims:

- No full ISO 52016 parity claim.
- No pyBuildingEnergy parity claim.
- No EnergyPlus parity claim.
- No ASHRAE 140 validation claim.

The registry and C# tool may mention external tools only as naming or methodology context. They must not claim complete ISO52010 or ISO52016 compliance.

## Generated Artifacts

Generated artifacts must not be tracked by git, including:

- `artifacts/iso52016/matrix-baselines`
- `artifacts/iso52016/external-validation-anchors`
- `artifacts/iso52016/engineering-edge-cases`
- `artifacts/iso52016/application-integration-hardening`
- physical-chain artifact folders listed in the registry

