# Archive Report v1.0.49b

## ZIP

Path:

```text
D:\AssistantEngineer-live-evidence\GREE-ALICE-API-DISCOVERY\03-android-lab\executenterp-target-artmethod-correlation\run-20260715-230048\GREE-executenterp-target-artmethod-correlation-v1.0.49b-report.zip
```

Size: 3167 bytes.

SHA256: `C90004A9ABA44DE118F8EDE1121390D36DC3377B4A243BBB4C364957C0748DEC`.

ZIP integrity: opened and extracted successfully with .NET/PowerShell `System.IO.Compression` / `Expand-Archive`.

## Internal files

| File | Size | SHA256 |
| --- | ---: | --- |
| `errors.json` | 2 | `4F53CDA18C2BAA0C0354BB5F9A3ECBE5ED12AB4D8E11BA873C2F11161202B945` |
| `leak-check.txt` | 469 | `5DA3C8A8677EAB454F48F83B955018EEB46C99178681D6C548048FA9296C0491` |
| `result.json` | 2755 | `F854B98CE9A7E0840ED2DE2EDC0B220061F07B113C7AAF4F904CC18899D7DBF2` |
| `statuses.json` | 1431 | `547560D0BEBE774E91AAAC7D8C386A4BAC44962EB3359304B7E73ADC0188CDCF` |
| `summary.txt` | 1506 | `20D071FD0AEF88A77DA4E7029EAC517740D29601E995F8407F0A541E04FC561A` |

## Report identity

- Version: `v1.0.49b`.
- Stage title: `GREE EXECUTENTERP TARGET ARTMETHOD CODEITEM CORRELATION GATE`.
- Run timestamp from status event: `2026-07-15T18:01:26.528Z`.
- Selected PID: 14245.
- Process command line: `com.gree.greeplus`.
- Frida client/server: `17.15.4`.

## Result fields

| Field | Value |
| --- | --- |
| ValidationStatus | `INVALID` |
| Attached | `True` |
| ScriptLoaded | `True` |
| HookReady | `False` |
| GateComplete | `False` |
| HostErrorType | `SessionDetachedBeforeTargetArtMethodGate` |
| SessionDetachedReason | `process-terminated` |
| ListenerDetached | `False` |
| CapturedSecondsActual | `0.0` |
| TotalStubHits | `0` |
| TotalTargetMatches | `0` |
| BaselineTargetMatches | `0` |
| InteractionTargetMatches | `0` |
| CooldownTargetMatches | `0` |
| UniqueArtMethodCandidates | `0` |
| ClassifiedArtMethodCandidates | `0` |
| CandidateLimitDroppedHits | `0` |
| MatchedMethodCount | `0/8` |
| InteractionMatchedMethodCount | `0/8` |
| Decision | `invalid-executenterp-target-codeitem-correlation-gate` |
| RecommendedNextBranch | `fix-first-host-or-agent-error` |

## Safety flags

The report states:

- `X0DereferencedInsideInterceptor: False`.
- `TargetCodeItemValuesExported: False`.
- `ArbitraryRegisterValuesExported: False`.
- `ArgumentObjectFieldsRead: False`.
- `RawByteBufferRead: False`.
- `PayloadValuesExported: False`.
- `NetworkHooksInstalled: False`.
- `HvacCommandsSent: False`.
- Native Interceptor scope, if reached, was intended as one exact `ExecuteNterpImpl` address.

Leak check: PASS.

## Status event analysis

`statuses.json` has two events:

1. `agent_loaded`, elapsed 0 ms.
2. `vm_perform_entered`, elapsed 2504 ms.

The initial `agent_loaded` status records the intended v1.0.49b design:

- `x0_dereferenced_inside_interceptor: false`;
- `deferred_candidate_classification: true`;
- `correlation_mode: artmethod-slot16-codeitem-equality-deferred`;
- `artmethod_codeitem_slot_offset: 16`;
- `max_unique_artmethod_candidates: 4096`.

No later status proves hook installation, hook-ready, candidate capture, detach, or classification.

## Discrepancies

The discrepancy is between intended configuration in `statuses.json` and final outcome in `result.json` / `summary.txt`:

- `statuses.json` says deferred candidate classification was configured at agent load.
- `result.json` and `summary.txt` say `DeferredCandidateClassification: False`, zero candidates, zero classified candidates, and no gate completion.

Interpretation: this is not evidence that deferred classification ran and failed. It is evidence that the process detached before the gate reached the point where classification could run. The final result resets or leaves post-capture fields empty/false because no capture phase completed.

No separate console output was present inside the ZIP. The local sanitized report directory contains matching `summary.txt`, `result.json`, `statuses.json`, `errors.json`, and `leak-check.txt`.

## Final classification

Final v1.0.49b outcome: `INVALID`.

Reason: the report is internally sufficient to show attach and script load, but it does not show hook-ready, gate-complete, listener-detached, capture duration, stub hits, candidate capture, or target matches. The host error is `SessionDetachedBeforeTargetArtMethodGate` with process termination.

It is not `VALID`.

It is not `REPORT-INCONSISTENT` because the only discrepancy is explainable by early detach before final capture/classification state existed.

It is not merely `INCOMPLETE` because the report itself declares `ValidationStatus: INVALID` and a concrete host error type.

## Recommended next branch

Do not build a new observer first. Diagnose the first host/agent/process termination that prevents v1.0.49b from reaching hook-ready. The next safe branch is:

```text
fix-first-host-or-agent-error
```
