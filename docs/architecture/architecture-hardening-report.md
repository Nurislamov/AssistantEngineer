# Architecture Hardening Report

Scope: architecture hardening only. No new engineering formulas were added in this pass.

Boundary statements:
- Internal deterministic engineering governance only.
- No EnergyPlus parity claim.
- No pyBuildingEnergy parity claim.
- No ASHRAE 140 validation claim.
- No full ISO/EN compliance claim.
- Validation anchors remain validation anchors only.

## Current architecture state

- Load-calculation API endpoints are routed through `ILoadCalculationsFacade` into `EnergyCalculationPipelineService`.
- ISO52016 simulation endpoints are routed through `IBuildingEnergyAnalysisFacade` and dedicated ISO52016 services.
- Multiple compatibility services remain in the codebase and DI container, which increases maintenance surface.
- Repository now has explicit hygiene guards for tracked local/generated artifacts.

## God-service and god-component risks

### EnergyCalculationPipelineService

- The class is a high-responsibility orchestrator spanning room/floor/building load flow, annual adaptation, and diagnostics adaptation.
- Risk: change coupling and onboarding cost are high.
- Action in this pass: first safe extraction phase completed without changing formulas or API contracts.
  - Extracted weather context preparation to `EnergyCalculationPipelineClimateContextBuilder`.
  - Extracted annual energy input adaptation to `EnergyCalculationPipelineAnnualInputAdapter`.
  - Extracted response/result mapping and compatibility-method diagnostics to `EnergyCalculationPipelineResultMapper`.
  - Shared internal records moved to `EnergyCalculationPipelineModelTypes`.
  - `EnergyCalculationPipelineService` remains orchestrator/facade.

### BuildingWorkspace.tsx

- `src/Frontend/src/widgets/building-workspace/ui/BuildingWorkspace.tsx` is a large multi-panel component with data fetching, mutations, and UI orchestration in one file.
- Risk: component growth, difficult local reasoning, and expensive UI regression testing.
- Action in this pass: first safe decomposition phase completed without backend/API contract changes.
  - Extracted workspace base loading into `useBuildingWorkspaceData`.
  - Extracted tab navigation/layout shell into `BuildingWorkspaceTabs`.
  - Extracted calculation execution logic into `useBuildingCalculationExecution` and moved calculation UI to `CalculationsPanel`.
  - Extracted reports async operations into `useBuildingReports` and moved report UI to `ReportsPanel`.
  - Extracted reusable UI primitives `RoomSelect` and `JsonBlock` into dedicated files.
- Compatibility note: `BuildingWorkspace.tsx` continues to export `ReportsPanel` for existing page imports; routing and user workflows are preserved.

### tools/*.Program.cs

- Tooling is split into focused C# executables (`tools/*/Program.cs`) for governance, verification, release, and boundaries.
- Positive: explicit command boundaries exist and are test-covered.
- Risk: command surfaces can drift without harmonized conventions.
- Action in this pass: three heavy tool entrypoints were hardened into thin composition roots with command logic extracted:
  - `tools/AssistantEngineer.Tools.EnergyPlusValidation/Program.cs` delegates to `EnergyPlusValidationToolRunner`.
  - `tools/AssistantEngineer.Tools.Iso52016Verification/Program.cs` delegates to `Iso52016VerificationRunner` and uses `Iso52016VerificationCommandOptions`.
  - `tools/AssistantEngineer.Tools.EngineeringCoreEvidence/Program.cs` delegates to `EngineeringCoreEvidenceToolRunner`.
- Safety note: CLI command names, switch names, output filenames, and report semantics were preserved; extraction moved orchestration code only.

### PowerShell scripts

- Scripts remain thin wrappers that delegate to C# tooling.
- Risk: wrapper drift if scripts begin accumulating business logic.
- Action in this pass: repository hygiene guards and tests reinforced; no thick orchestration added.

## Legacy services found

Reference inventory:
- `docs/architecture/calculation-legacy-inventory.md`

Key findings:
- `BuildingCoolingLoadService`, `FloorCalculationService`, `RoomCalculationService`, and `BuildingEnergyBalanceService` are compatibility-layer candidates.
- They are present in DI and compatibility tests, while first-party load controllers use the pipeline/facade path.
- `BuildingHeatingLoadService` is not on the first-party controller path, but is still used by compatibility/report test scaffolds and must not be removed yet.
- Documentation-level deprecation markers were added in the four primary candidates; `[Obsolete]` was intentionally deferred because warnings-as-errors would break current references.

Legacy migration status in this pass:
- Usage mapping was re-validated from source and tests.
- Replacement path is confirmed (`ILoadCalculationsFacade` -> `EnergyCalculationPipelineService`).
- Safe removal gates were documented in `calculation-legacy-inventory.md`.
- No runtime removal or DI unregistration was performed.

## Guardrails added in this pass

- `.gitignore` reinforced for:
  - `.vs/`
  - `bin/`
  - `obj/`
  - `*.user`, `*.suo`, `*.wsuo`
  - `artifacts/`, `generated/`, `TestResults/`
- `.vs` files removed from git tracking.
- `AssistantEngineer.Tools.RepositoryHygieneVerification` now checks tracked local/generated artifacts via `git ls-files`.
- Guard test added for repository index and `.gitignore` coverage.
- Guard tests added for architecture documentation presence/sections.
- `CalculationModuleDeepeningGuardTests` extended with pipeline growth guard (`EnergyCalculationPipelineService` line-count threshold and required extraction files).

## Extraction safety notes

- Extraction was done by moving deterministic code blocks 1:1 into internal helpers.
- Numerical formulas were not modified.
- Validation anchor values were not modified.
- Public API/controller contracts were not changed.
- Existing test suite remained green after extraction.

Responsibilities still inside `EnergyCalculationPipelineService`:
- orchestration flow across repositories and calculation engines;
- room/floor/building aggregation sequencing;
- ground and solar context usage decisions;
- equipment sizing orchestration and error propagation.

Planned next safe candidates (future work, not done here):
- ground/solar per-room context resolver split;
- aggregation-room input assembly split;
- diagnostics policy helpers split where still coupled to orchestration context.
- additional cleanup for other large `tools/*/Program.cs` where command handlers are still dense.
- next frontend decomposition slice for `BuildingWorkspace.tsx`:
  - extract `FloorsRoomsPanel` form/mutation logic into dedicated hooks,
  - split `EnvelopePanel` editor concerns (wall/window) into focused components,
  - isolate ventilation/ground mutation flows into per-panel hooks.

## Remaining risks

- Compatibility services still exist in DI and can be consumed by future code unless explicit deprecation policy is enforced.
- `EnergyCalculationPipelineService` remains large even after phase-1 extraction (risk reduced, not eliminated).
- `BuildingWorkspace.tsx` remains a large UI composition hotspot.
- `BuildingWorkspace.tsx` is smaller after phase-1 extraction but still contains multiple domain panels and mutation flows in one file.
- External numerical validation completeness is still separate from infrastructure readiness.
- Legacy calculation services remain compile-visible; removal cannot start before compatibility tests are migrated and DI consumers are proven absent.

## Validation and parity claim constraints

Must continue to avoid:
- must not claim EnergyPlus parity.
- must not claim pyBuildingEnergy parity.
- must not claim ASHRAE 140 validation.
- must not claim full ISO/EN compliance.

## Final hardening audit

Audit date: 2026-05-07

### Checks performed

- Repository hygiene index scan and `.gitignore` coverage verification.
- Architecture document boundary review:
  - `docs/architecture/calculation-legacy-inventory.md`
  - `docs/architecture/architecture-hardening-report.md`
- `docs/releases/EngineeringCoreV1Manifest.json` governance review (non-claims, opt-in limits, required docs).
- Backend architecture review for active production path (`ILoadCalculationsFacade` -> `EnergyCalculationPipelineService`) and legacy dependencies.
- Frontend architecture review for `BuildingWorkspace.tsx` decomposition boundaries.
- Tooling review for thin `Program.cs` composition roots and thin PowerShell wrappers.
- Verification runs:
  - `dotnet restore AssistantEngineer.sln`
  - `dotnet build AssistantEngineer.sln --no-restore`
  - `dotnet test AssistantEngineer.sln --no-build`
  - `npm --prefix .\src\Frontend run build` (attempted)
  - `scripts/engineering-core/verify-engineering-core-v1.ps1` (run with `-SkipFrontend`)
  - `scripts/engineering-core/assert-engineering-core-v1-release-ready.ps1` (run with `-SkipFrontend`)

### Passed checks

- Repository hygiene guard conditions pass:
  - no tracked `.vs/`, `bin/`, `obj/`, `TestResults/`, `coverage/`, `artifacts/`, `generated/`, `*.user`, `*.suo`, `*.wsuo`.
- Architecture boundary statements stay explicit:
  - no EnergyPlus parity claim,
  - no pyBuildingEnergy parity claim,
  - no ASHRAE 140 validation claim,
  - no full ISO/EN compliance claim.
- Validation anchors are treated as validation anchors only.
- Infrastructure readiness is kept separate from external numerical validation completeness.
- `dotnet restore`, `dotnet build`, and `dotnet test` pass.

### Manifest/governance verification

- `EngineeringCoreV1Manifest.json` keeps governance non-claims and planned-validation boundary.
- Restored missing opt-in limitation entries:
  - `SystemEnergyEngine compatibility path remains default.`
  - `EN15316-inspired modular chain is opt-in.`
  - `ISO12831-3-inspired DHW path is opt-in.`
- `documentationFiles` still includes core governance/release documentation set.

### Backend guard status

- `EnergyCalculationPipelineService` guard exists and remains below threshold.
- Extracted phase-1 helper files remain present and internal-scoped.
- New guard added: first-party controllers/facades must not directly depend on legacy calculation services.
- Active production path remains `ILoadCalculationsFacade` -> `EnergyCalculationPipelineService`.

### Frontend guard status

- `BuildingWorkspace.tsx` remains decomposed with extracted hooks/components present.
- New guard added:
  - line-count ceiling for `BuildingWorkspace.tsx`,
  - required decomposition file presence check,
  - no heavy state-manager import drift (`redux`/`zustand`/`mobx`) in workspace file.
- Frontend build could not run in this environment because `npm` is not available on `PATH`.

### Tools guard status

- `EnergyPlusValidation`, `Iso52016Verification`, and `EngineeringCoreEvidence` `Program.cs` files remain thin composition roots.
- Wrapper scripts remain thin delegates to C# tools.
- CLI options and command surfaces remain unchanged in this pass.

### Legacy enforcement status

- Legacy services were not removed.
- Documentation-level deprecation markers remain in compatibility candidates.
- New architecture guard prevents first-party controller/facade direct dependencies on legacy services.
- `[Obsolete]` attributes were not introduced to avoid warnings-as-errors breakage.

### Remaining risks

- `verify-engineering-core-v1.ps1` / release-ready flow regenerates `docs/api/engineering-core-v1/status.sample.json`; if governance non-claims drift, final full-suite step can fail.
- `BuildingWorkspace.tsx` is still large despite phase-1 decomposition.
- Legacy services are still DI-registered and compile-visible pending migration completion.

### Recommended next phase

1. Keep governance artifacts idempotent in generation flows and add a dedicated check that regenerated snapshots preserve required opt-in disclosures.
2. Continue incremental `EnergyCalculationPipelineService` extraction without formula/API changes.
3. Execute the next safe `BuildingWorkspace` split (floors/rooms and envelope editor concerns) while preserving contracts.
4. Start legacy retirement only after DI consumer removal is proven by tests and source scan.
