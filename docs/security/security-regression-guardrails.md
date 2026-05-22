# Security Regression Guardrails

## Purpose

This document defines security regression guardrails for staged SaaS hardening so that security posture does not drift between roadmap stages.

## Scope

Guardrails cover:

- route protection inventory;
- development-only endpoint gating;
- authentication boundary defaults;
- authorization rollout governance;
- audit log sanitization safety;
- rate limiting safety defaults;
- secret logging/source leak prevention;
- secure configuration checks;
- false security/compliance claim prevention;
- frontend auth shell and frontend secret safety.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No external identity provider integration claim.
- No certified/certification claim.
- No claim that all API endpoints are protected yet.

## Guardrail categories

- `SEC-GUARD-ROUTE-INVENTORY`
- `SEC-GUARD-DEV-ENDPOINT`
- `SEC-GUARD-SECRET-LOGGING`
- `SEC-GUARD-AUTH-DEFAULTS`
- `SEC-GUARD-RATE-LIMIT-DEFAULTS`
- `SEC-GUARD-AUDIT-SANITIZATION`
- `SEC-GUARD-INMEMORY-PRODUCTION`
- `SEC-GUARD-FALSE-CLAIMS`
- `SEC-GUARD-FRONTEND-SECRETS`
- `SEC-GUARD-TENANT-ISOLATION-MATRIX`
- `SEC-GUARD-PERSISTED-TENANT-OWNERSHIP`
- `SEC-GUARD-TENANT-QUERY-ISOLATION`
- `SEC-GUARD-TENANT-READ-CONTROLLER-INTEGRATION`
- `SEC-GUARD-WORKFLOW-TENANT-READ-INTEGRATION`
- `SEC-GUARD-WORKFLOW-OWNERSHIP-METADATA-COVERAGE`
- `SEC-GUARD-OWNERSHIP-BACKFILL-STRATEGY-EVIDENCE`
- `SEC-GUARD-OWNERSHIP-BACKFILL-DRY-RUN-TOOL`
- `SEC-GUARD-OWNERSHIP-BACKFILL-DATABASE-DRY-RUN`
- `SEC-GUARD-OWNERSHIP-BACKFILL-EVIDENCE-GATES`
- `SEC-GUARD-OWNERSHIP-BACKFILL-APPLY-DESIGN-DISABLED`
- `SEC-GUARD-OWNERSHIP-BACKFILL-PLAN-ONLY`
- `SEC-GUARD-OWNERSHIP-BACKFILL-PLAN-SIGNOFF`
- `SEC-GUARD-OWNERSHIP-BACKFILL-TEST-ONLY-APPLY-REHEARSAL`
- `SEC-GUARD-OWNERSHIP-BACKFILL-APPLY-READINESS`
- `SEC-GUARD-OWNERSHIP-BACKFILL-PRODUCTION-APPLY-PROPOSAL`
- `SEC-GUARD-OWNERSHIP-BACKFILL-STAGING-APPLY-RUNBOOK`
- `SEC-GUARD-OWNERSHIP-BACKFILL-STAGING-APPLY-EXECUTOR-DESIGN`
- `SEC-GUARD-OWNERSHIP-BACKFILL-STAGING-POST-RUN-EVIDENCE-ACCEPTANCE`
- `SEC-GUARD-OWNERSHIP-BACKFILL-PRODUCTION-PROMOTION-READINESS`
- `SEC-GUARD-OWNERSHIP-BACKFILL-MANUAL-WRITE-PATH-DECISION`
- `SEC-GUARD-OWNERSHIP-BACKFILL-APPLY-ARCHITECTURE-REVIEW`
- `SEC-GUARD-POST-P6-GOVERNANCE-AUDIT`
- `SEC-GUARD-SECURITY-GOVERNANCE-RELEASE-BOUNDARY`
- `SEC-GUARD-SECURITY-GOVERNANCE-INDEX-NORMALIZATION`
- `SEC-GUARD-GOVERNANCE-TEST-CONSOLIDATION`
- `SEC-GUARD-CI-GITHUB-CHECKS-VISIBILITY`
- `SEC-GUARD-ROUTE-INVENTORY-CLAIMS-CONSISTENCY`
- `SEC-GUARD-SECURITY-DOCS-MAP-ADR`
- `SEC-GUARD-SECURITY-ADR-DECISION-MATRIX`
- `SEC-GUARD-ENGINEERING-DOMAIN-ARCHITECTURE-AUDIT`
- `SEC-GUARD-ENGINEERINGWORKFLOW-BOUNDARY`
- `SEC-GUARD-MODULE-BOUNDARY-MATRIX`
- `SEC-GUARD-AUTHORIZATION-WORKFLOW-DECOMPOSITION-DESIGN`
- `SEC-GUARD-PROTECTED-ENDPOINT-AUTHORIZATION-GATE-CHARACTERIZATION`
- `SEC-GUARD-PROTECTED-ENDPOINT-AUTHORIZATION-GATE-SCOPE-PERMISSION`
- `SEC-GUARD-WORKFLOW-CONTROLLER-SHELL-CHARACTERIZATION`
- `SEC-GUARD-WORKFLOW-ORCHESTRATION-HELPER-MIGRATION`
- `SEC-GUARD-WORKFLOW-CONTROLLER-SHELL-REDUCTION`
- `SEC-GUARD-OWNERSHIPBACKFILL-CLI-PARSER-SIMPLIFICATION`
- `SEC-GUARD-ROUTE-INVENTORY-CLASSIFICATION-CLOSURE`
- `SEC-GUARD-TERMINOLOGY-CLAIMS-SURFACE`
- `SEC-GUARD-GOVERNANCE-TEST-BRITTLENESS-REDUCTION`
- `SEC-GUARD-P8-ENGINEERING-DOMAIN-CLOSURE`

## Enforcement model

- Use hard automated tests for high-confidence checks with low false-positive risk.
- Use explicit allowlists (with reasons) where discovery scans would otherwise be noisy.
- Keep fragile checks as documented-only TODO items until reliable automation is possible.
- Avoid hidden broad scans without reviewable scope or explainable failure output.

## Current guardrails

- Route inventory completeness and controller coverage checks (`ApiEndpointProtectionInventoryGuardTests`) including pilot status tracking in `docs/security/api-endpoint-protection-inventory.json` (for example `DevelopmentDemoDataController` in `AuthPilot` stage).
- Route inventory guardrails also track P5-10 read-pilot entries for `ProjectsController`/`BuildingsController` read endpoints and prevent unnoticed drift between protected and unprotected route groups.
- Route inventory guardrails also track P5-11 write-pilot entries for `ProjectsController`/`BuildingsController` write endpoints to prevent accidental unprotected mutating routes.
- Route inventory guardrails also track P5-12 execution-pilot entries for workflow/calculation endpoints so execution protection changes require inventory updates.
- Route inventory guardrails also track P5-13 report/artifact pilot entries so report/export/artifact route protections cannot drift without inventory updates.
- Route inventory guardrails also track P5-14 workflow read/history pilot entries so workflow state/scenario/job read protections cannot drift without explicit inventory updates.
- Tenant isolation matrix guardrails track cross-tenant expectations in `docs/security/tenant-isolation-integration-matrix.md` and verify representative P5-10 through P5-14 endpoint groups via `TenantIsolationAuthorizationGateMatrixTests`, `TenantIsolationProtectedEndpointSmokeTests`, `TenantIsolationAntiEnumerationTests`, and `TenantIsolationEndpointInventoryCoverageTests`.
- Persistence-backed ownership guardrails verify `docs/security/persistence-backed-tenant-ownership-fields.md`, the append-only `AddProjectTenantOwnershipFields` migration, and model snapshot ownership fields via `P5PersistenceBackedTenantOwnershipGovernanceTests`.
- Tenant-aware query isolation guardrails verify `docs/security/tenant-aware-query-isolation-services.md`, `TenantQueryIsolationPolicyTests`, `ProjectTenantScopedReadServiceTests`, `BuildingTenantScopedReadServiceTests`, and `TenantScopedReadServiceMatrixTests`.
- Tenant-aware read controller integration guardrails verify `docs/security/tenant-aware-read-controller-integration.md`, Project/Building matrix metadata in `docs/security/tenant-isolation-integration-matrix.json`, and `ProtectedReadControllersTenantScopedQueryIntegrationTests`.
- Workflow tenant-aware read integration guardrails verify `docs/security/workflow-tenant-aware-read-integration.md`, workflow matrix metadata in `docs/security/tenant-isolation-integration-matrix.json`, and `ProtectedWorkflowReadControllersTenantScopedQueryIntegrationTests`.
- Workflow ownership metadata coverage guardrails verify `docs/security/workflow-ownership-metadata-coverage.md`, `docs/security/workflow-ownership-metadata-coverage.json`, and workflow metadata coverage fields in `docs/security/tenant-isolation-integration-matrix.json`.
- Ownership backfill strategy/evidence guardrails verify `docs/security/ownership-backfill-strategy.md`, `docs/security/ownership-backfill-strategy.json`, `docs/security/ownership-backfill-evidence-model.md`, `docs/security/ownership-backfill-evidence.schema.json`, and roadmap/guardrail linkage.
- Ownership backfill dry-run tool guardrails verify `tools/AssistantEngineer.Tools.OwnershipBackfill`, no apply implementation, no write-oriented persistence calls, artifact ignore rules, and roadmap/guardrail linkage.
- Ownership backfill database dry-run guardrails verify read-only scanner presence, no-write scanner source constraints, and P6-02 documentation/roadmap linkage.
- Ownership backfill evidence-gate guardrails verify `validate-evidence` behavior, gate artifact ignore rules, threshold-policy documentation, and P6-03 roadmap linkage.
- Ownership backfill apply-design guardrails verify apply command remains disabled, apply design docs/json/schema stay aligned, and no write-path indicators appear in tool source.
- Ownership backfill plan-only guardrails verify `plan-apply` deterministic artifact generation stays no-write, apply remains disabled, plan artifact ignore rules stay active, and roadmap linkage includes P6-05.
- Ownership backfill plan-signoff guardrails verify `signoff-plan` deterministic sign-off artifact generation stays no-write, plan hash verification is enforced, apply remains disabled, signoff artifact ignore rules stay active, and roadmap linkage includes P6-06.
- Ownership backfill test-only apply rehearsal guardrails verify rehearsal executor behavior stays test-only, production apply remains disabled, rehearsal artifact ignore rules stay active, and roadmap linkage includes P6-07.
- Ownership backfill apply-readiness guardrails verify `validate-apply-readiness` artifact-chain hash consistency (`PlanHash`, sign-off hash, `ApplyInputHash`), sign-off TTL checks, previous-values completeness checks, readiness artifact ignore rules, and roadmap linkage includes P6-08 while apply remains disabled.
- Ownership backfill production-apply proposal guardrails verify proposal/template documents, machine-readable descriptors, required approval/go-no-go policy fields, `ApplyInputHash` change-management linkage, and roadmap linkage includes P6-09 while apply remains disabled.
- Ownership backfill staging-apply runbook guardrails verify staging runbook/checklist design artifacts, operator/environment/backup/rollback/promotion controls, `ApplyInputHash` staging evidence requirements, and roadmap linkage includes P6-10 while apply remains disabled.
- Ownership backfill staging-apply executor-design guardrails verify staging preflight contracts, disabled executor behavior, environment hard-deny rules for non-staging, no-write source constraints, and roadmap linkage includes P6-11 while apply remains disabled.
- Ownership backfill staging post-run evidence acceptance guardrails verify deterministic post-run evidence contract artifacts, `validate-staging-acceptance` command availability, staging acceptance artifact ignore rules, no-write source constraints, and roadmap linkage includes P6-12 while staging and production apply remain disabled.
- Ownership backfill production promotion readiness guardrails verify production promotion readiness docs/json/schema, `validate-production-promotion` command availability, production-promotion decision artifact ignore rules, no-write source constraints, and roadmap linkage includes P6-13 while staging and production apply remain disabled.
- Ownership backfill manual write-path decision guardrails verify manual decision docs/templates/json/schema, required hash/approval policy bindings, inventory and guardrail linkage, and no false claims about write-path enablement or backfill execution in P6-14.
- Ownership backfill apply-enablement architecture-review guardrails verify P6-15 architecture invariants/checklist artifacts, source-level no-wiring checks, disabled apply boundary regression checks, and roadmap linkage while staging and production apply remain disabled.
- Post-P6 governance audit guardrails verify P7-00 audit/index artifacts, release-boundary disabled flags, claims consistency checks, and roadmap linkage while apply remains disabled.
- Security release-boundary guardrails verify canonical release-boundary docs/json/schema and enforce disabled-capability flags remain false.
- Security governance index-normalization guardrails verify index categories/status/canonicalRole normalization and vocabulary alignment.
- Governance test consolidation guardrails verify helper-based consolidation artifacts exist, preserved guardrails are explicitly listed, and runtime/write-path boundaries remain disabled in P7-02.
- Route-inventory claims-consistency guardrails verify endpoint inventory coverage, classification-field completeness, protection-stage/tenant-scope/rate-limit/audit category consistency, and forbidden-claim posture in P7-06.
- Security docs map/ADR guardrails verify canonical docs-map and accepted ADR boundary remain indexed, machine-readable, and non-claim safe in P7-07.
- Engineering/domain architecture audit guardrails verify P8-00 architecture audit/map/inventory artifacts remain explicit, review-only, and no-change scoped for runtime/API/write-path/calculation behavior.
- EngineeringWorkflow boundary guardrails verify P8-01 namespace hardening artifacts and architecture tests keep EngineeringWorkflow module code out of `AssistantEngineer.Api.*` namespaces.
- Module-boundary matrix guardrails verify P8-02 matrix artifacts and dependency-direction tests (`ModuleBoundaryMatrixTests`, `EngineeringWorkflowModuleBoundaryTests`, `P8ModuleBoundaryTestExpansionTests`) remain active.
- Authorization/workflow decomposition-design guardrails verify P8-03 hotspot inventory and decomposition design artifacts stay explicit, compatibility-bound, and behavior-neutral.
- Protected-endpoint authorization gate characterization guardrails verify P8-03A decision/status matrix artifacts and behavior-lock test coverage remain explicit and no-change scoped.
- Protected-endpoint authorization gate scope/permission extraction guardrails verify P8-03C collaborator extraction tests and behavior-lock characterization coverage remain active.
- Workflow controller shell characterization guardrails verify route/action signature compatibility, status-code/response-shape behavior locks, and authorization-interaction compatibility before P8-03E/P8-03F decomposition.
- Workflow orchestration helper migration guardrails verify P8-03E helper relocation remains behavior-neutral and boundary-safe with characterization coverage preserved.
- Workflow controller shell reduction guardrails verify P8-03F API-shell extraction remains behavior-neutral and keeps route/signature/status/response/auth characterization contracts intact.
- OwnershipBackfill CLI parser simplification guardrails verify P8-04 descriptor/catalog/argument-reader decomposition remains semantics-neutral for commands/help/exit/redaction/apply-disabled behavior.
- Route inventory deferred classification closure guardrails verify P8-05 reclassification/ignore-list tightening remains coverage-safe and claims-consistent without changing runtime/controller behavior.
- Terminology and claims-surface guardrails verify canonical P8-07 vocabulary and cleanup artifacts, and block positive forbidden-claim drift in core governance docs/tests.
- Governance test brittleness reduction guardrails verify P8-08 semantic-assertion migration remains no-change scoped and preserves critical guardrail coverage.
- Final P8 closure guardrails verify closure-report consistency, boundary non-claims, deferred-backlog honesty, and linkage to readiness/guardrail registries.
- Development/demo endpoint environment-gating checks (`DevelopmentEndpointSecurityGuardTests`).
- Secret logging/source high-confidence leakage checks (`SecretLoggingSecurityGuardTests`).
- Authentication default compatibility and secret-free appsettings checks (`ApiAuthenticationDefaultsGuardTests`).
- Rate-limiting default compatibility and partition-key safety checks (`ApiRateLimitingDefaultsGuardTests`).
- Audit metadata sanitization and payload safety registry checks (`AuditLogSecurityGuardTests`).
- InMemory provider production-risk visibility and configuration guard checks (`InMemoryProductionProviderGuardTests`).
- False security/compliance claim scan in security/frontend docs (`SecurityFalseClaimsGuardTests`).
- Frontend token/api-key hardcoding guard checks (`FrontendSecretsGuardTests`).

## Future guardrails

- Real anonymous/protected endpoint integration tests after staged route protection rollout.
- Deeper tenant isolation tests after persistence-layer ownership fields and row-level query filters are introduced.
- API-key persistence lifecycle and rotation regression tests.
- Distributed rate-limiting integration tests.
- Durable audit storage retention and tamper-evidence regression tests.
## SEC-GUARD-OWNERSHIP-BACKFILL-CLI-UX
- Enforcement: AutomatedTest`r
- Tests: OwnershipBackfillCliCommandInventoryTests, OwnershipBackfillCliRedactionTests, OwnershipBackfillCliExitCodeConsistencyTests`r
- Risk: CLI UX changes may regress disabled-boundary signaling, exit-code consistency, or secret redaction behavior.

## SEC-GUARD-RELEASE-READY-OBSERVABILITY
- Enforcement: AutomatedTest`r
- Tests: ReleaseReadyScriptObservabilityTests, P7ReleaseReadyObservabilityAuditTests`r
- Risk: release-ready tooling changes may reduce diagnostics quality, hide failures, or accidentally leak sensitive values.

## SEC-GUARD-CI-GITHUB-CHECKS-VISIBILITY
- Enforcement: AutomatedTest
- Tests: P7CiGithubChecksVisibilityAuditTests, P7CiWorkflowInventoryTests
- Risk: CI workflow/check visibility drift can hide release-ready status or introduce unsafe apply-path commands in automation.

## SEC-GUARD-SECURITY-ADR-DECISION-MATRIX
- Enforcement: AutomatedTest
- Tests: P7SecurityArchitectureDecisionMatrixTests, P7FutureSecurityAdrBacklogTests
- Risk: ADR matrix/backlog drift can weaken boundary assumptions and future decision trigger clarity for write-path-related changes.

## SEC-GUARD-ENGINEERING-DOMAIN-ARCHITECTURE-AUDIT
- Enforcement: AutomatedTest
- Tests: P8EngineeringDomainArchitectureAuditTests, P8AssistantEngineerArchitectureMapTests, P8LegacyAndDeadCodeInventoryTests, P8ScriptsToolsInventoryTests
- Risk: Architecture hardening decisions can drift into undocumented runtime/API/calculation/write-path changes.

## SEC-GUARD-ENGINEERINGWORKFLOW-BOUNDARY
- Enforcement: AutomatedTest
- Tests: EngineeringWorkflowNamespaceBoundaryTests, P8EngineeringWorkflowBoundaryHardeningTests
- Risk: EngineeringWorkflow layer/naming boundaries can regress back into AssistantEngineer.Api namespace leakage without explicit governance checks.

## SEC-GUARD-MODULE-BOUNDARY-MATRIX
- Enforcement: AutomatedTest
- Tests: ModuleBoundaryMatrixTests, EngineeringWorkflowModuleBoundaryTests, P8ModuleBoundaryTestExpansionTests
- Risk: Module dependency-direction drift can reintroduce EngineeringWorkflow/API leakage and runtime-tools boundary regressions.

## SEC-GUARD-AUTHORIZATION-WORKFLOW-DECOMPOSITION-DESIGN
- Enforcement: AutomatedTest
- Tests: P8AuthorizationWorkflowHotspotInventoryTests, P8AuthorizationWorkflowDecompositionDesignTests
- Risk: Authorization/workflow hotspot changes can drift into undocumented behavior changes without staged design and compatibility constraints.

## SEC-GUARD-PROTECTED-ENDPOINT-AUTHORIZATION-GATE-CHARACTERIZATION
- Enforcement: AutomatedTest
- Tests: ProtectedEndpointAuthorizationGateCharacterizationTests, P8ProtectedEndpointAuthorizationGateCharacterizationTests
- Risk: Gate decomposition can regress decision precedence/status outcomes or anti-enumeration behavior without explicit characterization lock coverage.

## SEC-GUARD-PROTECTED-ENDPOINT-AUTHORIZATION-GATE-SCOPE-PERMISSION
- Enforcement: AutomatedTest
- Tests: ProtectedEndpointPermissionEvaluatorTests, ProtectedEndpointScopeEvaluationServiceTests, ProtectedEndpointAuthorizationGateCharacterizationTests
- Risk: Internal permission/scope collaborator extraction can drift from existing resolver/permission semantics without explicit decomposition guardrails.

## SEC-GUARD-WORKFLOW-CONTROLLER-SHELL-CHARACTERIZATION
- Enforcement: AutomatedTest
- Tests: EngineeringWorkflowControllerRouteSignatureTests, EngineeringWorkflowControllerCharacterizationTests, EngineeringWorkflowControllerAuthorizationCharacterizationTests, EngineeringWorkflowControllerResponseShapeTests, P8WorkflowControllerShellCharacterizationTests
- Risk: Workflow controller decomposition can regress route signatures, status outcomes, authorization interaction, or response-shape contracts without explicit characterization guardrails.

## SEC-GUARD-WORKFLOW-ORCHESTRATION-HELPER-MIGRATION
- Enforcement: AutomatedTest
- Tests: EngineeringWorkflowControllerRouteSignatureTests, EngineeringWorkflowControllerCharacterizationTests, EngineeringWorkflowNamespaceBoundaryTests, P8WorkflowOrchestrationHelperMigrationTests
- Risk: Workflow helper migration can regress controller contracts or reintroduce API-namespace boundary leakage without explicit migration guardrails.

## SEC-GUARD-WORKFLOW-CONTROLLER-SHELL-REDUCTION
- Enforcement: AutomatedTest
- Tests: EngineeringWorkflowControllerRouteSignatureTests, EngineeringWorkflowControllerCharacterizationTests, EngineeringWorkflowControllerAuthorizationCharacterizationTests, EngineeringWorkflowControllerResponseShapeTests, EngineeringWorkflowControllerShellShapeTests, P8WorkflowControllerShellReductionTests
- Risk: Controller shell refactor can regress route contracts, authorization interaction, or response behavior without explicit reduction and characterization guardrails.

## SEC-GUARD-OWNERSHIPBACKFILL-CLI-PARSER-SIMPLIFICATION
- Enforcement: AutomatedTest
- Tests: OwnershipBackfillCommandLineParserCharacterizationTests, OwnershipBackfillCliExitCodeConsistencyTests, OwnershipBackfillCliRedactionTests, OwnershipBackfillCommandDescriptorCatalogTests, OwnershipBackfillCommandLineParserSimplificationTests
- Risk: CLI parser decomposition can regress command/help/exit semantics, secret redaction, or disabled apply boundary guarantees without explicit characterization and decomposition guardrails.

## SEC-GUARD-SCRIPTS-TOOLS-RATIONALIZATION
- Enforcement: AutomatedTest
- Tests: P8ScriptsToolsRationalizationTests, P8ScriptsToolsInventoryTests, ScriptsToolsInventoryCoverageTests
- Risk: scripts/tools surface cleanup can accidentally weaken release-wrapper semantics or CI check visibility if inventory/coverage boundaries are not enforced.

## SEC-GUARD-ROUTE-INVENTORY-CLASSIFICATION-CLOSURE
- Enforcement: AutomatedTest
- Tests: P8RouteInventoryClassificationClosureTests, P7RouteInventoryCoverageTests, P7RouteClaimsConsistencyTests, P7RouteOperationalCategoryConsistencyTests, P7RouteTenantScopeConsistencyTests, P7ProtectionStageConsistencyTests
- Risk: Route inventory cleanup can hide endpoint coverage gaps or weaken staged claims if reclassification and ignore-list tightening are not verified.

## SEC-GUARD-TERMINOLOGY-CLAIMS-SURFACE
- Enforcement: AutomatedTest
- Tests: P8TerminologyClaimsVocabularyTests, P8TerminologyClaimsSurfaceCleanupTests, P7PostP6ClaimsConsistencyTests
- Risk: Terminology drift can reintroduce ambiguous or overclaim wording that implies unsupported parity, certification, or write-path enablement.

## SEC-GUARD-GOVERNANCE-TEST-BRITTLENESS-REDUCTION
- Enforcement: AutomatedTest
- Tests: P8GovernanceTestBrittlenessReductionTests, P8GovernanceGuardrailPreservationTests
- Risk: Governance assertion refactors can accidentally weaken release-boundary, claims, inventory, or apply-disabled protections if not explicitly preserved.

## SEC-GUARD-P8-ENGINEERING-DOMAIN-CLOSURE
- Enforcement: AutomatedTest
- Tests: P8EngineeringDomainHardeningClosureTests, P8EngineeringDomainHardeningClosureBoundaryTests
- Risk: Closure-stage drift can hide unresolved findings, imply unsupported behavior changes, or weaken boundary guarantees entering P9.
