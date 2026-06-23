# Gree GMV Mini manual import

## Source boundary

ED-24H.2 used only:

`artifacts/manual-intake/sources/gree/SERVICE_MANUAL_GMV_MINI.pdf`

The duplicate/revision candidate `SERVICE_MANUAL_GMV_MINI (1).pdf` was not used as source evidence or comparison input for this stage.

Verified identity from the selected PDF:

- Cover title: `DC INVERTER VRF SYSTEM SERVICE MANUAL(R410A)`.
- Running header: `DC Inverter Multi VRF System II Service Manual`.
- Page count: 173.
- SHA256: `2BCF93A066D61C935A4B82717A42E45F96D5D5CB425C62C363C5D089AD982B70`.
- Document code: not found.
- Diagnostic sections: Debugging & Maintenance, wired-controller malfunction list, outdoor main-board status table, and troubleshooting flowcharts.

## Import result

The selected manual identified 130 code contexts. ED-24H.2 imported only clear, non-duplicated meanings and merged exact same-meaning overlaps as source references.

- New packages: 3.
- New entries: 9.
- Existing entries receiving GMV Mini `sourceReferences[]`: 31.
- NeedsReview contexts: 90.
- Repository totals after import: 7 packages / 262 entries / 0 validator issues.

New packages:

- `gree-gmv-mini-vrf-indoor-controller-codes` - 2 entries.
- `gree-gmv-mini-vrf-outdoor-protection-codes` - 1 entry.
- `gree-gmv-mini-vrf-status-codes` - 6 entries.

New entries:

- Indoor: `C0`, `AJ`.
- Outdoor protection: `EC`.
- Outdoor status/settings: `A1`, `A5`, `A9`, `AA`, `n1`, `n2`.

Source-reference-only merges:

- Indoor: `L5`, `d3`, `d4`, `d6`, `d7`, `d8`, `d9`, `dE`.
- Outdoor/status/debug: `E1`, `E3`, `F1`, `F3`, `FP`, `J8`, `J9`, `b2`, `b3`, `C8`, `C9`, `CA`, `A3`, `A4`, `A6`, `A7`, `A8`, `AU`, `AH`, `AL`, `Ad`, `nA`, `nE`.

The remaining 90 contexts stay NeedsReview because wording, display context, or series/equipment meaning differed enough that a silent merge would be unsafe.

## Smoke codes

Use these after deployment:

- `Gree GMV Mini C0`
- `Gree GMV Mini EC`
- `Gree GMV Mini A1`
- Existing regression checks: `Gree d1`, `/manuals` after `Gree d1`, `Gree H5`, `Gree U0`.

## Production notes

No manual binary, runtime Telegram binding JSON, real Telegram `file_id`, env file, DB schema change, or EF migration belongs in this import. GMV Mini manual delivery is not connected until a reviewed server-local Telegram binding is created outside Git.
