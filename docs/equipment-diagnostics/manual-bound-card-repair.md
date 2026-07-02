# ED-24SRC manual-bound Gree diagnostic card repair

## Outcome and boundary

This is an honest emergency-scope repair, not a claim that all 1296 cards have been manually reconstructed from their
diagnostic chapters.

- All 1296 Gree runtime cards are included in the generated audit.
- ED-24SRC.10c normalizes every non-grouped GMV6 runtime title to the `Gree GMV6 — <code> — <meaning>` form and
  replaces the internal table-only `sourceNote` wording with a neutral specialist handoff. The guard covers all 263
  GMV6 cards, and formatter tests keep `sourceNote` out of Consumer, Installer, and Engineer output.
- ED-24SRC.11a repairs the first 18 indoor table-only cards (`d2`, `d5`, `d8`, `db`, `dd`, `dE`, `dF`, `dJ`, `dn`,
  `dP`, `dU`, `dy`, `L2`, `L6`, `L8`, `Lb`, `LE`, `LF`). Each card uses its table meaning, has no inferred causes,
  and provides only the common safe handoff; `db` remains a debugging status rather than a fault.
- ED-24SRC.11b-1 repairs the next 12 indoor table-only cards: `LH`, `LJ`, `LL`, `LP`, `LU`, and `o0`-`o6`.
- ED-24SRC.11b-2 repairs the final 11 indoor table-only cards: `o7`, `o8`, `o9`, `oA`, `ob`, `oC`, `y1`, `y2`,
  `y7`, `y8`, and `yA`.
- ED-24SRC.12 verifies all 60 indoor cards with no remaining detailed/table-only work. GMV6 indoor is CLOSED /
  manual-bound PASS.
- ED-24SRC.13a repairs the first 15 status rows as modes, settings, and operating conditions rather than faults.
- ED-24SRC.13b repairs the next 14 status rows (`An`, `AP`, `AU`, `Ay`, and `n0`-`n9`) as status-only guidance.
- ED-24SRC.1 found the known generic import template in 235 GMV6 cards. Their Telegram-visible summaries, checks, and
  recommended actions were reduced to neutral table-only-safe wording without adding causes or component repairs.
- GMV6 `AJ` was checked against section 2.12 and repaired as a filter-clean prompt with filter cleaning, prompt reset,
  and the next service cycle.
- ED-24SRC.2 repaired the first detailed GMV6 outdoor sensor/procedure batch: `b1`, `b2`, `b3`, `b4`, `b5`, `b6`,
  `b7`, `b8`, `b9`, and `bA`.
- GMV6 `b1`-`bA` were checked against sections 2.17-2.26 and their rendered flowcharts. The cards now include the
  30-second detection condition, all three possible causes, and the ordered connector, sensor, detection-circuit, and
  main-board actions with the correct sensor name for each code.
- ED-24SRC.2a corrected the repaired AJ and `b1`-`bA` visible wording without changing counts: `AJ` now includes the
  configurable filter-clean interval and neutral source note; `b1`-`bA` now include the manual fault-display locations
  and AD-value detection wording for Installer/Engineer audiences.
- ED-24SRC.4 repaired the next outdoor detailed-procedure batch: `F5`, `F6`, `F7`, `F8`, `F9`, and `FA`, covering
  sections 2.64-2.69 for discharge temperature sensor faults of compressors 1-6. The group is smaller than the
  preferred 8-20 cards because the adjacent same-structure manual block ends at `FA`; `FH` and the current-sensor
  `FC`/`FL`/`FE`/`FF`/`FJ` group were skipped for a later stage.
- ED-24SRC.5 repaired the next outdoor fan-drive detailed batch: `H0`, `H1`, `H2`, `H3`, `H5`, `H6`, `H7`, `H8`,
  `H9`, `HC`, `HH`, `HJ`, and `HL`, covering sections 2.78-2.90. The batch keeps the manual distinction between
  aggregate wired-controller codes (`H0`-`H2`) that require reading the outdoor 2-digit LED and the specific outdoor
  LED faults that carry direct flowchart actions.
- ED-24SRC.6 reconciled the closure inventory after ED-24SRC.2a/4/5. It regenerated the ignored GMV6 inventory artifacts
  and confirmed the current counts below with no conflicts, no unclassified rows, and no `NeedsManualReview` rows.
- ED-24SRC.7a repaired the first split remaining outdoor detailed batch: `FH`, `FC`, `FL`, `FE`, `FF`, `FJ`, `FU`, and
  `Fb`, covering sections 2.70-2.77. The batch stays within one adjacent compressor-sensor block: current-sensor faults
  for compressors 1-6 plus the immediately adjacent shell-roof temperature sensor faults for compressors 1-2.
- ED-24SRC.7b repaired the remaining E/F outdoor detailed batch: `E1`, `E2`, `E3`, `E4`, `F0`, `F1`, and `F3`,
  covering sections 2.57-2.63. The batch stays within the adjacent outdoor protection, main-board, and pressure-sensor
  block and does not mix in later J/P compressor-drive groups.
- ED-24SRC.7c repaired the remaining J outdoor detailed batch: `J0`, `J1`, `J2`, `J3`, `J4`, `J5`, `J6`, `J7`, `J8`,
  and `J9`, covering sections 2.91-2.100. The batch stays within the contiguous J group and leaves the P compressor-drive
  group for ED-24SRC.7d.
- ED-24SRC.7d repaired the remaining P outdoor compressor-drive detailed batch: `P0`, `P1`, `P2`, `P3`, `P5`, `P6`,
  `P7`, `P8`, `P9`, `PC`, `PH`, `PJ`, and `PL`, covering sections 2.125-2.137. This closes the outdoor
  `DetailedProcedureAvailable` class; remaining detailed cards are indoor only.
- ED-24SRC.8a repaired the first outdoor table-only batch: `bb`, `bd`, `bE`, `bF`, `bH`, `bJ`, `bn`, `bP`, `bU`,
  `E0`, `Ed`, `Fd`, `Fn`, and `FP`. These rows have table meanings only in the current source boundary, so their
  visible text contains no invented causes or flowchart actions.
- ED-24SRC.8b repaired the outdoor G-family table-only batch: `G0`, `G1`, `G2`, `G3`, `G4`, `G5`, `G6`, `G7`, `G8`,
  `G9`, `GA`, `Gb`, `GC`, `Gd`, `GE`, `GF`, `GH`, `GJ`, `GL`, `Gn`, `GP`, `GU`, and `Gy`. These rows remain
  table-meaning-only and contain no invented causes or flowchart actions.
- ED-24SRC.8c repaired the final outdoor H/J/P table-only batch: `H4`, `HA`, `HE`, `HF`, `HP`, `HU`, `JA`, `JC`,
  `JE`, `JF`, `JL`, `P4`, `PA`, `PE`, `PF`, `PP`, and `PU`. This brings GMV6 outdoor to zero remaining
  `DetailedProcedureAvailable` and zero remaining `TableOnlySafe` rows.
- ED-24SRC.8d verified GMV6 outdoor closure: all 121 GMV6 outdoor cards are classified as repaired in the closure
  inventory, with zero outdoor `DetailedProcedureAvailable`, zero outdoor `TableOnlySafe`, zero conflicts, and zero
  `NeedsManualReview` / `Unclassified` rows. GMV6 outdoor is closed; whole-GMV6 closure remains pending because indoor,
  status, and debugging classes are outside this outdoor stage.
- ED-24SRC.9 refreshed the GMV6 closure inventory after outdoor closure and recorded the exact remaining
  indoor/status/debugging work without changing runtime cards. GMV6 outdoor remains closed; GMV6 as a whole remains open
  until indoor detailed, indoor table-only, status, and debugging stages are repaired and verified.
- ED-24SRC.10a repaired the first GMV6 indoor detailed batch: `d1`, `d3`, `d4`, `d6`, `d7`, `d9`, `dA`, `dC`, `dH`,
  and `dL`. These rows now carry their indoor fault display, diagnosis, possible causes, and troubleshooting actions
  from sections 2.43, 2.44, 2.45, 2.47, 2.48, 2.50, 2.51, 2.52, 2.53, and 2.54. Adjacent rows `d5`, `d8`, and `dE`
  were skipped because they are reserved/no-procedure headings in the current extraction; `db` was skipped because the
  section identifies it as a commissioning/status code, not a fault.
- ED-24SRC.10b repaired the remaining usable GMV6 indoor detailed rows: `L0`, `L1`, `L3`, `L4`, `L5`, `L7`, `L9`,
  `LA`, and `LC`. It also corrected the inventory model so `d5`, `d8`, `dE`, `L2`, `L6`, and `LH` are treated as
  reserved/not-applied table-only rows, while `db` is not treated as a detailed fault procedure. GMV6 indoor detailed
  diagnostics are closed; indoor table-only diagnostics remain open.
- Remaining unrepaired GMV6 cards stay in the closure inventory as `DetailedProcedureAvailable`, `TableOnlySafe`,
  `StatusOrPrompt`, or `DebuggingOrCommissioning`. Detailed diagnostic chapters and flowcharts must be reviewed one
  source boundary at a time before those cards can be marked repaired.

Provenance remains in runtime metadata. Telegram-visible fields do not expose filenames, document codes, manual IDs,
source references, package IDs, source meanings, or generic import/evidence phrases.

## Audit

Run:

```powershell
.\scripts\equipment-diagnostics\invoke-gree-manual-bound-card-audit.ps1
```

Ignored outputs:

- `artifacts/verification/equipment-diagnostics/manual-bound-card-repair-audit.json`
- `artifacts/verification/equipment-diagnostics/manual-bound-card-repair-audit.csv`

The JSON report contains the package-to-runtime map and one row per runtime card. ED-24SRC.8d marks `AJ`, `b1`-`bA`,
the `bb`-`bU` table-only subset, `E0`-`E4`, `Ed`, `F0`, `F1`, `F3`, `F5`-`FA`, `Fd`, `Fn`, `FP`, the G-family
table-only batch, the fan-drive `H0`-`HL` batch, the remaining H/J/P table-only batch, the `J0`-`J9` batch, the
`P0`-`PJ`/`PL` compressor-drive batch, and
`FH`/`FC`/`FL`/`FE`/`FF`/`FJ`/`FU`/`Fb` as repaired in the GMV6 manual-bound scope; unreviewed manual-capability fields
are `null` rather than guessed.

## ED-24SRC.3 GMV6 closure inventory

ED-24SRC.3 adds a dedicated GMV6-only closure inventory runner:

```powershell
.\scripts\equipment-diagnostics\invoke-gmv6-manual-bound-closure-inventory.ps1
```

Ignored outputs:

- `artifacts/verification/equipment-diagnostics/gmv6-manual-bound-closure-inventory.json`
- `artifacts/verification/equipment-diagnostics/gmv6-manual-bound-closure-inventory.csv`

The inventory covers all 263 GMV6 runtime cards without editing card content. It records category, package/source
boundary, current visible text, source meaning/reference, inferred manual-section availability, visible-text safety, and
the next repair class used to plan ED-24SRC.4+.

Current ED-24SRC.10b inventory snapshot:

| Category | AlreadyRepaired | DetailedProcedureAvailable | TableOnlySafe | StatusOrPrompt | DebuggingOrCommissioning | Total |
|---|---:|---:|---:|---:|---:|---:|
| outdoor | 121 | 0 | 0 | 0 | 0 | 121 |
| indoor | 19 | 0 | 41 | 0 | 0 | 60 |
| debugging | 0 | 0 | 0 | 0 | 38 | 38 |
| status | 1 | 0 | 0 | 43 | 0 | 44 |
| **Total** | **141** | **0** | **41** | **43** | **38** | **263** |

No conflicting source boundary was found by the inventory. Every GMV6 card has an attached manual/source section or
source reference in the current runtime data. The inventory is a planning map, not a closure claim: cards in
`DetailedProcedureAvailable`, `TableOnlySafe`, `StatusOrPrompt`, and `DebuggingOrCommissioning` still require the later
staged repair/verification passes before GMV6 can be called closed. Current Conflict count is 0, and current
NeedsManualReview / Unclassified count is 0.

Exact ED-24SRC.10b remaining lists:

- Indoor `DetailedProcedureAvailable` (0): none.
- Indoor `TableOnlySafe` (41): `d2`, `d5`, `d8`, `db`, `dd`, `dE`, `dF`, `dJ`, `dn`, `dP`, `dU`, `dy`, `L2`, `L6`,
  `L8`, `Lb`, `LE`, `LF`, `LH`, `LJ`, `LL`, `LP`, `LU`, `o0`, `o1`, `o2`, `o3`, `o4`, `o5`, `o6`, `o7`, `o8`,
  `o9`, `oA`, `ob`, `oC`, `y1`, `y2`, `y7`, `y8`, and `yA`.
- Status `StatusOrPrompt` (43): `A0`, `A2`, `A3`, `A4`, `A6`, `A7`, `A8`, `A9`, `Ab`, `AC`, `Ad`, `AE`, `AF`, `AH`,
  `AL`, `An`, `AP`, `AU`, `Ay`, `n0`, `n1`, `n2`, `n3`, `n4`, `n5`, `n6`, `n7`, `n8`, `n9`, `nA`, `nb`, `nC`,
  `nE`, `nF`, `nH`, `nJ`, `nn`, `nU`, `qA`, `qC`, `qH`, `qP`, and `qU`.
- Debugging `DebuggingOrCommissioning` (38): `C0`, `C1`, `C2`, `C3`, `C4`, `C5`, `C6`, `C7`, `C8`, `C9`, `CA`,
  `Cb`, `CC`, `Cd`, `CE`, `CF`, `CH`, `CJ`, `CL`, `Cn`, `CP`, `CU`, `Cy`, `U0`, `U2`, `U3`, `U4`, `U5`, `U6`,
  `U8`, `U9`, `UC`, `Ud`, `UE`, `UF`, `UL`, `Un`, and `Uy`.

## Stable runtime counts

| Series | Cards |
|---|---:|
| GMV6 HR | 262 |
| GMV6 | 263 |
| GMV Mini | 136 |
| GMV X | 263 |
| GMV9 Flex | 260 |
| U-Match R32 | 107 |
| ERV B Series | 5 |
| **Total** | **1296** |

No runtime card was added or removed. No PDF, DOC, XLS, or XLSX source binary is part of this change.

## ED-24SRC.2a metadata boundary

The AJ manual section states that the prompt is displayed on the indoor-unit wired controller and indoor-unit receiver,
and applies to all indoor units. The current runtime package `gree-gmv6-status-codes` is still package-scoped as
`OutdoorUnit` / `OutdoorBoard`; changing only AJ would fail the package validator or require a package/routing split.
ED-24SRC.2a therefore fixes the user-visible answer and records the metadata/package split as a later safe design task
instead of silently changing routing metadata.

## Backlog

Continue from the generated `DetailedProcedureAvailable`, `TableOnlySafe`, `StatusOrPrompt`, and
`DebuggingOrCommissioning` rows. For each card, use only the manual named by its package/source boundary; determine
whether the source provides a table meaning, fault diagnosis, possible causes, troubleshooting, or flowchart; then
promote the inventory status to `AlreadyRepaired`, `TableOnlySafe`, or `Conflict` with evidence. Do not infer a
procedure from another Gree series or another document.

## Validation status

ED-24SRC.8d validation is recorded in `PROJECT_STATE.md` after the local gate completes.
