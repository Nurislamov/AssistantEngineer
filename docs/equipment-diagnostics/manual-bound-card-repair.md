# ED-24SRC.1 manual-bound Gree diagnostic card repair

## Outcome and boundary

This is an honest emergency-scope repair, not a claim that all 1296 cards have been manually reconstructed from their
diagnostic chapters.

- All 1296 Gree runtime cards are included in the generated audit.
- The known generic import template was present in 235 GMV6 cards. Their Telegram-visible summaries, checks, and
  recommended actions were reduced to neutral table-only-safe wording without adding causes or component repairs.
- GMV6 `AJ` was checked against section 2.12 and repaired as a filter-clean prompt with filter cleaning, prompt reset,
  and the next service cycle.
- GMV6 `b1` was checked against section 2.17 and its rendered flowchart. The card now includes the 30-second detection
  condition, all three possible causes, and the ordered connector, sensor, detection-circuit, and main-board actions.
- The remaining 1294 cards are marked `NeedsManualReview`. Detailed diagnostic chapters and flowcharts must be reviewed
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

The JSON report contains the package-to-runtime map and one row per runtime card. Unreviewed manual-capability fields are
`null` rather than guessed.

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

- Restore and build pass; build has 0 warnings and 0 errors.
- EquipmentDiagnostics tests pass 1112/1112.
- Telegram tests pass 640/640 with a disposable migrated local PostgreSQL test database.
- The exact plain full suite runs 5135/5136. Its sole failure is the existing unrelated Engineering Workflow artifact
  assertion receiving the configured 256 KiB truncation envelope instead of raw JSON.
- The full suite passes 5136/5136 with the existing test-only 10 MiB artifact-content override. No production
  configuration is changed.

The stage is not labelled full validation PASS while the exact plain full-suite command remains red.
