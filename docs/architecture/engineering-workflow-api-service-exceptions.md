# Engineering Workflow API Service Exceptions

This registry is used by the soft architecture guard to allow temporary, explicit exceptions when a workflow/scenario/job application-service-like file is added under:

- `src/Backend/AssistantEngineer.Api/Services/Calculations/**`

Policy:
- Prefer adding new workflow application logic to `AssistantEngineer.Modules.EngineeringWorkflow`.
- Use exceptions only for short-lived transition work.
- Each exception entry must include:
- Relative file path
- Reason
- Owner
- Date
- Planned removal milestone

Template:

`- path: src/Backend/AssistantEngineer.Api/Services/Calculations/<...>.cs | reason: <...> | owner: <...> | date: YYYY-MM-DD | remove-by: <milestone>`

Current exceptions:
- (none)
