# Gree Code Status Registry

Stage: ED-24GEC.10A3

Generated: 2026-06-26T23:00:59.4087818+05:00

This registry tracks the remaining Gree website/support candidate codes against runtime JSON and indexed service manuals.

## Status summary

| Status | Count |
|---|---:|
| AliasToExistingCode | 1 |
| BlockedNoiseOrVisualAmbiguity | 2 |
| BlockedNoManualEvidence | 5 |
| NeedGmvWRuntimeStructure | 5 |
| NeedSeriesDecision | 3 |
| ReadyAddGmv6 | 2 |
| ReadyAddMini | 1 |
| RuntimePartial | 1 |

## Codes

| Code | Status | Runtime target | Existing runtime | GMV6 | Mini | GMV-W | Next action |
|---|---|---|---|---:|---:|---:|---|
| by | BlockedNoiseOrVisualAmbiguity |  | no | 46 | 29 | 101 | Keep blocked until exact manual table row for code by is found. |
| E5 | NeedGmvWRuntimeStructure | GMV-W / Versati | no | 0 | 0 | 2 | Create GMV-W/Versati runtime structure and package before adding this code. |
| E6 | ReadyAddMini | GMV Mini | no | 0 | 1 | 12 | Review top evidence, then add GMV Mini runtime if meaning is exact. |
| E7 | NeedSeriesDecision |  | no | 0 | 0 | 0 | Review source series manually; do not add to GMV6 by default. |
| E9 | NeedSeriesDecision |  | no | 0 | 0 | 0 | Review source series manually; do not add to GMV6 by default. |
| eA | NeedSeriesDecision |  | no | 0 | 0 | 0 | Review source series manually; do not add to GMV6 by default. |
| Eb | BlockedNoManualEvidence |  | no | 0 | 0 | 0 | Keep in review until a matching service manual source is found. |
| EE | ReadyAddGmv6 | GMV6 | no | 1 | 0 | 0 | Review top evidence, then add GMV6 runtime if meaning is exact. |
| eH | NeedGmvWRuntimeStructure | GMV-W / Versati | no | 0 | 0 | 12 | Create GMV-W/Versati runtime structure and package before adding this code. |
| F2 | NeedGmvWRuntimeStructure | GMV-W / Versati | no | 0 | 0 | 2 | Create GMV-W/Versati runtime structure and package before adding this code. |
| F4 | NeedGmvWRuntimeStructure | GMV-W / Versati | no | 0 | 0 | 2 | Create GMV-W/Versati runtime structure and package before adding this code. |
| FH | ReadyAddGmv6 | GMV6 outdoor FH | no | 2 | 0 | 0 | Add GMV6 outdoor fh.json with normal staged diagnostic answer and package count update. |
| Fy | BlockedNoManualEvidence |  | no | 0 | 0 | 0 | Keep in review until a matching service manual source is found. |
| Ho | AliasToExistingCode | GMV6 H0 | no | 4 | 0 | 0 | Do not add separate Ho card. Add/verify visual input handling Ho/HO -> H0. |
| JJ | BlockedNoManualEvidence |  | no | 0 | 0 | 0 | Keep in review until a matching service manual source is found. |
| Jn | BlockedNoManualEvidence |  | no | 0 | 0 | 0 | Keep in review until a matching service manual source is found. |
| Jy | BlockedNoManualEvidence |  | no | 0 | 0 | 0 | Keep in review until a matching service manual source is found. |
| Ld | NeedGmvWRuntimeStructure | GMV-W / Versati | no | 0 | 0 | 6 | Create GMV-W/Versati runtime structure and package before adding this code. |
| N2 | RuntimePartial | GMV6 status n2 | yes | 2 | 4 | 2 | Add GMV6 n2 status entry using GMV6 service manual section 2.114. Keep existing GMV Mini n2. |
| No | BlockedNoiseOrVisualAmbiguity |  | no | 49 | 61 | 96 | Do not add as No. Review whether actual code is L7 / no master IDU for Mini or generic GMV. |

## Files

- gree-code-status-registry.csv
- gree-code-status-registry.json
- README.md

## Rules

- Website/support codes are candidates, not automatic runtime entries.
- A code is added to a series only when a service manual confirms that series and meaning.
- GMV-W/Versati codes are not added to GMV6.
- Visual aliases such as Ho/HO must map to confirmed canonical codes instead of creating duplicate cards.
- No internal catalog/staging/reference-only wording should appear in user-visible Telegram answers.
