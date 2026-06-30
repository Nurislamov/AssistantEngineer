# Gree GMV Mini/Slim Manual Coverage Audit

## Stage

ED-24SRC.2

## Source PDF

- Local path: `artifacts/manual-intake/sources/gree/Gree GMV Mini Slim Side Outlet Service Manual EN Rev S.pdf`
- SHA256: `E42C5BE4BAE5D74ECE380BB7C1D83FAD16639171918B153E7B8ADCA5602DAAF1`
- File size: 51,164,839 bytes
- PDF pages: 176
- Title found on page 1: `DC INVERTER VRF SYSTEM (R410A)`
- Document code: `GC202510-XIX`
- Revision: not separately stated beyond the local source filename suffix `Rev S`
- PDF metadata title: blank

## Manual Model Coverage

The source PDF is a broad Gree DC Inverter Multi VRF System R410A side-discharge Mini/Slim service manual. The title page states 8.0-33.5 kW capacity, 50 Hz / 60 Hz rate frequency, and -5-52 C operating range.

The product list contains 32 product rows and 29 unique model names. Covered model families include:

- `GMV-80WL/A-T`, `GMV-100WL/A-T`, `GMV-121WL/A-T`
- `GMV-80WL/C-T`, `GMV-100WL/C-T`, `GMV-121WL/C-T`, `GMV-141WL/C-T`
- `GMV-120WL/A-T`, `GMV-140WL/A-T`, `GMV-160WL/A-T`
- `GMV-120WL/A-X`, `GMV-140WL/A-X`, `GMV-160WL/A-X`
- `GMV-120WL/C-T`, `GMV-140WL/C-T`, `GMV-160WL/C-T`
- `GMV-120WL/C-X`, `GMV-140WL/C-X`, `GMV-160WL/C-X`, `GMV-180WL/C-X(D)`
- `GMV-H224WL/A-X`, `GMV-H280WL/A-X`, `GMV-H335WL/A-X`
- `GMV-224WL/C-X`, `GMV-280WL/C-X`, `GMV-335WL/C-X`
- `GMV-280WL/C1-X`, `GMV-335WL/C1-X`
- `GMV-280WL/C1-X(S)`, `GMV-335WL/C1-X(S)`

This is a reasonable broad source candidate for the current user-facing `Gree GMV Mini/Slim` family because it covers the Mini/Slim side-discharge outdoor-unit model range used by the existing GMV Mini runtime package. It should not be treated as an exact model-family split for every Mini, Star, Slim, or future variant. A later ED-24MAN.3-style model-family/manual taxonomy can still split exact manual variants without changing this audit result.

Document code `GC202510-XIX` is technical metadata only and should not be added to Telegram-visible diagnostic wording.

## Diagnostic Sections Found

| Section | PDF pages | Table | Entries count | Notes |
| --- | ---: | --- | ---: | --- |
| Debugging of Unit | 75-80 | Stage process instruction for debugging | 27 context occurrences | Contains process/status display values such as `01`-`08`, `db`, `A0`, `U4`, and mode/status values. |
| Malfunction List for the Wired Controller | 81 | Display code table | 28 raw rows | Runtime maps 27 as indoor/controller cards; `db` is stored in the status/debug bucket. |
| Status Display Table for Indicators on Main Board of Outdoor Unit | 82-83 | Display code table | 102 raw rows | Main outdoor board display table; overlaps with wired-controller entries for `C0` and `AJ`. |
| Function Setting of Outdoor Unit | 84-90 | Function setting tables | 20 function codes in the page 87 function list | Some function-setting codes are runtime status cards; others remain manual-only setting values. |
| Troubleshooting | 91-99 | Troubleshooting flow references | 17 referenced code occurrences | Flow diagrams reference existing drive/protection codes plus `PA`; not a full standalone display table. |

## Runtime GMV Mini Coverage

- Runtime folder: `data/equipment-diagnostics/error-knowledge/gree/gmv-mini`
- Runtime cards: 136
- Runtime unique codes: 136
- Runtime buckets:
  - `indoor`: 27
  - `outdoor`: 62
  - `status`: 47
- Runtime signal breakdown:
  - Fault: 41
  - Protection: 29
  - Warning: 19
  - Commissioning: 16
  - Status: 15
  - Debug: 10
  - Communication: 5
  - Maintenance: 1
- Package manifests:
  - `data/equipment-diagnostics/error-knowledge/packages/gree-gmv-mini-vrf-indoor-controller-codes.json`
  - `data/equipment-diagnostics/error-knowledge/packages/gree-gmv-mini-vrf-outdoor-protection-codes.json`
  - `data/equipment-diagnostics/error-knowledge/packages/gree-gmv-mini-vrf-status-codes.json`
- Runtime source references currently point to the existing GMV Mini manual source boundary `DC Inverter VRF System Service Manual (R410A)` with `ManualVerified` confidence metadata.

## Comparison Summary

- Manual-derived context occurrences extracted for audit: 202
- Manual unique normalized codes across diagnostic/status/function/troubleshooting contexts: 159
- Runtime cards: 136
- Runtime unique codes: 136
- Primary display-table misses in runtime: 0
- Extra runtime codes not found in this PDF: 0
- Manual-only context/function/debug values not represented as runtime cards: 23
- Blocking conflicts: 0
- Duplicate or multi-context codes observed: yes, non-blocking
- Confidence: PASS WITH NOTES

The package-compatible source coverage is strong: all current runtime GMV Mini cards were found in this local PDF, and every primary display-table code extracted from the PDF is represented in the runtime or mapped through the existing status/debug split.

The audit found 23 manual-only context/function/debug values that are not current runtime cards:

`00`, `09`, `10`, `12`, `15`, `16`, `17`, `AC`, `n3`, `n5`, `nL`, `nU`, `OC`, `OF`, `PA`, `q7`, `q8`, `q9`, `qd`, `qF`, `qL`, `qn`, `qU`.

These are not blockers for binding the PDF as a broad Mini/Slim manual candidate because they are setting/status/debug context values or troubleshooting-flow references rather than missing primary malfunction display rows. They should be reviewed before any future card import or exact model-family split.

Notable non-blocking duplicate/context findings:

- `C0` appears in both wired-controller and outdoor-board contexts with communication-malfunction wording.
- `AJ` appears in both wired-controller and outdoor-board contexts with filter-cleaning alarm wording.
- `db` appears as an engineering-debugging special code and also in debugging-stage context.
- `n2` appears as both a status display and function-setting context with consistent capacity-ratio meaning.
- `nH`, `nC`, `nA`, and `nF` appear as status/model indicators and also in function-setting contexts.

## Key Code Checks

| Code | In manual | In runtime | Manual meaning/context | Runtime meaning/context | Result |
| --- | --- | --- | --- | --- | --- |
| `n2` | yes | yes | Outdoor status and function-setting capacity-ratio context | Capacity-ratio limit setting | PASS |
| `C0` | yes | yes | Wired-controller and outdoor-board communication malfunction contexts | Communication malfunction | PASS |
| `AJ` | yes | yes | Filter-cleaning alarm contexts | Clean alarming for filter | PASS |
| `db` | yes | yes | Engineering debugging code and debugging-stage context | Engineering debugging code | PASS |
| `L0`-`L9` | yes | yes | Wired-controller malfunction/protection rows | Indoor/controller cards | PASS |
| `d1`, `d3`, `d4`, `d6`-`d9`, `dA`, `dC`, `dE`, `dH`, `dL` | yes | yes | Wired-controller sensor/PCB/address/capacity rows | Indoor/controller cards | PASS |
| `d2`, `d5` | no | no | Not found in this PDF audit scope | Not present | PASS |
| `E0`-`E4` | yes | yes | Outdoor board fault/protection rows | Outdoor fault/protection cards | PASS |
| `E5`-`E9` | no | no | Not found in this PDF audit scope | Not present | PASS |
| `A0` | yes | yes | Unit debugging/status context | Debugging for unit | PASS |
| `A9` | yes | yes | IPLV test status context | IPLV test | PASS |
| `nH`, `nC`, `nA` | yes | yes | Model/status and function-setting contexts | Model/status cards | PASS WITH NOTES |
| `Ed` | yes | yes | Low-temperature drive-module protection | Low-temperature drive-module protection | PASS |

## Decision

PASS WITH NOTES: the local PDF is safe to use as a broad Gree GMV Mini/Slim manual source candidate for the existing GMV Mini runtime coverage and a future Telegram library bind. It is not an exact model-family split, and the manual-only context/function/debug values listed above should remain review notes rather than immediate card imports.

## Next Action

- A future operator step may run `/manual_bind` for `Gree GMV Mini` using `Gree GMV Mini Slim Side Outlet Service Manual EN Rev S.pdf`.
- No `/manual_bind` was performed in ED-24SRC.2.
- No diagnostic JSON/cards/routing/sourceReferences/manual bindings were changed in ED-24SRC.2.
- No PDF binary was added to git in ED-24SRC.2.
- Visible Telegram UX can continue using `Gree GMV Mini`; docs can describe this source as a `Mini/Slim broad side-outlet manual`.
- Keep ED-24MAN.3 as a future exact model-family/manual-variant split if needed.
