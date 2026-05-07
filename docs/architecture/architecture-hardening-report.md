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

## Recommended next stage

1. Introduce explicit soft-deprecation annotations and architecture tests for compatibility service consumers.
2. Gradually extract `EnergyCalculationPipelineService` by orchestration slices without changing numerical behavior.
3. Split `BuildingWorkspace.tsx` into feature-level panels/hooks with unchanged API behavior.
4. Keep generated artifacts out of git and keep PowerShell as thin wrappers.
5. Start legacy-service retirement by removing DI registrations only after compatibility tests are re-homed to facade/pipeline contracts.
