# Confirmed Findings

Only facts backed by local evidence are listed here. Hypotheses and stopped branches are in [FAILED-AND-INVALID-BRANCHES.md](./FAILED-AND-INVALID-BRANCHES.md).

## Android/package/process model

- The package under Android lab observation is `com.gree.greeplus`.
- The lab phone is documented as Samsung Galaxy A71 / `SM-A715F` / Android 13 / serial `RZ8N92EZGHD` in the provided task context and repeated in multiple local summaries.
- Several successful runtime stages attach to foreground package PIDs, for example v1.0.44c PID 29537 and v1.0.48 PID from the selected foreground process.
- Multiple package PIDs can exist during bypass/preparation because the protection flow replaces or forks process state; foreground PID selection is therefore part of the lab procedure, not an optional detail.

Sources: `static-apk-inventory\run-20260712-103917\06-reports\summary.txt`, `post-bypass-direct-jni-methodchannel-gate\run-20260715-144607\sanitized-report\summary.txt`, `executenterp-register-filter-feasibility\run-20260715-164229\sanitized-report\summary.txt`.

## Bypass/preparation behavior

- GREE+ could not be treated as an ordinary manually launched app for later dynamic observation. The stable path required a launcher/preparation phase that handled termination behavior and selected a usable foreground process.
- Direct SVC bypass and dual termination route evidence show the protection line evolved through failed, partial, and stable runs. Stable preparation is the reason later post-bypass stages can attach safely.
- The preparation session must be cleaned up so the main observer can attach to the foreground PID without stale helper hooks or launcher ownership.
- The known v4.8.1 problem is missing `CLEANUP` marker after a 180-second preparation window even when attach success, patch-set-ready, heartbeat, and a live GREE+ PID were observed. This remains a launcher/preparation issue, not a successful observer result.

Sources: `root-detection\frida-direct-svc-bypass-20260711-151544\frida-direct-svc-bypass-report.txt`, `root-detection\frida-direct-svc-stability-20260711-152053\frida-direct-svc-stability-report.txt`, `intercept-1b-cold-launch\run-20260712-131232\v48.stdout.log`, task context for v4.8.1.

## Flutter architecture

- Static APK inventory confirmed package version `1.25.3.7`, 2 APKs, 2599 unpacked files, 13 native libraries, and a static Gree endpoint inventory.
- Flutter/plugin extraction confirmed `libapp.so` SHA256 `43071232B93304D2F2249DE1CB2EC72D9BDD5F42451EBF1D17CDCA9E70E7A720`, 68 plugin artifacts, 4699 combined contract records, 40 `sendDataToDevice` plugin hits, and 7 MQTT plugin hits.
- Flutter AOT channel proof confirmed candidate channel string `com.greeplus.flutter`, but did not prove direct initializer-to-link binding.

Sources: `static-apk-inventory\run-20260712-103917\06-reports\summary.txt` SHA256 `9BCAB6D27692A3B2F6900DCF1F739ECA89BFB021C61883AC2FCD2866E54FB305`; `flutter-plugin-contract\run-20260712-111123\06-reports\summary.txt` SHA256 `3025DEEB216309E1CC6F0FE63C7525F46C26C0A7F9EF03DDB45392246233E09A`; `flutter-aot\run-20260714-122159\channel-proof-v1.0.10\report\channel-proof-summary-v1.0.10.txt` SHA256 `52341CF287AFA0F8F64E1CBCB49E57573BCFDC4A6BBE8AE6CD96E726E5BEF0DD`.

## MethodChannel entrypoints

- v1.0.44c confirmed four application/runtime classes and eight exact JNI method IDs.
- Confirmed classes: `t7.l`, `l7.a`, `com.gree.adapter.GreeFlutterActivity`, `io.flutter.embedding.engine.FlutterJNI`.
- Confirmed method count: 8/8.
- This confirms entrypoint availability, not business semantics and not runtime target hits.

Source: `post-bypass-direct-jni-methodchannel-gate\run-20260715-144607\sanitized-report\summary.txt` SHA256 `DF9D8FCC39E304920F5AC410EF661640EC5B494EC90AB0ACD290810C929321FB`.

## JNI lookup

- Direct JNI lookup worked after bypass without Java method hooks, without native Interceptor hooks, without raw ByteBuffer reads, and without payload exports.
- Application and application ClassLoader were present.
- `Java.vm.perform` was used for direct JNI gate entry, while `Java.perform`, `ClassFactory`, Java method hooks, and native Interceptor hooks were not used in v1.0.44c.

Source: v1.0.44c sanitized `result.json` and `summary.txt`.

## ART method layout observations

- v1.0.45 showed all eight target methods initially appeared to resolve to one shared executable ART candidate.
- v1.0.46 separated the slots: slot `+16` had eight stable unique values, while slot `+24` had one stable shared executable value.
- v1.0.47a reclassified slot `+16` as CodeItem-like data and slot `+24` as shared `ExecuteNterpImpl`.

Sources: v1.0.45 summary SHA256 `EA939D9415B45CC957F0FE70328953827D5A62E22A4684C894EE1AD20A356A1A`; v1.0.46 summary SHA256 `D90EAD2FE862CF775F13690AC0DD53287B63CFF5D375EBFD3956E0CE87E410F7`; v1.0.47a summary SHA256 `0FBDC4E64589D586AC1B2D3C14242CFD3BFC87317C04D74B020F5C5FAB8F8C44`.

## DEX CodeItem observation

- Slot `+16` is not a native ARM64 entrypoint in this lab evidence.
- It is CodeItem-like and structurally plausible for all eight methods, with fields such as register/input/output sizes, debug info offset, and instruction size.
- Direct native Interceptor placement at slot `+16` is therefore invalid for this target.

Source: `slot16-code-kind-diagnostic\run-20260715-162142\sanitized-report\summary.txt`.

## ExecuteNterpImpl observation

- Slot `+24` for all eight methods points to a shared executable range in `libart.so`, symbolically interpreted as `ExecuteNterpImpl`.
- v1.0.48 placed one exact native Interceptor on the shared stub address and captured seven stub hits.
- Listener detach completed cleanly in v1.0.48.

Source: `executenterp-register-filter-feasibility\run-20260715-164229\sanitized-report\summary.txt` SHA256 `18839BD14753C8054EA74A47B06C327DDF36F97A5131EED2B281C8E6695E7427`.

## x0 ArtMethod candidate

- v1.0.48 found `x0` as the best plausible ArtMethod register candidate: 7/7 stub hits plausible for `x0`, zero for other registers.
- Exact `jmethodID` pointer matches were zero.
- Exact CodeItem pointer matches were zero.
- Therefore `x0` is ArtMethod-like but not directly equal to the JNI method ID values collected earlier.

Source: v1.0.48 summary.

## Safe metadata capture capabilities

- Confirmed safe exports include counts, hook status, class/method identifiers, signatures, module names, offsets, hashes, durations, boolean safety flags, and sanitized decision fields.
- Confirmed forbidden exports stayed absent in checked reports: raw ByteBuffer, argument object fields, payload values, arbitrary register values, network hooks, and HVAC commands.

Sources: leak checks for v1.0.44c through v1.0.49b and MethodChannel inventory reports.

## Things not yet confirmed

- Runtime target hit correlation for the eight exact methods is not confirmed.
- v1.0.49b does not confirm deferred candidate classification; it terminates before hook-ready.
- No exact command transport envelope is confirmed.
- No production bridge behavior is confirmed or changed by this documentation stage.
- No HVAC command execution was performed or approved.
