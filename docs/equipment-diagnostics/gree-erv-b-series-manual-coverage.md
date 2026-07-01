# Gree ERV B Series Manual Coverage

Status: ED-24E.5 imported E6 and L0. ED-24E.5a adds L9, dF, and dH from the wired-controller OwnerManual and polishes
all public ERV wording. Source PDFs are not committed.

## Source

- Local intake file: `artifacts/manual-intake/sources/gree/Gree ERV B Series Service Manual EN FHBQG-D3.5B-D60B.pdf`
- SHA-256: `867820A9260E419888A2BF658EDC9F820D5F5BDC12AE607EA36A1E28421BF407`
- PDF pages: 40
- Document code: not stated in the manual
- Diagnostics table: Troubleshooting and Maintenance / Diagnostics, PDF page 18 / manual page 16
- Additional source: `Gree ERV Wired Controller Owner Manual EN.pdf`
- Additional controller codes: L9, dF, dH, and corroborating E6

## Import Result

- Runtime package: `gree-erv-b-series-diagnostics`
- Runtime directory: `data/equipment-diagnostics/error-knowledge/gree/erv-b-series/system`
- Imported cards: 5
- Expected package count: 5
- Series: `ERV B Series`
- Equipment family: `SemiIndustrial`
- Imported codes: E6, L0, L9, dF, dH
- Source references: metadata only; visible Telegram text does not expose raw paths, package ids, or `sourceMeaning`

## Non-Imported Rows

- Symptom-only troubleshooting rows without a confirmed displayed code were not imported into runtime diagnostics.
- Rows that describe checks, components, or symptoms without a code remain source-context only.
- Missing selected code rows: 0
- Extra imported rows outside the selected code set: 0

## Guardrails

- PDF binaries remain intake-only and are not committed.
- User-visible card text is Russian, conservative, and specific to ERV rather than GMV/VRF.
- Routing recognizes `ERV`, `FHBQG`, Russian ventilation/recuperator wording, and `Energy Recovery Ventilation` as `ERV B Series`.
- Public cards do not expose source tables, sections, filenames, or evidence phrases.
- The diagnostic guide sends the single active ERV OwnerManual directly with protected content.
