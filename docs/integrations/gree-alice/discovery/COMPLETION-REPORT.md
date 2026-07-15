# GREE-ALICE API Discovery Documentation Report

Date: 2026-07-15

Repository:

```text
D:\Project\AssistantEngineer
```

Documentation root:

```text
docs/integrations/gree-alice/discovery
```

Local evidence root:

```text
D:\AssistantEngineer-live-evidence\GREE-ALICE-API-DISCOVERY
```

## Summary

The GREE-ALICE API discovery research history has been documented locally in the AssistantEngineer repository. This was a documentation-only recovery stage.

No production runtime, Telegram runtime, database, migrations, deployment files, Android package, Frida agents, launchers, observers, phone state, network state, or HVAC controls were changed or executed.

The documentation now separates confirmed facts, invalid assumptions, failed branches, evidence locations, and the current recovery state so the research can be resumed later without reading old chats.

## What was documented

The recovered flow covers:

- setup and local lab context;
- WireGuard/proxy and Android lab evidence roots;
- static APK inventory;
- Flutter/plugin contract extraction;
- Flutter AOT/channel proof;
- runtime MethodChannel inventory;
- direct JNI MethodChannel entrypoint confirmation;
- ART method slot analysis;
- CodeItem versus native-entrypoint interpretation;
- `ExecuteNterpImpl` observation;
- `x0` ArtMethod candidate evidence;
- invalid v1.0.49, v1.0.49a, and v1.0.49b target-correlation attempts;
- current blocker and next safe branch.

## Key result

Last valid completed ART/nterp stage:

```text
v1.0.48 GREE EXECUTENTERP REGISTER FILTER FEASIBILITY
```

Confirmed by evidence:

- one exact `ExecuteNterpImpl` hook was ready;
- gate complete;
- listener detached;
- total stub hits: 7;
- `x0` was the best plausible ArtMethod register candidate;
- exact `jmethodID` pointer matches: 0;
- exact CodeItem pointer matches: 0;
- leak check: PASS.

Last attempted stage:

```text
v1.0.49b GREE EXECUTENTERP TARGET ARTMETHOD CORRELATION
```

Final result:

```text
INVALID
```

Reason:

- attached: true;
- script loaded: true;
- hook ready: false;
- gate complete: false;
- host error type: `SessionDetachedBeforeTargetArtMethodGate`;
- session detached reason: `process-terminated`;
- captured seconds: 0.0;
- total stub hits: 0;
- total target matches: 0;
- matched method count: 0/8.

v1.0.49b report ZIP:

```text
D:\AssistantEngineer-live-evidence\GREE-ALICE-API-DISCOVERY\03-android-lab\executenterp-target-artmethod-correlation\run-20260715-230048\GREE-executenterp-target-artmethod-correlation-v1.0.49b-report.zip
```

ZIP SHA256:

```text
C90004A9ABA44DE118F8EDE1121390D36DC3377B4A243BBB4C364957C0748DEC
```

Leak check:

```text
PASS
```

## Current blocker

The active blocker is early process/session detach before the target ArtMethod/CodeItem correlation gate reaches hook-ready.

The deferred v1.0.49b design was configured in the initial status event, but the report does not prove that deferred candidate classification ran. The process terminated before capture and classification.

Current safe next branch:

```text
fix-first-host-or-agent-error
```

This should be an offline analysis of existing v1.0.49a/v1.0.49b host/agent logs before any new device work is considered.

## Documentation files

Entry point:

```text
docs/integrations/gree-alice/discovery/README.md
```

Main documents:

```text
docs/integrations/gree-alice/discovery/DISCOVERY-TIMELINE.md
docs/integrations/gree-alice/discovery/CONFIRMED-FINDINGS.md
docs/integrations/gree-alice/discovery/FAILED-AND-INVALID-BRANCHES.md
docs/integrations/gree-alice/discovery/METHODCHANNEL-ENTRYPOINTS.md
docs/integrations/gree-alice/discovery/ART-AND-NTERP-NOTES.md
docs/integrations/gree-alice/discovery/LAB-RUNBOOK.md
docs/integrations/gree-alice/discovery/EVIDENCE-INDEX.md
docs/integrations/gree-alice/discovery/ARCHIVE-REPORT-v1.0.49b.md
docs/integrations/gree-alice/discovery/CURRENT-STATE.md
docs/integrations/gree-alice/discovery/DECISION-LOG.md
docs/integrations/gree-alice/discovery/GLOSSARY.md
docs/integrations/gree-alice/discovery/DOCUMENTATION-VALIDATION.md
```

This report:

```text
docs/integrations/gree-alice/discovery/COMPLETION-REPORT.md
```

Project checkpoint updated:

```text
PROJECT_STATE.md
```

Parent GREE-ALICE index updated:

```text
docs/integrations/gree-alice/README.md
```

## Confirmed facts

- GREE+ package under lab observation: `com.gree.greeplus`.
- Static APK inventory confirmed app version `1.25.3.7`.
- Flutter/plugin extraction confirmed `libapp.so` SHA256:

```text
43071232B93304D2F2249DE1CB2EC72D9BDD5F42451EBF1D17CDCA9E70E7A720
```

- Direct JNI confirmed 4/4 classes and 8/8 method IDs.
- Slot `+16` is CodeItem-like data, not a native ARM64 hook target.
- Slot `+24` points to shared `libart.so` `ExecuteNterpImpl`.
- `x0` is the best plausible ArtMethod register candidate at `ExecuteNterpImpl`.

## Invalidated assumptions

- Slot `+16` should not be used as a native Interceptor target.
- `x0` is not proven equal to JNI `jmethodID`.
- v1.0.49b is not a successful deferred-classification pass.
- Static endpoint or MethodChannel names do not prove runtime command transport.

## Safety status

Documentation stage safety result:

```text
PASS
```

Checks performed:

- `git diff --check`: PASS.
- UTF-8 strict decode: PASS.
- Markdown links: PASS.
- New docs trailing whitespace: PASS.
- Secret scan over discovery docs: PASS.
- No binary evidence ZIP added to git: PASS.

Explicitly unchanged:

- production API runtime;
- Telegram runtime;
- database;
- migrations;
- deployment;
- Android package;
- Frida agents;
- PowerShell launchers;
- Python hosts;
- JavaScript observers;
- tests;
- live evidence directory.

## Recommended commit message

```text
GREE-ALICE Document API discovery research history
```
