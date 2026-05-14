# AssistantEngineer.Tools.PerformanceBenchmarks

Opt-in C# tool for performance benchmark campaign scaffolding.

This tool does **not** claim performance gains or measured results by itself.

## Commands

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.PerformanceBenchmarks\AssistantEngineer.Tools.PerformanceBenchmarks.csproj -- list-scenarios

dotnet run --project .\tools\AssistantEngineer.Tools.PerformanceBenchmarks\AssistantEngineer.Tools.PerformanceBenchmarks.csproj -- run-smoke-baseline

dotnet run --project .\tools\AssistantEngineer.Tools.PerformanceBenchmarks\AssistantEngineer.Tools.PerformanceBenchmarks.csproj -- run-smoke-baseline --output-directory .\artifacts\performance\benchmark-campaign
```

## Output

Default output directory:

- `artifacts/performance/benchmark-campaign/`

Generated files:

- `performance-benchmark-skeleton-baseline.json`
- `performance-benchmark-skeleton-baseline.md`

These artifacts are campaign scaffolding outputs and are ignored by git.

## CI posture

- Tool execution is opt-in.
- Normal `dotnet test` flow remains unchanged.
- Heavy benchmark execution should be triggered manually or via dedicated on-demand pipeline.
