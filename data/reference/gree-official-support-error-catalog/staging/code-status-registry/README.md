# Gree Code Status Registry

Stage: ED-24GEC.12

Generated: 2026-06-27T00:00:00+05:00; updated for ED-24GEC.12

This registry tracks the remaining Gree website/support candidate codes against runtime JSON and indexed service manuals.

## Status summary

| Status | Count |
|---|---:|
| AliasToExistingCode | 1 |
| BlockedNoManualEvidence | 5 |
| BlockedNoiseOrSeriesMismatch | 1 |
| BlockedNoiseOrVisualAmbiguity | 2 |
| ManualReviewNoMiniRuntime | 1 |
| NeedGmvWRuntimeStructure | 5 |
| NeedSeriesDecision | 3 |
| RuntimeAdded | 2 |

## Codes

| Code | Status | Runtime target | Existing runtime | GMV6 | Mini | GMV-W | Next action |
|---|---|---|---|---:|---:|---:|---|
| by | BlockedNoiseOrVisualAmbiguity |  | no | 46 | 29 | 101 | Keep blocked until exact manual table row for code by is found. |
| E5 | NeedGmvWRuntimeStructure | GMV-W / Versati | no | 0 | 0 | 2 | Create GMV-W/Versati runtime structure and package before adding this code. |
| E6 | ManualReviewNoMiniRuntime | GMV Mini | no | 0 | 1 | 12 | Do not add GMV Mini E6 until an exact Mini service-manual row is found. |
| E7 | NeedSeriesDecision |  | no | 0 | 0 | 0 | Review source series manually; do not add to GMV6 by default. |
| E9 | NeedSeriesDecision |  | no | 0 | 0 | 0 | Review source series manually; do not add to GMV6 by default. |
| eA | NeedSeriesDecision |  | no | 0 | 0 | 0 | Review source series manually; do not add to GMV6 by default. |
| Eb | BlockedNoManualEvidence |  | no | 0 | 0 | 0 | Keep in review until a matching service manual source is found. |
| EE | BlockedNoiseOrSeriesMismatch | GMV6 | no | 1 | 0 | 0 | Keep blocked; do not add GMV6 runtime unless exact GMV6 EE table evidence appears. |
| eH | NeedGmvWRuntimeStructure | GMV-W / Versati | no | 0 | 0 | 12 | Create GMV-W/Versati runtime structure and package before adding this code. |
| F2 | NeedGmvWRuntimeStructure | GMV-W / Versati | no | 0 | 0 | 2 | Create GMV-W/Versati runtime structure and package before adding this code. |
| F4 | NeedGmvWRuntimeStructure | GMV-W / Versati | no | 0 | 0 | 2 | Create GMV-W/Versati runtime structure and package before adding this code. |
| FH | RuntimeAdded | GMV6 outdoor FH | yes | 2 | 0 | 0 | Added GMV6 outdoor fh.json with staged diagnostic answer and package count update. |
| Fy | BlockedNoManualEvidence |  | no | 0 | 0 | 0 | Keep in review until a matching service manual source is found. |
| Ho | AliasToExistingCode | GMV6 H0 | alias | 4 | 0 | 0 | Alias handling complete for Ho/HO to existing H0; do not add separate Ho card. |
| JJ | BlockedNoManualEvidence |  | no | 0 | 0 | 0 | Keep in review until a matching service manual source is found. |
| Jn | BlockedNoManualEvidence |  | no | 0 | 0 | 0 | Keep in review until a matching service manual source is found. |
| Jy | BlockedNoManualEvidence |  | no | 0 | 0 | 0 | Keep in review until a matching service manual source is found. |
| Ld | NeedGmvWRuntimeStructure | GMV-W / Versati | no | 0 | 0 | 6 | Create GMV-W/Versati runtime structure and package before adding this code. |
| N2 | RuntimeAdded | GMV6 status n2 | yes | 2 | 4 | 2 | Added GMV6 n2 status entry using GMV6 service manual section 2.114; kept existing GMV Mini n2. |
| No | BlockedNoiseOrVisualAmbiguity |  | no | 49 | 61 | 96 | Do not add as No. Review whether actual code is L7 / no master IDU for Mini or generic GMV. |

## Files

- gree-code-status-registry.csv
- gree-code-status-registry.json
- README.md
