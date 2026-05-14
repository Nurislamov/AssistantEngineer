# Performance Benchmark Plan

## Purpose

Define a production-scale benchmark campaign skeleton for AssistantEngineer without changing calculation physics.

This document is a planning baseline and does not claim performance improvements.

## Scope boundaries

- No calculation formula changes.
- No silent EF query rewrites.
- No fake performance claims.
- Benchmarks stay opt-in and do not block normal CI.

## Target scenarios

1. Small building
- Goal: baseline latency/allocation for low-complexity building workloads.
- Data shape: low zone/room count.

2. Medium building
- Goal: observe scaling from small profile.
- Data shape: moderate room/envelope complexity.

3. Large building
- Goal: evaluate high-complexity latency and memory envelope.
- Data shape: high room/zone/boundary count.

4. 8760 hourly simulation
- Goal: isolate annual true-hourly execution cost.
- Data shape: complete 8760 weather/profile inputs where applicable.

5. Multi-zone case
- Goal: capture multi-zone topology/adjacency path cost.
- Data shape: multiple conditioned/unconditioned zones with adjacency links.

6. Report generation
- Goal: quantify report assembly/export overhead after calculations.
- Data shape: scenario result with diagnostics and trace sections.

7. Workflow snapshot creation
- Goal: measure workflow state/snapshot building overhead.
- Data shape: realistic workflow project/building/zones diagnostics payload.

## Measurement dimensions

For each scenario capture:

- wall-clock duration (median/p95 over repeated runs);
- managed allocations (if profiling mode enabled);
- peak process working set (where available);
- scenario input metadata (size, zone count, hourly record count).

## Campaign phases

1. Skeleton phase (current)
- Establish scenario catalog, output schema, and run instructions.
- Produce non-claim baseline artifacts (no measured benchmark claims).

2. Controlled measurement phase
- Execute each scenario with fixed fixture inputs.
- Capture repeated-run metrics and environment metadata.

3. Analysis phase
- Compare trend across commits/branches.
- Identify hotspots for targeted optimization proposals.

4. Optimization phase (future)
- Propose changes only with regression tests and physics-safety checks.

## Tooling approach

- BenchmarkDotNet is not currently standardized in repo tooling; this plan uses an opt-in C# harness in `tools/AssistantEngineer.Tools.PerformanceBenchmarks`.
- Harness commands are manual/on-demand and not wired into standard CI gating.

## Output and retention

Default output path:

- `artifacts/performance/benchmark-campaign/`

Output files:

- `performance-benchmark-skeleton-baseline.json`
- `performance-benchmark-skeleton-baseline.md`

Artifacts are generated evidence for campaign runs and should remain out of normal source control workflows.

## Non-claims

- This plan does not claim current performance is sufficient for all production loads.
- This plan does not claim any optimization effect.
- No benchmark result claim is valid without measured run data.
