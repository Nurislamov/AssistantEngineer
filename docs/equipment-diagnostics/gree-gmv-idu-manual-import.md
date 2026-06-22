# ED-24F.1 Gree GMV IDU manual analysis

## Decision

ED-24F.1 stopped the diagnostic import for design review. ED-24F.1b completed the safe merge path by attaching the manual as additional `sourceReferences[]` on existing GMV6 indoor entries. No package manifest or diagnostic entry was added.

The source identity and diagnostic sections are clear, but every identified code overlaps an existing GMV6 indoor entry. ED-24F.1b therefore did not create a second set of broad-GMV indoor entries. It preserved the existing GMV6 answers and added `GC202004-X` as an additional source reference where the equipment type and meaning matched.

## Exclusive source

Only this local file was used:

`artifacts/manual-intake/sources/gree/SERVICE_MANUAL_GMV_IDU.pdf`

No internet source, model memory, GMV6 manual, GMV X manual, GMV Mini manual, controller manual, or other local file was used for technical content.

## Manual identity

| Field | Verified value |
|---|---|
| Manufacturer | Gree |
| Cover title | `Service Manual - Multi Variable Air Conditioners Indoor Units` |
| Document code | `GC202004-X` |
| Language | English |
| PDF pages | 403 |
| Equipment family | Multi-variable / GMV VRF |
| Equipment type | Indoor units |
| Series | Broad GMV indoor-unit scope; no narrower series name is stated on the cover |
| Version/date | No explicit version or publication date found on the reviewed identity pages |

PDF metadata contains processing dates, but those dates are not treated as document version evidence.

The product section covers multiple indoor-unit forms, including floor-standing, duct, cassette, floor-ceiling, fresh-air, air-handler, console, wall-mounted, AHU-KIT, and concealed units. Model tables include GMV-ND and related indoor-unit model families.

## Diagnostic content

The table of contents identifies:

- `MAINTENANCE`
- `1 Troubleshooting`
- `1.1 Malfunction List for the Wired Controller`
- `1.2 Exception Analyzing and Troubleshooting`

Reviewed source range:

- Code table: manual page 173, PDF page 178.
- Troubleshooting details: manual pages 173-185, PDF pages 178-190.

The code table states that errors are displayed by the IDU wired controller and IDU receive light board. The `db` project-debugging section also mentions the ODU mainboard.

The manual contains 38 table codes:

`L0, L1, L2, L3, L4, L5, L7, L8, L9, LA, LH, LC, d1, d3, d4, d6, d7, d8, d9, dA, dH, dC, dL, dE, o1, o2, o3, o4, o5, o6, o7, o8, o9, oA, ob, oC, o0, db`

Detailed judgment/reason/troubleshooting sections are present for 19 codes:

`d1, d3, d4, d6, d7, d9, dA, dH, dC, dL, db, L1, L3, L4, L5, L7, L9, LA, LC`

`db` is explicitly described as project-debugging status, not an error code.

The control chapter also contains engineering parameter/query interfaces such as `C00` and `P00`. These are not imported as diagnostic errors.

## Collision analysis

The existing GMV6 import contains 60 indoor entries. All 38 codes identified in `GC202004-X` already exist in that GMV6 indoor package:

- Overlap: 38.
- New codes unique to this manual relative to GMV6 indoor: 0.

The JSON validator taxonomy key includes series, so `series=GMV` and `series=GMV6` could coexist structurally. That alone is not sufficient for safe runtime behavior.

The current Telegram candidate flow:

1. Finds candidates by code.
2. Prompts for manufacturer when needed.
3. Prompts for equipment type when needed.
4. Prompts for display context when needed.
5. Selects the first remaining candidate.

It does not retain or prompt for a selected series/manual source. For overlapping entries, both candidates would be Gree, Indoor Unit, and the same display context. Silent first-candidate selection would violate the manual-bound rule.

## ED-24F.1a design update

ED-24F.1a adds optional multi-source diagnostic references to the repository-backed error-knowledge model. This changes the import path for `GC202004-X`: when a GMV IDU code has the same equipment type and same meaning as an existing GMV6 indoor entry, the future import should add an additional `sourceReferences[]` item to the existing diagnostic answer instead of creating a duplicate production entry or asking the user to choose a manual.

The design rule is now:

1. Same code + same equipment type + same meaning = one diagnostic answer.
2. That answer may carry multiple manual/source references.
3. If the user requests manuals in a future manual-library stage, return all reviewed manuals where the selected code appears.
4. If a code has different meanings across equipment types or series, ask for equipment/series context, not for source/manual choice.

ED-24F.1a does not import the 38 GMV IDU codes, does not add production diagnostic entries, and does not implement Telegram manual file delivery.

## Required design decision before import

ED-24F.1b merged reviewed IDU manual references without changing the user-facing source-selection boundary:

1. Add `GC202004-X` as additional `sourceReferences[]` only where the meaning matches the existing GMV6 indoor answer.
2. Preserve one diagnostic answer for same-code/same-equipment/same-meaning cases.
3. Add equipment/series clarification only for genuinely different meanings.
4. Preserve `/last` and Russian output normalization.
5. Keep regression guarantees for existing GMV6 queries.

The 19 detailed procedure codes were reviewed. Their detailed sections were not copied into localized Installer/Engineer prose in ED-24F.1b because the existing qualified-service text already points to manual-bound procedure sections and several reviewed procedures include component replacement or electrical service actions. Consumer guidance was not expanded.

## Import result

- Package manifests added: 0.
- Diagnostic entries added: 0.
- Existing indoor entries receiving `sourceReferences[]`: 38.
- Detailed procedure codes reviewed: 19.
- Installer/Engineer localized procedure text updated: 0.
- Codes left `NeedsReview`: 0.
- Existing packages: unchanged at 4.
- Existing entries: unchanged at 253.
- Registry status: `PartiallyImported`.
- Coverage status: `PartialDiagnosticScopeImported`.
- Import decision: `MergedAsSourceReferencesNoNewEntries`.
- Binary PDF committed: no.

Same code + same equipment type + same meaning remains one Telegram answer. Telegram does not ask the user to choose a manual/source. Future manual delivery can use the merged `manualId` values, but ED-24F.1b does not implement Telegram manual file delivery.

## ED-24F.1d follow-up

ED-24F.1d did not import this manual again and did not change the IDU merge decision. The stage improved Telegram
message quality for selected existing GMV6 entries, including `L1`, `d1`, and `o1`, while preserving their
`SERVICE_MANUAL_GMV_IDU.pdf` / `GC202004-X` `sourceReferences[]`.

No new package, diagnostic entry, manual file delivery, database change, migration, or environment variable was added.
Package and entry counts remain unchanged at 4 packages / 253 entries.
