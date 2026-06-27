# ED-24GEC.14 GMV X service manual import

Manual used: `JF00305173, Outlet T1R410A50 & 60HzGMVX Heat Pump GMV Service Manual, A.3.pdf`.

Document/version: `GC202209-I`.

Manual identity: `GMV X DC Inverter VRF Units`; local PDF confirms service, troubleshooting, and after-sales maintenance scope.

Source path: `artifacts/manual-intake/sources/gree/JF00305173, Outlet T1R410A50 & 60HzGMVX Heat Pump GMV Service Manual, A.3.pdf`.

Inventory result:
- code rows found: 263
- ready-for-import: 263
- imported: 263
- skipped/manual-review: 0

Category counts:
- indoor: 60
- outdoor: 121
- debugging: 38
- status: 44

Package files created:
- `gree-gmv-x-indoor-fault-codes.json`
- `gree-gmv-x-outdoor-fault-protection-codes.json`
- `gree-gmv-x-debugging-codes.json`
- `gree-gmv-x-status-codes.json`

Runtime counts after import:
- GMV6: 263
- GMV Mini: 136
- GMV X: 263
- Total Gree: 662

Representative Telegram queries:
- `Gree GMV X E1`
- `Gree X H5`
- `Gree X series C0`
- `Gree X-series A0`

Scope notes:
- GMV6 runtime was not changed.
- GMV Mini runtime was not changed.
- GMV9 Flex runtime was not created or imported in this stage.
- GMV X, GMV9 Flex, GMV6, and GMV Mini packages must stay separate.

Recommended next stage: GMV9 Flex service manual import as a separate stage.
