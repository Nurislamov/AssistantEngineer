# Gree GMV6 HR manual coverage

## Stage

ED-24E.3 Add Gree GMV6 HR diagnostics.

Decision: imported GMV6 HR as a separate runtime series. Do not merge with plain GMV6, GMV X, GMV9 Flex, or GMV Mini.

## Source PDFs

| Manual | Local source | Size | Pages | SHA256 | Runtime use |
| --- | --- | ---: | ---: | --- | --- |
| Service Manual | `artifacts/manual-intake/sources/gree/Gree GMV6 HR Service Manual EN.pdf` | 22,232,816 bytes | 427 | `CABDC29423A28E846EBC7A9F7DA1EC69002033E8550AFB89D540A3342A49411E` | Primary diagnostic extraction source |
| Owner Manual | `artifacts/manual-intake/sources/gree/Gree GMV6 HR Owner Manual EN.pdf` | 22,595,872 bytes | 76 | `2B516736DF5ED4AB0AF4F7407C53F35031122688CA1662BD6ED42BB9675347C5` | Owner-facing cross-check only |

PDF files remain local artifacts and are not committed.

## Manual Model Coverage

- Service title observed in metadata: `GMV5/GMV5S...`; visible page headers identify `Gree GMV6 HR DC Inverter VRF Units Service Manual`.
- Owner visible title: `GMV Heat Recovery DC Inverter VRF`.
- Document code/revision: not stated in the extracted metadata or visible diagnostic sections.
- Covered family: GMV6 HR heat recovery DC inverter VRF units, including mode exchange / heat recovery context.

## Diagnostic Sections Found

- Service Manual TOC: Chapter 3 `Faults`; `1 Error Indication`; `2 Troubleshooting`; `3 Non-fault Type Troubleshooting`.
- Service Manual diagnostic tables: PDF pages 89-92 for Error Indication.
- Service Manual troubleshooting example used outside the table: section `2.135 "n2"` on PDF page 191.
- Owner Manual: `7 Troubleshooting`, `7.1 Common Malfunction and Troubleshooting`, and `7.2 Error Indication`; Error Indication cross-check on PDF pages 71-75.
- Debugging/status flow tables were reviewed on Service PDF pages 26-33 and Owner PDF pages 58-66.

## Extraction Summary

Normalized runtime extraction:

| Category | Source | Count |
| --- | --- | ---: |
| Indoor | Service Error Indication table | 60 |
| Outdoor | Service Error Indication table | 120 |
| Debugging | Service Error Indication table | 38 |
| Status | Service Error Indication table | 43 |
| Status | Service troubleshooting section `2.135 "n2"` | 1 |
| **Total** |  | **262** |

The working extraction tracked `SourceFile`, `SourceManualType`, `Page`, `Section`, `Table`, `RawCode`, `NormalizedCode`, `RawMeaning`, `DeviceContext`, `CategoryGuess`, `ShouldBecomeDiagnosticCard`, and `Notes`. Raw code case was preserved in runtime `code`; filenames remain lowercase as in the existing catalog convention.

## Comparison

Baseline before ED-24E.3:

- Total Gree runtime: 922.
- GMV Mini: 136.
- GMV6: 263.
- GMV X: 263.
- GMV9 Flex: 260.
- GMV6 HR: 0.

After ED-24E.3:

- GMV6 HR: 262.
- Total Gree runtime: 1184.
- Existing GMV Mini, GMV6, GMV X, and GMV9 Flex counts unchanged.

## Runtime Import Summary

New runtime series path: `data/equipment-diagnostics/error-knowledge/gree/gmv6-hr`.

New packages:

- `gree-gmv6-hr-indoor-fault-codes`: 60.
- `gree-gmv6-hr-outdoor-fault-protection-codes`: 120.
- `gree-gmv6-hr-debugging-codes`: 38.
- `gree-gmv6-hr-status-codes`: 44.

Manual registry entries were added for HR ServiceManual and OwnerManual audit state only. No Telegram file bindings or production DB records were added.

## Key Code Checks

- `Gree GMV6 HR E0`: resolves to GMV6 HR.
- `Gree GMV6 HR U4`: resolves to GMV6 HR debugging.
- `Gree GMV6 HR C2`: resolves to GMV6 HR debugging.
- `Gree GMV6 HR n2`: resolves to GMV6 HR status from troubleshooting section `2.135`.
- `Gree GMV6 HR A9`: resolves to GMV6 HR status.
- `Gree GMV6 E0`: remains a GMV6/HR ambiguity when applicable and does not return an HR-only answer.

## Decision

ED-24E.3 imports GMV6 HR diagnostics as manual-verified runtime JSON/cards and package metadata. ServiceManual remains library-only. Diagnostic guide delivery remains OwnerManual-only; with no OwnerManual binding in production, HR diagnostic guide should return the existing "manual not added yet" response and must not send the ServiceManual.
