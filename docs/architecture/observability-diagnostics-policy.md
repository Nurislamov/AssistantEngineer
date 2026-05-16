# Observability Diagnostics Policy

## Purpose

This policy defines structured logging, diagnostic event taxonomy, correlation identifiers, and safe observability rules for engineering workflow execution and supporting services.

## Scope

This policy covers:

- API requests;
- engineering workflow;
- calculation jobs;
- calculation pipeline;
- input quality checks;
- validation fixture execution;
- artifact storage;
- durable persistence;
- reports and calculation traces.

## Non-claims

- No production monitoring certification claim.
- No globally exactly-once distributed execution claim.
- No exact EnergyPlus equivalence claim.
- No ASHRAE 140 compliance claim.
- No full ISO/EN compliance claim.
- No certified/certification claim.

## Identifier model

The observability identifier model uses:

- `correlationId`
- `requestId`
- `workflowId`
- `scenarioId`
- `jobId`
- `buildingId`
- `roomId`
- `calculationType`
- `artifactId`
- `validationCaseId`

## Structured logging rule

- Use `ILogger` structured templates with named properties.
- Do not use message string concatenation for operational events.
- Prefer stable event codes and categories.
- Include relevant identifiers as named properties.
- Do not log full request/response payload bodies by default.
- Do not log secrets, API keys, bearer tokens, or credential material.
- Reference large artifacts by `artifactId` and `sha256`; do not embed full content.

## Diagnostic event severity model

Logging severity model:

- `Trace`
- `Debug`
- `Information`
- `Warning`
- `Error`
- `Critical`

Engineering diagnostic severity model:

- `Info`
- `Warning`
- `Error`
- `Blocking`

The two severity models are related but not identical. Engineering severity describes domain readiness, while logging severity describes operational impact.

## Event taxonomy

Event categories:

- `Workflow`
- `Job`
- `Calculation`
- `InputQuality`
- `Validation`
- `ArtifactStorage`
- `Persistence`
- `Reporting`
- `Governance`

## Required event metadata

Diagnostic and operational events should include where applicable:

- `eventCode`
- `category`
- `severity`
- `correlationId`
- `workflowId`
- `jobId`
- `buildingId` or `roomId`
- `calculationType`
- `artifactId`
- `durationMs`
- `resultStatus`
- `errorCode`
- `diagnosticCount`

## Timing/duration policy

- Measure duration for long-running workflow, job, calculation, and artifact operations.
- Use `Stopwatch` or `TimeProvider` according to project conventions.
- Duration measurements are for diagnostics only and must not alter deterministic calculation outputs.

## Privacy/security policy

- No API keys, secrets, access tokens, or authentication credentials in logs.
- No raw large request bodies unless explicitly safe and size-limited.
- No full engineering payloads in logs.
- No full trace/report/artifact contents in logs.
- No personal data unless explicitly reviewed and approved.

## Future OpenTelemetry integration

Future work only:

- `ActivitySource` spans;
- metrics counters and histograms;
- distributed traces;
- dashboards and alerts.

This policy does not require external collectors for normal local runs.
