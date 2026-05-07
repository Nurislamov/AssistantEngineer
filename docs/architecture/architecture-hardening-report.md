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
  - `npm --prefix .\src\Frontend run build`
  - `scripts/engineering-core/verify-engineering-core-v1.ps1`
  - `scripts/engineering-core/assert-engineering-core-v1-release-ready.ps1`

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
- Frontend build now runs in the default gate path (no `-SkipFrontend`).
- `-SkipFrontend` is retained only as an explicit emergency override and is printed as a visible warning when used.

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

- `BuildingWorkspace.tsx` is still large despite phase-1 decomposition.
- Legacy services are still DI-registered and compile-visible pending migration completion.

### Recommended next phase

1. Continue incremental `EnergyCalculationPipelineService` extraction for diagnostics/equipment sizing orchestration without formula/API changes.
2. Execute the next safe `BuildingWorkspace` split for envelope editor and ventilation/ground mutation flows while preserving UX/API contracts.
3. Keep legacy services fenced to compatibility-only references until migration gates are fully met.
4. Keep governance generation checks strict for non-claims and opt-in boundaries across manifest/sample artifacts.

## Engineering Core Hardening Phase 2

### Frontend gate status

- `verify-engineering-core-v1.ps1` and `assert-engineering-core-v1-release-ready.ps1` run frontend checks by default.
- `-SkipFrontend` remains available only as explicit manual override.
- Script/tool output now visibly warns when `-SkipFrontend` is used.
- Current frontend gate is build-first (`npm --prefix .\src\Frontend run build`); no separate `typecheck`/`test` scripts are currently declared in `src/Frontend/package.json`.

### Governance generation idempotency

- Status sample generation path is protected by tests that run the contracts generator and validate regenerated `status.sample.json` against `EngineeringCoreV1Manifest.json` governance boundaries.
- Critical non-claims stay enforced:
  - no EnergyPlus parity claim,
  - no pyBuildingEnergy parity claim,
  - no ASHRAE 140 validation claim,
  - no full ISO/EN compliance claim.
- Opt-in boundaries are validated as preserved:
  - `SystemEnergyEngine compatibility path remains default.`,
  - `EN15316-inspired modular chain is opt-in.`,
  - `ISO12831-3-inspired DHW path is opt-in.`

### Backend pipeline extraction phase 2

- Extracted room context resolution into `EnergyCalculationPipelineRoomContextResolver` (ground/solar context + internal-gain schedule diagnostics policy helper).
- Extracted aggregation room input assembly into `EnergyCalculationPipelineAggregationRoomAssembler`.
- `EnergyCalculationPipelineService` remains orchestrator; formulas, validation anchors, and API contracts are unchanged.

### Frontend workspace decomposition phase 2

- Extracted floors/rooms mutations into `useFloorsRoomsMutations`.
- Extracted floors/rooms UI panel into `FloorsRoomsPanel`.
- `BuildingWorkspace.tsx` remains orchestration shell for tab routing and cross-panel composition.
- No route/API contract changes and no global state manager introduction.

### Legacy enforcement

- Legacy services remain registered for compatibility.
- Added stronger architecture guard that fences legacy service references to:
  - compatibility service definitions, and
  - composition registrations (`LoadCalculationRegistration`, `EnergyAnalysisRegistration`).
- Production controller/facade direct dependencies on legacy services remain prohibited.

### Verification results

- Backend: `dotnet restore`, `dotnet build`, `dotnet test` in full solution gate.
- Frontend: `npm --prefix .\src\Frontend run build` in default gate path.
- Engineering scripts: both verify and release-ready scripts run without `-SkipFrontend`.

### Remaining risks

- `BuildingWorkspace.tsx` is reduced but still hosts multiple panel compositions.
- `EnergyCalculationPipelineService` is reduced but still couples equipment sizing orchestration and error-propagation flow.
- Legacy services remain compile-visible while compatibility paths exist.

### Recommended next phase

1. Extract envelope editor concerns (walls/windows) into dedicated components and mutation hooks.
2. Extract ventilation/ground mutation flows into focused hooks/components.
3. Continue pipeline extraction around equipment sizing orchestration/error mapping, keeping formulas and anchors unchanged.
4. Define and execute compatibility retirement gates for each legacy service before DI removal.

## Engineering Core Hardening Phase 3

### Backend pipeline extraction phase 3

- `EnergyCalculationPipelineService` was further reduced by extracting:
  - `EnergyCalculationPipelineEquipmentSizingOrchestrator` (equipment sizing input/candidate preparation, sizing execution, and compatibility diagnostics attachment),
  - `EnergyCalculationPipelineDiagnosticsPolicy` (centralized failure/validation mapping from room and aggregation diagnostics).
- Extraction remained refactor-only:
  - no physical formula changes,
  - no validation anchor value changes,
  - no public API/controller contract changes.
- Capacity margin behavior and `EquipmentSizing.HeatingCapacityUnavailable` warning semantics remain unchanged.

### Frontend envelope decomposition

- Envelope concerns were extracted from `BuildingWorkspace.tsx` into:
  - `ui/EnvelopePanel.tsx`,
  - `ui/WallEditor.tsx`,
  - `ui/WindowEditor.tsx`,
  - `model/useEnvelopeMutations.ts`.
- API routes/contracts and user flows remain unchanged.
- `BuildingWorkspace.tsx` now stays as orchestration shell for tabs/panels.

### Frontend ventilation/ground decomposition status

- Ventilation flow was extracted into:
  - `ui/VentilationPanel.tsx`,
  - `model/useVentilationMutations.ts`.
- Ground panel remains inline for now and is the next safe candidate.
- No route changes, no global state manager introduction, and no UX redesign were introduced.

### Legacy retirement preparation

- Added `docs/architecture/calculation-legacy-retirement-plan.md` with per-service retirement gates for:
  - `BuildingCoolingLoadService`,
  - `FloorCalculationService`,
  - `RoomCalculationService`,
  - `BuildingEnergyBalanceService`,
  - `BuildingHeatingLoadService`.
- Added documentation guard coverage to ensure retirement plan presence and required sections.
- Legacy services and DI registrations remain intact in this phase by design.

### Frontend gate / CI readiness

- Full engineering gate remains frontend-on by default (no `-SkipFrontend` for normal flow).
- `-SkipFrontend` remains emergency/manual override only.
- CI workflow `engineering-core-v1.yml` includes Node setup and frontend dependency install/build path through the default verify gate.

### Governance/generated artifacts stability

- Governance non-claims remain explicit:
  - No EnergyPlus parity claim.
  - No pyBuildingEnergy parity claim.
  - No ASHRAE 140 validation claim.
  - No full ISO/EN compliance claim.
- Opt-in boundaries remain explicit and unchanged:
  - `SystemEnergyEngine compatibility path remains default.`,
  - `EN15316-inspired modular chain is opt-in.`,
  - `ISO12831-3-inspired DHW path is opt-in.`.
- Canonical generated governance artifacts remain controlled through verify/release-ready flows and related tests.

### Verification results

- Backend verification target remains:
  - `dotnet restore AssistantEngineer.sln`
  - `dotnet build AssistantEngineer.sln --no-restore`
  - `dotnet test AssistantEngineer.sln`
- Frontend verification target remains:
  - `npm --prefix .\src\Frontend ci`
  - `npm --prefix .\src\Frontend run build`
- Engineering scripts target remains:
  - `.\scripts\engineering-core\verify-engineering-core-v1.ps1`
  - `.\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1`

### Remaining risks

- `GroundContactPanel` is still inline in `BuildingWorkspace.tsx` and should be extracted next.
- Legacy services are still compile-visible until retirement gates are closed per service.
- Infrastructure readiness remains distinct from external numerical validation completeness.

### Recommended next phase

1. Extract ground-contact panel mutations/UI from `BuildingWorkspace.tsx`.
2. Continue pipeline extraction for any remaining application-level finalization coupling only where tests keep behavior stable.
3. Execute legacy retirement PRs service-by-service using documented gates and sequence.
4. Keep governance artifact generation checks strict and idempotent in CI/release paths.
