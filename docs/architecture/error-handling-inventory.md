# Error Handling Inventory

## Scan metadata

- Date (UTC): 2026-05-14
- Scope: `src/Backend`
- Commands:
  - `rg "throw new Exception\(" src/Backend -n`
  - `rg "throw new InvalidOperationException\(" src/Backend -n`
  - `rg "throw new ArgumentException\(" src/Backend -n`
  - `rg "catch\s*\([^\)]*\)" src/Backend -n`

## High-level counts

- `throw` keyword occurrences (all forms): **220**
- `throw new Exception(...)`: **0**
- `throw new InvalidOperationException(...)`: **70**
- `throw new ArgumentException(...)`: **11**
- `catch (...)` blocks: **36**
- empty `catch` blocks: **0**

Heuristic catch review:
- `catch` without log/throw/return: **6**
- `catch` returning fallback but without log/throw: **14**

## Top throw concentration (by file)

1. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Validation/Iso52016/Iso52016ExternalValidationFixtureLoader.cs` (25)
2. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation/Iso16798/Iso16798NaturalVentilationCalculator.cs` (5)
3. `src/Backend/AssistantEngineer.SharedKernel/Primitives/Result.cs` (5)
4. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ground/Iso13370/Iso13370VirtualGroundTemperatureCalculator.cs` (4)
5. `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Matrix/Iso52016MatrixHourlySolver.cs` (3)

## Classification

### Acceptable

- `Result` invariant guards in `AssistantEngineer.SharedKernel/Primitives/Result.cs`.
- Pure-calculator invariant checks in:
  - `.../Iso16798NaturalVentilationCalculator.cs`
  - `.../Iso13370VirtualGroundTemperatureCalculator.cs`
  - `.../Iso52016MatrixHourlySolver.cs`
  - `.../Iso52016MultiZoneLinearSystem.cs`
- Startup/config fail-fast checks in infrastructure:
  - `Infrastructure/Composition/PersistenceRegistration.cs`
  - `Infrastructure/Configuration/ConfigurationSecurityValidator.cs`

Reason: these are invariant/programmer/configuration failures, not expected end-user validation flow.

### Should become `Result<T>` (incremental, later refactor)

- `Iso52016ExternalValidationFixtureLoader` currently throws for many expected fixture-shape failures.
- Benchmark-side loaders/importers that already sit on boundary could return richer `Result<T>` payloads instead of exception-centric control.

Reason: these failures are expected at data boundary and can be represented as controlled failures.

### Should become guard validation

- Argument validation in adapters where input is boundary-provided and can be rejected before deep execution:
  - `Iso52016ConstructionAssemblyApplicationAdapter`
  - `AnnualProfileGenerator`

Reason: keep hard-fail for programmer misuse, but prefer explicit boundary validation before adapter invocation.

### Needs later refactor

Potential catch quality hotspots (no log and no rethrow):
- `AssistantEngineer.Api/Controllers/Calculations/EngineeringWorkflowController.cs:85`
- `AssistantEngineer.Api/Services/Calculations/Idempotency/EfEngineeringIdempotencyService.cs:84,195`
- `AssistantEngineer.Api/Services/Calculations/Workflow/EngineeringWorkflowSubmissionService.cs:177`
- `AssistantEngineer.Infrastructure/Integrations/Benchmarks/EnergyPlusBenchmarkOptionsValidator.cs:48`
- `AssistantEngineer.Infrastructure/Integrations/Benchmarks/EnergyPlusBenchmarkRunner.cs:235`

Potentially acceptable but should be reviewed for observability/documentation (return fallback without logging):
- codec/payload parsing fallbacks returning `null` or empty collection in workflow persistence/payload codecs.

## Notes

- This inventory does **not** change runtime behavior.
- `throw new Exception` is currently absent and now protected by architecture guard test.
- No claim of exactly-once/distributed guarantees is introduced by this policy work.
