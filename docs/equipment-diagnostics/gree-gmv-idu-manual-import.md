# ED-24F.1 Gree GMV IDU manual analysis

## Decision

Diagnostic import was stopped for design review. No package manifest or diagnostic entry was added.

The source identity and diagnostic sections are clear, but every identified code overlaps an existing GMV6 indoor entry. The current Telegram conversation can distinguish manufacturer, equipment type, and display context, but cannot ask the user to choose between two indoor-unit candidates that differ only by series/manual source. Importing now would allow deterministic ordering to select one source silently.

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

## Required design decision before import

A separate stage should define and test:

1. Series-aware candidate filtering and a user-facing series/model-family clarification.
2. Session persistence of selected series.
3. Exact-series localization selection and precedence rules.
4. Behavior for broad `GMV` sources versus specific `GMV6` sources.
5. `/last` source/series reconstruction.
6. Regression guarantees for existing GMV6 queries.

After that design is implemented, this manual can be imported as a distinct source without overwriting GMV6 entries.

## Import result

- Package manifests added: 0.
- Diagnostic entries added: 0.
- Existing packages: unchanged at 4.
- Existing entries: unchanged at 253.
- Registry status: `NeedsReview`.
- Coverage status: `DiagnosticSectionsIdentified`.
- Import decision: `BlockedPendingSeriesAwareDisambiguation`.
- Binary PDF committed: no.
