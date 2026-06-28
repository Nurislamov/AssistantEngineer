# ED-24GEC.15 GMV9 Flex import

Source manual: JF00305819, export T3R410AGMV9 Flex GMV Service Manual (Middle East energy-inefficient), B .2.pdf

Document: GC202512-I

Runtime package: `data/equipment-diagnostics/error-knowledge/gree/gmv9-flex`

Inventory rows imported: 260

Ready-for-import rows: 260

Imported runtime cards: 260

Skipped placeholder rows: 2

Manual-review rows: 0

Category counts:

| Category | Count |
| --- | ---: |
| indoor | 60 |
| outdoor | 120 |
| debugging | 37 |
| status | 43 |

Package files created:

- `data/equipment-diagnostics/error-knowledge/packages/gree-gmv9-flex-indoor-fault-codes.json`
- `data/equipment-diagnostics/error-knowledge/packages/gree-gmv9-flex-outdoor-fault-protection-codes.json`
- `data/equipment-diagnostics/error-knowledge/packages/gree-gmv9-flex-debugging-codes.json`
- `data/equipment-diagnostics/error-knowledge/packages/gree-gmv9-flex-status-codes.json`

Runtime counts after import:

- GMV6: 263
- GMV Mini: 136
- GMV X: 263
- GMV9 Flex: 260
- Total Gree: 922

Representative Telegram queries:

- `Gree GMV9 Flex E0`
- `Gree GMV9 H5`
- `Gree 9 series Flex C0`
- `Gree 9-Flex A0`

Next recommended stage: production Telegram smoke for the representative GMV9 Flex queries, then project-state documentation after smoke confirmation.

GMV9 Flex is imported as a separate runtime series/package and must not be mixed with GMV6, GMV Mini, or GMV X.
