# AssistantEngineer Goal Protocol

## Purpose

AssistantEngineer Goal Protocol is a deterministic engineering workflow for planning, executing, verifying, and auditing large brownfield changes. It adapts useful workflow principles to this repository without importing external workflow code or creating a runtime orchestration feature.

The protocol has no runtime AI agent, no RAG/vector search, no Telegram command execution, and no production/public release claim. It does not authorize autonomous engineering execution. Human review remains required for scope, evidence, safety, and merge decisions.

## Protocol Stages

### 1. Intake

Capture the requested outcome, source context, target branch, acceptance conditions, and expected final report. Resolve contradictions before execution.

### 2. Constraints

List explicit in-scope and out-of-scope boundaries, forbidden changes, safety rules, and claims that evidence cannot support.

### 3. Brownfield Recon

Inspect the current branch, working tree, relevant modules, existing conventions, tests, scripts, documentation, and known constraints before proposing edits. Existing user changes are preserved and incorporated.

### 4. Roadmap

Translate the goal into an ordered roadmap with observable deliverables and verification points. The roadmap is the final-audit baseline, not a promise that every initial implementation detail will remain unchanged.

### 5. Adaptive Phases

Choose the smallest useful number of phases. Phase count may change when recon or verification reveals new information, but scope and constraints remain explicit.

### 6. Preflight

Before execution, confirm the branch, working-tree state, required tools, baseline commands, dependency availability, and protected files or behaviors.

### 7. Phase Execution Rules

Each phase follows its phase spec, changes only its declared surface, preserves protected behavior, and records deviations. Work does not advance past unresolved blockers.

### 8. Phase Verification Evidence

Run the mandatory commands and capture concise evidence for each acceptance criterion. A completion statement must match actual verification; warnings and skipped checks remain visible.

### 9. Recovery And Fix-Spec Handoff

When a phase fails, identify the failing evidence, preserve useful work, and create a focused fix-spec or handoff note. Recovery must not weaken checks or silently expand scope.

### 10. Final Audit

Audit the completed work against the roadmap, phase specs, constraints, changed files, forbidden files, forbidden claims, mandatory commands, and remaining warnings. Merge readiness is a reviewed decision based on evidence.

### 11. Generated Artifacts Policy

Committed protocol documents and templates live under `docs/engineering-workflow/`. Generated run reports belong under ignored `artifacts/verification/` or `artifacts/planning/`. Generated artifacts are not committed unless a reviewer explicitly converts selected evidence into a maintained document.

## Repository Safety Boundary

- This protocol is documentation and tests, not a runtime service or endpoint.
- Calculation physics, public calculation routes, and ISO52016 runtime behavior remain outside ED-21A.
- `scripts/engineering-core/verify-engineering-core-v1.ps1` is protected and remains unchanged by goal-protocol work.
- Evidence must never overstate verification, coverage, safety, or release readiness.
