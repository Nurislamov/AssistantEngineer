# AssistantEngineer Project State

## Current stage

GMV / Telegram diagnostics stabilization is completed through:

- ED-24TG.1
- ED-24TG.2
- ED-24GEC.7D
- ED-24GEC.8
- ED-24GEC.9

Current production-safe baseline is on `master`.

## Current branch

`master`

Latest confirmed commits:

```text
eee53ccc ED-24GEC.9 Review blocked GMV error cards
8c7ad798 ED-24GEC.7D Update Telegram wording tests for polished GMV answers
f490733c ED-24GEC.8 Add remaining GMV card inventory and guards
64862049 ED-24TG.2 Resume pending service request after phone capture
6c7d7d97 ED-24TG.1 Make Telegram polling resilient to outbound failures
```

Local, origin, and VPS were confirmed synchronized after `eee53ccc`.

## Last completed work

### ED-24TG.1

Telegram polling resilience was fixed.

Before this stage, one failed outbound send from a group update could block the polling offset and make the bot stop responding to new messages.

Now `OutboundFailed` no longer blocks the whole polling batch. The failed update is logged/skipped, the offset moves forward, and the bot continues processing next updates.

### ED-24TG.2

Pending service request after phone capture was fixed.

Before this stage:

```text
Gree H5 -> request master -> bot asks phone -> user sends contact -> bot saved phone but did not create request
```

Now:

```text
Gree H5 -> request master -> bot asks phone -> user sends contact -> phone saved -> service request created immediately
```

Production was deployed and checked.

### ED-24GEC.7D

Old broad Telegram wording tests were updated to match polished GMV visible answers from `ED-24GEC.7C.1/7C.2`.

This was test-only. Runtime JSON and bot behavior were not changed.

### ED-24GEC.8

Remaining Gree GMV official support cards were inventoried.

Result:

```text
Official support cards: 256
Already present in GMV6 runtime: 236
Blocked/manual-review: 20
New runtime entries added: 0
Total runtime knowledge count: 262
GMV6 runtime count: 253
Package counts changed: none
```

Added generator, staging preview and guard tests.

### ED-24GEC.9

All 20 blocked/manual-review GMV cards were manually reviewed and documented.

Reviewed codes:

```text
by, E5, E6, E7, E9, eA, Eb, EE, eH, F2, F4, FH, Fy, Ho, JJ, Jn, Jy, Ld, N2, No
```

Decision:

```text
added-runtime: none
```

Reason: sources are not safe enough for GMV6 runtime promotion. Issues include GMV-W / GMVT / GMV Mini context, table-only evidence, casing conflicts, visual ambiguity, shared code cards, or missing safe GMV6-specific source evidence.

## Current production status

VPS path:

```text
/opt/assistantengineer
```

VPS confirmed on:

```text
eee53ccc ED-24GEC.9 Review blocked GMV error cards
```

No Docker rebuild was needed for `ED-24GEC.8`, `ED-24GEC.9`, or `ED-24GEC.7D` because they did not change runtime bot behavior.

Last runtime-changing/deployed stages:

- `ED-24TG.1`
- `ED-24TG.2`
- `ED-24GEC.7C.1`
- `ED-24GEC.7C.2`

## Important decisions

1. Do not auto-promote the remaining 20 blocked GMV cards into runtime.
2. Do not merge GMV Mini, GMV-W, GMVT, or ambiguous official support cards into GMV6 runtime without stronger source confirmation.
3. Keep visible bot answers free from internal terms:
   - raw
   - review
   - approved
   - runtime
   - staging
   - internal
   - machine translated
4. Consumer answers must not contain unsafe advice:
   - measuring live voltage/current
   - opening electrical cabinets
   - bypassing protections
   - repeated reset to force operation
   - direct board/component replacement
5. Public wording must avoid unsupported parity claims. Prefer:
   - standard-based
   - standard-inspired
   - external reference validation
   - engineering-core validation

## Files changed recently

Important recent files:

```text
src/Backend/AssistantEngineer.Api/Services/EquipmentDiagnostics/EquipmentDiagnosticTelegramPollingBackgroundService.cs
tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticTelegramPollingTests.cs

src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/Conversations/TelegramDiagnosticConversationService.cs
src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/EquipmentDiagnosticTelegramAdapter.cs
tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticTelegramServiceRequestTests.cs

tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticTelegramAdapterTests.cs
tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticTelegramFormatterTests.cs
tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticTelegramManualLibraryTests.cs
tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticTelegramUserAccessTests.cs

tools/gree-support/generate-gree-gmv-remaining-runtime-candidates.ps1
tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvRemainingRuntimeCardsTests.cs

data/reference/gree-official-support-error-catalog/staging/remaining-runtime-candidates/
data/reference/gree-official-support-error-catalog/staging/manual-review-batch-9/
```

## Validation status

Recent targeted validations passed during stages:

```text
EquipmentDiagnosticTelegram: 373 passed
GreeGmvRemainingRuntimeCardsTests: passed
GreeGmvApprovedRuntimeWordingTests: passed
Telegram service request + polling smoke: 36 passed
Review catalog validator: passed
Approved priority catalog validator: passed
Runtime overlay staging validator: passed
```

Known caveat:

One published-assembly smoke test was reported by Codex as hanging when run alone:

```text
PublishedApiAssemblyLoadsEmbeddedGreeH5
```

This was not treated as part of ED-24GEC.9 because runtime JSON/packages were unchanged. Investigate separately only if it blocks CI or full test runs.

## Current blocker

No active production blocker.

Bot and service request flow were restored after ED-24TG.1 and ED-24TG.2.

The remaining 20 Gree GMV official support cards are intentionally blocked from runtime until stronger source evidence is available.

## Next step

Recommended next stage:

```text
ED-24GEC.10 — Decide next source strategy for blocked GMV cards
```

Options:

1. Find stronger official GMV6 manuals/source evidence for blocked codes.
2. Keep the 20 blocked cards as reference-only and move to another diagnostic family.
3. Investigate the published-assembly smoke test hang if full CI becomes noisy.
4. Continue expanding Telegram bot UX/features now that GMV baseline is stable.

Do not start by adding the 20 blocked codes to runtime without new source evidence.
