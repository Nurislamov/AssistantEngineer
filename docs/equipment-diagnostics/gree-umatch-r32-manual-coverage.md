# Gree U-Match R32 Manual Coverage

Status: ED-24E.4 imported the confirmed runtime error-code table from the local intake PDF. The source PDF is not committed.

## Source

- Local intake file: `artifacts/manual-intake/sources/gree/Gree U-Match R32 Service Manual EN 3.5-16kW.pdf`
- SHA-256: `D8738715618C3246E6C8A2DD67AD157C46944E38B7E1A2B9E552253ECF10288A`
- PDF pages: 188
- Document code: `GC202209-I`
- Main table: section 3.3 `Error Code`, PDF pages 51-54
- Detailed troubleshooting starts in section 3.4, PDF pages 55-56 and following pages where present

## Import Result

- Runtime package: `gree-umatch-r32-error-codes`
- Runtime directory: `data/equipment-diagnostics/error-knowledge/gree/umatch-r32/system`
- Imported cards: 107
- Expected package count: 107
- Series: `U-Match R32`
- Equipment family: `SemiIndustrial`
- Source references: metadata only; visible Telegram text does not expose `GC202209-I`, raw paths, package ids, or `sourceMeaning`

## Detail Split

- Detailed troubleshooting cards: E0, E1, E2, E3, E4, E6, E9, C6, F3, CE, CJ, H4, H5, HC, Lc, U7, qC, PA, PL, PH, C8, EL
- Table-only cards: remaining confirmed section 3.3 rows
- Missing confirmed table rows: 0
- Extra imported rows outside the confirmed table: 0
- Known conflicts: shared Gree codes now participate in series refinement instead of falling back to GMV-only behavior

## Guardrails

- PDF binaries remain intake-only and are not committed.
- User-visible card text is Russian, conservative, and manual-bound.
- Source meanings remain English metadata from the source table.
- Routing recognizes `U-Match`, `UMatch`, `U Match`, `GUD`, and Russian semi-industrial wording as `U-Match R32`.
