# AssistantEngineer Project State

## Current stage

ED-24SRC.7d - VALIDATED / commit pending. ED-24SRC.1, ED-24SRC.1b, ED-24SRC.2, ED-24SRC.3, ED-24SRC.2a,
ED-24SRC.4, ED-24SRC.5, ED-24SRC.6, ED-24SRC.7a, ED-24SRC.7b, and ED-24SRC.7c are CLOSED / pushed.

Next recommended steps:

1. Commit and push ED-24SRC.7d.
2. Continue with ED-24SRC.7e only if the updated inventory still exposes in-scope GMV6 outdoor work.
3. Deploy only through a separately authorized production operation; this stage performs no production deployment.

## Current branch

master

## Last completed work

ED-24SRC.7d repairs the remaining GMV6 outdoor P compressor-drive detailed-procedure batch without changing runtime
counts.

ED-24SRC.7d work-log selection:

- Selected codes: `P0`, `P1`, `P2`, `P3`, `P5`, `P6`, `P7`, `P8`, `P9`, `PC`, `PH`, `PJ`, and `PL`.
- Manual sections: 2.125-2.137 in `Service Manual for GMV6 v_2020.09.pdf`.
- Batch reason: this is the contiguous P outdoor compressor-drive group, including aggregate wired-controller codes
  `P0`-`P2` and specific outdoor 2-digit LED compressor-drive protections/faults.
- Skipped codes: `P4`, `PA`, `PE`, `PF`, `PP`, and `PU` because they remain `TableOnlySafe` outdoor cards without a
  promoted detailed troubleshooting section in the current inventory; no indoor, status, or debugging cards were
  changed.

ED-24SRC.7d implementation notes:

- `P0`, `P1`, and `P2` now direct the technician to read the 2-digit LED on the outdoor main board and branch to the
  specific compressor-drive code reported there.
- `P3`, `P7`, and `PC` now carry the repeated power-cycle check and compressor drive board replacement branch.
- `P5`, `P6`, `P9`, and `PJ` now carry the UVW wiring, winding resistance, insulation, clogged-system, control-valve,
  simultaneous-fault, compressor replacement, and compressor drive board replacement flow.
- `P8` now carries the IPM over-temperature screw/thermal-grease checks and compressor drive board replacement branch.
- `PH` and `PL` now carry the 460 В / 320 В input-voltage thresholds, 380 В correction action, and compressor drive
  board replacement branch.
- Telegram-visible `sourceNote` fields are neutral and do not expose section/manual/source wording.
- `invoke-gmv6-manual-bound-closure-inventory.ps1` classifies the ED-24SRC.7d batch as repaired.
- Inventory counts after script rerun: AlreadyRepaired 68; DetailedProcedureAvailable 26; TableOnlySafe 88;
  StatusOrPrompt 43; DebuggingOrCommissioning 38.
- Category split after script rerun: outdoor = 67 AlreadyRepaired, 0 DetailedProcedureAvailable, 54 TableOnlySafe;
  indoor = 26 DetailedProcedureAvailable, 34 TableOnlySafe; debugging = 38 DebuggingOrCommissioning; status =
  1 AlreadyRepaired, 43 StatusOrPrompt.
- Runtime counts are unchanged: Gree 1296; GMV6 263; GMV6 outdoor 121; GMV6 indoor 60; GMV6 debugging 38; GMV6 status
  44.
- No PDF/manual binary, card count, package manifest, source reference, routing rule, migration, secret, or deploy file
  changed.
- Validation:
  - `dotnet restore .\AssistantEngineer.sln`: PASS.
  - `dotnet build .\AssistantEngineer.sln --no-restore`: PASS with 6 existing nullable warnings in architecture guard
    tests and 0 errors.
  - `dotnet test .\AssistantEngineer.sln --filter "FullyQualifiedName~EquipmentDiagnostics" --logger "console;verbosity=minimal"`:
    PASS (1119/1119).
  - `dotnet test .\AssistantEngineer.sln --filter "FullyQualifiedName~Telegram" --logger "console;verbosity=minimal"`:
    PASS (640/640).
  - `dotnet test .\AssistantEngineer.sln --filter "FullyQualifiedName~EquipmentDiagnosticTelegramWebhookApiIntegrationTests" --logger "console;verbosity=minimal"`:
    PASS (10/10).
  - `dotnet test .\AssistantEngineer.sln --logger "console;verbosity=minimal"`: PASS (5143/5143).
  - `git diff --check`: PASS.

ED-24SRC.7c repairs the remaining GMV6 outdoor J detailed-procedure batch without changing runtime counts.

ED-24SRC.7c work-log selection:

- Selected codes: `J0`, `J1`, `J2`, `J3`, `J4`, `J5`, `J6`, `J7`, `J8`, and `J9`.
- Manual sections: 2.91-2.100 in `Service Manual for GMV6 v_2020.09.pdf`.
- Batch reason: this is the contiguous J outdoor group: other-module protection, compressor over-current protections,
  four-way-valve air-backflow protection, and high/low pressure-ratio protections.
- Skipped codes: `P0`-`PJ` because they are the separate compressor-drive P group planned for ED-24SRC.7d.

ED-24SRC.7c implementation notes:

- `J0` now carries the multi-module diagnosis: another module fault causes otherwise normal modules to display J0; the
  repair path is to troubleshoot the originating module.
- `J1`-`J6` now carry the compressor 1-6 over-current display locations, current-limit diagnosis, three manual causes,
  and the power-on / 60 °C high-pressure / fan-ambient-power / drive-module / compressor troubleshooting flow.
- `J7` now carries the four-way-valve air-backflow display locations, <0.1 MPa pressure-difference condition, three
  manual causes, and coil / 220 V board-output / pressure-difference / pipe-temperature / valve replacement flow.
- `J8` and `J9` now carry the pressure-ratio display locations, >8 and <1.8 ratio conditions, two manual causes, and
  operating-limit / high-low pressure-sensor checks.
- Telegram-visible `sourceNote` fields are neutral and do not expose section/manual/source wording.
- `invoke-gmv6-manual-bound-closure-inventory.ps1` classifies the ED-24SRC.7c batch as repaired.
- Inventory counts after script rerun: AlreadyRepaired 55; DetailedProcedureAvailable 39; TableOnlySafe 88;
  StatusOrPrompt 43; DebuggingOrCommissioning 38.
- Category split after script rerun: outdoor = 54 AlreadyRepaired, 13 DetailedProcedureAvailable, 54 TableOnlySafe;
  indoor = 26 DetailedProcedureAvailable, 34 TableOnlySafe; debugging = 38 DebuggingOrCommissioning; status =
  1 AlreadyRepaired, 43 StatusOrPrompt.
- Runtime counts are unchanged: Gree 1296; GMV6 263; GMV6 outdoor 121; GMV6 indoor 60; GMV6 debugging 38; GMV6 status
  44.
- No PDF/manual binary, card count, package manifest, source reference, routing rule, migration, secret, or deploy file
  changed.
- Validation:
  - `dotnet restore .\AssistantEngineer.sln`: PASS.
  - `dotnet build .\AssistantEngineer.sln --no-restore`: PASS with 6 existing nullable warnings in architecture guard
    tests and 0 errors.
  - `dotnet test .\AssistantEngineer.sln --filter "FullyQualifiedName~EquipmentDiagnostics" --logger "console;verbosity=minimal"`:
    PASS (1118/1118).
  - `dotnet test .\AssistantEngineer.sln --filter "FullyQualifiedName~Telegram" --logger "console;verbosity=minimal"`:
    PASS (640/640).
  - `dotnet test .\AssistantEngineer.sln --filter "FullyQualifiedName~EquipmentDiagnosticTelegramWebhookApiIntegrationTests" --logger "console;verbosity=minimal"`:
    PASS (10/10).
  - `dotnet test .\AssistantEngineer.sln --logger "console;verbosity=minimal"`: PASS (5142/5142).
  - `git diff --check`: PASS.

ED-24SRC.7b repairs the remaining GMV6 outdoor E/F detailed-procedure batch without changing runtime counts.

ED-24SRC.7b work-log selection:

- Selected codes: `E1`, `E2`, `E3`, `E4`, `F0`, `F1`, and `F3`.
- Manual sections: 2.57-2.63 in `Service Manual for GMV6 v_2020.09.pdf`.
- Batch reason: this is the remaining adjacent E/F outdoor protection, main-board, and pressure-sensor group. It is
  smaller than the preferred eight-card batch, but mixing in J/P compressor-drive groups would cross manual-family
  boundaries.
- Skipped codes: `J0`-`J9` and `P0`-`PJ` because they are separate manual structures planned for ED-24SRC.7c and
  ED-24SRC.7d.

ED-24SRC.7b implementation notes:

- `E1` now carries the high-pressure display locations, >65 °C / high-pressure-switch condition, eight manual causes,
  and the overpressure troubleshooting flow covering pressure measurement, pressure switch/sensor, valves, panels,
  airflow, fans, louvers, expansion valves, fins, ambient temperature, clogged pipes, and excess refrigerant.
- `E2` now carries the discharge low-temperature display locations, <10 °C condition, four manual causes, and checks for
  discharge/shell-roof sensors, indoor/outdoor EXV reset behavior, and project refrigerant charge.
- `E3` now carries the low-pressure display locations, -41 °C saturation condition, seven manual causes, and checks for
  sensor/pressure, refrigerant, valves, panel, airflow, fans, louver, indoor capacity DIP switch, EXV, filter, and pipes.
- `E4` now carries the high discharge temperature display locations, >118 °C condition, seven manual causes, and checks
  for valves, EXV reset, fans, indoor filter/air resistance, operating limits, refrigerant charge, and pipe/EXV blockage.
- `F0` now carries the outdoor main-board chip diagnosis, three manual causes, and CPU small-board / compressor
  drive-board / main-control-board troubleshooting flow.
- `F1` and `F3` now carry the pressure-sensor display locations, 30-second AD-value condition, four manual causes, and
  connector / pressure-contact / sensor / main-control-board troubleshooting flow.
- Telegram-visible `sourceNote` fields are neutral and do not expose section/manual/source wording.
- `invoke-gmv6-manual-bound-closure-inventory.ps1` classifies the ED-24SRC.7b batch as repaired.
- Inventory counts after script rerun: AlreadyRepaired 45; DetailedProcedureAvailable 49; TableOnlySafe 88;
  StatusOrPrompt 43; DebuggingOrCommissioning 38.
- Category split after script rerun: outdoor = 44 AlreadyRepaired, 23 DetailedProcedureAvailable, 54 TableOnlySafe;
  indoor = 26 DetailedProcedureAvailable, 34 TableOnlySafe; debugging = 38 DebuggingOrCommissioning; status =
  1 AlreadyRepaired, 43 StatusOrPrompt.
- Runtime counts are unchanged: Gree 1296; GMV6 263; GMV6 outdoor 121; GMV6 indoor 60; GMV6 debugging 38; GMV6 status
  44.
- No PDF/manual binary, card count, package manifest, source reference, routing rule, migration, secret, or deploy file
  changed.
- Validation:
  - `dotnet restore .\AssistantEngineer.sln`: PASS.
  - `dotnet build .\AssistantEngineer.sln --no-restore`: PASS with 6 existing nullable warnings in architecture guard
    tests and 0 errors.
  - `dotnet test .\AssistantEngineer.sln --filter "FullyQualifiedName~EquipmentDiagnostics" --logger "console;verbosity=minimal"`:
    PASS (1117/1117) after preserving the existing approved `Gree GMV E1` / `Gree GMV F3` title wording.
  - `dotnet test .\AssistantEngineer.sln --filter "FullyQualifiedName~Telegram" --logger "console;verbosity=minimal"`:
    PASS (640/640).
  - `dotnet test .\AssistantEngineer.sln --filter "FullyQualifiedName~EquipmentDiagnosticTelegramWebhookApiIntegrationTests" --logger "console;verbosity=minimal"`:
    PASS (10/10).
  - `dotnet test .\AssistantEngineer.sln --logger "console;verbosity=minimal"`: PASS (5141/5141).
  - `git diff --check`: PASS.
  - `git status --short`: only intended ED-24SRC.7b card, test, inventory-script, docs, and state files changed before
    commit.

ED-24SRC.7a repairs the first split GMV6 outdoor detailed-procedure batch after ED-24SRC.6 without changing runtime
counts.

ED-24SRC.7a work-log selection:

- Selected codes: `FH`, `FC`, `FL`, `FE`, `FF`, `FJ`, `FU`, and `Fb`.
- Manual sections: 2.70-2.77 in `Service Manual for GMV6 v_2020.09.pdf`.
- Batch reason: adjacent outdoor compressor sensor sections. `FH`/`FC`/`FL`/`FE`/`FF`/`FJ` share the current-sensor
  flowchart for compressors 1-6, and `FU`/`Fb` are the immediately adjacent shell-roof temperature sensor sections for
  compressors 1-2.
- Skipped codes: `E1`-`E4`, `F0`, `F1`, `F3`, `J0`-`J9`, and `P0`-`PJ` because they are different manual structures
  planned for later ED-24SRC.7 batches.

ED-24SRC.7a implementation notes:

- `FH`, `FC`, `FL`, `FE`, `FF`, and `FJ` now carry the manual display locations, 3-second AD-value condition, three
  possible causes, and connector / current-sensor small-board / main-board troubleshooting flowchart.
- `FU` and `Fb` now carry the manual display locations, 30-second AD-value condition, three possible causes, and
  connector / shell-roof temperature sensor / main-board troubleshooting flowchart.
- Telegram-visible `sourceNote` fields are neutral and do not expose section/manual/source wording.
- `invoke-gmv6-manual-bound-closure-inventory.ps1` classifies the ED-24SRC.7a batch as repaired.
- Inventory counts after script rerun: AlreadyRepaired 38; DetailedProcedureAvailable 56; TableOnlySafe 88;
  StatusOrPrompt 43; DebuggingOrCommissioning 38.
- Category split after script rerun: outdoor = 37 AlreadyRepaired, 30 DetailedProcedureAvailable, 54 TableOnlySafe;
  indoor = 26 DetailedProcedureAvailable, 34 TableOnlySafe; debugging = 38 DebuggingOrCommissioning; status =
  1 AlreadyRepaired, 43 StatusOrPrompt.
- Runtime counts are unchanged: Gree 1296; GMV6 263; GMV6 outdoor 121; GMV6 indoor 60; GMV6 debugging 38; GMV6 status
  44.
- No PDF/manual binary, card count, package manifest, source reference, routing rule, migration, secret, or deploy file
  changed.
- Validation:
  - `dotnet restore .\AssistantEngineer.sln`: PASS.
  - `dotnet build .\AssistantEngineer.sln --no-restore`: PASS with 6 existing nullable warnings in architecture guard
    tests and 0 errors.
  - `dotnet test .\AssistantEngineer.sln --filter "FullyQualifiedName~EquipmentDiagnostics" --logger "console;verbosity=minimal"`:
    PASS (1116/1116).
  - `dotnet test .\AssistantEngineer.sln --filter "FullyQualifiedName~Telegram" --logger "console;verbosity=minimal"`:
    PASS (640/640).
  - `dotnet test .\AssistantEngineer.sln --filter "FullyQualifiedName~EquipmentDiagnosticTelegramWebhookApiIntegrationTests" --logger "console;verbosity=minimal"`:
    PASS (10/10).
  - `dotnet test .\AssistantEngineer.sln --logger "console;verbosity=minimal"`: PASS (5140/5140).
  - `git diff --check`: PASS.
  - `git status --short`: only intended ED-24SRC.7a card, test, inventory-script, docs, and state files changed before
    commit.

ED-24SRC.6 reconciles the GMV6 manual-bound closure inventory after ED-24SRC.2a, ED-24SRC.4, and ED-24SRC.5.

ED-24SRC.6 implementation notes:

- Verified `git log -12 --oneline`: ED-24SRC.2a, ED-24SRC.4, and ED-24SRC.5 are present on `master` after
  ED-24SRC.3.
- Reran `scripts/equipment-diagnostics/invoke-gmv6-manual-bound-closure-inventory.ps1`; ignored JSON/CSV artifacts were
  regenerated under `artifacts/verification/equipment-diagnostics/`.
- Runtime counts are unchanged: Gree 1296; GMV6 263; GMV6 outdoor 121; GMV6 indoor 60; GMV6 debugging 38; GMV6 status
  44.
- Current repair classes: AlreadyRepaired 30; DetailedProcedureAvailable 64; TableOnlySafe 88; StatusOrPrompt 43;
  DebuggingOrCommissioning 38.
- Current category split: outdoor = 29 AlreadyRepaired, 38 DetailedProcedureAvailable, 54 TableOnlySafe; indoor = 26
  DetailedProcedureAvailable, 34 TableOnlySafe; debugging = 38 DebuggingOrCommissioning; status = 1 AlreadyRepaired,
  43 StatusOrPrompt.
- Conflict count: 0. NeedsManualReview / Unclassified count: 0.
- GMV6 remains open. ED-24SRC.6 is a reconciliation stage only and does not mark GMV6 closed.
- No PDF/manual binary, runtime card, card count, package manifest, source reference, routing rule, migration, secret, or
  deploy file changed.
- Validation:
  - `dotnet test .\AssistantEngineer.sln --filter "FullyQualifiedName~EquipmentDiagnostics" --logger "console;verbosity=minimal"`:
    PASS (1115/1115).
  - `git diff --check`: PASS.
  - `git status --short`: only ED-24SRC.6 documentation/state files changed before commit.

ED-24SRC.5 repairs the next controlled GMV6 outdoor fan-drive detailed batch without changing runtime counts.

ED-24SRC.5 work-log selection:

- Selected codes: `H0`, `H1`, `H2`, `H3`, `H5`, `H6`, `H7`, `H8`, `H9`, `HC`, `HH`, `HJ`, and `HL`.
- Manual sections: 2.78-2.90 in `Service Manual for GMV6 v_2020.09.pdf`.
- Batch reason: adjacent outdoor fan-drive sections with one source boundary. `H0`-`H2` are aggregate
  wired-controller codes that route to the outdoor 2-digit LED; the remaining codes are specific fan-drive LED faults.
- Skipped codes: current-sensor compressor group `FH`, `FC`, `FL`, `FE`, `FF`, `FJ`, casing-top temperature sensor
  group `FU`/`Fb`, and other later H/J/P groups because they are different manual structures.

ED-24SRC.5 implementation notes:

- `H0`-`H2` now preserve the manual behavior: check the wired-controller code, read the outdoor 2-digit LED, then use
  the procedure for the specific LED code.
- `H3`, `H7`, and `HC` now carry the power-cycle / fan-drive-board flowchart, with the documented `P3` branch for `H3`.
- `H5`, `H6`, `H9`, and `HJ` now carry the fan UVW wiring, winding resistance, grounding insulation, blade blockage,
  related-fault, fan replacement, and fan-drive-board flowchart.
- `H8`, `HH`, and `HL` now carry their thermal-grease/screw or input-voltage threshold checks and final fan-drive-board
  branch.
- `sourceNote` is neutral and does not expose section/manual/source wording.
- `invoke-gmv6-manual-bound-closure-inventory.ps1` and the all-Gree manual-bound audit classify the H fan-drive batch
  as repaired.
- Inventory counts after script rerun: AlreadyRepaired 30; DetailedProcedureAvailable 64; TableOnlySafe 88;
  StatusOrPrompt 43; DebuggingOrCommissioning 38.
- Runtime counts are unchanged: Gree 1296; GMV6 263; GMV6 outdoor 121; GMV6 indoor 60; GMV6 debugging 38; GMV6 status
  44.
- No PDF/manual binary, card count, package manifest, source reference, routing rule, migration, secret, or deploy file
  changed.
- Focused ED-24SRC.5 guard: PASS, 7/7.
- `dotnet restore .\AssistantEngineer.sln`: PASS.
- `dotnet build .\AssistantEngineer.sln --no-restore`: PASS with 0 warnings and 0 errors.
- EquipmentDiagnostics filter: PASS, 1115/1115 after preserving the existing H5 Telegram wording/safety anchor.
- Telegram filter: PASS, 640/640.
- EquipmentDiagnosticTelegramWebhookApiIntegrationTests filter: PASS, 10/10.
- Exact full solution suite: PASS, 5139/5139.
- `git diff --check`: PASS.

ED-24SRC.4 repairs the next controlled GMV6 outdoor detailed-procedure batch without changing runtime counts.

ED-24SRC.4 work-log selection:

- Selected codes: `F5`, `F6`, `F7`, `F8`, `F9`, and `FA`.
- Manual sections: 2.64-2.69 in `Service Manual for GMV6 v_2020.09.pdf`.
- Batch reason: adjacent same-structure outdoor sections for discharge temperature sensor faults of compressors 1-6.
- Skipped codes: `FH` / 2.70 and the current-sensor group `FC`, `FL`, `FE`, `FF`, `FJ` / 2.71-2.75 because they are
  different manual structures and should be handled in a later batch.

ED-24SRC.4 implementation notes:

- `F5`-`FA` visible summaries now identify the discharge temperature sensor for the exact compressor, the manual fault
  display locations, and the AD-value / 30-second detection condition.
- Possible causes now match the manual: poor contact between discharge temperature sensor and main-board interface,
  abnormal discharge temperature sensor, and abnormal detection circuit.
- Check steps now follow the rendered flowchart: connector/foreign matter -> replace sensor -> replace main control
  board.
- `sourceNote` is neutral and does not expose section/manual/source wording.
- `invoke-gmv6-manual-bound-closure-inventory.ps1` and the all-Gree manual-bound audit classify `F5`-`FA` as repaired.
- Inventory counts after script rerun: AlreadyRepaired 17; DetailedProcedureAvailable 77; TableOnlySafe 88;
  StatusOrPrompt 43; DebuggingOrCommissioning 38.
- Runtime counts are unchanged: Gree 1296; GMV6 263; GMV6 outdoor 121; GMV6 indoor 60; GMV6 debugging 38; GMV6 status
  44.
- No PDF/manual binary, card count, package manifest, source reference, routing rule, migration, secret, or deploy file
  changed.
- Focused ED-24SRC.4 guard: PASS, 6/6.
- `dotnet restore .\AssistantEngineer.sln`: PASS.
- `dotnet build .\AssistantEngineer.sln --no-restore`: PASS with 0 warnings and 0 errors.
- EquipmentDiagnostics filter: PASS, 1114/1114.
- Telegram filter: PASS, 640/640.
- EquipmentDiagnosticTelegramWebhookApiIntegrationTests filter: PASS, 10/10.
- Exact full solution suite: PASS, 5138/5138.
- `git diff --check`: PASS.

ED-24SRC.2a corrects the already repaired AJ and GMV6 outdoor sensor batch wording without changing runtime counts.

ED-24SRC.2a implementation notes:

- GMV6 `AJ` visible text now includes the manual's configurable filter-clean interval, keeps the indoor-filter service
  cycle action, and removes section/source wording from `sourceNote`.
- `AJ` metadata is not changed in this stage because `gree-gmv6-status-codes` currently allows `OutdoorUnit` /
  `OutdoorBoard` for the whole status package. Correctly splitting status display/applicability to
  `WiredRemote`/indoor receiver needs a separate package/routing-safe design stage.
- GMV6 `b1`-`bA` visible summaries now include the manual fault display: outdoor main board, indoor-unit wired
  controller, or indoor-unit receiver.
- Installer/Engineer summaries for `b1`-`bA` now use the manual AD-value detection wording while keeping the existing
  connector -> sensor -> detection circuit/main-board troubleshooting flow.
- No PDF/manual binary, card count, package manifest, source reference, routing rule, migration, secret, or deploy file
  changed.
- Focused ED-24SRC.2a guard: PASS, 5/5.
- `dotnet restore .\AssistantEngineer.sln`: PASS.
- `dotnet build .\AssistantEngineer.sln --no-restore`: PASS with 0 warnings and 0 errors.
- EquipmentDiagnostics filter: PASS, 1113/1113 after correcting the receiver wording caught by the existing grammar
  guard.
- Telegram filter: PASS, 640/640.
- EquipmentDiagnosticTelegramWebhookApiIntegrationTests filter: PASS, 10/10.
- Exact full solution suite: PASS, 5137/5137.
- Architecture scripts/tools inventory guard: PASS after adding the ED-24SRC.3 inventory runner to governance inventory.
- `git diff --check`: PASS.

ED-24SRC.3 maps all 263 GMV6 runtime cards for staged closure. The ignored inventory outputs are:

- `artifacts/verification/equipment-diagnostics/gmv6-manual-bound-closure-inventory.json`
- `artifacts/verification/equipment-diagnostics/gmv6-manual-bound-closure-inventory.csv`

ED-24SRC.3 inventory results:

- Counts are unchanged: GMV6 total 263; outdoor 121; indoor 60; debugging 38; status 44; Gree total 1296.
- Repair classes: AlreadyRepaired 11; DetailedProcedureAvailable 83; TableOnlySafe 88; StatusOrPrompt 43;
  DebuggingOrCommissioning 38.
- Category split: outdoor = 10 AlreadyRepaired, 57 DetailedProcedureAvailable, 54 TableOnlySafe; indoor = 26
  DetailedProcedureAvailable, 34 TableOnlySafe; debugging = 38 DebuggingOrCommissioning; status = 1 AlreadyRepaired,
  43 StatusOrPrompt.
- Conflict count: 0. Every GMV6 runtime card has an attached manual/source section or source reference in the current
  runtime data.
- The inventory is a planning map only. It does not rewrite runtime cards and does not mark GMV6 closed.
- Validation: EquipmentDiagnostics filter PASS, 1113/1113; `git diff --check` PASS.
- No PDF/manual binary, runtime card, migration, database model, Telegram callback, routing rule, environment file,
  secret, deploy file, or production state changed.

ED-24SRC.2 repaired the first detailed GMV6 outdoor sensor/procedure batch directly from
`Service Manual for GMV6 v_2020.09.pdf`: `b1`, `b2`, `b3`, `b4`, `b5`, `b6`, `b7`, `b8`, `b9`, and `bA`.
ED-24SRC.1 and ED-24SRC.1b already repaired the critical GMV6 `AJ` / `b1` examples and removed the known generic import
template from 235 GMV6 cards.

ED-24SRC.2 implementation notes:

- GMV6 `AJ` now means a filter-clean prompt and tells the user to clean the indoor-unit filter, reset the prompt, and
  begin the next service cycle.
- GMV6 `b1`-`bA` now identify the correct outdoor sensor fault, the 30-second detection condition, the three documented
  causes, and the connector -> sensor -> detection circuit -> main-board flowchart.
- Telegram-visible diagnostic fields, including `sourceNote`, reject generic import/evidence and provenance phrases.
  Source/provenance data stays in metadata-only fields.
- `invoke-gree-manual-bound-card-audit.ps1` generates JSON and CSV reports under ignored verification artifacts. The
  report contains 1296 rows, the 21-package runtime map, and marks `AJ` plus `b1`-`bA` as repaired in this manual-bound
  scope.
- Runtime counts are unchanged: Gree 1296, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260,
  U-Match R32 107, ERV B Series 5.
- No PDF/manual binary, migration, database model, Telegram callback, routing rule, environment file, secret, or deploy
  file changed.
- Focused ED-24SRC.2 guard: PASS, 5/5.
- `dotnet restore .\AssistantEngineer.sln`: PASS.
- `dotnet build .\AssistantEngineer.sln --no-restore`: PASS with 0 warnings and 0 errors.
- EquipmentDiagnostics filter: PASS, 1113/1113.
- Telegram filter: PASS, 640/640.
- EquipmentDiagnosticTelegramWebhookApiIntegrationTests filter: PASS, 10/10.
- Exact full solution suite: PASS, 5137/5137.
- `git diff --check`: PASS.
- `git status --short`: only the intended ED-24SRC.2 card, package, audit-script, test, docs, and `PROJECT_STATE.md`
  changes.

ED-24SR.3 fixes group-safe Telegram keyboards. Production rejected a service-group response with
`Bad Request: phone number can be requested in private chats only` because a private reply keyboard containing
`request_contact=true` reached a group send.

Implementation commit: current commit (`ED-24SR.3 Fix group-safe Telegram keyboards`).

ED-24SR.3 implementation notes:

- The webhook transport now sanitizes every adapter response using the original Telegram `ChatType`. Group and supergroup
  responses discard reply-keyboard rows and preserve only inline actions; a safe remove-keyboard marker remains allowed.
- The outbound Telegram client adds a second defensive guard for negative group chat ids, covering direct group sends that
  do not pass through the webhook response path. Text, photo, document, video, and edited-message reply markup use it.
- Service request cards, reply/dialog history, user attachment messages, take/assign/close/status actions remain inline
  keyboards. Their callback payloads and behavior are unchanged.
- Private chat reply keyboards are unchanged. `/start`, main-menu, phone registration, and change-phone flows retain
  `request_contact=true` in private chats.
- No database schema, EF migration, runtime diagnostic JSON/card, configuration secret, PDF, or intake artifact changed.
  Runtime remains Gree 1296, U-Match R32 107, ERV B Series 5.
- ED-24SR.1 and ED-24SR.2 remain CLOSED / pushed; their production PASS remains pending.

ED-24SR.3 local validation:

- Targeted webhook/outbound keyboard safety tests: PASS, 48/48.
- `dotnet restore .\AssistantEngineer.sln`: PASS.
- `dotnet build .\AssistantEngineer.sln --no-restore`: PASS with the six existing nullable warnings in architecture guards.
- Focused service-request/Telegram/keyboard/attachment/broadcast/user/manual/Gree tests: PASS, 871/871.
- Gree diagnostics smoke: PASS, 14/14.
- Equipment Diagnostics CI-equivalent filter: PASS, 1108/1108.
- Exact plain full suite executed: 5131/5132; the sole failure is the already recorded unrelated Engineering Workflow
  artifact assertion receiving the configured 256 KiB truncation envelope.
- Full suite with the existing local test-only
  `EngineeringWorkflowPersistence__PayloadLimits__ArtifactContentMaxBytes=10485760` override: PASS, 5132/5132.
  Production configuration was not changed.
- Final EF/OpenAPI/diff/repository-hygiene checks run immediately before commit.

## Previous completed work

ED-24SR.2 adds photo/document/video attachments to the service-request dialog. The stage is CLOSED / local commit pending;
production PASS remains pending until the migration is applied, GitHub Actions are green, and the VPS live-check passes.

Implementation commit: current commit (`ED-24SR.2 Add service request dialog attachments`).

ED-24SR.2 implementation notes:

- Operator and Consumer dialog messages support photo, document, and video with captions. One attachment completes and
  clears an operator pending reply; another message requires pressing `💬 Ответить` again.
- Delivery reuses Telegram `file_id` through `sendPhoto`, `sendDocument`, or `sendVideo`; the application never downloads
  or stores file bytes. `file_unique_id`, filename, MIME type, size, dimensions, and duration are metadata only.
- Service-request dialog media is sent with `protect_content=true` for privacy. Consumer attachments reach the configured
  service group with request context plus `💬 Ответить` / `📜 Диалог`; operator delivery posts a safe group receipt.
- Unsupported voice/audio/video-note/location/animation content receives a clear Russian message and does not crash or
  clear an operator pending reply. Album items are handled independently by normal Telegram updates.
- Disabled/blocked users cannot participate; an operator cannot send an attachment to a blocked requester. Owner, Admin,
  and Engineer retain operator attachment access; Consumer cannot use operator reply callbacks.
- Migration `20260701101337_AddTelegramServiceRequestDialogAttachments` adds
  `TelegramServiceRequestMessageAttachments` as a child of persisted dialog messages.
- ED-24SR.1 remains CLOSED in commit `ce1b79e3`; text routing, persistent pending state, ambiguity selection, dialog
  history, command precedence, callbacks, and lifecycle actions remain unchanged.
- Runtime catalogs are unchanged: Gree 1296, U-Match R32 107, ERV B Series 5. No PDF, manual intake artifact, secret,
  `.env` backup, or runtime catalog change was added.

ED-24SR.2 local validation:

- Dialog-focused text and attachment tests: PASS, 22/22.
- `dotnet restore .\AssistantEngineer.sln`: PASS.
- `dotnet build .\AssistantEngineer.sln --no-restore`: PASS with the six existing nullable warnings in architecture guards.
- Focused Telegram/service-request/attachment/broadcast/user/manual/Gree tests: PASS, 865/865.
- Gree diagnostics smoke: PASS, 14/14.
- Equipment Diagnostics workflow test command: PASS, 1102/1102.
- Full solution suite: PASS, 5126/5126, with a local test-only
  `EngineeringWorkflowPersistence__PayloadLimits__ArtifactContentMaxBytes=10485760` override. Without the override, one
  unrelated Engineering Workflow artifact assertion receives the configured 256 KiB truncation envelope instead of raw
  JSON; the isolated test passes with the larger test limit and no production configuration was changed.
- EF model validation: PASS; no pending changes after
  `20260701101337_AddTelegramServiceRequestDialogAttachments`.
- Equipment Diagnostics branch-readiness tool was executed. Its build/restore and diagnostics checks pass, but the legacy
  narrow-skeleton scope policy reports the explicitly required Infrastructure migrations as forbidden and repeats the
  unrelated 256 KiB artifact-test failure in its internally launched plain full suite.
- Final OpenAPI, repository hygiene, diff, commit, and push checks follow immediately after this state update.

## Previous completed work

ED-24CI.1 and ED-24MAN.4c fix the GitHub restore failure from `Microsoft.OpenApi 2.0.0`, harden the ERV owner repair
script against controller misclassification, and make Gree Controllers file buttons readable. The stage is CLOSED /
pushed; production PASS remains pending until GitHub Actions are green and the VPS live-check passes.

Implementation commit: `945e1164` (`ED-24CI.1 Fix OpenAPI vulnerability and controller labels`).

Package implementation notes:

- Root cause: `Microsoft.AspNetCore.OpenApi 10.0.5` resolved transitive `Microsoft.OpenApi 2.0.0`; with NU1903 treated as
  an error, `dotnet restore .\AssistantEngineer.sln` failed in GitHub Actions, including the OwnershipBackfill tool graph.
- Fix: `AssistantEngineer.Api.csproj` now has a direct `Microsoft.OpenApi 2.7.5` package override. A dependency security
  guard verifies the patched 2.x pin and restored assets reject `Microsoft.OpenApi/2.0.0`.
- `dotnet list .\AssistantEngineer.sln package --include-transitive` resolves `Microsoft.OpenApi 2.7.5`; `2.0.0` is gone.
- ED-24MAN.4c hardens `fix-gree-erv-b-series-owner-manual-bindings.sql`: it still uses production `FileName`/`UpdatedAt`
  columns, avoids broad `%Controller%` matching, corrects
  `Gree ERV B Series Installation Startup Maintenance Manual EN FHBQG-D3.5B-D60B.pdf` as the ERV diagnostic OwnerManual,
  and reclassifies XK46, XE7A, YAP1F/YV1L1, and ERV Wired Controller rows back to Controllers / ControllerGuide.
- Controllers buttons now show readable short labels: `ERV Wired Controller`, `YAP1F / YV1L1`,
  `XE7A-23H / XE7A-23HC`, `XE7A-24H / XE7A-24HC`, and `XK46`; callback payloads remain short id-based values without
  filenames, Telegram file ids, file_unique_id, or sourceReferences.
- ERV diagnostic guide delivery now accepts only the exact ERV B Series Installation Startup Maintenance OwnerManual
  binding and does not offer secondary controller manuals as diagnostic guides.
- U-Match guide buttons still show Cassette/Duct labels, and full filenames remain in message bodies.
- Runtime counts remain unchanged: Gree 1296, U-Match R32 107, ERV B Series 5.
- Manual policies remain unchanged: ServiceManual is library-only, InstallationManual remains hidden from generic visible
  installation menus, and diagnostic guide delivery uses only safe OwnerManual guide bindings.
- GitHub CI-equivalent commands run locally for the failed workflows: `dotnet restore .\AssistantEngineer.sln`,
  `npm --version`, `npm ci --prefix .\src\Frontend`,
  `.\scripts\engineering-core\verify-engineering-core-v1-smoke.ps1`,
  `.\scripts\engineering-core\verify-engineering-core-v1.ps1`, and
  `.\scripts\engineering-core\verify-engineering-core-v1-validation.ps1`.
- EF model validation: no migration was added; no model changes are expected.
- No PDF binaries, manual intake artifacts, certificates, passwords, secrets, `.env` backups, or unrelated files were
  committed.

Package local validation:

- `dotnet restore .\AssistantEngineer.sln`: PASS.
- `dotnet build .\AssistantEngineer.sln --no-restore`: PASS with the known TD-BUILD-001 nullable warnings in architecture
  guard tests only.
- Focused EngineeringCore/Validation/Telegram/ManualLibrary/Gree/UMatch/ERV/User tests: PASS.
- Gree diagnostics smoke: PASS.
- Full solution suite: PASS.
- Engineering Core V1 Smoke CI-equivalent: PASS.
- Engineering Core V1 CI-equivalent: PASS.
- Engineering Core V1 Validation CI-equivalent: PASS.
- `git diff --check`: PASS.
- Production PASS remains pending until GitHub Actions are green and the VPS live-check is completed.

Previous completed work:

ED-24USR.5, ED-24MAN.4b, and ED-24UX.8a add Owner-only Telegram user management, correct the ERV/U-Match
diagnostic-guide UX, and remove duplicated codes from rendered Gree diagnostic titles. The stage is CLOSED / pushed;
production PASS remains pending until the VPS live-check.

Implementation commit: current commit (`ED-24USR.5 Add user management and fix ERV guides`).

Package implementation notes:

- `👥 Пользователи` role lists now contain short user buttons that open a phone-free user card with role, active/blocked
  status, private-chat availability, broadcast reachability, Telegram id, and last activity.
- Owner can change roles among Owner/Admin/Engineer/Installer/Consumer and block or unblock users through confirmation
  screens. Mutations use typed application results and existing safe user audit events.
- Owner cannot block self or change own role; the last Owner cannot be demoted. Stale/direct non-Owner `usr:*` callbacks
  and legacy `au:*` callbacks return a compact denial without user data.
- Blocking uses the existing `TelegramUsers.IsBlocked` field. No EF migration was added.
- Blocked users are denied at the beginning of Telegram adapter handling with `Доступ к боту ограничен.` for commands,
  diagnostics, library, service requests, broadcasts, and callbacks.
- Blocked users remain visible to Owner with status `Заблокирован` and are excluded from broadcast reachability.
- ERV diagnostic-guide selection accepts only active Gree + ERV B Series + OwnerManual +
  `CanUseForDiagnostics = true` bindings. ControllerGuide and Controllers rows are excluded.
- The production repair script binds
  `Gree ERV B Series Installation Startup Maintenance Manual EN FHBQG-D3.5B-D60B.pdf` as the diagnostic OwnerManual,
  disables secondary ERV diagnostic OwnerManual flags, and reclassifies wrongly attached XK46/XE7A rows without
  deleting files.
- U-Match guide buttons now read `Кассетные 3.5-16kW` and `Канальные 3.5-16kW`; full filenames remain in the message
  body and callback payloads remain short opaque tokens.
- Gree title rendering extracts meaning after the code segment, so U-Match E0/H5 and ERV E6/L9 render the code once.
- Runtime counts remain unchanged: Gree 1296, U-Match R32 107, ERV B Series 5; existing GMV counts are unchanged.
- Manual policies remain unchanged: ServiceManual library-only, InstallationManual hidden from generic visible menus,
  and diagnostic guide delivery uses safe OwnerManual bindings.
- No PDF binaries, intake artifacts, certificates, passwords, secrets, or `.env` backups were committed.

Package local validation:

- `dotnet restore .\AssistantEngineer.sln`: PASS.
- `dotnet build .\AssistantEngineer.sln --no-restore`: PASS with 0 warnings and 0 errors.
- Focused Telegram/UserOverview/UserAccess/Broadcast/EquipmentDiagnostics/ManualLibrary/Gree tests: PASS, 1523/1523.
- Gree diagnostics smoke: PASS, 14/14.
- Full solution suite: PASS, 5102/5102.
- EF model validation: PASS; no pending model changes and no new migration.
- `git diff --check`: PASS.
- Production PASS remains pending until the VPS live-check is completed.

ED-24UX.8, ED-24MAN.4a, and ED-24E.5a polish public U-Match/ERV diagnostics, repair OwnerManual diagnostic-guide
bindings, and expand ERV controller diagnostics. The stage is CLOSED / pushed; production PASS is not marked yet because
the VPS live-check is still pending.

Implementation commit: current commit (`ED-24UX.8 Polish U-Match ERV diagnostics and guide bindings`).

Package implementation notes:

- Public U-Match R32 and ERV B Series answers no longer expose source tables, manual sections, document codes, or
  validation wording. Evidence remains in metadata and coverage documents only.
- U-Match E0 now identifies a fan fault and gives direct power, connection, wiring, and free-rotation checks.
- ERV E6 now identifies communication loss between the wired controller and the ERV unit.
- ERV runtime expands from 2 to 5 cards by adding L9, dF, and dH from the wired-controller OwnerManual.
- Runtime counts: Gree 1296, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260, U-Match R32 107,
  ERV B Series 5.
- Existing GMV counts and the U-Match R32 count are unchanged.
- U-Match R32 and ERV B Series OwnerManual uploads now set `CanUseForDiagnostics = true`.
- U-Match with multiple OwnerManual files shows full filenames in the message and short safe callbacks; ERV with one
  OwnerManual sends it directly with `protect_content=true`.
- Manual policies remain unchanged: ServiceManual library-only, InstallationManual hidden from visible library/upload
  menus, diagnostic guide OwnerManual-only.
- Four idempotent PostgreSQL metadata scripts use the real `FileName` and `UpdatedAt` columns and normalize service and
  owner rows for U-Match R32 and ERV B Series.
- No EF migration is expected because the persistence model is unchanged.
- No PDF binaries, intake artifacts, certificates, passwords, or secrets were committed.

Package local validation:

- `dotnet restore .\AssistantEngineer.sln`: PASS.
- `dotnet build .\AssistantEngineer.sln --no-restore`: PASS with 0 errors and the 6 known TD-BUILD-001 nullable warnings
  in architecture guard tests.
- Focused diagnostics/Telegram/manual-library/Gree tests: PASS, 1512/1512.
- Gree diagnostics smoke: PASS, 14/14.
- Full solution suite: PASS, 5091/5091.
- Production PASS remains pending until the VPS live-check is completed.

ED-24OPS.4 adds a controlled PostgreSQL EF migration runner for the main `AppDbContext`; the stage is CLOSED / pushed.
Production PASS is not marked yet because the VPS live-check is still pending.

ED-24OPS.4 implementation notes:

- Added explicit migration-only API entrypoint: `dotnet AssistantEngineer.Api.dll --migrate-database`.
- Added VPS wrapper: `./scripts/deployment/apply-production-migrations.sh`.
- The runner resolves the existing production `ConnectionStrings__DefaultConnection` through normal container
  configuration, lists pending migrations, applies them through `AppDbContext.Database.MigrateAsync()`, and reports the
  final applied/latest migration status.
- The migration command returns exit code 0 on success and non-zero on errors.
- Error output redacts password-like connection string values and does not print the full connection string.
- Normal API startup does not auto-apply migrations; `docker compose up -d --build assistantengineer-api` keeps the
  current migration policy.
- Migration-only mode does not start Kestrel/web server, Telegram polling, Telegram command-menu synchronization, or
  normal API hosted services.
- No EF migration was added because the database model did not change.
- Runtime counts are unchanged: Gree 1184, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260.
- Manual policies are unchanged: ServiceManual library-only, InstallationManual hidden from visible library/upload menus,
  diagnostic guide OwnerManual-only.
- Diagnostic JSON/cards/codes/sourceReferences and routing are unchanged.
- Telegram UX and broadcast behavior are unchanged.
- No PDFs, generated artifacts, certificates, passwords, or secrets were committed.
- Local validation: `dotnet restore .\AssistantEngineer.sln` PASS.
- Local validation: `dotnet build .\AssistantEngineer.sln` PASS with 0 errors; known TD-BUILD-001 nullable warnings
  remain in test guard files.
- Focused regression tests PASS: 1120/1120.
- Gree diagnostics smoke PASS: 14/14.
- Full suite PASS: 5062/5062.
- EF/model validation PASS: no pending model changes and no new EF migration.
- `git diff --check` PASS.
- Production PASS remains pending until the ED-24OPS.4 VPS live-check is completed.

ED-24BCAST.1 adds the Owner-only Telegram text broadcast foundation; the stage is CLOSED / production PASS.

Implementation commit: `2682440f` (`ED-24BCAST.1 Add owner text broadcasts`).

ED-24BCAST.1 implementation notes:

- Owner-only `📣 Рассылка` was added to the Telegram file-library/admin home menu.
- Broadcast callbacks are Owner-only; Admin, Engineer, Installer, and Consumer cannot open broadcast menus or start a
  broadcast.
- Supported audiences are all active reachable users or a single role: Owner, Admin, Engineer, Installer, Consumer.
- The flow is text-only: select audience, enter text, preview, optionally send a test message to self, then confirm send.
- Unsupported media, empty text, command-like text, and overlong text are rejected before a broadcast can be confirmed.
- Recipients are persisted with per-recipient status: Pending, Sent, Skipped, or Failed.
- Skipped recipients cover unavailable private messaging cases such as inactive users, blocked users, missing private chat
  id, or duplicate private chat id.
- Send failures mark only that recipient as Failed, sanitize the error message, and continue with remaining recipients.
- Broadcast text is not placed in callback data, recipient error messages, or diagnostic identifiers.
- New EF migration: `20260630195828_AddTelegramBroadcasts`.
- No PDFs, generated artifacts, certificates, passwords, or secrets were committed.
- Runtime counts are unchanged: Gree 1184, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260.
- Manual policies are unchanged: ServiceManual library-only, InstallationManual hidden from visible library/upload menus,
  diagnostic guide OwnerManual-only.
- Diagnostic JSON/cards/codes/sourceReferences and routing are unchanged.
- Deploy scripts are unchanged.
- Restore: PASS.
- Build: PASS, 0 errors; the known `TD-BUILD-001` nullable warnings in test architecture guard files remain
  non-blocking.
- Focused Broadcast tests: 6/6 passed.
- Focused Telegram/UserOverview/UserAccess/EquipmentDiagnostics/ManualLibrary/Gree tests: 1083/1083 passed.
- Local Gree diagnostics smoke: 14/14 passed.
- Full solution suite: 5055/5055 passed.
- EF migration/model validation: PASS; no pending model changes after `20260630195828_AddTelegramBroadcasts`.
- `git diff --check`: PASS.
- Production live-check: PASS.
- VPS: `assistantengineer-beta-01`; repo: `/opt/assistantengineer`; deploy dir: `/opt/assistantengineer/deploy`.
- Production HEAD: `2682440f`; service: `assistantengineer-api`.
- Broadcast migration `20260630195828_AddTelegramBroadcasts` was applied on the VPS by a manual SQL script because the
  main PostgreSQL `AppDbContext` does not yet have a controlled production auto-migration runner.
- This manual production migration step does not block ED-24BCAST.1 production PASS, but it creates future ops debt:
  `ED-24OPS.4 Add controlled PostgreSQL EF migration runner`.
- Production DB migration history confirms `20260630195828_AddTelegramBroadcasts` with `ProductVersion = 10.0.6`.
- Production DB tables confirmed: `public.TelegramBroadcastCampaigns` and `public.TelegramBroadcastRecipients`.
- Owner audience broadcast live-check: preview rendered, test-to-self sent, confirm completed; final report showed
  Audience Owner, Recipients 1, Sent 1, Skipped 0, Failed 0.
- Engineer audience broadcast live-check: preview rendered and confirm completed; final report showed Audience Engineer,
  Recipients 1, Sent 1, Skipped 0, Failed 0.
- Production DB campaign validation: Campaign 1 Role/Owner Completed with TotalRecipients 1, SentCount 1, SkippedCount 0,
  FailedCount 0; Campaign 2 Role/Engineer Completed with TotalRecipients 1, SentCount 1, SkippedCount 0, FailedCount 0.
- Production DB recipient validation: Owner and Engineer recipient rows have `Status = Sent`, empty `SkipReason`, and
  empty `ErrorCode`.
- Production logs: Telegram command menu synchronized, polling started, startup `deleteWebhook` succeeded, updates were
  processed with `Status: Processed`, callback answers/message edits/responses were sent.
- Production logs contain no `BUTTON_DATA_INVALID`, `OutboundFailed`, `error`, `exception`, or `failed` entries in the
  checked live window.
- Runtime counts remain unchanged: Gree 1184, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260.
- Manual policies remain unchanged: ServiceManual library-only, InstallationManual hidden from visible library/upload
  menus, diagnostic guide OwnerManual-only, protected `sendDocument` remains, and `forwardMessage`/`copyMessage` are not
  used for protected library/manual delivery.
- Diagnostic JSON/cards/codes/sourceReferences, routing, deploy scripts, PDFs/artifacts, and secrets are unchanged.

ED-24USR.4 adds an Owner-only Telegram user overview; the stage is CLOSED / production PASS.

Implementation commit: `700fd4bd` (`ED-24USR.4 Add owner user overview`).

ED-24USR.4 implementation notes:

- Owner-only `👥 Пользователи` was added to the Telegram file-library/admin home menu.
- The overview shows total users, active users, users reachable for future private broadcasts, users unavailable for
  private messages, and counts by role: Owner, Admin, Engineer, Installer, Consumer.
- Role lists are available for Owner/Admin/Engineer/Installer/Consumer with 10-user pagination.
- User list rows show display name, username, TelegramId, private-chat availability, and future-broadcast reachability.
- Phone numbers and contact values are not rendered in the overview or role lists.
- Access is Owner-only: Admin, Engineer, Installer, and Consumer cannot see the `👥 Пользователи` button and stale
  callbacks return a compact denial without leaking user data.
- Callback payloads use short `usr:*` values and do not include names, usernames, phone numbers, TelegramIds, or chat ids.
- Reachability uses existing Telegram user fields only: positive `TelegramChatId` as private chat id, `IsEnabled = true`,
  and `IsBlocked = false`. There is no separate Bot API reachability probe in this stage.
- Broadcast sending is not implemented in ED-24USR.4; the overview is the foundation for ED-24BCAST.1.
- No migration was added.
- Runtime counts are unchanged: Gree 1184, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260.
- Manual policies are unchanged: ServiceManual library-only, InstallationManual hidden from visible library/upload menus,
  diagnostic guide OwnerManual-only.
- Diagnostic JSON/cards/codes/sourceReferences and routing are unchanged.
- Deploy scripts are unchanged.
- No PDFs, generated artifacts, certificates, passwords, or secrets were committed.
- Focused UserOverview tests: 5/5 passed.
- Focused TelegramUser/UserOverview/UserAccess/Telegram tests: 589/589 passed.
- Production live-check: PASS.
- VPS: `assistantengineer-beta-01`; repo: `/opt/assistantengineer`; deploy dir: `/opt/assistantengineer/deploy`.
- Production HEAD: `2682440f`; service: `assistantengineer-api`.
- Owner opened `📚 Библиотека` -> `👥 Пользователи`.
- Visible stats confirmed: total users 6, active 6, reachable for future private broadcast 4, unavailable for private
  messages 2.
- Role counts confirmed: Owner 1, Admin 0, Engineer 1, Installer 0, Consumer 4.
- Owner sees `👥 Пользователи`; role buttons Owner/Admin/Engineer/Installer/Consumer are visible.
- Phone numbers are not shown in the overview screen.
- Owner-only placement under the library/admin area works.
- Runtime counts remain unchanged: Gree 1184, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260.
- Manual policies remain unchanged: ServiceManual library-only, InstallationManual hidden from visible library/upload
  menus, diagnostic guide OwnerManual-only.
- Diagnostic JSON/cards/codes/sourceReferences, routing, deploy scripts, PDFs/artifacts, and secrets are unchanged.

ED-24BRAND.1 polishes visible Telegram-facing branding and help copy; the stage is CLOSED / production PASS.

ED-24BRAND.1 implementation notes:

- `/start` copy now uses the visible bot brand `AEngineer HVAC Service`, includes examples `Gree H5`,
  `Gree GMV6 HR U4`, and `GMV Mini n2`, and lists diagnostics, file library, service request, history, and requests
  actions.
- `/help` copy now starts with `Как пользоваться AEngineer HVAC Service`, explains multi-series selection, mentions the
  `📚 Библиотека файлов`, and lists only public commands: `/history`, `/last`, and `/start`.
- The saved-phone status text was removed from `/start`; `/start` does not claim that a phone number is already saved.
- `/manual_bind` is absent from public `/help`; owner-only manual binding remains protected outside public help copy.
- Telegram-facing branding is aligned with the manually configured names: bot `AEngineer HVAC Service`, username
  `@AEngineerBot`, main group `AEngineer HVAC`, inbox group `AEngineer Inbox`.
- Reply keyboard labels are aligned with the polished visible set: `🔎 Новый код`, `📚 Библиотека`,
  `🛠 Оставить заявку`, `📋 Мои заявки`, `🕘 История`, and `✏️ Изменить номер`.
- User-facing Telegram copy no longer uses old visible branding strings such as `Assistant Engineer`,
  `AssistantEngineer:`, `Assistant Engineer Inbox`, or `Assistant Engineer Service`.
- Manual policies are unchanged: ServiceManual library-only, InstallationManual hidden from visible library/upload menus,
  diagnostic guide OwnerManual-only.
- Manual labels are unchanged: OwnerManual remains `📘 Руководства пользователя`, ServiceManual remains
  `📕 Сервисные мануалы`, and the diagnostic contextual button remains `📘 Руководство`.
- Runtime counts are unchanged: Gree 1184, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260.
- Diagnostic JSON/cards/codes/sourceReferences and routing are unchanged.
- Deploy scripts are unchanged; the only recent deploy script changes remain the ED-24OPS.3 Docker Compose/configuration
  work.
- No migration was added.
- No PDFs, generated artifacts, certificates, passwords, or secrets were committed.
- Restore: PASS.
- Build: PASS, 0 errors; the known `TD-BUILD-001` nullable warnings in test architecture guard files remain
  non-blocking.
- Focused Telegram/manual-library/diagnostics/Gree tests: 1073/1073 passed.
- Local Gree diagnostics smoke: 14/14 passed.
- Full solution suite: 5045/5045 passed.
- `git diff --check`: PASS.
- Production live-check: PASS.
- VPS: `assistantengineer-beta-01`; deploy dir: `/opt/assistantengineer/deploy`.
- `/start` shows `AEngineer HVAC Service`, the HVAC/VRF error-code / manuals / service-request copy, examples
  `Gree H5`, `Gree GMV6 HR U4`, `GMV Mini n2`, and the diagnostics, file-library, service-request, history/request
  capabilities.
- `/start` does not show `Ваш номер уже сохранён`.
- `/help` shows `Как пользоваться AEngineer HVAC Service`, mentions `📚 Библиотека файлов`, lists only public commands
  `/history`, `/last`, and `/start`, and does not show `/manual_bind`.
- Telegram polling started; Telegram updates were processed with `Status: Processed`; sending Telegram responses was
  observed.
- Production logs contain no `OutboundFailed`, `BUTTON_DATA_INVALID`, `error`, `exception`, or `failed` entries in the
  checked window.

ED-24MAN.3a localizes visible Telegram library document labels and hides InstallationManual from visible Telegram
library/upload menus; the stage is CLOSED / production PASS.

ED-24MAN.3a implementation notes:

- Production UX finding after ED-24MAN.3: outdoor document-type menus still showed `📘 Owner Manual` and
  `🛠 Installation Manual`.
- Visible OwnerManual document-type labels now show `📘 Руководства пользователя` in library and `/manual_bind` menus.
- The diagnostic contextual button remains the shorter `📘 Руководство`.
- Visible ServiceManual labels remain `📕 Сервисные мануалы`; the internal enum/database value remains `ServiceManual`.
- Internal enum/database values `OwnerManual` and `InstallationManual` are unchanged.
- Existing manual bindings are unchanged.
- InstallationManual remains an internal/library-only document type, but it is hidden from visible Telegram
  library/upload menus for now.
- Stale InstallationManual callbacks are handled safely: they return the current menu or deny file delivery without
  sending documents.
- Protected `sendDocument(file_id)` delivery is unchanged; `forwardMessage` and `copyMessage` remain unused.
- Manual policies are unchanged: ServiceManual library-only, InstallationManual hidden/library-only, diagnostic guide
  OwnerManual-only, ControllerGuide remains a library category.
- No migration was added.
- Runtime counts are unchanged: Gree 1184, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260.
- Diagnostic JSON/cards/codes/sourceReferences and routing are unchanged.
- Deploy scripts and the ED-24MAN.3 SQL correction script are unchanged.
- No PDFs, generated artifacts, certificates, passwords, or secrets were committed.
- Restore: PASS.
- Build: PASS, 0 errors; the known `TD-BUILD-001` nullable warnings in test architecture guard files remain
  non-blocking.
- Focused Telegram/manual-library/diagnostics/Gree tests: 1073/1073 passed.
- Focused manual-library quick slice: 59/59 passed.
- Local Gree diagnostics smoke: 14/14 passed.
- Full solution suite: 5045/5045 passed.
- `git diff --check`: PASS.
- Production live-check: PASS.
- VPS: `assistantengineer-beta-01`; deploy dir: `/opt/assistantengineer/deploy`.
- Gree -> Наружные -> GMV6 shows `📕 Сервисные мануалы`, `📘 Руководства пользователя`, and `Назад`; it no longer
  shows `📘 Owner Manual` or `🛠 Installation Manual`.
- Manual policies remain unchanged: ServiceManual library-only, InstallationManual hidden from visible library/upload
  menus, diagnostic guide OwnerManual-only.
- Protected `sendDocument(file_id)` delivery remains unchanged; `forwardMessage` and `copyMessage` are not used for
  protected library/manual delivery.
- Telegram polling started; Telegram updates were processed with `Status: Processed`; sending Telegram responses,
  editing Telegram messages, and sending Telegram documents were observed.
- Production logs contain no `BUTTON_DATA_INVALID`, `OutboundFailed`, `error`, `exception`, or `failed` entries in the
  checked window.

ED-24MAN.3 fixes Telegram library generic file callbacks and adds typed Gree Indoor/Controllers categories; the stage is
CLOSED / production PASS.

ED-24MAN.3 implementation notes:

- Root cause: generic Telegram library file-list buttons could build `callback_data` from long/manual-specific payloads,
  creating `BUTTON_DATA_INVALID` risk for Indoor/Controllers file lists.
- Generic library file buttons now use short persisted binding callbacks in the form `lib:f:<bindingId>`; legacy
  `lib:file:` callbacks remain readable for older inline messages.
- File callback handling resolves the current binding by persisted id and re-checks `IsActive`, `IsLibraryVisible`, user
  role/access, and allowed library rules before protected `sendDocument(file_id)` delivery.
- Full filenames remain visible in the numbered message body; inline file buttons use short readable labels and callback
  payloads stay under Telegram's 64-byte limit without filename, Telegram `file_id`, `file_unique_id`,
  `sourceReferences`, or long manual ids.
- Gree Indoor now has explicit typed categories: `Настенные`, `Кассетные`, `Канальные`, and
  `📕 Сервисные мануалы`.
- Gree Controllers now has explicit typed categories: `Настенные` and `Беспроводные ИК`.
- There is intentionally no `Прочее` bucket under Indoor or Controllers; unclassified future files require a new explicit
  category rule before they become visible.
- The visible ServiceManual label is now `📕 Сервисные мануалы`; the internal enum/database value remains
  `ServiceManual`.
- ServiceManual access remains restricted and library-only; InstallationManual remains library-only; diagnostic guide
  delivery remains OwnerManual-only.
- GMV Mini multi OwnerManual selection, GMV6 HR OwnerManual diagnostic delivery, and existing outdoor library menus remain
  covered by regression tests.
- Prepared idempotent production data-correction SQL:
  `scripts/deployment/manual-library/fix-gree-indoor-service-manual-binding.sql`.
- The SQL targets only the known Gree Indoor service manual row
  `Gree_GMV_Indoor_Units_Service_Manual_EN_GC202603_I_1_5_79kW_R410A.pdf`, sets `DocumentType = 'ServiceManual'`,
  `MinRole = 'Engineer'`, `CanUseForDiagnostics = false`, and `IsLibraryVisible = true`, and includes select-before /
  update / select-after checks.
- The SQL correction was executed and verified on production. Final row state: `Id = 14`, `Brand = Gree`,
  `Series = Indoor`, `FileName = Gree_GMV_Indoor_Units_Service_Manual_EN_GC202603_I_1_5_79kW_R410A.pdf`,
  `DocumentType = ServiceManual`, `MinRole = Engineer`, `CanUseForDiagnostics = false`, `IsLibraryVisible = true`,
  `IsActive = true`.
- No migration was added.
- Runtime counts are unchanged: Gree 1184, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260.
- Diagnostic JSON/cards/codes/sourceReferences and routing are unchanged.
- No PDFs, generated artifacts, certificates, passwords, or secrets were committed.
- Restore: PASS.
- Build: PASS, 0 errors; the known `TD-BUILD-001` nullable warnings in test architecture guard files remain
  non-blocking.
- Focused Telegram/manual-library/diagnostics/Gree tests: 1070/1070 passed.
- Focused manual-library quick slice: 56/56 passed.
- Local Gree diagnostics smoke: 14/14 passed.
- Full solution suite: 5042/5042 passed.
- `git diff --check`: PASS.
- Production live-check: PASS.
- VPS: `assistantengineer-beta-01`; deploy dir: `/opt/assistantengineer/deploy`.
- Gree -> Внутренние shows `Настенные`, `Кассетные`, `Канальные`, `📕 Сервисные мануалы`, and `Назад`; no `Прочее`
  bucket was added.
- Gree -> Внутренние -> `📕 Сервисные мануалы` shows the Gree Indoor Units Service Manual, the file button works, and
  no callback error was observed.
- Gree -> Пульты / Controllers shows `Настенные`, `Беспроводные ИК`, and `Назад`; no `Прочее` bucket was added.
- Telegram polling started; Telegram updates were processed with `Status: Processed`; sending Telegram responses,
  editing Telegram messages, and sending Telegram documents were observed.
- Production logs contain no `BUTTON_DATA_INVALID`, `OutboundFailed`, `error`, `exception`, or `failed` entries in the
  checked window.

ED-24OPS.3 persists ASP.NET DataProtection keys outside the API container; the stage is CLOSED / production PASS.

Implementation commit: `1e46ab85` (`ED-24OPS.3 Persist DataProtection keys`).

ED-24OPS.3 implementation notes:

- API startup now explicitly configures DataProtection with the stable application name `AssistantEngineer`.
- `ASSISTANTENGINEER_DATAPROTECTION_KEYS_PATH` selects the key-ring directory; startup creates the directory when it is
  absent.
- Docker Compose uses `/home/app/.aspnet/DataProtection-Keys` and mounts the persistent named volume
  `assistantengineer_dataprotection_keys` there.
- The backend image prepares the mount path with ownership for the non-root `$APP_UID` runtime user.
- Optional certificate-backed key encryption is supported through
  `ASSISTANTENGINEER_DATAPROTECTION_CERTIFICATE_PATH` and
  `ASSISTANTENGINEER_DATAPROTECTION_CERTIFICATE_PASSWORD`; no certificate, password, or secret is committed or logged.
- Missing certificate configuration does not block startup. Optional encryption at rest remains tracked as
  `TD-OPS-002` and is not claimed complete without a production-mounted PFX/certificate.
- Focused tests cover the configured path, directory creation, stable application discriminator, persisted XML key
  generation, optional PFX encryption, and password-free persisted/error output.
- Deployment tests cover the named volume, writable image path, and secret-free environment placeholders.
- Deployment environment documentation includes the path, volume, optional certificate variables, and VPS verification
  commands.
- Technical debt `TD-OPS-001` moved to `Resolved in ED-24OPS.3`; optional certificate hardening remains `TD-OPS-002`.
- No migration was added.
- Runtime counts are unchanged: Gree 1184, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260.
- Manual policy is unchanged: ServiceManual and InstallationManual remain library-only; diagnostic guide delivery remains
  OwnerManual-only.
- Diagnostic JSON/cards/codes/sourceReferences and routing are unchanged.
- Telegram UX and manual bindings are unchanged.
- Deploy scripts changed only as part of the ED-24OPS.3 Docker Compose/configuration work: Dockerfile, Compose scaffold,
  and environment template.
- No PDF, generated artifact, certificate, password, or secret was committed.
- Restore: PASS.
- Build: PASS, 0 warnings / 0 errors.
- Focused DataProtection/deployment/configuration/persistence/diagnostics/Gree/Telegram manual tests: 1155/1155 passed.
- Local Gree diagnostics smoke: 14/14 passed.
- Full solution suite: 5037/5037 passed.
- Docker Compose configuration validation: PASS.
- `git diff --check`: PASS.
- Production live-check: PASS.
- VPS: `assistantengineer-beta-01`; deploy dir: `/opt/assistantengineer/deploy`.
- `assistantengineer-api` rebuilt and restarted successfully.
- DataProtection keys are persisted in Docker volume `assistantengineer_dataprotection_keys`.
- DataProtection key path: `/home/app/.aspnet/DataProtection-Keys`.
- Telegram polling started and the Telegram command menu synchronized.
- Telegram updates were processed successfully; sending Telegram responses and documents was observed.
- Production logs contain no `OutboundFailed`, `error`, `exception`, or `failed` entries in the checked window.
- The DataProtection container-persistence warning no longer appears in the checked production logs.
- The earlier `Telegram polling stopped` line was from container restart and is expected.
- Runtime counts remain unchanged: Gree 1184, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260.
- Manual policy remains unchanged: ServiceManual and InstallationManual are library-only; diagnostic guide delivery
  remains OwnerManual-only.
- Diagnostic JSON/cards/codes/sourceReferences and routing are unchanged.
- Deploy scripts changed only as part of the ED-24OPS.3 Docker Compose/configuration work.
- No migration was added and no PDF/generated artifact/certificate/password/secret was committed.
- Remaining debt: `TD-OPS-002` optional DataProtection key encryption at rest with a production-owned PFX/certificate.

ED-24EF.2 fixed the EF value comparer warning for `HourlySchedule.Factors`; the stage is CLOSED / production PASS.

Implementation commit: `c775f936` (`ED-24EF.2 Fix HourlySchedule value comparer`).

ED-24EF.2 implementation notes:

- `HourlySchedule.Factors` remains the existing required `IReadOnlyList<double>` property persisted as JSON.
- A typed `ValueComparer<IReadOnlyList<double>>` now uses ordered sequence equality, element-based hashing, and a cloned
  `double[]` snapshot.
- Null-aware equality is explicit for EF metadata calls; the domain property itself remains non-nullable.
- Equivalent replacement sequences are not marked modified, while an in-place element change is detected and persisted.
- Model metadata, snapshot independence, hash/equality, SQLite round-trip, equivalent replacement, and element mutation
  are covered by two focused persistence tests.
- No migration is required; `dotnet ef migrations has-pending-model-changes` reports no pending model changes.
- Runtime counts are unchanged: Gree 1184, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260.
- Manual policy is unchanged: ServiceManual and InstallationManual remain library-only; diagnostic guide delivery remains
  OwnerManual-only.
- Diagnostic JSON/cards/codes/sourceReferences and routing are unchanged.
- Telegram UX, manual bindings, and manual library access policy are unchanged.
- Deploy scripts are unchanged.
- No PDF or generated artifacts were committed.
- Technical debt register updated: `HourlySchedule.Factors` moved to `Resolved in ED-24EF.2`.
- Remaining top debt: DataProtection key-ring persistence under `/home/app/.aspnet/DataProtection-Keys` remains a separate
  operations/configuration candidate ED-24OPS.3.
- Restore: PASS.
- Build: PASS, 0 warnings / 0 errors.
- Focused persistence/schedule/calculation-preferences/diagnostics/Gree tests: 1118/1118 passed.
- Local Gree diagnostics smoke: 14/14 passed.
- Full solution suite: 5032/5032 passed.
- `git diff --check`: PASS.
- Production live-check: PASS.
- VPS: `assistantengineer-beta-01`; deploy dir: `/opt/assistantengineer/deploy`.
- `assistantengineer-api` rebuilt and restarted successfully.
- Telegram polling started and the Telegram command menu synchronized.
- Telegram updates were processed successfully; sending Telegram responses and documents was observed.
- Production logs contain no `OutboundFailed`, `error`, `exception`, or `failed` entries.
- The `HourlySchedule.Factors` value comparer warning is absent.
- The ED-24EF.1 database-generated default sentinel warnings remain absent for `RequestedRole`, `DocumentType`, and
  `MinRole`.
- Runtime counts remain unchanged: Gree 1184, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260.
- Manual policy remains unchanged: ServiceManual and InstallationManual are library-only; diagnostic guide delivery
  remains OwnerManual-only.
- Diagnostic JSON/cards/codes/sourceReferences and routing are unchanged.
- Deploy scripts are unchanged.
- No migration was added and no PDF/generated artifacts were committed.
- Remaining unrelated/non-blocking warning: DataProtection keys under `/home/app/.aspnet/DataProtection-Keys` may not
  persist outside the container; this remains the ED-24OPS.3 operations/configuration cleanup candidate.

ED-24EF.1 fixed the known Telegram EF enum sentinel warnings and added a focused technical debt register; the stage is
CLOSED / production PASS.

Implementation commit: `63547146` (`ED-24EF.1 Fix Telegram EF sentinel warnings`).

ED-24EF.1 implementation notes:

- Root cause: `RequestedRole`, `DocumentType`, and `MinRole` had database-generated defaults without an explicit
  out-of-domain sentinel, so valid CLR-zero enum values such as `Owner` and `Unknown` could be treated as "not set".
- `TelegramLibraryAccessRequestEntity.RequestedRole` now uses `(TelegramUserRole)(-1)` as its EF sentinel.
- `TelegramManualBindingEntity.DocumentType` now uses `(TelegramLibraryDocumentType)(-1)` as its EF sentinel.
- `TelegramManualBindingEntity.MinRole` now uses `(TelegramUserRole)(-1)` as its EF sentinel.
- Existing database defaults remain unchanged; all valid enum values are now written explicitly by EF.
- No migration is required: `dotnet ef migrations has-pending-model-changes` reports no pending model changes.
- Existing production rows and model snapshot remain compatible.
- Model metadata tests assert all three explicit sentinel values.
- Persistence round-trip tests cover `RequestedRole = Owner`, ServiceManual, OwnerManual, InstallationManual,
  ControllerGuide, and multiple minimum roles including Owner.
- ServiceManual and InstallationManual remain library-only; diagnostic guide delivery remains OwnerManual-only.
- Mini multiple OwnerManual selection, GMV6 HR OwnerManual delivery, user/admin role behavior, and access requests remain
  covered by the focused regression suite.
- Runtime counts are unchanged: Gree 1184, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260.
- Diagnostic JSON/cards/codes/sourceReferences and routing are unchanged.
- Telegram visible UX and manual binding/access policy are unchanged.
- Deploy scripts are unchanged.
- No PDF or generated artifacts were committed.
- Technical debt register created:
  `docs/technical-debt/assistantengineer-technical-debt-register.md`.
- Highest-priority remaining debt: add an EF value comparer for `HourlySchedule.Factors`; planned follow-ups also cover
  test nullable warnings, Telegram identity duplicate cleanup, manual coverage/exact-family matching, and stale manual docs.
- Restore: PASS.
- Build: PASS, 0 warnings / 0 errors.
- Focused persistence/Telegram/manual/diagnostics/Gree tests: 1108/1108 passed.
- Local Gree diagnostics smoke: 14/14 passed.
- Full solution suite: 5030/5030 passed.
- `git diff --check`: PASS.
- Production live-check: PASS.
- VPS: `assistantengineer-beta-01`; deploy dir: `/opt/assistantengineer/deploy`.
- `assistantengineer-api` rebuilt and restarted successfully.
- Telegram polling started and the Telegram command menu synchronized.
- Telegram updates were processed successfully; sending Telegram responses and documents was observed.
- Production logs contain no `OutboundFailed`, `error`, `exception`, or `failed` entries.
- The previous database-generated default sentinel warnings are absent for
  `TelegramLibraryAccessRequestEntity.RequestedRole`, `TelegramManualBindingEntity.DocumentType`, and
  `TelegramManualBindingEntity.MinRole`.
- Runtime counts remain unchanged: Gree 1184, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260.
- Manual policy remains unchanged: ServiceManual and InstallationManual are library-only; diagnostic guide delivery is
  OwnerManual-only.
- Diagnostic JSON/cards/codes/sourceReferences and routing are unchanged.
- Deploy scripts are unchanged.
- No migration was added and no PDF/generated artifacts were committed.
- Remaining unrelated/non-blocking warnings:
  - The then-known `HourlySchedule.Factors` comparer warning was later resolved in ED-24EF.2.
  - DataProtection keys are stored under `/home/app/.aspnet/DataProtection-Keys` and may not persist outside the
    container; track this as an operations/configuration cleanup candidate.

ED-24UX.7 local implementation notes:

- Generic Gree ambiguity/refinement now uses actual searchable runtime candidates and includes GMV6 HR whenever the
  requested code exists in that series.
- The stable series order is GMV6 HR, GMV6, GMV Mini, GMV X, GMV9 Flex; only matching series are shown.
- The refinement keyboard uses at most two series buttons per row and keeps `Не знаю` available.
- `Gree GMV6 HR n2` and `Gree GMV6 n2` remain separate direct routes; generic `n2` offers both plus GMV Mini and GMV X.
- `docs/equipment-diagnostics/gree-series-code-overlap-audit.md` records 275 unique codes, 263 overlap codes, the
  complete grouped matrix, and representative checks.
- Runtime counts are unchanged: Gree 1184, GMV6 HR 262, GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260.
- No diagnostic cards were added or removed; JSON cards and sourceReferences are unchanged.
- Existing multi OwnerManual selection remains readable, uses callback data below 64 bytes, keeps full filenames in the
  message body, and maps the selected short token to the selected OwnerManual.
- Manual policy is unchanged: ServiceManual and InstallationManual remain library-only; diagnostics remain
  OwnerManual-only.
- No migration was added.
- Deploy scripts and production DB are unchanged.
- No PDF files were committed.
- Restore: PASS.
- Build: PASS, 0 warnings / 0 errors.
- Focused Gree tests: 211/211 passed.
- ED-24UX.7 refinement tests: 16/16 passed.
- Required focused diagnostics/Telegram filter: 1102/1102 passed.
- Local Gree diagnostics smoke: 14/14 passed.
- Full solution suite: 5028/5028 passed.
- `git diff --check`: PASS.
- Production live-check: PASS.
- Generic `n2` refinement includes GMV6 HR.
- Series buttons are compact and use at most two series buttons per row:
  - `GMV6 HR | GMV6`
  - `GMV Mini | GMV X`
  - `Не знаю`
- `Gree GMV6 HR n2` opens the HR-specific card directly.
- Refinement is built from actual runtime candidates.
- ED-24UX.7 made no card/runtime changes; runtime remains 1184.

ED-24E.3 added GMV6 HR diagnostic runtime coverage from local GMV6 HR Service/Owner manuals; the stage is CLOSED / production PASS.

Implementation commit: `37a92ae371cd607bc60e4c692bf1f813dd2890c2` (`ED-24E.3 Add Gree GMV6 HR diagnostics`).

ED-24E.3 local implementation notes:

- Local sources audited: `Gree GMV6 HR Service Manual EN.pdf` and `Gree GMV6 HR Owner Manual EN.pdf`.
- Service SHA256: `CABDC29423A28E846EBC7A9F7DA1EC69002033E8550AFB89D540A3342A49411E`; 22,232,816 bytes; 427 pages.
- Owner SHA256: `2B516736DF5ED4AB0AF4F7407C53F35031122688CA1662BD6ED42BB9675347C5`; 22,595,872 bytes; 76 pages.
- Added separate runtime series `GMV6 HR` under `data/equipment-diagnostics/error-knowledge/gree/gmv6-hr`.
- Added 262 GMV6 HR cards: 60 indoor, 120 outdoor, 38 debugging, 44 status.
- `n2` is sourced from Service Manual troubleshooting section `2.135 "n2"` because it is not present in the Error Indication table.
- New total Gree runtime: 1184 cards.
- Existing counts unchanged: GMV Mini 136, GMV6 263, GMV X 263, GMV9 Flex 260.
- Key HR queries resolve: `Gree GMV6 HR E0`, `U4`, `C2`, `n2`, and `A9`.
- Plain `Gree GMV6 E0` remains ambiguity-safe when HR is applicable and does not return an HR-only answer.
- `docs/equipment-diagnostics/gree-gmv6-hr-manual-coverage.md` records source hashes, section/page coverage, extraction counts, comparison, runtime import summary, key checks, and decision.
- ServiceManual remains library-only and is not diagnostic-visible.
- Diagnostic guide policy remains OwnerManual-only.
- GMV6 HR OwnerManual source was audited locally.
- No migration was added.
- JSON/cards/sourceReferences changed only for the new HR runtime series plus HR manual registry metadata.
- Routing changed only to recognize explicit `GMV6 HR` hints and preserve GMV6/HR separation.
- Deploy scripts are unchanged.
- No PDF files were committed.
- Restore/build/focused validation/smoke/full suite/diff-check: PASS locally for ED-24E.3; full solution suite 5027/5027 passed.
- Production live-check: PASS.
- `Gree GMV6 HR E0`, `Gree GMV6 HR U4`, and `Gree GMV6 HR n2` open GMV6 HR cards.
- GMV6 HR OwnerManual is delivered through `📘 Руководство`.
- ServiceManual is not delivered through diagnostics.
- Production runtime after import: Gree 1184; GMV6 HR 262.
- Existing series counts remain unchanged: GMV6 263, GMV Mini 136, GMV X 263, GMV9 Flex 260.

ED-24MAN.2b fixed diagnostic multi OwnerManual selection for GMV Mini / Slim so long OwnerManual filenames no longer break Telegram inline keyboard payload limits; the stage is CLOSED / production PASS.

Implementation commit: `96cf065e` (`ED-24MAN.2b Fix OwnerManual selection buttons`).

ED-24MAN.2b local implementation notes:

- Diagnostic `📘 Руководство` multiple OwnerManual selection now uses Telegram-safe short button labels such as `1) 8-16kW A-T C-T C-X`, `2) 12-18kW C1-S`, and `3) 22-35kW H C-X C1-X`.
- Long OwnerManual filenames remain visible in the selection message body as a numbered list, so users can distinguish files without oversized button text.
- New diagnostic OwnerManual `callback_data` is short opaque token data (`dm:file:<token>`), stays within 64 bytes, and does not include filename, Telegram `file_id`, `file_unique_id`, sourceReferences, or raw manual id.
- Selected OwnerManual callbacks re-check the latest completed Gree diagnostic context and active OwnerManual diagnostic bindings before protected `sendDocument` delivery.
- Selected OwnerManual protected delivery works for each GMV Mini / Slim OwnerManual option.
- ServiceManual remains library-only and is not diagnostic-visible.
- InstallationManual remains library-only and is not diagnostic-visible.
- Diagnostic guide policy remains OwnerManual-only.
- Existing production Mini OwnerManual bindings from IDs 9/10/11 do not need re-upload.
- No migration was added.
- Runtime remains 922 total Gree cards / 136 GMV Mini cards.
- JSON/cards/codes/sourceReferences/routing are unchanged.
- Manual source data and deploy scripts are unchanged.
- No PDF files were committed.
- Telegram outbound non-success logging now includes sanitized Telegram API status, description, and response body without logging bot token, authorization header, request text, chat id, or full file ids.
- Restore: PASS.
- Build: PASS, 0 warnings / 0 errors.
- Focused Telegram manual/library/user/webhook/outbound tests: 173/173 passed.
- Local Gree diagnostics smoke: 9/9 passed.
- Full solution suite: 5009/5009 passed.
- `git diff --check`: PASS.
- Production live-check: PASS.
- `Gree GMV Mini n2` followed by `📘 Руководство` shows a choice of three OwnerManual files.
- The selection message contains the full list of files.
- Buttons are short and readable: `1) 8-16kW A-T C-T C-X`, `2) 12-18kW C1-S`, and
  `3) 22-35kW H C-X C1-X`.
- All three selected OwnerManual files are delivered correctly.
- `OutboundFailed` is no longer reproducible; the callback payload fix is confirmed by production behavior.
- ServiceManual remains library-only; the diagnostic guide remains OwnerManual-only.

Production validation shared constraints after ED-24MAN.2b, ED-24E.3, and ED-24UX.7:

- Latest VPS log check is clean: no `error`, `exception`, `failed`, or `OutboundFailed`.
- The then-known EF sentinel warnings were unrelated/non-blocking for this production point and were later fixed locally
  in ED-24EF.1.
- No migrations were added.
- No PDF files were committed.
- Deploy scripts are unchanged.
- Access/manual policy is unchanged: ServiceManual and InstallationManual remain library-only; the diagnostic guide
  remains OwnerManual-only.

ED-24MAN.2a added multiple active GMV Mini / Slim OwnerManual support in the Telegram manual library; the stage is local validation PASS and pushed, with production live-check still pending.

Implementation commit: current commit (`ED-24MAN.2a Support multiple GMV Mini owner manuals`).

ED-24MAN.2a local implementation notes:

- `Gree -> Наружные -> GMV Mini / Slim -> 📘 Owner Manual` now supports multiple active PDF bindings by safe title/filename-derived `ManualId`.
- Adding a new GMV Mini / Slim OwnerManual does not deactivate existing GMV Mini / Slim OwnerManual files or the GMV Mini ServiceManual binding.
- Re-uploading the same OwnerManual title/filename asks for replace confirmation; cancel preserves the old file, and confirm replaces only the matching title/filename key.
- Library buckets list all active GMV Mini / Slim OwnerManual files by safe display title/filename; empty buckets still show `Пока файлов нет.`
- Diagnostic `📘 Руководство` remains OwnerManual-only: zero files returns `Руководство пока не добавлено`, one file sends immediately, and multiple files show a safe selection list before protected `sendDocument`.
- ServiceManual, InstallationManual, and ControllerGuide remain library-only and are not sent by diagnostics.
- GMV9 Flex OwnerManual is still unavailable/pending and not required; Flex diagnostics still return `Руководство пока не добавлено` until an OwnerManual is bound.
- Existing `TelegramManualBindings` storage is reused; no parallel storage system and no migration were added.
- Runtime Gree diagnostics remains 922; GMV Mini runtime remains 136.
- JSON/cards/codes/sourceReferences/routing/manual bindings/deploy scripts are unchanged.
- No PDF files were committed.

Previous baseline: ED-24MAN.2 added the structured Telegram manual library tree and minimum manual taxonomy for Gree; the stage is production PASS after VPS live-check on `assistantengineer-beta-01`.

Implementation commit: `7de9c663` (`ED-24MAN.2 Add library tree and manual taxonomy`).

Production live-check point: ED-24OPS.2 (`ec553a8a`), ED-24OPS.2a (`4cf00444`), ED-24OPS.2b (`c44eb2db`), and ED-24MAN.2 (`7de9c663`) are production PASS.

ED-24MAN.2 local implementation notes:

- Telegram library root now shows `Gree`, Owner-only `➕ Добавить файл`, access requests, access management, and cancel.
- `Gree` now contains `Наружные`, `Внутренние`, `Пульты / Controllers`, and `Аксессуары и прочее`; root `Пульты` moved under `Gree`.
- Outdoor product lines are fixed to GMV6, GMV6 HR, GMV Mini / Slim, GMV X, and GMV9 Flex.
- Outdoor product lines expose `📕 Service Manual`, `📘 Owner Manual`, and `🛠 Installation Manual` buckets; empty buckets show `Пока файлов нет.`
- Free sections list uploaded files directly by safe display title/filename with pagination and no nested model tree.
- Minimum taxonomy now includes `ServiceManual`, `OwnerManual`, `InstallationManual`, and `ControllerGuide`.
- Diagnostic guide delivery is `OwnerManual` only; service, installation, and controller documents remain library-only.
- Existing ServiceManual bindings for GMV9 Flex, GMV X, GMV6, and GMV Mini stay visible under their outdoor Service Manual buckets.
- Owner-only upload flow supports brand, section, outdoor product line, document type, PDF validation, confirmation, same-key replacement confirmation, and cancel-preserves-old-binding behavior.
- Protected library `sendDocument` delivery, access re-checks, `protect_content`, no `forwardMessage`, JSON/cards/codes/sourceReferences/routing/manual bindings, and deploy scripts remain unchanged.
- No migration was added for ED-24MAN.2.
- Runtime Gree diagnostics remains 922; GMV Mini runtime remains 136.

ED-24MAN.2 production live-check notes:

- VPS: `assistantengineer-beta-01`; deploy dir: `/opt/assistantengineer/deploy`; service: `assistantengineer-api`.
- Library root visual check PASS: `Gree`, `➕ Добавить файл`, `Запросы доступа`, `Управление доступом`, `Отмена`.
- Owner-only `➕ Добавить файл` visibility PASS.
- Diagnostic OwnerManual-only policy PASS: GMV Mini n2 showed `📘 Руководство`; clicking it did not send ServiceManual and returned `Руководство пока не добавлено`.
- ServiceManual library-only behavior PASS; existing ServiceManual bindings preserved.
- Production logs PASS: Telegram polling active; updates `41767405`-`41767437` processed; sending Telegram response and editing Telegram message observed; no error / exception / failed found.
- The EF sentinel warnings for `TelegramLibraryAccessRequestEntity.RequestedRole`,
  `TelegramManualBindingEntity.DocumentType`, and `TelegramManualBindingEntity.MinRole` were unrelated/non-blocking for
  this production point and were later fixed locally in ED-24EF.1.
- No migration was added; no PDF files committed; JSON/cards/codes/sourceReferences/routing and deploy scripts unchanged.

## Current working point

- ED-24GEC.15.1 - CLOSED / production PASS.
- ED-24QA.1 - CLOSED / pushed.
- ED-24OPS.1 - CLOSED / pushed.
- ED-24UX.4 - CLOSED / pushed.
- ED-24UX.4a - CLOSED / production PASS.
- ED-24UX.5 - CLOSED / pushed.
- ED-24UX.6 - CLOSED / production PASS.
- ED-24SRC.1 - CLOSED / pushed.
- ED-24USR.2 - CLOSED / pushed.
- ED-24SRC.1a - CLOSED / production PASS.
- ED-24USR.3 - CLOSED / production PASS.
- ED-24MAN.1 - CLOSED / production PASS.
- ED-24LIB.1 - CLOSED / pushed.
- ED-24LIB.1a - CLOSED / pushed.
- ED-24LIB.1c - CLOSED / pushed.
- ED-24OPS.2 - CLOSED / production PASS.
- ED-24OPS.2a - CLOSED / production PASS.
- ED-24OPS.2b - CLOSED / production PASS.
- ED-24SRC.2 - CLOSED / pushed.
- ED-24MAN.2 - CLOSED / production PASS.
- ED-24MAN.2a - CLOSED / pushed.
- ED-24MAN.2b - CLOSED / production PASS.
- ED-24E.3 - CLOSED / production PASS.
- ED-24UX.7 - CLOSED / production PASS.
- ED-24EF.1 - CLOSED / production PASS.
- ED-24EF.2 - CLOSED / production PASS.
- ED-24OPS.3 - CLOSED / production PASS.
- ED-24MAN.3 - CLOSED / production PASS.
- ED-24MAN.3a - CLOSED / production PASS.
- ED-24BRAND.1 - CLOSED / production PASS.

## Gree diagnostics runtime status

### GMV6

- Runtime: 263 cards.
- Fresh delta from GMV6 manual GC202203-IV was imported earlier.
- Production smoke passed.

### GMV6 HR

- Runtime: 262 cards.
- Imported from local GMV6 HR service manual in ED-24E.3.
- OwnerManual source audited and production delivery confirmed through `📘 Руководство`.
- Production smoke passed.

### GMV Mini

- Runtime: 136 cards.
- Routing and visible wording stabilized.
- Production smoke passed.

### GMV X

- Runtime: 263 cards.
- Imported from GMV X service manual.
- Visible text encoding issue was fixed.
- Grammar polish completed.
- Production smoke passed.

### GMV9 Flex

- Runtime: 260 cards.
- Imported from GMV9 Flex service manual.
- After ED-24GEC.15.1 visible manual/document references were removed.
- Production smoke passed.

## Current runtime counts

- GMV6: 263
- GMV6 HR: 262
- GMV Mini: 136
- GMV X: 263
- GMV9 Flex: 260
- Total Gree runtime: 1184

## Validation status

ED-24OPS.1 local smoke runner:

`.\scripts\diagnostics\run-gree-diagnostics-smoke.ps1`

ED-24OPS.1 smoke:

9/9 passed

Full baseline after ED-24OPS.1:

4922/4922 passed

Latest validation after ED-24OPS.2:

- Telegram operator inbox added behind `TELEGRAM_OPERATOR_INBOX_ENABLED`, `TELEGRAM_OPERATOR_CHAT_ID`, and `TELEGRAM_OPERATOR_LOG_DIAGNOSTICS`; docker compose, `.env.example`, deployment validators, and environment docs are updated.
- `/operator_chat_id` works only for the linked Owner in a Telegram group/supergroup and reports the chat id needed for `TELEGRAM_OPERATOR_CHAT_ID`.
- User fallback/support messages are mirrored to the configured operator group as safe request cards; normal commands, default diagnostics, and manual/library protected content are not mirrored.
- Media mirroring uses Telegram `copyMessage` only from the user chat to the configured operator group and does not expose `file_id`, `file_unique_id`, source references, or secrets in cards.
- Owner reply bridge sends text replies only when the Owner replies to a mirrored request card/message in the configured operator group; non-owner replies, unknown reply targets, and non-text replies are blocked with safe messages.
- EF migration `20260629193430_AddTelegramOperatorInbox` adds `TelegramOperatorInboxThreads` and `TelegramOperatorInboxMessages` with lookup indexes for operator/user reply routing.
- Library access-request empty state now has a working Back button to the library root.
- Validation: `dotnet restore .\AssistantEngineer.sln` passed; `dotnet build .\AssistantEngineer.sln` passed with 0 warnings/errors; focused Telegram/operator/library/persistence tests passed 787/787; operator-only tests passed 8/8; deployment scaffold validator passed; production-env placeholder validator passed; local Gree diagnostics smoke passed 9/9; full solution test suite passed 4982/4982; `git diff --check` passed.
- Runtime total: 922 confirmed by counting `data/equipment-diagnostics/error-knowledge/gree/**/*.json`.
- Runtime JSON cards, diagnostic codes, source references, and routing unchanged.

Latest production validation after ED-24OPS.2:

- Implementation commit: `ec553a8a` (`ED-24OPS.2 Add Telegram operator inbox`).
- VPS deploy: PASS.
- Production migration apply: PASS; `20260629193430_AddTelegramOperatorInbox` was applied on production.
- Operator env configured:
  - `TELEGRAM_OPERATOR_INBOX_ENABLED=true`
  - `TELEGRAM_OPERATOR_CHAT_ID=-5382766285`
  - `TELEGRAM_OPERATOR_LOG_DIAGNOSTICS=false`
- Operator inbox live-check: PASS.
- User free text mirrored to the operator group.
- Operator card shows display name, username, role, chat id, library access, and message.
- Owner reply bridge: PASS; reply in operator group was delivered to the user as `Ответ специалиста`.
- Operator group confirmation: PASS; bot confirms `Ответ отправлен пользователю`.
- Photo/video/document/voice media mirroring: PASS.
- Library empty access request Back fix: PASS.
- Security preserved: only the configured operator group is used; only Owner can reply through the bridge; Admin does not get operator power by default.
- `forwardMessage` remains unused.
- `copyMessage` remains internal operator media mirroring only.
- Protected library `sendDocument` remains unchanged.
- Service manuals remain library-only.
- Diagnostic Owner/User manual-only policy unchanged.
- Runtime total: 922.
- Runtime JSON cards, diagnostic cards, diagnostic codes, sourceReferences, and routing unchanged.

Latest validation after ED-24OPS.2a:

- Telegram `video_note` is accepted by webhook/polling contracts with Telegram API metadata fields parsed but not logged or exposed.
- Operator inbox classifies the attachment as `VideoNote`; no migration was added because `MessageKind` is stored as a string.
- Private user video notes create/reuse inbox threads, send a safe operator card with `[Видео-кружок]`, and mirror the media to the configured operator group through the existing internal `copyMessage` path.
- Successfully mirrored video notes return `Сообщение передано специалисту.` to the user instead of the old unsupported text/contact fallback.
- Owner text reply bridge works when replying to either the video-note operator card or the copied video-note media message; owner media replies remain unsupported.
- `forwardMessage` remains unused; `copyMessage` remains internal operator media mirroring only; library/manual delivery to users still uses protected `sendDocument(file_id)`.
- Service manuals remain library-only, and the diagnostic Owner/User manual-only policy is unchanged.
- Restore: PASS.
- Build: PASS, 0 warnings / 0 errors.
- Focused Operator/Inbox/Webhook/Telegram/Library/Manual/Persistence tests: 843/843 passed.
- Local Gree diagnostics smoke: 9/9 passed.
- Full solution suite: 4986/4986 passed.
- `git diff --check`: PASS.
- No migration added.
- Runtime total: 922 confirmed by counting `data/equipment-diagnostics/error-knowledge/gree/**/*.json`.
- Runtime JSON cards, diagnostic cards, diagnostic codes, sourceReferences, and routing unchanged.

Latest production validation after ED-24OPS.2a:

- Implementation commit: `4cf00444` (`ED-24OPS.2a Support Telegram video notes`).
- VPS deploy: PASS.
- No migration was added or required for ED-24OPS.2a.
- Telegram `video_note` / "кружочек" production live-check: PASS.
- User `video_note` mirrored to the operator group.
- Operator card shows safe label `[Видео-кружок]`.
- `video_note` copied via the internal `copyMessage` operator-inbox path.
- User receives `Сообщение передано специалисту.`.
- Owner reply to the `video_note` card/media was delivered to the user as `Ответ специалиста`.
- Security preserved: only the configured operator group is used; only Owner can reply through the bridge; Admin does not get operator power by default.
- `forwardMessage` remains unused.
- `copyMessage` remains internal operator media mirroring only.
- Protected library `sendDocument` remains unchanged.
- Service manuals remain library-only.
- Diagnostic Owner/User manual-only policy unchanged.
- Logs clean except known non-blocking EF enum sentinel warnings.
- Runtime total: 922.
- Runtime JSON cards, diagnostic cards, diagnostic codes, sourceReferences, and routing unchanged.

Latest validation after ED-24OPS.2b:

- Owner text/link replies still use `sendMessage` with the `Ответ специалиста:` prefix and preserve the URL text in the delivered user message.
- Owner attachment replies are supported for `document`, `photo`, `video`, `video_note`, `voice`, `audio`, `contact`, `location`, and `animation`.
- Attachment replies are delivered from the configured operator group to the original user with `copyMessage`; `forwardMessage` remains unused.
- `copyMessage` remains limited to internal operator media mirroring and the Owner-to-user operator reply bridge; protected library/manual delivery still uses the existing protected `sendDocument` path.
- Operator replies can target either the request card or copied operator media message.
- Copy failures return `Не удалось отправить вложение пользователю.` to the operator group.
- Unsupported reply types return `Этот тип ответа пока не поддерживается.`.
- Only the configured operator group is accepted, only Owner can reply through the bridge, wrong groups are ignored, and Admin does not gain operator reply power by default.
- `OperatorToUser` messages persist the correct `MessageKind` for text and media replies.
- No migration was added or required because `MessageKind` is persisted as a string.
- Webhook/polling mapping now accepts and flags `audio`, `location`, and `animation` updates in addition to the existing supported media kinds.
- Service manuals remain library-only, and the diagnostic Owner/User manual-only policy is unchanged.
- Restore: PASS.
- Build: PASS, 0 warnings / 0 errors.
- Focused Telegram operator/webhook/persistence tests: 52/52 passed.
- Focused Operator/Inbox/Webhook/Telegram/Library/Manual/Persistence tests: 859/859 passed.
- Local Gree diagnostics smoke: 9/9 passed.
- Full solution suite: 5002/5002 passed.
- `git diff --check`: PASS.
- No migration added.
- Runtime total: 922 confirmed by counting `data/equipment-diagnostics/error-knowledge/gree/**/*.json`.
- Runtime JSON cards, diagnostic cards, diagnostic codes, sourceReferences, and routing unchanged.

Latest production validation after ED-24OPS.2b:

- Implementation commit: `c44eb2db` (`ED-24OPS.2b Support operator media replies`).
- VPS host `assistantengineer-beta-01`, repo `/opt/assistantengineer`, deploy dir `/opt/assistantengineer/deploy`, service `assistantengineer-api`.
- Production live-check: PASS.
- Operator text replies: PASS; delivered to the user as `Ответ специалиста:` followed by the Owner text.
- Operator document/PDF replies: PASS.
- Operator photo replies: PASS.
- Operator `video_note` replies: PASS.
- Operator group confirmation: PASS; bot replies `Ответ отправлен пользователю.`.
- Media replies use the `copyMessage` path: PASS.
- `forwardMessage` is absent from logs and remains unused: PASS.
- Protected library/manual `sendDocument` delivery is unchanged.
- Telegram polling started; private and group updates were processed.
- UpdateId range 41767382-41767385 processed with `Status: Processed`.
- Production logs are clean for this live-check: no error, exception, or failed entries were found.
- No migration was added for ED-24OPS.2b.
- Runtime total remains 922.
- Runtime JSON cards, diagnostic cards, diagnostic codes, sourceReferences, routing, manual bindings, and deploy scripts unchanged.
- EF enum default/sentinel warnings were observed for `TelegramLibraryAccessRequestEntity.RequestedRole`,
  `TelegramManualBindingEntity.DocumentType`, and `TelegramManualBindingEntity.MinRole`; they were unrelated/non-blocking
  for ED-24OPS.2b and were later fixed locally in ED-24EF.1.

Latest validation after ED-24SRC.2:

- Audited local source PDF: `artifacts/manual-intake/sources/gree/Gree GMV Mini Slim Side Outlet Service Manual EN Rev S.pdf`.
- PDF SHA256: `E42C5BE4BAE5D74ECE380BB7C1D83FAD16639171918B153E7B8ADCA5602DAAF1`.
- PDF size: 51,164,839 bytes; page count: 176.
- PDF is a local source artifact and was not added to git.
- Report path: `docs/equipment-diagnostics/gree-gmv-mini-slim-manual-coverage.md`.
- Decision: `PASS WITH NOTES`.
- Manual selected as a broad source candidate for `Gree GMV Mini/Slim`, not as an exact model-family split.
- Manual identity: `DC INVERTER VRF SYSTEM (R410A)`, document code `GC202510-XIX`, local filename suffix `Rev S`.
- Manual model coverage: 32 product rows, 29 unique model names, 8.0-33.5 kW title-page capacity range.
- Manual-derived audit extraction: 202 context occurrences, 159 unique normalized codes.
- Current GMV Mini runtime: 136 cards / 136 unique codes.
- Runtime breakdown: 27 indoor/controller, 62 outdoor/protection, 47 status/debug/function cards.
- Primary display-table misses in runtime: 0.
- Extra runtime codes not found in this PDF: 0.
- Manual-only context/function/debug values not represented as runtime cards: 23 (`00`, `09`, `10`, `12`, `15`, `16`, `17`, `AC`, `n3`, `n5`, `nL`, `nU`, `OC`, `OF`, `PA`, `q7`, `q8`, `q9`, `qd`, `qF`, `qL`, `qn`, `qU`).
- Blocking conflicts: 0.
- Non-blocking duplicate/context notes: `C0`, `AJ`, `db`, `n2`, `nH`, `nC`, `nA`, and `nF` appear in more than one manual context.
- Key checks `n2`, `C0`, `AJ`, `db`, `L0-L9`, `d1-dE` where present, `E0-E4`, `A0`, `A9`, `nH`, `nC`, `nA`, and `Ed` were covered honestly in the report.
- Telegram binding was not performed in this stage.
- Gree GMV Mini production binding remains pending / not bound unless an operator binds it manually later.
- No diagnostic JSON/cards/routing/sourceReferences/manual bindings/deploy scripts changed.
- Runtime total remains 922.
- GMV Mini runtime remains 136 cards.

Latest validation after ED-24UX.4:

- Local Gree diagnostics smoke: 9/9 passed.
- Full solution baseline: 4922/4922 passed.
- Runtime total: 922.
- Runtime JSON cards, diagnostic codes, source references, and routing unchanged.
- Telegram formatting remains plain text (`ParseMode: null`); Gree technical answers now use structured headings, meaning, first checks, and series sections.

Latest validation after ED-24UX.4a:

- Commit: `e24ae712`.
- VPS deploy to `assistantengineer-beta-01`: PASS.
- Telegram live-review: PASS.
- Telegram polling logs: clean; updates 41767183-41767187 were processed successfully with no provided error, exception, failed, or polling-batch-failed entries.
- Gree technical diagnostic answers, n2 ambiguity, and explicit-series not-found use safe escaped HTML with `ParseMode: HTML`.
- The service-request button is `🛠 Оставить заявку`; the previous label remains accepted as a legacy input alias.
- The repetitive `Дальше:` block is absent from found Gree Telegram answers.
- `Ограничения вывода:` is replaced by `Ограничения:`.
- Local Gree diagnostics smoke: 9/9 passed.
- EquipmentDiagnostics tests: 940/940 passed.
- Full solution baseline: 4924/4924 passed.
- Runtime total: 922.
- Runtime JSON cards, diagnostic codes, source references, and routing unchanged.

Latest validation after ED-24UX.5:

- Found Gree Telegram answers are shorter and retain at most three focused checks.
- Separate `Техническая заметка:`, `Ограничения:`, and `Дальше:` blocks are absent.
- A single short `Важно:` block preserves the one-code, protection-bypass, power-circuit, refrigerant-circuit, and qualified-specialist safety boundaries.
- Safe escaped HTML and the existing narrow `ParseMode: HTML` scope are unchanged.
- Local Gree diagnostics smoke: 9/9 passed.
- EquipmentDiagnostics tests: 940/940 passed.
- Full solution baseline: 4924/4924 passed.
- Runtime total: 922.
- Runtime JSON cards, diagnostic codes, source references, and routing unchanged.

Latest validation after ED-24UX.6:

- Implementation commit: `d8fdc3d1`.
- Project-state hash update commit: `5d944e12`.
- VPS deploy to `assistantengineer-beta-01`: PASS.
- Telegram live-review: PASS.
- Telegram polling logs: clean; updates 41767191-41767193 were processed successfully with `Status: Processed` and no provided error, exception, failed, or polling-batch-failed entries.
- Found Gree `Что проверить:` sections use three short, non-duplicating bullets.
- Fault/protection-style answers confirm code, series, and indication location, then separate model and occurrence-context checks.
- Status/service-function answers confirm code, signal category, and display location, then separate model, settings, and related-message checks.
- Grouped answers retain a neutral reference to the service procedure in the applicable-series manual.
- Separate `Техническая заметка:`, `Ограничения:`, and `Дальше:` blocks remain absent.
- The compact `Важно:` safety block, safe HTML escaping, and narrow `ParseMode: HTML` scope are unchanged.
- The `🛠 Оставить заявку` button remains in place.
- Restore: PASS.
- Build: PASS, 0 warnings / 0 errors.
- Focused formatter/smoke tests: 40/40 passed.
- Local Gree diagnostics smoke: 9/9 passed.
- EquipmentDiagnostics tests: 940/940 passed.
- Full solution baseline: 4924/4924 passed.
- `git diff --check`: PASS.
- Runtime total: 922.
- Runtime JSON cards, diagnostic codes, source references, and routing unchanged.

Latest validation after ED-24SRC.1:

- Concrete found Gree diagnostics show `📄 Мануал` only to Installer, Engineer, Admin, and Owner roles.
- Consumer users do not see the action; manually submitted text/callback actions are denied before manual metadata is resolved.
- Not-found, ambiguity, non-Gree, and non-concrete diagnostic states do not expose the contextual manual action.
- The action uses the existing latest completed diagnostic history and requires the same manufacturer, concrete series, and code.
- Existing reviewed Telegram `file_id` bindings are delivered through `sendDocument`.
- `copyMessage` remains reserved for future reviewed source chat/message metadata; no fake identifiers were added.
- `forwardMessage` is intentionally not used.
- Missing bindings return `Мануал пока не привязан` without titles, source references, document codes, file IDs, or storage identifiers.
- Existing `/last`, history, service-request buttons, `📘 Руководства`, and manual registration flows remain intact.
- Restore: PASS.
- Build: PASS, 6 existing nullable warnings in unrelated architecture tests / 0 errors.
- Focused manual-library tests: 28/28 passed.
- EquipmentDiagnostics tests: 949/949 passed.
- Local Gree diagnostics smoke: 9/9 passed.
- Full solution baseline: 4933/4933 passed.
- `git diff --check`: PASS.
- Runtime total: 922.
- Runtime JSON cards, diagnostic codes, source references, and routing unchanged.

Latest validation after ED-24USR.2:

- Telegram admin callback actor resolution now checks Telegram user id first, then safely falls back to the private chat stored user record when identity was not backfilled yet.
- Private chat fallback backfills Telegram identity details through the existing user store path.
- Owner/Admin user-card callbacks from `/admin_users`, including `Открыть: <user>`, should no longer fall into `Нет доступа` when the manager record was created by chat id/bootstrap with missing `TelegramUserId`.
- Duplicate Telegram identity risk is covered: if `TelegramUserId` lookup finds a non-manager duplicate, a private-chat Owner/Admin record remains authoritative for that private callback.
- Group callbacks do not inherit permissions from group chat id fallback.
- ED-24SRC.1 manual access gating is preserved: focused `EquipmentDiagnosticTelegramManualLibraryTests` passed as part of validation.
- Restore: PASS.
- Build: PASS, 6 existing nullable warnings in unrelated architecture tests / 0 errors.
- Focused Telegram admin/manual/adapter tests: 131/131 passed.
- Local Gree diagnostics smoke: 9/9 passed.
- Full solution baseline: 4936/4936 passed.
- `git diff --check`: PASS.
- Runtime total: 922.
- Runtime JSON cards, diagnostic codes, source references, and routing unchanged.

Latest production validation after ED-24SRC.1a:

- Implementation commit: `4231cb9de4a9ed760e399f2defa696ec4342266f`.
- Project-state commit before production pass: `17150363`.
- VPS deploy to `assistantengineer-beta-01`: PASS.
- Telegram live-review: PASS.
- Consumer manual gate: PASS; after `Gree GMV9 Flex E0`, diagnostics are shown without `📄 Мануал` and without `📘 Руководства`.
- Technical manual button: PASS; after `Gree GMV9 Flex E0`, diagnostics are shown with contextual `📄 Мануал` and without `📘 Руководства`.
- Manual not-linked fallback keyboard retention: PASS; pressing `📄 Мануал` shows `Мануал пока не привязан`, includes `Gree GMV9 Flex / E0`, keeps contextual `📄 Мануал`, and does not restore `📘 Руководства`.
- Telegram reply keyboard no longer exposes the global `📘 Руководства` button.
- `📄 Мануал` remains the only contextual manual action.
- Compact keyboard layout confirmed in production:
  - Consumer rows: `🔎 Новый код` / `📋 История`, then `🛠 Оставить заявку` / `📄 Мои заявки`.
  - Technical rows: `🔎 Новый код` / `📄 Мануал`, then `📋 История` / `🛠 Оставить заявку`, then `📄 Мои заявки`.
- Polling logs: clean; container logs showed command menu sync, polling start, successful `deleteWebhook`, `Sending Telegram response`, processed updates, and `Status: Processed` with no `error`, `exception`, or `failed` entries.
- ED-24SRC.1 manual access gating is preserved: consumers are still denied, technical roles retain contextual access only.
- ED-24USR.2 production behavior was confirmed during the same live-check through role switching/admin UI.
- Runtime total: 922.
- Runtime JSON cards, diagnostic cards, diagnostic codes, sourceReferences, and routing unchanged.

Latest local validation after ED-24SRC.1a:

- Implementation commit: `4231cb9de4a9ed760e399f2defa696ec4342266f`.
- Telegram reply keyboard no longer exposes the global `📘 Руководства` button.
- Technical concrete found Gree diagnostics now show only contextual `📄 Мануал` with compact rows: `🔎 Новый код` / `📄 Мануал`, then `📋 История` / `🛠 Оставить заявку`, then `📄 Мои заявки`.
- Consumer concrete found diagnostics use compact rows: `🔎 Новый код` / `📋 История`, then `🛠 Оставить заявку` / `📄 Мои заявки`; no manual actions or phone-row are shown on the diagnostic answer.
- Manual-not-linked replies preserve the contextual `📄 Мануал` keyboard while the last concrete diagnostic context remains valid.
- ED-24SRC.1 manual access gating is preserved: consumers are still denied, technical roles retain contextual access only.
- Restore: PASS.
- Build: PASS, 0 warnings / 0 errors.
- Focused Telegram manual/adapter/admin tests: 133/133 passed.
- Local Gree diagnostics smoke: 9/9 passed.
- Full solution baseline: 4938/4938 passed.
- `git diff --check`: PASS.
- Runtime total: 922.
- Runtime JSON cards, diagnostic cards, diagnostic codes, sourceReferences, and routing unchanged.

Latest validation after ED-24USR.3:

- Implementation commit: `a33ea0ea`.
- Project-state commit before production pass: `4055d1ff`.
- Telegram user roles/access state now use the existing persistent EF Core `TelegramUsers` store in production/default infrastructure DI (`ITelegramUserStore` -> `EfTelegramUserStore`).
- Existing migrations cover the persistent state: `20260617062738_AddTelegramUsers` and `20260617120000_AddTelegramUserPhoneSource`.
- Persisted fields include role, enabled/blocked flags, Telegram identity fields, phone state/source, `LastSeenAt`, and `LastAccessDeniedAt`.
- Bootstrap owner by chat id remains supported and `GetOrCreateConsumerAsync` does not downgrade existing Owner/Admin/Engineer/Installer records.
- Duplicate Telegram identity handling is deterministic: active unblocked manager records are selected before consumer duplicates, preserving ED-24USR.2 admin callback actor resolution.
- Private-chat Owner/Admin fallback remains authoritative when a Telegram user id lookup hits a non-manager duplicate; group callbacks still do not inherit chat-id fallback permissions.
- ED-24SRC.1/ED-24SRC.1a manual access gating is preserved: consumers remain denied for `рџ“„ РњР°РЅСѓР°Р»`, technical roles retain the contextual manual action, and `рџ“ Р СѓРєРѕРІРѕРґСЃС‚РІР°` does not return.
- Existing in-memory role assignments from older container lifetimes do not auto-migrate; any missing production roles need one-time admin assignment and then persist in the database.
- Restore: PASS.
- Build: PASS, 0 warnings / 0 errors.
- Focused Telegram/admin/manual/adapter tests: 442/442 passed.
- Migration/DI validator slice: 28/28 passed.
- Local Gree diagnostics smoke: 9/9 passed.
- Full solution baseline: 4948/4948 passed.
- `git diff --check`: PASS.
- Runtime total: 922.
- Runtime JSON cards, diagnostic cards, diagnostic codes, sourceReferences, routing, manual bindings, and deployment scripts unchanged.

Latest production validation after ED-24USR.3:

- Implementation commit: `a33ea0ea`.
- Previous project-state commit: `4055d1ff`.
- VPS deploy to `assistantengineer-beta-01`: PASS.
- Service/container `assistantengineer-api`: PASS; container restarted and application started successfully.
- PostgreSQL health: PASS.
- Telegram polling startup: PASS.
- Restart persistence check: PASS; technical role persisted after container restart/redeploy.
- Roles persistence after restart: PASS; the technical role still sees the contextual manual action after `Gree GMV9 Flex E0`.
- Manual gate after restart: PASS; consumers still do not see the contextual manual action, technical roles do, and the global guides action did not return.
- Compact Telegram keyboard layout remained confirmed after restart.
- Telegram polling logs: clean; observed `Telegram polling started`, `Application started`, `Telegram polling update processed`, and `Status: Processed` with no `error`, `exception`, or `failed` entries.
- Existing EF warning `HourlySchedule.Factors ... value converter but with no value comparer` was observed; it is unrelated to Telegram user persistence and does not block ED-24USR.3.
- Runtime total: 922.
- Runtime JSON cards, diagnostic cards, diagnostic codes, sourceReferences, routing, manual bindings, and deployment scripts unchanged.

Latest validation after ED-24LIB.1:

- ED-24LIB.1 status: CLOSED / pushed.
- Protected Telegram file library foundation added on top of existing `TelegramManualBindings`.
- Migration added: `20260629165833_AddTelegramFileLibrary`.
- `TelegramManualBindings` were extended with library/document classification fields for title, document type, minimum role, library visibility, and diagnostic eligibility.
- New persistent library access storage added through `TelegramLibraryAccessGrants` and `TelegramLibraryAccessRequests`.
- Owner has implicit full library access and is the only role that manages library grants/requests.
- Admin does not manage the library by default and needs an explicit Owner grant to use the library.
- Engineer and Installer library entry is both role-gated and Owner-grant-gated; Consumer users do not get library access.
- The `Library` button is shown only to active, unblocked users who satisfy role and grant requirements, while Owner always sees it.
- Every library action and callback re-checks role, active/blocked state, and library access grant before listing or sending files.
- Service manuals are library-only.
- Diagnostic context now only allows safe `OwnerManual` / `UserGuide` documents marked for diagnostics.
- Existing service manual bindings do not bypass the diagnostic policy and no longer satisfy diagnostic manual delivery by themselves.
- Protected delivery through `sendDocument(file_id)` with protected content is preserved.
- `forwardMessage` and `copyMessage` are not used.
- `/manual_bind` remains the file registration path and now creates service/library-only bindings by default.
- Restore: PASS.
- Build: PASS.
- Focused Telegram manual/library/user/persistence tests: PASS, 752/752 passed.
- Migration/DI/persistence validator slice: PASS.
- Local Gree diagnostics smoke: PASS, 9/9 passed.
- Full solution suite: PASS.
- `git diff --check`: PASS.
- Runtime total: 922.
- Runtime JSON cards, diagnostic cards, diagnostic codes, sourceReferences, and routing unchanged.

Latest validation after ED-24LIB.1a:

- ED-24LIB.1a status: CLOSED / pushed.
- Fixed fresh Telegram library callback stale handling for library navigation, Gree, remotes, access management, access requests, back/cancel, and repeated library opens.
- Stable library navigation callbacks no longer depend on short-lived ephemeral state; the stale-action response is reserved for truly unknown or invalid callback payloads.
- Access request lists now show requester display name, username when available, role, and chat id instead of only `chat <id>`.
- Owner approve/reject actions notify the requester in private chat through bot `sendMessage`.
- Approve sends a refreshed main reply keyboard with the Library entry when the requester still has valid role, enabled/unblocked state, and active grant.
- Reject keeps the requester without the Library entry unless another active grant already allows access.
- Revoke/grant management paths refresh the target user's keyboard when a notification can be delivered.
- Owner-only approval is preserved; Admin cannot approve/reject or manage library access by default.
- Library actions still re-check user existence, active/enabled state, blocked state, role, Owner implicit access, explicit non-owner grant, file `MinRole`, `IsActive`, and `IsLibraryVisible`.
- Service manuals remain library-only.
- Diagnostic context remains limited to `OwnerManual` / `UserGuide` documents marked for diagnostics.
- Existing service manual bindings still do not bypass diagnostic policy.
- Protected delivery through `sendDocument(file_id)` with protected content is preserved.
- `forwardMessage` and `copyMessage` remain unused.
- No migration was added for ED-24LIB.1a.
- EF enum default/sentinel warnings for library/manual enum fields remain non-blocking known technical debt; revisit before OwnerManual upload/taxonomy flow.
- Restore: PASS.
- Build: PASS, 0 warnings / 0 errors.
- Focused manual-library tests: PASS, 45/45 passed.
- Focused Telegram manual/library/user/persistence tests: PASS, 756/756 passed.
- Local Gree diagnostics smoke: PASS, 9/9 passed.
- Full solution suite: PASS, 4970/4970 passed.
- `git diff --check`: PASS.
- Runtime total: 922.
- Runtime JSON cards, diagnostic cards, diagnostic codes, sourceReferences, and routing unchanged.

Latest validation after ED-24LIB.1c:

- ED-24LIB.1c status: CLOSED / pushed.
- Telegram library callback navigation now edits the current inline message instead of creating a new text message for normal navigation.
- Initial library open from `/library` or the reply keyboard still sends one new message with the inline library menu.
- Owner and granted Engineer navigation through Gree, remotes, access requests, access management, back, cancel, repeated callbacks, file list, and empty sections no longer creates extra navigation sendMessages.
- File callbacks edit the current library message with a short sending status and then send the PDF/document separately through protected `sendDocument(file_id)`.
- Access request approve/reject/grant/revoke notifications still use separate user notifications where needed.
- Owner-only access management is preserved.
- Admin still cannot manage library access by default.
- Service manuals remain library-only.
- Diagnostic Owner/User manual-only policy is preserved.
- `forwardMessage` and `copyMessage` remain unused.
- No migration was added for ED-24LIB.1c.
- EF enum default/sentinel warnings remain non-blocking known debt.
- Restore: PASS.
- Build: PASS, 0 warnings / 0 errors.
- Focused Telegram manual/library/user/persistence tests: PASS, 759/759 passed.
- Focused library/webhook edit-message tests: PASS, 87/87 passed.
- Local Gree diagnostics smoke: PASS, 9/9 passed.
- Full solution suite: PASS, 4973/4973 passed.
- `git diff --check`: PASS.
- Runtime total: 922.
- Runtime JSON cards, diagnostic cards, diagnostic codes, sourceReferences, and routing unchanged.

Latest validation after ED-24MAN.1:

- Implementation commit: `8a3edb6a`.
- ED-24MAN.1 status: CLOSED / production PASS.
- Admin/Owner `/manual_bind` flow added: choose Gree series, send PDF document to the bot, validate filename/series, confirm bind, and explicitly confirm replacement for an existing active series binding.
- Supported production series bindings: Gree GMV6, Gree GMV Mini, Gree GMV X, and Gree GMV9 Flex.
- Production manual bindings use existing EF Core persistence through `TelegramManualBindings` and migration `20260629042754_AddTelegramManualBindings`.
- Stored binding metadata includes Telegram `file_id`, optional `file_unique_id`, safe filename, content type, file size, uploader Telegram user/chat ids, registered role, source, timestamps, and active state.
- No local PDF archive/storage was added; real manual binaries and real Telegram file ids remain out of source control.
- Diagnostic `📄 Мануал` delivery now resolves the latest completed Gree diagnostic series and sends the bound document with `sendDocument(file_id)` and `protect_content=true`.
- `forwardMessage` and `copyMessage` are not used.
- Consumers remain denied; Installer/Engineer/Admin/Owner can receive contextual diagnostic manuals when a binding exists.
- Missing binding fallback remains `Мануал пока не привязан` and preserves the contextual compact keyboard.
- ED-24SRC.1/ED-24SRC.1a manual access gating is preserved.
- ED-24USR.2/ED-24USR.3 Telegram admin identity and persistent role behavior are preserved.
- Restore: PASS.
- Build: PASS, 0 warnings / 0 errors.
- Focused manual/Telegram/user tests: 679/679 passed.
- Migration/DI persistence slice: 8/8 passed.
- Local Gree diagnostics smoke: 9/9 passed.
- Runtime count baseline: 922 confirmed.
- Full solution baseline: 4959/4959 passed.
- `git diff --check`: PASS.
- Runtime JSON cards, diagnostic cards, diagnostic codes, sourceReferences, routing, manual bindings data, and deploy scripts unchanged.

Latest production validation after ED-24MAN.1:

- Implementation commit: `8a3edb6a`.
- Previous project-state commit: `6fbf2685`.
- ED-24MAN.1 status: CLOSED / production PASS.
- VPS deploy to `assistantengineer-beta-01`: PASS; ED-24MAN.1 was pulled on the VPS.
- Initial production `/manual_bind` manual check failed because the new `TelegramManualBindings` table was missing in PostgreSQL.
- Production migration apply: PASS; migration `20260629042754_AddTelegramManualBindings` was applied manually with SQL generated from the EF migration because `dotnet` / `dotnet ef` are not available on the VPS.
- `__EFMigrationsHistory` contains `20260629042754_AddTelegramManualBindings`.
- `TelegramManualBindings` table exists in production PostgreSQL.
- Manual binding flow: PASS; `/manual_bind` worked in Telegram, Gree GMV9 Flex PDF was accepted, the `Привязать` confirmation worked, and the production DB binding was created.
- Gree GMV9 Flex binding DB-confirmed: Brand `Gree`, Series `GMV9 Flex`, FileName `Gree GMV9 Flex Service Manual EN Rev B.pdf`, IsActive `true`.
- Protected document delivery: PASS; after `Gree GMV9 Flex E0`, pressing `📄 Мануал` sent the stored PDF through Telegram document delivery.
- Consumer gate: PASS; consumer live-check confirmed `📄 Мануал` is not shown.
- Global guides action remained removed: `📘 Руководства` did not return.
- Telegram logs after the fix showed processed updates and Telegram document sending without new blocking errors.
- Current production manual bindings:
  - Gree GMV9 Flex - confirmed DB / delivered.
  - Gree GMV X - added through the live bind workflow per operator action.
  - Gree GMV6 - added through the live bind workflow per operator action.
  - Gree GMV Mini - pending / not bound.
- Uploaded `product_paper_manual_130090867.pdf` is an Owner's Manual for the Gree GMV DC Inverter VRF G-X branch (`GMV-224WM/G-X` ... `GMV-2720WM/G-X`) and is kept only for future analysis; it is not bound in Telegram.
- Owner/service split is not implemented.
- `ManualKind` / `Audience` are not implemented.
- Regional GMV6 EU H/H1 manual remains untouched and was not bound instead of the current G-X manual.
- Runtime total: 922.
- Runtime JSON cards, diagnostic cards, diagnostic codes, sourceReferences, routing, manual binding logic, and deploy scripts unchanged.

Latest stable production point:

- ED-24BRAND.1 - production PASS.
- ED-24MAN.3a - production PASS.
- ED-24MAN.3 - production PASS.
- ED-24OPS.3 - production PASS.
- ED-24EF.2 - production PASS.
- ED-24EF.1 - production PASS.
- ED-24UX.7 - production PASS.
- ED-24E.3 - production PASS.
- ED-24MAN.2b - production PASS.
- ED-24OPS.2a - production PASS.
- ED-24OPS.2 - production PASS.
- ED-24MAN.1 - production PASS.
- ED-24USR.3 - production PASS.
- ED-24SRC.1a - production PASS.

Latest pushed local point:

- ED-24BRAND.1 - Telegram-facing branding and help copy validated locally, pushed, and production-confirmed.
- ED-24MAN.3a - Telegram library OwnerManual label localization and InstallationManual hiding validated locally, pushed,
  and production-confirmed.
- ED-24MAN.3 - Telegram library callback safety, typed Gree Indoor/Controllers categories, and the Gree Indoor
  ServiceManual metadata correction validated locally, pushed, and production-confirmed.
- ED-24OPS.3 - DataProtection key persistence validated locally, pushed, and production-confirmed.
- ED-24EF.2 - HourlySchedule value comparer validated locally, pushed, and production-confirmed.
- ED-24EF.1 - Telegram EF enum sentinels validated locally, pushed, and production-confirmed.
- ED-24UX.7 - Gree runtime-based series refinement and compact keyboard validated, pushed, and production-confirmed.
- ED-24E.3 - GMV6 HR diagnostics validated, pushed, and production-confirmed.
- ED-24MAN.2b - OwnerManual selection callbacks validated, pushed, and production-confirmed.
- ED-24OPS.2a - Telegram video notes in operator inbox validated locally, pushed, and production-confirmed.
- ED-24OPS.2 - Telegram operator inbox validated locally, pushed, and production-confirmed.
- ED-24LIB.1c - Telegram library callback navigation edits the current inline message, validated locally and pushed.
- ED-24LIB.1a - Telegram library callback freshness and access UX validated locally and pushed.
- ED-24LIB.1 - protected Telegram file library foundation validated locally and pushed.
- ED-24MAN.1 - protected Telegram manual binding validated locally, pushed, and production-confirmed.
- ED-24USR.3 - persistent Telegram user roles validated locally and pushed.

Validated Gree scenarios after ED-24UX.4:

Gree n2 -> ambiguity includes GMV Mini / GMV6 / GMV X
Gree GMV X n2 -> GMV X n2
Gree GMV9 Flex n2 -> not found, no fallback
Gree GMV9 Flex E0 -> OK, no GC/manual code in visible text
Gree GMV9 H5 -> OK, no GC/manual code in visible text
Gree 9 series Flex C0 -> OK, no GC/manual code in visible text
Gree 9-Flex A0 -> OK, no GC/manual code in visible text
Gree GMV6 A9 -> OK, no GC/manual code in visible text
Gree GMV6 Uy -> OK, no GC/manual code in visible text

## Important commits

38e7b48d ED-24BRAND.1 Polish Telegram branding copy
40f90aea ED-24MAN.3a Polish library document labels
2b59abd0 ED-24MAN.3 Fix library categories and callbacks
44f5a0a4 Update project state after ED-24OPS.3 production pass
1e46ab85 ED-24OPS.3 Persist DataProtection keys
c775f936 ED-24EF.2 Fix HourlySchedule value comparer
63547146 ED-24EF.1 Fix Telegram EF sentinel warnings
d01cd488 ED-24UX.7 Fix Gree series refinement layout
37a92ae3 ED-24E.3 Add Gree GMV6 HR diagnostics
96cf065e ED-24MAN.2b Fix OwnerManual selection buttons
4cf00444 ED-24OPS.2a Support Telegram video notes
ec553a8a ED-24OPS.2 Add Telegram operator inbox
e7577c46 ED-24LIB.1 Add protected Telegram file library
8a3edb6a ED-24MAN.1 Bind protected Telegram manuals
6fbf2685 Update project state after ED-24MAN.1
4231cb9d ED-24SRC.1a Fix diagnostic manual keyboard UX
a33ea0ea ED-24USR.3 Persist Telegram user roles
85515a14 ED-24USR.2 Fix Telegram admin user identity
afc3e325 ED-24SRC.1 Add role-gated diagnostic manual action
5d944e12 Update project state after ED-24UX.6
d8fdc3d1 ED-24UX.6 Compact Gree diagnostic first-check bullets
e24ae712 ED-24UX.4a Polish live-reviewed Gree Telegram answer UX
80947fdb ED-24UX.4 Polish Gree diagnostic answer structure
96a9d62e ED-24OPS.1 Add repeatable Gree diagnostics smoke runner
60f11980 ED-24QA.1 Lock existing Gree diagnostics quality baseline
02217540 ED-24TD.4 Exclude helper tooling from GitHub language stats
20fb7ef0 ED-24GEC.15.1 Clean visible manual references and n2 ambiguity
a7aad11a ED-24GEC.15 Import GMV9 Flex manual codes
bdcff4f0 Update project state after GMV X diagnostics stabilization
ede84516 ED-24GEC.14.2 Polish GMV X visible wording grammar
99f73ef0 ED-24GEC.14.1 Fix GMV X visible wording encoding

## Important product decisions

- Do not show document codes like GC202512-I / GC202209-I / GC202203-IV in Telegram visible diagnostic answers.
- Keep document/manual references only in metadata/sourceReferences.
- Later manuals should be delivered by a separate button/action, not by embedding document codes in every answer.
- Explicit series query must not fallback to other series.
- General Gree n2 must show only real series where n2 exists: GMV6 HR, GMV6, GMV Mini, GMV X.
- GMV9 Flex n2 must not be added unless runtime/manual confirms it.
- Keep visible answers readable Russian: no mixed translation, no question-mark placeholders.
- Guard grammar: no 'к наружного блока', no 'к внутреннего блока', no 'к наладки системы'.
- Do not give Codex prompts automatically before discussing the next stage.

## Future candidates

- ED-24MAN.1 follow-up - Production library finalization / bind GMV Mini after ED-24SRC.2 audit, if still pending.
- ED-24BCAST.2 - Broadcast history/retry.
- ED-24MAN.4 - Manual variants by model family / exact model matching.
- TD-OPS-002 - optional DataProtection certificate hardening: mount and rotate a production-owned PFX/secret for key
  encryption at rest.
- ED-24QA.2 - Clean nullable warnings in architecture guard tests.

## Current blocker

No implementation blocker. ED-24OPS.4 is implemented and pushed pending VPS live-check; ED-24USR.4 and ED-24BCAST.1
remain production PASS.

## Next step

Complete ED-24OPS.4 production live-check, then continue with ED-24BCAST.2 Broadcast history/retry.


<!-- ED-24-CI-MAN-USR-PRODUCTION-PASS:BEGIN -->

## ED-24 production pass update

Updated: 2026-07-01 09:09:29 UTC

### Production status

The following stages are marked as CLOSED / production PASS:

- ED-24USR.5 - Owner-only user cards, role changes, block/unblock, self/last-owner protection.
- ED-24MAN.4b - ERV/U-Match diagnostic guide binding fixes.
- ED-24UX.8a - duplicate diagnostic code in visible titles fixed.
- ED-24CI.1 - Microsoft.OpenApi restore vulnerability fixed; Engineering Core CI-equivalent checks passed.
- ED-24MAN.4c - ERV SQL script hardened against misclassifying controller manuals.
- ED-24MAN.4d - controller manual button labels and selected-file captions fixed.

### Production live-check

VPS live-check passed after deployment:

- Telegram polling starts normally.
- Telegram callbacks are processed.
- Telegram document delivery works.
- U-Match diagnostic guide sends the selected cassette/duct OwnerManual correctly.
- ERV B Series diagnostic guide sends only the ERV-specific Installation Startup Maintenance manual.
- ERV user guides section contains the single expected ERV guide.
- Controllers section contains controller manuals under Controllers, not ERV.
- Controller buttons are readable:
  - ERV Wired Controller
  - XE7A-23H / XE7A-23HC
  - XE7A-24H / XE7A-24HC
  - XK46
- Selected controller file text/caption matches the selected binding.
- No OutboundFailed, BUTTON_DATA_INVALID, error, exception, or failed entries were observed in the checked production log window.

### CI status

GitHub CI issue root cause:

- Microsoft.AspNetCore.OpenApi 10.0.5 pulled vulnerable transitive Microsoft.OpenApi 2.0.0.
- NU1903 became WarningAsError during dotnet restore.
- Fixed by direct safe override to Microsoft.OpenApi 2.7.5.
- Guard test added to prevent Microsoft.OpenApi 2.0.0 from returning.

Validation reported for ED-24CI.1 / ED-24MAN.4d:

- Restore: PASS.
- Build: PASS, 0 warnings / 0 errors.
- Focused manual-library tests: PASS, 64/64.
- OpenAPI/import/runtime/manual-registry guards: PASS, 37/37.
- Gree diagnostics smoke: PASS, 14/14.
- Full solution tests: PASS, 5104/5104.
- Engineering Core V1 Smoke CI-equivalent: PASS.
- Engineering Core V1 Validation + artifact diff/regenerate: PASS.
- Microsoft.OpenApi resolves to 2.7.5; 2.0.0 absent.
- EF pending model check: no model changes.
- git diff --check: PASS.

### Runtime and boundaries

Runtime counts unchanged:

- Gree total: 1296.
- U-Match R32: 107.
- ERV B Series: 5.

No new migrations were added for these final fixes.

No PDFs, manual intake artifacts, secrets, .env backups, or unrelated files were committed.

Manual policy remains:

- ServiceManual remains library-only.
- InstallationManual remains hidden from generic visible installation menus.
- Diagnostic guide uses safe user-facing guide bindings.

### Current next step

Recommended next stage:

- ED-24USR.6 or later polish only if user-management UX needs refinement after more real use.
- Otherwise continue adding equipment manuals/diagnostic cards in small verified batches.

<!-- ED-24-CI-MAN-USR-PRODUCTION-PASS:END -->


<!-- ED-24SR-PRODUCTION-PASS:BEGIN -->

## ED-24SR production pass update

Updated: 2026-07-01 11:40:43 UTC

### Production status

The following stages are marked as CLOSED / production PASS:

- ED-24SR.1 - Service request text dialog.
- ED-24SR.2 - Service request dialog attachments.
- ED-24SR.3 - Group-safe Telegram keyboards hotfix.

### Production live-check

VPS live-check passed after deployment:

- Telegram polling started normally.
- Service request creation still works.
- Group service request notification works.
- Group actions are processed with Status=Processed.
- Text dialog is persisted and delivered both directions.
- Operator-to-user text messages are saved.
- User-to-operator text messages are saved and routed to the service group.
- Dialog history is available from the request card.
- The group-safe keyboard hotfix worked: no request_contact keyboard was sent to group chats.
- No OutboundFailed, BUTTON_DATA_INVALID, phone-number request error, error, exception, or failed entries were observed in the checked production log windows.

### Attachment live-check

TelegramServiceRequestMessageAttachments contains production rows for:

- Photo with FileId metadata.
- Document with FileId, filename, mime type and file size metadata.
- Video with FileId, mime type, file size, width, height and duration metadata.

Confirmed attachment types:

- Photo.
- Document.
- Video.

Attachment handling policy:

- Uses Telegram file_id metadata.
- Does not download/store file bytes.
- Keeps protect_content=true for service request dialog media.

### Database status

Production migrations applied:

- 20260701095907_AddTelegramServiceRequestDialog.
- 20260701101337_AddTelegramServiceRequestDialogAttachments.

Production tables validated:

- TelegramServiceRequestMessages.
- TelegramServiceRequestMessageAttachments.

### Validation summary

Validation reported before deployment:

- Restore/build: PASS.
- Focused tests: PASS.
- EquipmentDiagnostics CI-equivalent: PASS.
- Full solution tests: PASS.
- Gree diagnostics smoke: PASS.
- EF model validation: clean.
- Microsoft.OpenApi resolves to 2.7.5; 2.0.0 absent.
- git diff --check: PASS.

Runtime counts unchanged:

- Gree total: 1296.
- U-Match R32: 107.
- ERV B Series: 5.

No PDFs, manual intake artifacts, secrets, .env backups, or unrelated files were committed.

### Known follow-up

Branch-readiness still needs a separate cleanup because its narrow-skeleton policy treats the intentionally required EF migrations as forbidden, and its plain full-suite path can hit the known workflow artifact truncation limit. This is not a production blocker for ED-24SR.

### Current next step

Recommended next stages:

- ED-24SR.4 - polish service request dialog UX after real usage if needed.
- ED-24OPS.4 - controlled PostgreSQL EF migration runner / branch-readiness cleanup.
- Continue adding equipment manuals and diagnostics in small verified batches.

<!-- ED-24SR-PRODUCTION-PASS:END -->
