# Current State

## Current stage

`GREE-ALICE-API-DISCOVERY-DOC2` - offline documentation recovery for the Android/ART/nterp GREE+ discovery stream.

## Last valid completed stage

`v1.0.48 GREE EXECUTENTERP REGISTER FILTER FEASIBILITY`.

Evidence:

```text
D:\AssistantEngineer-live-evidence\GREE-ALICE-API-DISCOVERY\03-android-lab\executenterp-register-filter-feasibility\run-20260715-164229\sanitized-report\summary.txt
```

Outcome: `VALID`, leak check PASS, `x0` ArtMethod register candidate confirmed, seven total stub hits, listener detached true.

## Last attempted stage

`v1.0.49b GREE EXECUTENTERP TARGET ARTMETHOD CORRELATION`.

Evidence:

```text
D:\AssistantEngineer-live-evidence\GREE-ALICE-API-DISCOVERY\03-android-lab\executenterp-target-artmethod-correlation\run-20260715-230048\GREE-executenterp-target-artmethod-correlation-v1.0.49b-report.zip
```

Outcome: `INVALID`, leak check PASS, process terminated before hook-ready/gate-complete.

ZIP SHA256: `C90004A9ABA44DE118F8EDE1121390D36DC3377B4A243BBB4C364957C0748DEC`.

## Current blocker

The target ArtMethod/CodeItem correlation gate fails before hook-ready. v1.0.49 and v1.0.49a/v1.0.49b fail for different immediate reasons, but none produces target matches:

- v1.0.49: `shared_stub_validation_failed`.
- v1.0.49a: `SessionDetachedBeforeTargetArtMethodGate`.
- v1.0.49b: `SessionDetachedBeforeTargetArtMethodGate`.

## Confirmed facts

- Static and AOT evidence confirms Flutter/plugin/API architecture markers.
- Runtime inventory confirms MethodChannel objects can be observed safely as identifiers only.
- v1.0.44c confirms eight target method IDs through direct JNI.
- v1.0.47a confirms slot `+16` is CodeItem-like data, not a native hook target.
- v1.0.48 confirms `x0` is the best plausible ArtMethod register candidate at shared `ExecuteNterpImpl`.

## Invalid assumptions

- Slot `+16` is not a native ARM64 hook target.
- `x0` is not proven equal to JNI `jmethodID`.
- v1.0.49b did not validate deferred classification.
- Static endpoint/method names do not prove runtime command transport.

## Active evidence

The active evidence set is indexed in [EVIDENCE-INDEX.md](./EVIDENCE-INDEX.md). Do not copy raw evidence into git.

## Next safe experiment

Before any future device work, perform offline analysis of existing v1.0.49a/v1.0.49b host/agent logs to explain why the process terminates before hook-ready. The next branch remains:

```text
fix-first-host-or-agent-error
```

## Explicitly prohibited actions

- No GREE+ launch.
- No phone connection.
- No Frida observer run.
- No network request.
- No HVAC command.
- No production/API/Telegram/runtime/deployment/database/migration change.
- No secrets or raw payloads in git.
