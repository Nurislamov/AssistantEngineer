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
- `BuildingCoolingLoadService` and `RoomCalculationService` remain compatibility-layer candidates.
- `BuildingEnergyBalanceService` was retired in Phase 5 (DI registration removed, implementation deleted, reintroduction guard added).
- `FloorCalculationService` was retired in Phase 6 (DI registration removed, implementation deleted, reintroduction guard extended).
- Remaining compatibility services stay present in DI/tests where noted, while first-party load controllers use the pipeline/facade path.
- `BuildingHeatingLoadService` is not on the first-party controller path, but is still used by compatibility/report test scaffolds and must not be removed yet.
- Documentation-level deprecation markers remain for active compatibility candidates where applicable; `[Obsolete]` remains intentionally deferred because warnings-as-errors can break existing compatibility references.

Legacy migration status in this pass:
- Usage mapping was re-validated from source and tests.
- Replacement path is confirmed (`ILoadCalculationsFacade` -> `EnergyCalculationPipelineService`).
- Safe removal gates were documented in `calculation-legacy-inventory.md`.
- Single-service pilot retirement remained enforced per pass (`BuildingEnergyBalanceService` in Phase 5, `FloorCalculationService` in Phase 6).

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
- Remaining legacy calculation services are still compile-visible and require the same proof-first retirement gates.

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

- `BuildingEnergyBalanceService` and `FloorCalculationService` were retired service-by-service in Phase 5 and Phase 6.
- Remaining legacy compatibility services stay fenced by architecture guardrails.
- Documentation-level deprecation markers remain in active compatibility candidates.
- New architecture guard prevents first-party controller/facade direct dependencies on legacy services.
- New architecture guard blocks backend source reintroduction of retired `BuildingEnergyBalanceService` and `FloorCalculationService`.
- `[Obsolete]` attributes were not introduced to avoid warnings-as-errors breakage.

### Remaining risks

- `BuildingWorkspace.tsx` is still large despite phase-1 decomposition.
- Three legacy compatibility services are still DI-registered/compile-visible pending migration completion.

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

## Engineering Core Hardening Phase 4

### Ground contact frontend extraction

- Ground-contact concerns were extracted from `BuildingWorkspace.tsx` into:
  - `ui/GroundContactPanel.tsx`,
  - `model/useGroundContactMutations.ts`.
- `BuildingWorkspace.tsx` now acts as composition shell for summary/tabs/panel routing.
- UX, API contracts, and route behavior remain unchanged.

### Backend finalization extraction status

- Extracted building-heating room-result finalization into:
  - `EnergyCalculationPipelineBuildingHeatingResultAssembler` (internal sealed).
- `EnergyCalculationPipelineService` line count reduced further while preserving:
  - existing diagnostics severity semantics,
  - existing result mapping behavior,
  - existing formulas and validation anchors.

### Legacy retirement pilot status

- Phase 4 performed a fresh usage scan for:
  - `BuildingCoolingLoadService`,
  - `FloorCalculationService`,
  - `RoomCalculationService`,
  - `BuildingEnergyBalanceService`,
  - `BuildingHeatingLoadService`.
- No candidate passed full removal gates without compatibility-test or DI-policy churn.
- Decision: no runtime removal in Phase 4; blockers and exact PR sequence remain documented in `calculation-legacy-retirement-plan.md`.

### CI/generated artifact stability

- Default verification path remains frontend-on (`verify-engineering-core-v1.ps1` without `-SkipFrontend`).
- `-SkipFrontend` remains emergency/manual override only.
- CI workflow `engineering-core-v1.yml` remains aligned with mandatory frontend checks (`setup-node`, `npm ci`, verify script).
- Release-ready/verify flows continue to regenerate canonical governance artifacts through thin wrappers and C# tools.

### Verification results

- Target full gate for this phase:
  - `dotnet restore AssistantEngineer.sln`
  - `dotnet build AssistantEngineer.sln --no-restore`
  - `dotnet test AssistantEngineer.sln`
  - `npm --prefix .\src\Frontend ci`
  - `npm --prefix .\src\Frontend run build`
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\verify-engineering-core-v1.ps1`
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1`

### Remaining risks

- Legacy services remain compile-visible until per-service removal gates close.
- `BuildingWorkspace.tsx` is now smaller, but remaining panel-level complexity can still grow if future mutations are inlined.
- Infrastructure readiness and governance closure remain separate from external numerical validation completeness.

### Recommended next phase

1. Start legacy pilot retirement only after isolating one service with zero direct compatibility test dependency and proven DI independence.
2. Continue incremental extraction of remaining UI mutation flows only when they preserve current UX/API behavior.
3. Keep governance regeneration and non-claim boundaries enforced in CI and release-ready checks.

## Legacy Retirement Pilot Phase 5

### Usage re-scan

- Re-scan completed across `src` + `tests` for:
  - `BuildingCoolingLoadService`,
  - `FloorCalculationService`,
  - `RoomCalculationService`,
  - `BuildingEnergyBalanceService`,
  - `BuildingHeatingLoadService`.
- No direct first-party controller/facade constructor dependency on legacy service types was found.
- `BuildingEnergyBalanceService` usage was limited to DI compatibility registration and guard/docs references before retirement.

### Selected pilot candidate

- Selected by priority and gate fit: `BuildingEnergyBalanceService`.
- Selection rationale:
  - no direct controller/facade usage,
  - lowest churn footprint,
  - replacement path already active through facade/pipeline flow.

### Replacement coverage

- Replacement behavior coverage exists through active path tests:
  - `tests/AssistantEngineer.Tests/Calculations/EnergyCalculationPipelineServiceTests.cs`:
    - `EnergyBalanceApplicationPathUsesAnnualEnergyEngineAdapterWithDiagnostics`,
    - `EnergyBalanceApplicationPathUsesHourlyRecordsWhenSourceProvidesThem`,
    - `EnergyBalanceApplicationPathReturnsValidationWhenSourceUnavailable`.
- Coverage remains focused on externally relevant behavior and diagnostics, not private implementation details.
- Formula outputs and validation-anchor expectations were not changed.

### Retirement action

- Retired exactly one service: `BuildingEnergyBalanceService`.
- Changes:
  - removed DI registration from `src/Backend/AssistantEngineer.Modules.Calculations/Composition/EnergyAnalysisRegistration.cs`,
  - deleted implementation file `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Buildings/BuildingEnergyBalanceService.cs`,
  - updated architecture guard allowlist and added retired-service reintroduction guard in `tests/AssistantEngineer.Tests/Architecture/LegacyCalculationServiceDependencyGuardTests.cs`.
- No public API/controller contracts were changed.
- No frontend UX path was changed.

### Remaining legacy services

- `BuildingCoolingLoadService`
- `FloorCalculationService`
- `RoomCalculationService`
- `BuildingHeatingLoadService`

### Verification results

- Full backend/frontend/engineering verification completed without `-SkipFrontend`:
  - `dotnet restore AssistantEngineer.sln`
  - `dotnet build AssistantEngineer.sln --no-restore`
  - `dotnet test AssistantEngineer.sln`
  - `npm --prefix .\src\Frontend ci`
  - `npm --prefix .\src\Frontend run build`
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\verify-engineering-core-v1.ps1`
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1`

### Remaining risks

- Remaining compatibility services can still be consumed by new code if not continuously fenced.
- `BuildingHeatingLoadService` remains high-risk due to report-lane compatibility coupling.
- Infrastructure readiness remains separate from external numerical validation completeness.

### Recommended next phase

1. Target `FloorCalculationService` as the next candidate only after explicit facade-level floor-path regression guard evidence is in place.
2. Keep single-service retirement scope with full proof gates and no formula/anchor/API/UX changes.
3. Continue strict guard enforcement to prevent legacy backflow and retired-service reintroduction.

## Legacy Retirement Pilot Phase 6

### Usage re-scan

- Re-scan completed across `src` + `tests` for `FloorCalculationService`.
- Confirmed usage before retirement was limited to:
  - service definition,
  - DI registration in `LoadCalculationRegistration`,
  - architecture docs/guard references.
- No direct first-party controller/facade constructor dependency was found.
- No runtime application-service usage outside compatibility DI registration was found.

### Selected pilot candidate

- Selected candidate: `FloorCalculationService`.
- Selection rationale:
  - no direct controller/facade dependency,
  - bounded DI-only runtime footprint,
  - active replacement path already available through `ILoadCalculationsFacade` -> `EnergyCalculationPipelineService` floor methods.

### Replacement floor-path coverage

- Active floor path coverage is enforced via `tests/AssistantEngineer.Tests/Calculations/EnergyCalculationPipelineServiceTests.cs`:
  - floor cooling/heating aggregation parity against room sums,
  - requested/actual method compatibility diagnostics on floor results,
  - floor not-found behavior (`ResultErrorType.NotFound`).
- Coverage is behavior-level through active path and avoids private implementation detail assertions.
- Formula outputs and validation anchors were unchanged.

### Retirement action

- Retired exactly one service in this phase: `FloorCalculationService`.
- Changes:
  - removed DI registration from `src/Backend/AssistantEngineer.Modules.Calculations/Composition/LoadCalculationRegistration.cs`,
  - deleted implementation file `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Floors/FloorCalculationService.cs`,
  - updated architecture guard in `tests/AssistantEngineer.Tests/Architecture/LegacyCalculationServiceDependencyGuardTests.cs`:
    - removed `FloorCalculationService` from active legacy allowlist,
    - added `FloorCalculationService` to retired-service reintroduction guard.
- `BuildingCoolingLoadService`, `RoomCalculationService`, and `BuildingHeatingLoadService` were not removed in this phase.

### Remaining legacy services

- `BuildingCoolingLoadService`
- `RoomCalculationService`
- `BuildingHeatingLoadService`

### Verification results

- Full backend/frontend/engineering verification completed without `-SkipFrontend`:
  - `dotnet restore AssistantEngineer.sln`
  - `dotnet build AssistantEngineer.sln --no-restore`
  - `dotnet test AssistantEngineer.sln`
  - `npm --prefix .\src\Frontend ci`
  - `npm --prefix .\src\Frontend run build`
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\verify-engineering-core-v1.ps1`
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1`

### Remaining risks

- `BuildingCoolingLoadService` and `RoomCalculationService` still have compatibility-test constructor dependencies.
- `BuildingHeatingLoadService` remains high-risk due to report-lane compatibility coupling.
- Infrastructure readiness remains separate from external numerical validation completeness.

### Recommended next phase

1. Target `BuildingCoolingLoadService` next, only after migrating direct compatibility behavior tests to active facade/pipeline path.
2. Keep strict single-service retirement scope and proof-first gates.
3. Preserve non-claim boundaries (no EnergyPlus parity, no pyBuildingEnergy parity, no ASHRAE 140 validation, no full ISO/EN compliance).
