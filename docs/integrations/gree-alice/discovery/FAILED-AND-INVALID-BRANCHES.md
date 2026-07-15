# Failed And Invalid Branches

This file records approaches that were stopped or downgraded. A failed branch is useful evidence: it prevents the next engineer from repeating the same unsafe or unproductive path.

## Direct native hook on slot +16

Hypothesis: ART slot `+16` might be a unique native entrypoint for each target method.

Attempted method: slot discrimination and runtime hit correlation around the slot `+16` values.

Observed result: v1.0.46 showed slot `+16` was unique and stable, but v1.0.47a showed it was CodeItem-like data, not an ARM64 native entrypoint.

Current interpretation: slot `+16` can identify method code metadata, but should not receive a native Interceptor.

Stopped because: direct native hook placement on CodeItem-like data is invalid and unsafe.

Safe alternative: use shared `ExecuteNterpImpl` plus safe register/candidate classification.

## jmethodID equality against x0

Hypothesis: the `x0` register at `ExecuteNterpImpl` entry might equal one of the exact target JNI `jmethodID` values.

Attempted method: v1.0.49 filtered `x0` equality against the eight exact JNI method IDs.

Observed result: the gate did not reach capture. v1.0.48 had already shown exact method pointer matches were zero.

Current interpretation: `x0` is ArtMethod-like but not directly equal to JNI `jmethodID` values in this lab.

Stopped because: equality against JNI method IDs is not supported by the evidence.

Safe alternative: derive a non-live-dereference candidate classification strategy after first host/agent failure is fixed.

## Invalid shared stub validator

Hypothesis: the v1.0.49 validator could prove the shared stub address before installing the target correlation gate.

Attempted method: shared stub validation for one exact `ExecuteNterpImpl` address.

Observed result: `HostErrorType: shared_stub_validation_failed`, `HookReady: False`, `GateComplete: False`, while prior evidence showed slot `+24` shared `libart.so` `ExecuteNterpImpl`.

Current interpretation: this is a validator/host logic failure, not proof that `ExecuteNterpImpl` was absent.

Stopped because: it blocked the observer before capture.

Safe alternative: fix validator logic offline before a future run is considered.

## Live dereference `*(x0 + 16)` inside hot callback

Hypothesis: reading `*(x0 + 16)` inside the `ExecuteNterpImpl` callback would allow target CodeItem matching.

Attempted method: v1.0.49a CodeItem equality against target CodeItem values.

Observed result: process terminated before target gate completion with `SessionDetachedBeforeTargetArtMethodGate`. The surrounding task context records bad access due to invalid address.

Current interpretation: live dereference inside the hot nterp callback is unsafe in this setup.

Stopped because: it can crash or terminate the target process.

Safe alternative: do not dereference in the hot callback; collect bounded candidates only, detach, and classify after detach.

## v1.0.49b deferred classification did not complete

Hypothesis: deferred candidate classification would avoid the v1.0.49a live dereference hazard.

Attempted method: v1.0.49b set `x0_dereferenced_inside_interceptor: false`, `deferred_candidate_classification: true` in the initial status event.

Observed result: attach and script load succeeded, `vm_perform_entered` was emitted, then process terminated before hook-ready/gate-complete. The final result had zero hits and `deferred_candidate_classification: false` because classification never ran.

Current interpretation: the concept remains plausible, but this report does not validate it. The first problem to solve is early process/session termination.

Stopped because: no capture occurred.

Safe alternative: offline-diagnose first host/agent/process termination from existing logs.

## Missing CLEANUP marker in preparation

Hypothesis: v4.8.1 preparation would always reach a `CLEANUP` marker after the login window.

Attempted method: launcher/preparation with attach-success, patch-set-ready, heartbeat, and a 180-second window.

Observed result: some runs reached heartbeat around 179 seconds and left GREE+ alive with reapplyCount 0, but the launcher did not see `CLEANUP`; the observer may not have started.

Current interpretation: this is a launcher/preparation coordination problem, separate from ART/nterp target validity.

Stopped because: main observer ownership becomes ambiguous without cleanup.

Safe alternative: document the condition and require explicit cleanup marker before treating a run as observer-ready.

## Stale preparation helpers

Hypothesis: leaving helper Frida sessions around would be harmless.

Observed result: the runbook and task context require cleanup before main observer attach. Stale helpers can own hooks, alter PID selection, or hide why a later observer did not start.

Stopped because: it makes evidence attribution unreliable.

Safe alternative: preparation session cleanup, then attach the observer to the foreground PID.

## ADB daemon bootstrap NativeCommandError noise

Hypothesis: every ADB bootstrap message indicates a target-stage failure.

Observed result: evidence contains bootstrap/noise around device and tooling preparation; not every host-side NativeCommandError is an observer result.

Current interpretation: classify ADB/bootstrap noise separately from target gate validation.

Safe alternative: record command, phase, and whether app/observer state changed.

## PowerShell interactive `else` parsing mistake

Hypothesis: interactive PowerShell fragments can be pasted as if they were complete scripts.

Observed result: the task context says this case should be documented if found; this recovery pass did not find a decisive local artifact proving it as a stage blocker.

Status: PARTIAL-EVIDENCE / not independently confirmed in the inspected summaries.

Safe alternative: run complete `.ps1` scripts or single complete scriptblocks, and record the transcript path.

## TLS/native Conscrypt side branches

Hypothesis: scoped TLS/native verification hooks could produce clean QueryOnline/API captures.

Observed result: the `intercept-1c-single-host` line has mixed PASS and FAIL outcomes. Some target TLS ownership and wrapper/verify diagnostics passed leak checks; several native Conscrypt inventory/trace runs timed out or failed.

Stopped because: this line did not establish the later MethodChannel/ART target hit correlation and includes sensitive network-adjacent evidence.

Safe alternative: keep only sanitized counts and leak-checked summaries in documentation.

## Static-only endpoint and operation interpretation

Hypothesis: static endpoints and operation names prove runtime command transport.

Observed result: static and AOT evidence found markers, but exact runtime envelope, auth/method/body/response contract, and target MethodChannel hits remain unknown.

Stopped because: static text matches are not runtime proof.

Safe alternative: treat static evidence as architecture context only.
