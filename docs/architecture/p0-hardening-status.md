# AssistantEngineer P0 Hardening Status

This document records the P0 architecture hardening baseline for the engineering workflow API.

## Closed P0 items

- Workflow diagnostics, step-status, trace-preview and report-preview logic are extracted from `EngineeringWorkflowController` into workflow services.
- Workflow state building and infrastructure fallback state construction are extracted into `EngineeringWorkflowStateBuilder`.
- Workflow durable persistence uses EF Core migrations for the SQLite workflow store instead of `EnsureCreated()`.
- A minimal API-key authentication boundary exists for non-development environments.
- Engineering workflow calculation execution endpoints use the long-running request-timeout policy.
- Architecture guard tests prevent the workflow controller from reabsorbing the extracted implementation details.

## Still intentionally open after P0

- Queued jobs still need a real background worker before the queued execution path can be treated as production-ready.
- Workflow state loading still needs a batch-input snapshot boundary to remove per-room query loops.
- Scenario orchestration still needs decomposition before the runner can move out of the API layer.
- Rate limiting, CORS, health/readiness endpoints and JSON artifact size gates are P1/P2 product-hardening work.

## Guardrail

`EngineeringWorkflowController` is now treated as a thin HTTP adapter. New workflow behavior should be added behind named services instead of private controller methods.