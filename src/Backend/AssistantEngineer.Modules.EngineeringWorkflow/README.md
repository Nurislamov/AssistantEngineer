# AssistantEngineer.Modules.EngineeringWorkflow

This project is a migration skeleton for extracting engineering workflow/job/scenario/idempotency application logic out of `AssistantEngineer.Api`.

Current status:
- First safe extraction slice completed.
- Non-HTTP workflow contracts/builders moved from API in compatibility mode.
- Job lifecycle policies, payload codec, and in-memory idempotency abstractions/services moved in second extraction slice.
- Runtime behavior preserved (no route/JSON/physics changes in this slice).

See:
- `docs/architecture/engineering-workflow-module-migration-plan.md`
