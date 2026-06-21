# Equipment Catalog Registries

This directory contains non-runtime equipment coverage maps for EquipmentDiagnostics planning.

The registries are catalog-level references. They identify equipment series, indoor-unit types, controllers, gateways, and manual-search backlog items from local source catalogues. They do not add diagnostic codes, diagnostic cases, runtime lookup behavior, Telegram role rules, or manual delivery.

Current registry:

- `gree-vrf-equipment-map.json` - Gree VRF/GMV catalog map built from three local product catalogues under `artifacts/manual-intake/sources/gree`.

Rules:

- Use local repository or local intake evidence only.
- Keep source binaries untracked; `committed` stays `false` for local PDF catalogues.
- Every series, indoor type, control/accessory device, and backlog item must reference known source catalog IDs or related registry IDs.
- Use `NeedsReview` notes instead of inventing exact service-manual scope when a catalogue only identifies a family or accessory.
- Treat these files as planning evidence for future manual intake and Telegram manual-library work, not as runtime diagnostic knowledge.
