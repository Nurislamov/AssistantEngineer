# Gree GMV6 service manual import

## Source boundary

- Local source: `artifacts/manual-intake/sources/gree/Service Manual for GMV6 v_2020.09.pdf`
- Title: `Service Manual`
- Equipment: `GMV6 DC Inverter VRF Units`
- Document code: `GC202001-I`
- Version: `2020.09`
- Manufacturer: `Gree`
- Source language: English
- Imported scope: Chapter 3 `Faults`, section 1 `Error Indication`

No internet, model-memory code knowledge, other Gree manual, or external diagnostic source was used. The PDF remains
a local ignored artifact and is not committed.

## Packages and counts

| Package | Category | Entries |
| --- | --- | ---: |
| `gree-gmv6-indoor-fault-codes` | Indoor | 60 |
| `gree-gmv6-outdoor-fault-protection-codes` | Outdoor | 120 |
| `gree-gmv6-debugging-codes` | Debugging | 37 |
| `gree-gmv6-status-codes` | Status | 36 |
| **Total** |  | **253** |

Every table row from PDF pages 74-77 (manual pages 72-75) is represented. Exact English table wording is retained in
`sourceMeaning`; Russian Consumer, Installer, and Engineer views are stored separately.

## Troubleshooting extraction

Chapter 3 contains 147 code-specific troubleshooting headings. This import fully transfers the detailed causes and
bounded checks for H5 from section 2.82 (PDF page 135 / manual page 133). The remaining entries are deliberately
table-bound in this stage: their exact table meaning and detailed-section reference are retained, but their flowchart
steps and causes are not copied into localized guidance. Their Russian text explicitly says when the detailed procedure
was not transferred.

No Error Indication row was skipped. No table/OCR ambiguity remained after visual review of the four catalog pages.

## H5 correction

The former generic GMV H5 seed is replaced with the GMV6 manual-backed meaning:

`Over-current protection of inverter fan`

Classification: VRF / OutdoorUnit / Protection / OutdoorBoard / Fan / High. Qualified service is required and customer
operation is marked false. Consumer text prohibits panel opening, measurements, component replacement, protection
bypass, and repeated restarts. Installer and Engineer text preserve the manual's cable, fan, blade/shaft, and fan-drive
board checks within qualified-service safety boundaries.

## Model scope

Entries use the nine basic outdoor models explicitly identified for the manual family:

`GMV-224WM/G-X`, `GMV-280WM/G-X`, `GMV-335WM/G-X`, `GMV-400WM/G-X`, `GMV-450WM/G-X`,
`GMV-504WM/G-X`, `GMV-560WM/G-X`, `GMV-615WM/G-X`, and `GMV-680WM/G-X`.

Combined models are covered by the `GMV6` series boundary instead of being exhaustively repeated in every entry.
