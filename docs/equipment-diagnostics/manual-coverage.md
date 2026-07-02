# Diagnostic manual coverage

## Purpose

The manual coverage registry records which local Gree source files are available, which have been reviewed, which diagnostic scope has been imported, and which sources still need identification or analysis.

Machine-readable source:

`data/equipment-diagnostics/manual-library/manuals.json`

The registry is metadata only. It is not loaded by Telegram diagnostics and it does not add diagnostic codes. Binary manuals remain local under the ignored path:

`artifacts/manual-intake/sources/gree`

ED-24SRC.1 applies a strict presentation boundary: source names, document identifiers, package identifiers, source
references, and import evidence remain metadata and are not emitted as Telegram diagnostic guidance. The stage audits
all 1296 Gree runtime cards, removes the known generic import template from 235 GMV6 cards, and completes a
manual-section repair for GMV6 `AJ` and `b1`.

ED-24SRC.10c applies the same presentation boundary across all 263 GMV6 runtime cards: non-grouped visible titles use
the GMV6 series name, internal table-only `sourceNote` wording is replaced with a neutral handoff, and formatter tests
confirm that `sourceNote` remains metadata for Consumer, Installer, and Engineer responses. Source mappings and binary
manuals are unchanged.

ED-24SRC.11a promotes the first 18 GMV6 indoor table-only rows to safe visible cards using only their mapped table
meanings. Possible causes remain empty, no troubleshooting flow is inferred, and the `db` debugging status is not
presented as a fault.

ED-24SRC.11b-1/11b-2 complete the remaining 23 indoor table-only rows. ED-24SRC.12 verifies all 60 GMV6 indoor cards
and marks the indoor category CLOSED with no remaining detailed or table-only rows.

ED-24SRC.13a/13b/13c repair all 43 remaining status rows as modes, settings, prompts, and operating conditions.
Together with the existing AJ prompt guard, all 44 GMV6 status cards are CLOSED / manual-bound PASS.

ED-24SRC.2 repairs the first detailed GMV6 batch from the same service-manual boundary: outdoor sensor/procedure cards
`b1`, `b2`, `b3`, `b4`, `b5`, `b6`, `b7`, `b8`, `b9`, and `bA` now carry the documented 30-second sensor-detection
condition, three possible causes, and connector -> sensor -> detection-circuit/main-board troubleshooting flow. Runtime
counts do not change, remaining unrepaired cards stay `NeedsManualReview` in the generated repair audit, and no
PDF/manual binary is committed.

ED-24SRC.2a refines the already repaired `AJ` and `b1`-`bA` visible wording from the same manual boundary. `AJ` now
mentions the configurable filter-clean interval and keeps the prompt/reset/next-service-cycle meaning without visible
section/source wording. `b1`-`bA` now include the manual fault-display locations and AD-value detection wording for
technical audiences. The AJ package-level metadata remains runtime-compatible until a separate status package/routing
split can safely model wired-controller/indoor-receiver applicability.

ED-24SRC.4 repairs the next controlled GMV6 outdoor detailed batch from sections 2.64-2.69: `F5`, `F6`, `F7`, `F8`,
`F9`, and `FA` now describe discharge temperature sensor faults for compressors 1-6 with the manual fault display,
AD-value / 30-second diagnosis, three possible causes, and connector -> sensor -> detection-circuit/main-board
flowchart. `FH` and the later current-sensor group remain intentionally skipped for later stages.

ED-24SRC.5 repairs the next controlled GMV6 outdoor fan-drive batch from sections 2.78-2.90: aggregate codes `H0`-`H2`
now instruct technical users to read the outdoor 2-digit LED before following the specific procedure, while `H3`, `H5`,
`H6`, `H7`, `H8`, `H9`, `HC`, `HH`, `HJ`, and `HL` now carry their documented causes and flowchart actions. Runtime
counts remain unchanged and GMV6 remains open for the remaining detailed, table-only, status, and debugging inventory.

ED-24SRC.8d closes GMV6 outdoor manual-bound diagnostics: all 121 outdoor cards are classified as repaired with zero
remaining outdoor `DetailedProcedureAvailable` or `TableOnlySafe` rows. ED-24SRC.9 refreshes the remaining GMV6 closure
inventory: indoor has 26 `DetailedProcedureAvailable` and 34 `TableOnlySafe` rows; status has 43 `StatusOrPrompt` rows;
debugging has 38 `DebuggingOrCommissioning` rows. Conflict and NeedsManualReview / Unclassified counts remain zero.

ED-24SRC.10a repairs the first GMV6 indoor detailed batch from sections 2.43, 2.44, 2.45, 2.47, 2.48, 2.50, 2.51,
2.52, 2.53, and 2.54: `d1`, `d3`, `d4`, `d6`, `d7`, `d9`, `dA`, `dC`, `dH`, and `dL`. Runtime counts remain unchanged.
Remaining indoor `DetailedProcedureAvailable` rows are 16 after this batch.

ED-24SRC.10b repairs the remaining usable GMV6 indoor detailed rows: `L0`, `L1`, `L3`, `L4`, `L5`, `L7`, `L9`, `LA`,
and `LC`. Reserved/not-applied or status-only rows `d5`, `d8`, `db`, `dE`, `L2`, `L6`, and `LH` are no longer treated as
detailed procedure rows. GMV6 indoor `DetailedProcedureAvailable` is now zero; indoor table-only remains open with 41
rows.

Current snapshot:

- Source files tracked: 52.
- Imported manuals: 5.
- Partially imported manuals: 2.
- New, not imported: 28.
- Needs identity, duplicate, or import-design review: 16.
- Analyzed manuals: 1.
- Repo-backed diagnostic packages: 21.
- Repo-backed diagnostic entries: 1296.
- Runtime series counts: GMV6 HR 262; GMV6 263; GMV Mini 136; GMV X 263; GMV9 Flex 260; U-Match R32 107;
  ERV B Series 5.

Only the GMV6 service manual diagnostic scope has been production smoke verified. ED-24H.2 partially imports
`SERVICE_MANUAL_GMV_MINI.pdf` as repository knowledge, but production deployment and smoke verification are still pending.
Telegram manual-library delivery foundations exist for eligible reviewed manuals, but this document does not commit manual
binaries or runtime Telegram bindings.

## Coverage status model

`importStatus` describes the source workflow:

- `New`
- `Analyzed`
- `PartiallyImported`
- `Imported`
- `Skipped`
- `NeedsReview`
- `Superseded`

`coverageStatus` describes diagnostic coverage:

- `NotAnalyzed`
- `DiagnosticSectionsUnknown`
- `DiagnosticSectionsIdentified`
- `DiagnosticScopeImported`
- `PartialDiagnosticScopeImported`
- `NoDiagnosticScope`
- `NeedsManualReview`

`New` or `NeedsReview` never means that a code can be used in production. A manual becomes a diagnostic source only through a reviewed, manual-bound import.

## A. VRF / GMV coverage

| Manual ID | Local filename | Status | Coverage | Notes |
|---|---|---|---|---|
| `gree-gmv6-service-manual-2020-09` | `Service Manual for GMV6 v_2020.09.pdf` | Imported | DiagnosticScopeImported | 253 entries; 4 packages; deployed and smoke verified |
| `gree-gmv-idu-service-manual` | `SERVICE_MANUAL_GMV_IDU.pdf` | PartiallyImported | PartialDiagnosticScopeImported | 38 existing GMV6 indoor entries received `sourceReferences[]`; 19 detailed procedure codes reviewed; 0 new entries |
| `gree-gmv-mini-service-manual` | `SERVICE_MANUAL_GMV_MINI.pdf` | PartiallyImported | PartialDiagnosticScopeImported | ED-24H.2 selected source only; 9 new entries, 31 `sourceReferences[]`, 90 NeedsReview contexts |
| `gree-gmv-mini-service-manual-copy-1` | `SERVICE_MANUAL_GMV_MINI (1).pdf` | NeedsReview | NeedsManualReview | Duplicate/revision candidate; not used by ED-24H.2 |
| `gree-gmv-x-owner-manual` | `Owner's Manual GMV X DC Inverter VRF Units.pdf` | New | DiagnosticSectionsUnknown | Owner-level source; usefulness not yet established |
| `gree-gmv-x-technical-sales-guide` | `Technical Sales Guide GMV X DC Inverter VRF Units.pdf` | New | DiagnosticSectionsUnknown | Do not treat as troubleshooting authority without analysis |
| `gree-gmv6-owner-manual` | `Owners-Manual-for GMV6.pdf` | New | DiagnosticSectionsUnknown | Separate source; do not merge with service-manual meanings |
| `gree-gmv-c-series-owner-manual` | `DC Inverter Multi VRF System C sesies.pdf` | New | DiagnosticSectionsUnknown | Filename spelling preserved |
| `gree-gmv-f-series-owner-manual` | `DC Inverter Multi VRF System F sesies.pdf` | New | DiagnosticSectionsUnknown | Filename spelling preserved |
| `gree-vrf-service-manual-russian-filename` | `Сервис мануал GREE VRF.pdf` | NeedsReview | NeedsManualReview | Generic filename; identify exact series and document code |

Imported GMV6 package IDs:

- `gree-gmv6-indoor-fault-codes` — 60 entries.
- `gree-gmv6-outdoor-fault-protection-codes` — 120 entries.
- `gree-gmv6-debugging-codes` — 37 entries.
- `gree-gmv6-status-codes` — 36 entries.

Imported GMV Mini package IDs:

- `gree-gmv-mini-vrf-indoor-controller-codes` - 2 entries.
- `gree-gmv-mini-vrf-outdoor-protection-codes` - 1 entry.
- `gree-gmv-mini-vrf-status-codes` - 6 entries.

Known production smoke codes: `Gree C0`, `Gree U0`, `Gree U3`, `Gree H5`, `Gree E1`, `Gree A0`.

ED-24F.1 verified the IDU manual as `Service Manual - Multi Variable Air Conditioners Indoor Units`, document code `GC202004-X`. Its code table is on manual page 173 / PDF page 178, with troubleshooting through manual page 185 / PDF page 190. No entries were imported because every one of its 38 codes overlaps the existing GMV6 indoor package and the previous flow could not safely represent a second manual source for the same indoor code.

ED-24F.1a added optional `sourceReferences[]` support to the error-knowledge model. ED-24F.1b used that model to merge `GC202004-X` as an additional reviewed source on 38 existing GMV6 indoor entries. The 19 detailed procedure codes were reviewed, but localized Installer/Engineer procedure text was not changed because existing qualified-service guidance already keeps procedure execution bounded by the manual and several procedures include component replacement or electrical service actions. Counts remain 4 packages / 253 entries. See [gree-gmv-idu-manual-import.md](gree-gmv-idu-manual-import.md).

ED-24F.1d is a GMV6 message-quality stage, not a manual coverage expansion. It improves selected existing GMV6
diagnostic texts and Telegram formatting while preserving the same 4 packages / 253 entries and the ED-24F.1b
`sourceReferences[]` merge.

ED-24G.0 adds the Telegram manual-library foundation. Eligible technical roles can request manuals after a diagnostic;
Consumer users are denied. If a reviewed manual is known but no server-local Telegram file binding exists, Telegram says
the file is not connected yet. Real bindings live outside git under the runtime binding path and use Telegram `file_id`
handles. ED-24G.0 did not change diagnostic counts.

ED-24H.1 is a planning-only selection stage. It selected `SERVICE_MANUAL_GMV_MINI.pdf` as the next local service-manual candidate at that time. It did not change manual registry statuses, import diagnostic entries, or change Telegram/manual delivery behavior. See [gree-vrf-next-manual-selection.md](gree-vrf-next-manual-selection.md).

ED-24H.2 partially imports `SERVICE_MANUAL_GMV_MINI.pdf` without using `SERVICE_MANUAL_GMV_MINI (1).pdf`. It adds 3 GMV Mini packages, 9 new entries, and 31 source-reference merges on existing GMV6 entries. The 90 remaining context variants stay NeedsReview. Counts are now 7 packages / 262 entries. See [gree-gmv-mini-manual-import.md](gree-gmv-mini-manual-import.md).

## B. Controllers / wired remotes / commissioning tools

| Manual ID | Local filename | Status | Coverage | Notes |
|---|---|---|---|---|
| `gree-ce41-controller-manual` | `CE41-24F(C).pdf` | NeedsReview | NeedsManualReview | Possible filename/cover mismatch |
| `gree-ce42-controller-manual-2020-10-29` | `CE42-24_F(C)  v2020.10.29.pdf` | New | NotAnalyzed | Commissioning-tool/controller scope |
| `gree-ce52-controller-manual` | `CE52-24F(C).pdf` | New | NotAnalyzed | Central-controller scope |
| `gree-ce41-portable-commissioning-tool` | `Manual Portable Commissioning Tool CE41-24F(C).pdf` | New | NotAnalyzed | Tool operation is not automatically fault meaning |
| `gree-fcu-wired-controller-manual` | `Wired Controller for FCUs.pdf` | New | NotAnalyzed | Exact controller models unknown |
| `gree-screw-chiller-controller-russian` | `Пульт от винтовых.pdf` | NeedsReview | NeedsManualReview | Exact identity unknown |
| `gree-fan-coil-controllers-spreadsheet` | `Пульты на фанкойлы.xlsx` | NeedsReview | NeedsManualReview | Spreadsheet, not assumed to be a manual |

## C. Chillers

| Manual ID | Local filename | Status | Coverage |
|---|---|---|---|
| `gree-a-series-chiller-tsm` | `A-Series-Chiller-TSM_k.pdf` | New | NotAnalyzed |
| `gree-e-series-modular-scroll-chiller` | `E Series Modular Type Scroll Chillers.pdf` | New | NotAnalyzed |
| `gree-air-cooled-screw-chiller-heat-recovery-jf00303869` | `JF00303869_Service_Manual_of_Air_cooled_Screw_Chillerwith_Heat_Recovery.pdf` | New | NotAnalyzed |
| `gree-air-cooled-screw-chiller-service-manual-jf00304179` | `JF00304179 Service Manual of Air-cooled Screw Chiller.pdf` | New | NotAnalyzed |
| `gree-screw-chiller-service-manual-russian-filename` | `Сервис мануал чиллер GREE винтовой.pdf` | NeedsReview | NeedsManualReview |
| `gree-d-series-chiller-characteristics-spreadsheet` | `Характеристики чиллеров серии D.xls` | NeedsReview | NeedsManualReview |

## D. FCU / fan coils

| Manual ID | Local filename | Status | Coverage |
|---|---|---|---|
| `gree-fcu-concealed-ceiling-jf00303582` | `（JF00303582）Concealed ceiling type FCU.pdf.pdf` | New | NotAnalyzed |
| `gree-fan-coil-service-manual-labonallo` | `gree-labonallo-fan-coil-service-manual.pdf` | NeedsReview | NeedsManualReview |

The FCU wired controller and controller spreadsheet are listed in section B because their primary filename scope is controller metadata.

## E. U-Match / Split

| Manual ID | Local filename | Status | Coverage |
|---|---|---|---|
| `gree-u-match-inverter-duct-type` | `DC Inverter U-match Series Duct Type Unit.pdf` | New | NotAnalyzed |
| `gree-u-match-gu50-160k-on-off-service-manual` | `GU50-160K-Btu_h-U-Match-On_Off-Service-manual.pdf` | New | NotAnalyzed |
| `gree-u-match-series-air-conditioners` | `U-MATCH SERIES AIR CONDITIONERS.pdf` | New | NotAnalyzed |
| `gree-split-duct-inverter-series-2` | `Duct type split Air conditioner Inverter Series2.pdf` | New | NotAnalyzed |
| `gree-split-duct-inverter-series` | `The_Ducted_Split_Type_Air_Conditioning_UnitsInverter_Series.pdf` | New | NotAnalyzed |
| `gree-multi-split-service-manual` | `Multi Split Service Manual.pdf` | New | NotAnalyzed |

## F. Versati / heat pumps

| Manual ID | Local filename | Status | Coverage |
|---|---|---|---|
| `gree-versati-air-to-water-split` | `Air-to-water Heat Pump Split Versati.pdf` | New | NotAnalyzed |
| `gree-versati-iii-all-in-one-heat-pump` | `VERSATI III All-in-one Type Air To Water Heat Pump.pdf` | New | NotAnalyzed |
| `gree-versati-iii-split-heat-pump` | `VERSATI III Split Type Air To Water Heat Pump.pdf` | New | NotAnalyzed |

## G. ERV

| Manual ID | Local filename | Status | Coverage |
|---|---|---|---|
| `gree-energy-recovery-ventilation-system` | `Energy-recovery Ventilation System.pdf` | New | NotAnalyzed |
| `gree-erv-installation-startup-maintenance` | `Installation, Startup and Maintenance Manual ERV.pdf` | New | NotAnalyzed |
| `gree-erv-service-manual` | `Service Manual ERV.pdf` | New | NotAnalyzed |

## H. Non-diagnostic or identity-review references

| Manual ID | Local filename | Status | Reason |
|---|---|---|---|
| `gree-design-selection-reference` | `Design & Selection.pdf` | New | Design reference; diagnostic scope unknown |
| `gree-engineering-data-reference` | `Engineering Data.pdf` | New | Engineering reference; diagnostic scope unknown |
| `gree-unit-control-reference` | `Unit Control.pdf` | NeedsReview | Equipment applicability unknown |
| `gree-unit-installation-reference` | `Unit Installation.pdf` | NeedsReview | Likely installation-only until reviewed |
| `gree-gva55al-m3nnc7a-220` | `GVA55AL-M3NNC7A_220.pdf` | NeedsReview | Model-like filename without equipment identity |
| `gree-hwr36-60na-b-m-35982-r410` | `HWR(36...60)NA_B-M 35982   R410.pdf` | NeedsReview | Exact equipment identity unknown |
| `gree-lsqwrf249mnad-m-doc` | `LSQWRF249MNaD-M.doc` | NeedsReview | Possible format duplicate |
| `gree-lsqwrf249mnad-m-pdf` | `LSQWRF249MNaD-M.pdf` | NeedsReview | Possible format duplicate |
| `gree-wk-011pm-owner-manual` | `OM_WK-011PM_600005062067.pdf` | NeedsReview | Exact controller/equipment relationship unknown |
| `gree-test-operation-troubleshooting-maintenance` | `Test Operation & Troubleshooting & Maintenance.pdf` | NeedsReview | Potentially diagnostic, but equipment identity absent |

## Manual-bound import rule

Every import must preserve:

`One manual = one source`

For each future import:

1. Confirm the manual identity, document code/version, series, models, and equipment type.
2. Identify exact diagnostic/error/status/debug sections.
3. Define the package boundary from that manual only.
4. Store source references for every imported entry. When a later manual confirms the same code, equipment type, and meaning, append a `sourceReferences[]` item instead of duplicating the diagnostic answer.
5. Do not use internet sources, model memory, another manual, or another Gree series to fill gaps.
6. Validate package and entry counts.
7. Update `manuals.json`, this document, the import report, tests, and `PROJECT_STATE.md`.
8. Mark production status only after deployment and smoke evidence.

## Future Telegram manual library policy

The future manual library is design-only in ED-24F.0.

- Consumer / Client: denied.
- Installer / Монтажник: allowed after a manual is reviewed and made eligible.
- Engineer / Сервис-инженер: allowed.
- Admin: allowed.
- Owner: allowed.

Telegram must not become the source of truth. Repository or database metadata must retain manual identity, access policy, and source references. See [telegram-manual-library-plan.md](telegram-manual-library-plan.md).
