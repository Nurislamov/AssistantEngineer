# Error Handling Policy

## Scope

This policy applies to backend code under `src/Backend`.

## Core rules

1. Expected application/domain failures must be represented with `Result<T>` (or `Result`) at application boundaries.
2. Controllers map `Result<T>` into HTTP responses; controller actions should not use exceptions for expected validation flow.
3. Pure calculators may throw `InvalidOperationException` or `ArgumentOutOfRangeException` only for programmer error / invariant violation (impossible state), not user-input validation flow.
4. `throw new Exception(...)` is forbidden in backend source.
5. No catch-swallow: `catch` blocks must do at least one of:
   - translate into `Result<T>.Failure(...)` / deterministic fallback;
   - log and continue intentionally;
   - rethrow.
6. No behavior drift in this policy step:
   - no public API response contract changes;
   - no calculation physics changes;
   - no diagnostics wording/order changes unless explicitly planned.

## Decision guide

Use `Result<T>` when:
- input/business validation can fail in normal operation;
- infrastructure dependency is unavailable but flow can return controlled failure;
- caller is expected to branch on success/failure.

Use exceptions when:
- guard clause protects invariant that should never fail in valid call graph;
- execution reached impossible internal state indicating programmer/configuration error;
- startup/bootstrap configuration is invalid and app should fail fast.

## Mapping guidance

- Application services: return `Result<T>` for expected failures.
- API composition/controllers: map `Result<T>` to HTTP (`BadRequest`, `NotFound`, `Conflict`, etc.).
- Pure numeric kernels: keep invariant exceptions explicit and narrow (`InvalidOperationException`, `ArgumentOutOfRangeException`).
- Avoid broad `catch (Exception)` unless it is an explicit boundary with logging/translation.

## Guardrails

- Architecture test `ErrorHandlingRawExceptionGuardTests` bans raw `throw new Exception` across backend source.
- Existing and future refactors should move expected failures away from exception-only signaling to `Result<T>` incrementally.

## Out of scope for this step

- Mass replacement of existing throw sites.
- Public API error-response redesign.
- Reordering/changing diagnostics text for existing workflows.
