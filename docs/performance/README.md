# Performance Benchmarks

This folder contains campaign planning and run guidance for opt-in performance benchmarks.

## Quick start

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.PerformanceBenchmarks\AssistantEngineer.Tools.PerformanceBenchmarks.csproj -- list-scenarios

dotnet run --project .\tools\AssistantEngineer.Tools.PerformanceBenchmarks\AssistantEngineer.Tools.PerformanceBenchmarks.csproj -- run-smoke-baseline
```

## Output location

Default generated outputs:

- `artifacts/performance/benchmark-campaign/performance-benchmark-skeleton-baseline.json`
- `artifacts/performance/benchmark-campaign/performance-benchmark-skeleton-baseline.md`

## CI behavior

- Benchmark campaign runs are opt-in.
- Standard CI (`dotnet build` / `dotnet test`) is unaffected.
- Heavy runs should be manual or in dedicated on-demand pipelines.

## Claims policy

- Do not publish performance claims without measured data.
- Do not infer optimization impact from skeleton outputs.
