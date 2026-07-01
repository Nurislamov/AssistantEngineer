# ED-24SRC.2 manual-bound Gree diagnostic card repair

## Outcome and boundary

This is an honest emergency-scope repair, not a claim that all 1296 cards have been manually reconstructed from their
diagnostic chapters.

- All 1296 Gree runtime cards are included in the generated audit.
- ED-24SRC.1 found the known generic import template in 235 GMV6 cards. Their Telegram-visible summaries, checks, and
  recommended actions were reduced to neutral table-only-safe wording without adding causes or component repairs.
- GMV6 `AJ` was checked against section 2.12 and repaired as a filter-clean prompt with filter cleaning, prompt reset,
  and the next service cycle.
- ED-24SRC.2 repaired the first detailed GMV6 outdoor sensor/procedure batch: `b1`, `b2`, `b3`, `b4`, `b5`, `b6`,
  `b7`, `b8`, `b9`, and `bA`.
- GMV6 `b1`-`bA` were checked against sections 2.17-2.26 and their rendered flowcharts. The cards now include the
  30-second detection condition, all three possible causes, and the ordered connector, sensor, detection-circuit, and
  main-board actions with the correct sensor name for each code.
- The remaining 1285 cards are marked `NeedsManualReview`. Detailed diagnostic chapters and flowcharts must be reviewed
  one source boundary at a time before those cards can be marked repaired.

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

The JSON report contains the package-to-runtime map and one row per runtime card. ED-24SRC.2 marks only `AJ` and
`b1`-`bA` as repaired in the GMV6 manual-bound scope; unreviewed manual-capability fields are `null` rather than
guessed.

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

## Backlog

Continue from the generated `NeedsManualReview` rows. For each card, use only the manual named by its package/source
boundary; determine whether the source provides a table meaning, fault diagnosis, possible causes, troubleshooting, or
flowchart; then promote the audit status to `TableOnlySafe`, `Repaired`, or `Conflict` with evidence. Do not infer a
procedure from another Gree series or another document.

## Validation status

ED-24SRC.2 validation is recorded in `PROJECT_STATE.md` after the local gate completes.
