# Engineering Artifact Storage Abstraction

## Purpose

This document defines the foundation abstraction for large engineering artifacts:

- calculation trace artifacts;
- report artifacts;
- validation comparison artifacts;
- workflow scenario artifacts;
- diagnostic payload artifacts.

This step introduces a storage abstraction only. It does not migrate existing workflow persistence records.

## Scope

This abstraction covers:

- artifact descriptor contract;
- artifact write/read/delete contract;
- in-memory storage provider for local/test usage;
- file-system storage provider for local durable artifact files;
- size and checksum integrity policies.

This step does not include object/blob providers (S3/Azure/MinIO) and does not change public API contracts.

## Non-claims

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No pyBuilding\u0045nergy parity claim.
- No full ISO/EN compliance claim.
- No certified/certification claim.

## Artifact kinds

Supported kind labels are application-defined string values. Recommended kinds:

- `CalculationTrace`
- `EngineeringReport`
- `ValidationComparison`
- `WorkflowScenarioResult`
- `DiagnosticPayload`
- `ManualValidationFixture`

## Storage providers

- `InMemory`: thread-safe in-memory storage for tests/local runtime.
- `FileSystem`: file-backed storage using a configured root path.

Provider selection is controlled via `EngineeringArtifacts:Provider`.

## Descriptor model

`EngineeringArtifactDescriptor` tracks:

- identity (`artifactId`, `artifactKind`, `scope`, optional subject fields);
- content metadata (`contentType`, `sizeBytes`);
- integrity (`sha256`);
- storage metadata (`storageProvider`, `storageKey`);
- creation timestamp and optional key-value metadata.

Descriptor compatibility is a migration requirement for future provider changes.

## Integrity/SHA256 policy

- Each stored artifact content is hashed with SHA256.
- Hash is persisted in descriptor metadata.
- File-system provider can verify SHA256 on read when enabled.
- Integrity mismatch is reported as a failure result.

## Size limit policy

- `EngineeringArtifacts:MaxArtifactBytes` defines max accepted artifact payload size.
- Oversized artifacts are rejected with validation failures.
- Existing workflow payload truncation policy remains unchanged in current persistence flow.

## Relationship to workflow persistence

- Existing workflow persistence (`EngineeringWorkflowPersistence*`) remains source of truth for current workflow artifact endpoints.
- This abstraction is introduced as a forward-compatible extension seam for future large artifact migration.
- Current step does not migrate persisted records and does not change endpoint behavior.

## Relationship to calculation trace explainability

This abstraction is intended as the future storage path for large explainability traces defined in `docs/engineering/calculation-trace-explainability.md`.

## Relationship to reports

This abstraction is intended as the future storage path for larger engineering report payloads and exports, without forcing immediate schema migrations.

## Relationship to observability diagnostics policy

Artifact storage operational logging should follow `docs/architecture/observability-diagnostics-policy.md` and event codes in `docs/architecture/observability-diagnostic-events.json`.

## Future object/blob storage providers

Future providers may include object/blob adapters (for example S3/Azure-compatible storage). Those providers must:

- preserve descriptor fields and checksum semantics;
- preserve artifact kind and scope metadata;
- maintain backward-compatible read paths during migration windows.

## Migration policy

- No implicit migration in this phase.
- Existing persisted artifacts remain valid and unchanged.
- Any future migration must provide:
  - explicit cutover plan;
  - descriptor compatibility checks;
  - checksum parity verification;
  - rollback path.
