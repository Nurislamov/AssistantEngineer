# GREE-ALICE API Discovery

This folder is the local recovery point for the `GREE-ALICE-API-DISCOVERY` research stream. It documents completed offline/lab evidence only. It does not continue GREE+ research, run Frida, attach to the phone, change production runtime, send HVAC commands, add migrations, or copy raw private evidence into git.

Local evidence root:

```text
D:\AssistantEngineer-live-evidence\GREE-ALICE-API-DISCOVERY
```

## Status

Documentation recovery stage: `GREE-ALICE-API-DISCOVERY-DOC2`.

Last valid ART/nterp stage: `v1.0.48 GREE EXECUTENTERP REGISTER FILTER FEASIBILITY`.

Last attempted stage: `v1.0.49b GREE EXECUTENTERP TARGET ARTMETHOD CORRELATION`.

Current blocker: the target ArtMethod/CodeItem correlation gate does not reach hook-ready or gate-complete. The v1.0.49b report shows attach and script load, then `SessionDetachedBeforeTargetArtMethodGate` with process termination before capture.

## Navigation

- [DISCOVERY-TIMELINE.md](./DISCOVERY-TIMELINE.md) - chronological recovery of discovered stages.
- [CONFIRMED-FINDINGS.md](./CONFIRMED-FINDINGS.md) - evidence-backed facts only.
- [FAILED-AND-INVALID-BRANCHES.md](./FAILED-AND-INVALID-BRANCHES.md) - stopped branches and why.
- [METHODCHANNEL-ENTRYPOINTS.md](./METHODCHANNEL-ENTRYPOINTS.md) - the eight target Java/JNI methods.
- [ART-AND-NTERP-NOTES.md](./ART-AND-NTERP-NOTES.md) - project-specific ART/nterp observations.
- [LAB-RUNBOOK.md](./LAB-RUNBOOK.md) - safe offline recovery runbook.
- [EVIDENCE-INDEX.md](./EVIDENCE-INDEX.md) - indexed local artifacts and hashes.
- [ARCHIVE-REPORT-v1.0.49b.md](./ARCHIVE-REPORT-v1.0.49b.md) - full analysis of the provided ZIP.
- [CURRENT-STATE.md](./CURRENT-STATE.md) - short handoff state.
- [DECISION-LOG.md](./DECISION-LOG.md) - decision records.
- [GLOSSARY.md](./GLOSSARY.md) - terms.
- [DOCUMENTATION-VALIDATION.md](./DOCUMENTATION-VALIDATION.md) - validation and leak/secret checks for these docs.

## Safety Boundary

Allowed here: documentation, local evidence indexing, sanitized-derived facts, and `PROJECT_STATE.md` checkpoint updates.

Forbidden here: HVAC commands, power/mode/fan/temperature changes, credentials, access tokens, cookies, raw payloads, raw ByteBuffer data, arbitrary register exports, production integration, Telegram integration, database changes, migrations, deployment changes, Android package changes, Frida agent changes, launcher changes, phone connection, observer runs, and network requests.

Every imported fact must remain either sanitized or structural: counts, status markers, module names, offsets, decisions, hashes, and local artifact paths.

## Current Architecture Picture

The evidence supports this picture:

- GREE+ package under test: `com.gree.greeplus`, observed app version `1.25.3.7`.
- Flutter/AOT and plugin static analysis found a `com.greeplus.flutter` candidate channel and Gree transport/API markers.
- Runtime inventory confirmed live MethodChannel objects, but did not prove business payloads or target method runtime hits.
- Direct JNI after bypass confirmed four classes and eight MethodChannel/FlutterJNI method IDs.
- ART slot analysis showed slot `+24` shared `libart.so` `ExecuteNterpImpl`, while slot `+16` is CodeItem-like data, not a native ARM64 hook target.
- ExecuteNterpImpl register feasibility confirmed `x0` as the best ArtMethod-like register candidate, but not equal to JNI `jmethodID`.

## Next Safe Question

The next safe research question is not a new live observer. It is: why does the target ArtMethod/CodeItem correlation gate terminate before hook-ready in v1.0.49a/v1.0.49b, and can that first host/agent failure be diagnosed offline from existing logs before any new device work is considered?
