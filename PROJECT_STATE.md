\# AssistantEngineer Project State



Last updated: 2026-06-21

Primary repo: https://github.com/Nurislamov/AssistantEngineer

Primary local path: `D:\\Project\\AssistantEngineer`

Production host path: `/opt/assistantengineer`

Production deploy path: `/opt/assistantengineer/deploy`



\## Current stage



Current recommended next stage:



`ED-24H.1 Import next reviewed Gree VRF manual from the local backlog`



Purpose:



Continue the reviewed local-manual import backlog after ED-24F.1b. Prefer the next locally available Gree VRF/GMV manual identified by the ED-24H catalog map, and import only evidence that is directly verified from that manual.



`Same code + same equipment type + same meaning = one diagnostic answer`



Do not mix internet data, memory-based assumptions, or cross-series Gree meanings. Multiple manuals may be attached only as reviewed source references when the diagnostic meaning is the same.



Before starting the next import stage, identify the exact local source manual and record:



manufacturer, equipment family, model/series scope, document code/version, diagnostic sections, troubleshooting detail, collision risk, and candidate package manifests.



Expected next action:



1\. Use only the selected local manual.

2\. Preserve one diagnostic answer for same-code/same-equipment/same-meaning cases.

3\. Add or refine reviewed procedures only from the same manual evidence.

4\. If a code has genuinely different meanings across equipment types or series, ask for equipment/series context, not source/manual choice.

5\. Keep `/last`, Russian output normalization, and existing GMV6 smoke behavior unchanged.



\## Current branch



`master`



Latest production commit after ED-24E.2a deployment:



`8c89219 ED-24E.2a Fix Russian diagnostic wording normalization`



Latest known production status:



\* API starts successfully.

\* Telegram polling starts successfully.

\* Telegram command menu syncs successfully.

\* No `InvalidOperationException`.

\* No `Telegram polling batch failed`.

\* No duplicate skip after failed processing.

\* No EF migration required.

\* No DB changes.

\* No env changes.

\* Postgres container remains healthy.

\* Orphan Postgres compose warning is known and should not be acted on unless explicitly planned.



\## Last completed work



\### ED-24F.1b - CLOSED



Title:



`ED-24F.1b Merge GMV IDU manual references`



Purpose:



Merge reviewed `SERVICE_MANUAL_GMV_IDU.pdf` (`GC202004-X`) evidence into existing GMV6 indoor diagnostics as additional source references, without creating duplicate answers or changing runtime behavior outside the embedded knowledge data.



Source used:



`D:\\Project\\AssistantEngineer\\artifacts\\manual-intake\\sources\\gree\\SERVICE_MANUAL_GMV_IDU.pdf`



Results:



\* Codes reviewed: 38

\* Existing GMV6 indoor entries updated with `sourceReferences[]`: 38

\* Detailed procedure-rich codes reviewed: 19

\* Localized Installer/Engineer procedure text updated: 0

\* NeedsReview codes after merge: 0

\* Duplicate diagnostic entries created: 0

\* Packages remain: 4

\* Entries remain: 253



Procedure note:



The detailed troubleshooting sections in `GC202004-X` were reviewed, but localized procedure prose was not expanded in this stage. Existing qualified-service text already keeps procedure execution bounded by the reviewed manuals, and several IDU procedures involve component replacement or electrical service actions.



Manual-bound rule:



`Same code + same equipment type + same meaning = one diagnostic answer with multiple reviewed source references`



No external sources, other manuals, or model-memory technical content were used.

No PDF, DOC, XLS, or XLSX source file was committed. No Telegram manual file delivery, Mini App, role policy, DB schema, EF migration, deployment scaffold, or env change was added.



\### ED-24F.1a - CLOSED



Title:



`ED-24F.1a Add multi-source diagnostic references`



Purpose:



Add optional multi-source diagnostic references so future manual imports can preserve multiple reviewed source/manual references for the same diagnostic answer without prompting users to choose a manual.



Root design issue solved:



Same code + same equipment type + same meaning now remains one answer with multiple source references. Different meanings across equipment types or series still require equipment/series clarification, not manual/source selection.



Added:



\* `ErrorKnowledgeSourceReferenceV2`.

\* Optional JSON `sourceReferences[]`.

\* Validation for non-empty arrays when present, required reference fields, allowed source type/language/confidence/verification values, sensitive platform content scanning, and optional package-link checks.

\* Telegram source labeling for multiple reviewed manual references as `руководства производителя`.

\* Deterministic tests for source-reference validation, same-code same-equipment answer behavior, `/last`, and equipment clarification for different meanings.



Counts remain:



\* Packages: 4

\* Entries: 253



No GMV IDU codes were imported. No production diagnostic package or entry was added. No PDF, DOC, XLS, or XLSX source file was committed. No Telegram manual file delivery, role policy, DB schema, EF migration, deployment, or env change was made.



Next stage:



`ED-24H.1 Import next reviewed Gree VRF manual from the local backlog`



\### ED-24H.0 — CLOSED



Title:



`ED-24H.0 Add Gree VRF equipment catalog map`



Purpose:



Create a non-runtime catalog-level map of Gree VRF/GMV series, indoor unit types, controls, commissioning tools, BMS/cloud/billing/remote gateways, and manual-search backlog items from local catalogues only.



Sources:



\* `artifacts/manual-intake/sources/gree/GMV6 Catalouge.pdf`

\* `artifacts/manual-intake/sources/gree/141367.pdf`

\* `artifacts/manual-intake/sources/gree/GMV6 2023 РУС.pdf`



Added:



\* Machine-readable registry:

&#x20; `data/equipment-diagnostics/equipment-catalog/gree-vrf-equipment-map.json`

\* Registry README:

&#x20; `data/equipment-diagnostics/equipment-catalog/README.md`

\* Human-readable report:

&#x20; `docs/equipment-diagnostics/gree-vrf-equipment-map.md`

\* Deterministic registry tests.



Catalog-identified series:



\* GMV6

\* GMV6 Anti-corrosion Series

\* GMV6 HR

\* GMV X

\* GMV X PRO

\* GMV9 Flex

\* GMV5 MAX

\* GMV Mini Star

\* GMV5 Mini

\* GMV5 Slim

\* GMV5 Home



Manual backlog:



\* GMV X / GMV X PRO service manuals

\* GMV6 HR service manual

\* GMV9 Flex service manual

\* GMV5 MAX service manual

\* GMV Mini Star / GMV5 Mini / GMV5 Slim / GMV5 Home service manuals

\* Controller, commissioning-tool, PC debugging, BMS/gateway, Wi-Fi/cloud, remote monitoring, and billing manuals



Future Telegram manual-library policy remains plan-only:



\* Consumer: denied

\* Installer: allowed

\* Engineer: allowed

\* Admin: allowed

\* Owner: allowed



No new diagnostic codes, packages, entries, or runtime source selection were added.

No Telegram role, lookup, database, EF migration, deployment, or env change was made.

No PDF, DOC, XLS, or XLSX source file was committed.

Packages and entries remain:



\* Packages: 4

\* Entries: 253



\### ED-24F.1 — STOPPED FOR DESIGN REVIEW



Title:



`ED-24F.1 Import Gree GMV IDU manual knowledge`



Source analyzed:



`artifacts/manual-intake/sources/gree/SERVICE_MANUAL_GMV_IDU.pdf`



Verified identity:



\* Title: `Service Manual - Multi Variable Air Conditioners Indoor Units`

\* Document code: `GC202004-X`

\* Manufacturer: Gree

\* Language: English

\* PDF pages: 403

\* Scope: broad GMV multi-variable indoor units

\* Explicit version/date: not found on reviewed identity pages



Diagnostic evidence:



\* Malfunction table: manual page 173 / PDF page 178

\* Troubleshooting: manual pages 173-185 / PDF pages 178-190

\* Codes identified: 38

\* Codes with detailed procedures: 19

\* Display contexts: wired controller and IDU receive light board

\* `db` is explicitly project-debugging status, not an error



Collision:



\* All 38 identified codes overlap existing GMV6 indoor entries.

\* New unique codes relative to the GMV6 indoor package: 0.

\* Validator taxonomy can separate `GMV` and `GMV6` by series.

\* Current Telegram conversation cannot clarify two candidates that share Gree + Indoor Unit + display context but differ by series/manual source.

\* Importing now could silently select the first candidate.



Decision:



Import stopped. No package or diagnostic entry was added. The registry is marked:



\* `importStatus: NeedsReview`

\* `coverageStatus: DiagnosticSectionsIdentified`

\* `importDecision: BlockedPendingSeriesAwareDisambiguation`



Analysis report:



`docs/equipment-diagnostics/gree-gmv-idu-manual-import.md`



Counts remain:



\* Packages: 4

\* Entries: 253



No external sources, other manuals, or model-memory technical content were used.

No PDF binary, EF migration, DB schema, runtime diagnostic behavior, or env change was added.



Validation:



\* Restore: PASS

\* Build: PASS, 0 errors; 6 pre-existing nullable warnings in architecture tests

\* Full tests: PASS — 4630

\* EquipmentDiagnostics: PASS — 634

\* Knowledge validator: PASS — 4 packages, 253 entries, 0 issues

\* Deployment validators: PASS

\* Release publish smoke: PASS

\* EF migration: none

\* DB/env changes: none



\### ED-24F.0 — CLOSED



Title:



`ED-24F.0 Add diagnostic manual coverage matrix`



Purpose:



Add non-runtime coverage tracking for local Gree manuals without importing diagnostic codes, changing Telegram behavior, or committing source binaries.



Added:



\* Machine-readable registry:

&#x20; `data/equipment-diagnostics/manual-library/manuals.json`

\* Human-readable coverage matrix:

&#x20; `docs/equipment-diagnostics/manual-coverage.md`

\* Future Telegram manual-library design:

&#x20; `docs/equipment-diagnostics/telegram-manual-library-plan.md`

\* Registry tests for unique IDs, GMV6 package/count consistency, access policy, next-manual priority, and ignored binary sources.



Coverage snapshot:



\* Local Gree source files tracked: 47

\* Imported manuals: 1

\* Imported diagnostic source: `Service Manual for GMV6 v_2020.09.pdf`

\* GMV6 packages: 4

\* GMV6 entries: 253

\* Other manuals: not imported

\* Next recommended source: `SERVICE_MANUAL_GMV_IDU.pdf`



Manual-bound rule remains:



`One manual = one source`



No internet, model-memory additions, mixed-manual meanings, or cross-series generalization.



Future Telegram manual-library access policy:



\* Consumer: denied

\* Installer: allowed

\* Engineer: allowed

\* Admin: allowed

\* Owner: allowed



This is a plan only. ED-24F.0 does not implement Telegram file storage or delivery.



No PDF, DOC, XLS, or XLSX manual source files were committed.

No new diagnostic codes, packages, entries, or sources were imported.

No EF migration, DB schema, runtime, or env changes.



Validation:



\* Restore: PASS

\* Build: PASS, 0 errors; 6 pre-existing nullable warnings in architecture tests

\* Full tests: PASS — 4629

\* EquipmentDiagnostics: PASS — 633

\* Deployment validators: PASS

\* Knowledge validator: PASS — 4 packages, 253 entries, 0 issues

\* EF migration: none

\* DB/env changes: none



\### ED-24E.2a — CLOSED



Commit:



`8c89219634b1c017968f0de76c2f8c9f29452800`



Title:



`ED-24E.2a Fix Russian diagnostic wording normalization`



Root cause:



Russian text normalization was applied to selected fields, but was missing at the final Telegram response boundary and in `/last` diagnostic history. Therefore generated/manual-derived text could still display the bad duplicated phrase:



`сообщение о связи связи и адресации`



Fix:



\* Applied Russian wording normalization at the final Telegram response boundary.

\* Applied normalization in diagnostic history `/last`.

\* Preserved all technical meanings.

\* Did not add new codes.

\* Did not add new sources.

\* Did not change package count or entry count.



Before:



`сообщение о связи связи и адресации`



After:



`сообщение о связи и адресации`



Changed areas:



\* Telegram response formatter.

\* Telegram diagnostic history service.

\* Formatter/history/conversation tests.

\* Russian terminology tests.



Validation:



\* Restore: PASS

\* Build: PASS, 0 warnings/errors

\* Full tests: PASS — 4623

\* EquipmentDiagnostics: PASS — 627

\* Deployment validators: PASS

\* Knowledge validator: PASS — 4 packages, 253 entries, 0 issues

\* Release publish smoke: PASS

\* EF migrations: none

\* DB schema changes: none

\* Env changes: none

\* Working tree: clean

\* `master == origin/master`



Production smoke:



\* `Gree C0`: PASS

\* `/last` after C0: PASS

\* `Gree U0`: PASS

\* `Gree H5`: PASS

\* `Gree E1`: PASS

\* `Gree A0`: PASS

\* Polling logs: clean



Confirmed production C0 wording:



`Gree GMV6 C0 — сообщение о связи и адресации`



No bad duplicate wording remains in Telegram output:



`связи связи` — not present.



\### ED-24E.2 — CLOSED



Commit:



`a55beb10ecfcdee0ef2939b536bb1c0015415501`



Title:



`ED-24E.2 Improve GMV6 Russian diagnostics text`



Purpose:



Improve Russian Telegram diagnostic text quality without changing technical meaning, imported manual data, package counts, entry counts, database schema, or environment files.



Changes:



\* Fixed mechanical boundary phrases.

\* Added shared Russian terminology helper for diagnostic categories and equipment types.

\* Improved formatter-level wording.

\* Fixed some JSON-level Russian text issues.

\* Added validator coverage for known duplicated phrases.

\* Added tests for Russian terminology.



No new sources or codes were added.



Counts:



\* Packages: 4 → 4

\* Entries: 253 → 253



Validation:



\* Restore: PASS

\* Build: PASS, 0 errors

\* Full tests: PASS — 4620/4620

\* EquipmentDiagnostics: PASS — 639/639

\* Deployment validators: PASS

\* Knowledge validator: PASS — 4 packages, 253 entries, 0 issues

\* Release publish smoke: PASS

\* EF model changes: none

\* Migration: none

\* DB/env changes: none



Note:



Initial ED-24E.2 production smoke showed that `Gree C0` still contained `связи связи`. ED-24E.2 was closed only after ED-24E.2a fixed final response and `/last` normalization.



\### ED-24E.1a — CLOSED



Commit:



`bf6c1dd54c4a1f9fabbded92c971e9d54508aba7`



Title:



`ED-24E.1a Fix GMV6 debugging code lookup`



Root cause:



Telegram lookup searched only the runtime/static diagnostic catalog. Repo-backed debugging/status JSON entries were used for formatting but did not fully participate in lookup. As a result, `U0` existed in JSON but was not found by Telegram.



Fix:



\* Debugging / commissioning / status entries now participate in Telegram lookup.

\* Parser preserves GMV6 and debugging hints.

\* `Gree U0`, `Gree GMV6 U0`, and `Gree debugging U0` now resolve.

\* Existing behavior for `H5`, `E1`, `A0`, `/last`, and Installer permissions is preserved.



Validation:



\* Restore: PASS

\* Build: PASS, 0 errors

\* Full tests: PASS — 4603/4603

\* EquipmentDiagnostics: PASS — 622/622

\* Deployment validators: PASS

\* Knowledge validator: PASS — 4 packages, 253 entries, 0 issues

\* Release publish smoke: PASS, U0 resource and entry found

\* EF pending model changes: none

\* Migration: none

\* DB/env/manual data changes: none



Production smoke:



\* `Gree U0`: PASS

\* `Gree GMV6 U0`: PASS

\* `Gree debugging U0`: PASS

\* `Gree H5`: PASS

\* `Gree E1`: PASS

\* `Gree A0`: PASS

\* `/last`: PASS

\* Polling logs: clean



\### ED-24E.1 — CLOSED



Commit:



`32cfd432d6de17bc6fe98393dd839ea840a5b320`



Title:



`ED-24E.1 Import Gree GMV6 manual knowledge`



Source:



`Service Manual for GMV6 v\_2020.09.pdf`

Document code: `GC202001-I`



Manual location used locally:



`D:\\Project\\AssistantEngineer\\artifacts\\manual-intake\\sources\\gree\\Service Manual for GMV6 v\_2020.09.pdf`



Rules followed:



\* One manual = one source.

\* No external sources.

\* No model-memory technical additions.

\* PDF was not committed.

\* Source references stored in JSON/import docs.



Imported:



\* Total GMV6 entries: 253

\* Indoor: 60

\* Outdoor: 120

\* Debugging: 37

\* Status: 36

\* Package manifests: 4



Important correction:



Old generic seeded `Gree GMV H5` was replaced with manual-verified GMV6 H5.



For GMV6:



`H5 = over-current protection of inverter fan`



Telegram output now says in Russian:



`Gree GMV6 H5 — защита инверторного вентилятора по току`



Validation:



\* Restore: PASS

\* Build: PASS, 0 errors; 6 unrelated nullable warnings in architecture tests

\* Full tests: PASS — 4592/4592

\* EquipmentDiagnostics: PASS — 611/611

\* Deployment validators: PASS

\* Knowledge validator: PASS — 257 files, 4 packages, 253 entries, 0 issues

\* Release publish smoke: PASS

\* EF model changes: none

\* Migration: none

\* DB/env changes: none



Import report:



`docs/equipment-diagnostics/gree-gmv6-manual-import.md`



\### ED-24D — CLOSED



Commit:



`570dd5c71dffd10baac1710ee5fbfdf9f0a722cb`



Title:



`ED-24D Add error knowledge taxonomy manifests`



Purpose:



Add taxonomy and package manifests for safe grouped expansion of the diagnostic knowledge base.



Added taxonomy fields:



\* equipment family

\* equipment type

\* signal type

\* display source

\* system part

\* severity

\* service requirement

\* continued-operation flag

\* package ID



Added manifest support:



\* package manifests under repo-backed knowledge data

\* compatibility validation between entries and packages

\* expected count validation

\* duplicate package ID validation

\* taxonomy key validation



Validation:



\* Restore: PASS

\* Build: PASS

\* Full tests: PASS — 4585/4585

\* EquipmentDiagnostics: PASS — 604/604

\* Deployment validators: PASS

\* Knowledge validator: PASS — 1 package, 1 entry, 0 issues

\* Release publish smoke: PASS

\* Migration: none

\* Env changes: none



\### ED-24C.1 — CLOSED



Commit:



`b1793ce1aa33b31edc1d3edb09610452e8d4c24b`



Title:



`ED-24C.1 Fix JSON knowledge runtime loading`



Root cause:



Dockerfile did not copy `data/equipment-diagnostics/error-knowledge/`, so published Docker runtime did not contain the H5 resource. Also, Telegram updates were marked processed before successful processing, so retry could be skipped as duplicate.



Fix:



\* Docker image now includes JSON knowledge resources.

\* Release publish smoke verifies embedded entries.

\* Telegram processing no longer loses failed updates.

\* Safe Russian fallback added.

\* Safer logs added without secrets or Telegram IDs.



Production result:



\* `Gree H5`: PASS

\* `/last`: PASS

\* No `InvalidOperationException`

\* No polling batch failure

\* No duplicate skip after failed processing



\### ED-24C — CLOSED after ED-24C.1 hotfix



Commit:



`1881f7d5aff8a926aacaf45a1a0a045260573fa9`



Title:



`ED-24C Add repo-backed error knowledge validation`



Purpose:



Move diagnostic knowledge to repo-backed JSON with deterministic loading and strict validation.



Added:



\* JSON knowledge path:

&#x20; `data/equipment-diagnostics/error-knowledge/{manufacturer}/{series}/{code}.json`

\* Loader for embedded JSON resources.

\* Validator for knowledge files.

\* Gree GMV H5 initial JSON entry.

\* Command:

&#x20; `dotnet run --project tools/AssistantEngineer.Tools.EquipmentDiagnosticsVerification -- verify-knowledge`



Important note:



ED-24C initially failed in production due to missing JSON resources in Docker image. It was hotfixed by ED-24C.1.



\### ED-24B — CLOSED



Commit:



`1a1c5ed721da0f96b4b417e42cad79ff103e68a7`



Title:



`ED-24B Add localized error knowledge foundation`



Purpose:



Add localization-ready model for diagnostic knowledge.



Added:



\* source language separated from localized text;

\* support for `ru` and `en`;

\* architecture ready for future `uz`;

\* Telegram always chooses Russian text;

\* missing translations use safe Russian fallback;

\* audience-specific text: Consumer, Installer, Engineer;

\* Admin/Owner use Engineer variant.



Result:



\* English labels no longer leak into Russian Telegram output.

\* `Safety`, `Step`, `Source`, `Confidence`, `Preliminary diagnostic entry` removed from Russian Telegram output.



\### ED-23R — CLOSED



Commit:



`ba49fad534eea7011d8fc1d8349b2d57906bdc3c`



Title:



`ED-23R Add Installer Telegram role`



Purpose:



Add a role for installers / монтажники.



Role model:



\* Consumer / Клиент

\* Installer / Монтажник

\* Engineer / Сервис-инженер

\* Admin

\* Owner



Installer permissions:



\* Can use technical diagnostics.

\* Can use `/history` and `/last`.

\* Cannot access service queue.

\* Cannot take/assign/close/cancel service requests.

\* Cannot access customer contacts.

\* Cannot use admin commands.



Validation:



\* Restore: PASS

\* Build: PASS

\* Tests: PASS — 4531/4531

\* Deployment validators: PASS

\* EF migration: not required

\* Env changes: none



\### ED-23F.3 — CLOSED



Commit:



`475b1a3ab785468f693ae91ca584459523a80791`



Title:



`ED-23F.3 Add Telegram user management audit events`



Purpose:



Add persistent audit for Telegram user-management actions.



Added:



\* table `TelegramUserAuditEvents`

\* EF migration `AddTelegramUserAuditEvents`

\* audit for role changes;

\* enable/disable;

\* block/unblock;

\* denied events;

\* `/admin\_audit` for Owner/Admin.



Migration was applied on production.



Production verification:



\* migration exists in `\_\_EFMigrationsHistory`;

\* table `TelegramUserAuditEvents` exists;

\* columns verified;

\* role change to Installer was audited.



\### ED-23F.2 — CLOSED



Commit:



`2b5962d3df067bf2d98887cdc3835612a94eb18a`



Title:



`ED-23F.2 Expand Telegram service request audit events`



Purpose:



Extend service request audit.



Added events:



\* `ContactDenied`

\* `HistoryViewed`

\* `HistoryDenied`

\* `ActionDenied`



Also updated:



\* contact requested/sent/failed events;

\* safe allowlist metadata;

\* compact audit history output.



Validation:



\* Restore: PASS

\* Build: PASS

\* Tests: PASS — 4508/4508

\* Deployment validators: PASS

\* Migration: none

\* Env changes: none



Production verification:



\* `HistoryViewed`, `ContactRequested`, `ContactSent`, `ActionDenied` events observed in DB.

\* `MetadataJson` column verified.

\* No phone/raw IDs/callback payload in history/logs.



\### ED-23G.1 — CLOSED



Commit:



`ecb99bf`



Title:



`ED-23G.1 Harden Telegram request lifecycle UX`



Purpose:



Harden service request lifecycle UX around Telegram buttons/actions.



Status:



\* Completed before ED-23X.

\* Deployed before later audit/role stages.

\* No active blocker known.



\### ED-23X — CLOSED



Commit:



`49f0cbc`



Title:



`ED-23X Fix SQLitePCLRaw NU1903 restore failure`



Purpose:



Fix CI restore blocked by `NU1903` high severity vulnerability warning-as-error for `SQLitePCLRaw.lib.e\_sqlite3` 2.1.11.



Goal followed:



\* real dependency fix preferred;

\* no warning suppression as primary fix;

\* no Telegram logic changes;

\* no EF migration;

\* no env changes.



Production status:



\* Commit pulled and deployed.

\* API started successfully.



\### ED-23G — CLOSED



Commit:



`2a7bff8d210ab2cd4f0ced211d29744e0d24bbc6`



Title:



`ED-23G Add Telegram service request queue filters`



Purpose:



Add queue filters and improve service request queue handling.



Added:



\* queue/store changes;

\* callback hardening;

\* help updates;

\* tests and docs.



Validation:



\* Build: PASS

\* Tests: PASS — 4496/4496

\* Deployment validators: PASS

\* Migration: none

\* Env changes: none



Smoke checklist used:



\* check `/queue` variants;

\* check `/my\_requests` for Engineer;

\* press six inline filters;

\* verify callback spinner cleanup;

\* verify Consumer denial;

\* verify no full phone in group;

\* verify `/take`, `/assign`, `/contact`, `/done`, history.



\### ED-23F.1 — CLOSED



Commit:



`b3e1c32`



Title:



`ED-23F.1 Harden Telegram audit history callbacks`



Status:



\* Deployed to production.

\* Telegram polling processed callback updates successfully.

\* No active blocker known.



\### ED-20A — CLOSED



Stage:



`EquipmentDiagnostics beta release readiness consolidation`



Known result:



\* Readiness: PASS

\* Blockers: 0

\* Warnings: 3

\* Backend tests: PASS

\* Frontend tests: PASS



Important decision:



Do not touch or split `Verify Engineering Core V1` yet because it is a long full gate.



\## Earlier completed Telegram / EquipmentDiagnostics stages



Already merged before ED-20A:



\* `ED-17A` — Telegram adapter skeleton

\* `ED-17B` — Telegram webhook transport

\* `ED-17C` — Telegram access operations readiness

\* `ED-18A` — provider-neutral production deployment scaffold

\* `ED-18B` — production hardening checklist and validators

\* `ED-18C` — CI deployment dry-run readiness

\* `ED-19A` — runtime observability foundation

\* `ED-19B` — structured request logging and correlation foundation

\* `ED-19C` — operational incident runbooks and sanitized log review



\## Current Telegram bot capabilities



Implemented:



\* Telegram polling production mode.

\* Telegram command menu sync.

\* Access roles:



&#x20; \* Consumer

&#x20; \* Installer

&#x20; \* Engineer

&#x20; \* Admin

&#x20; \* Owner

\* Consumer can request help/service.

\* Installer can view diagnostics but cannot access service/admin functions.

\* Engineer can access service queue and service actions.

\* Admin/Owner can manage users and audit.

\* Service request queue exists.

\* Queue filters exist.

\* Inline buttons and callback handling exist.

\* Service request lifecycle actions exist:



&#x20; \* create

&#x20; \* notify group

&#x20; \* queue

&#x20; \* take

&#x20; \* assign/reassign

&#x20; \* contact

&#x20; \* done/resolve

&#x20; \* cancel/denied actions

&#x20; \* history

\* Audit exists for service request events.

\* Audit exists for Telegram user management events.

\* Safe metadata rules exist.

\* No phone/raw IDs/callback payloads should be shown in public history/logs.

\* `/last` diagnostic history works.

\* `/history` works.

\* `/admin\_audit` exists for Owner/Admin.



Known Telegram production warnings:



\* `Microsoft.AspNetCore.Hosting.Diagnostics\[15]` about `HTTP\_PORTS/URLS` is known and not related to Telegram.

\* DataProtection key persistence warnings exist and are known production-hardening items, not current blockers.



\## Equipment diagnostics knowledge state



Current source-of-truth:



Repo-backed JSON:



`data/equipment-diagnostics/error-knowledge/`



Manual coverage registry:



`data/equipment-diagnostics/manual-library/manuals.json`



Coverage documentation:



`docs/equipment-diagnostics/manual-coverage.md`



Current registry snapshot:



\* 47 local Gree source files tracked

\* 1 imported manual

\* 29 new/unanalysed records

\* 17 records needing identity, duplicate, or import-design review



Current imported manual:



`Service Manual for GMV6 v\_2020.09.pdf`

Document code: `GC202001-I`



Imported GMV6 knowledge:



\* 4 package manifests

\* 253 entries

\* Indoor: 60

\* Outdoor: 120

\* Debugging: 37

\* Status: 36



Current validator command:



`dotnet run --project tools/AssistantEngineer.Tools.EquipmentDiagnosticsVerification -- verify-knowledge`



Current validator expected result:



`4 packages, 253 entries, 0 issues`



Important manual-intake rule:



One manual = one source.



Do not:



\* use internet;

\* use model memory;

\* mix manuals;

\* generalize one code across Gree series;

\* commit full PDF manuals unless explicitly decided;

\* infer unsupported repair steps.



Current known working smoke queries:



\* `Gree C0`

\* `Gree U0`

\* `Gree GMV6 U0`

\* `Gree debugging U0`

\* `Gree H5`

\* `Gree E1`

\* `Gree A0`

\* `/last`



\## Current blocker



No production blocker.



\## Important decisions



\### Project process



\* Use staged work with explicit stage IDs: `ED-xx`, `P0`, `P1`, `P2`, `P3`.

\* Do not mix unrelated work in one stage.

\* Prefer practical, production-safe steps.

\* User prefers PowerShell scripts and direct commands.

\* User runs commands locally/server-side and sends output.

\* GitHub may be temporarily unavailable from the user's network.

\* Avoid unnecessary clarifying questions when safe assumptions are possible.

\* Keep `PROJECT\_STATE.md` in repository root and update it after meaningful stage closures.



\### Documentation / claims



Forbidden public wording:



\* `pyBuildingEnergy parity`

\* exact EnergyPlus match

\* full ASHRAE 140 validation claim

\* full external library parity claim



Preferred wording:



\* `standard-based`

\* `standard-inspired`

\* `external reference validation`

\* `engineering-core validation`



Do not make engineering compliance claims without validation.



\### Manual import policy



\* One manual = one source.

\* Add only what is in the manual.

\* Do not use internet for manual imports.

\* Do not mix Gree GMV6 / semi-industrial / split / chiller / controller meanings.

\* Store source name, source reference, manual section/page/table in JSON metadata.

\* Prefer not to commit PDF manuals.

\* Consumer text must be safe.

\* Installer text can be technical but no service permissions.

\* Engineer text can include manual-derived checks with qualified-service boundaries.



\### Telegram safety



Do not log or display:



\* full phone numbers in group/public output;

\* raw Telegram user IDs;

\* raw chat IDs;

\* bot token;

\* webhook secret;

\* callback payloads;

\* unsafe diagnostic instructions for consumers.



\### Deployment



For changes affecting Dockerfile, embedded resources, JSON knowledge, formatter output, diagnostics lookup, or publish behavior, use no-cache API build:



`docker compose --env-file .env -f docker-compose.yml build --no-cache assistantengineer-api`



Then recreate API:



`docker compose --env-file .env -f docker-compose.yml up -d --no-deps --force-recreate assistantengineer-api`



Known compose warning:



`Found orphan containers (\[assistantengineer-postgres-1])`



Do not remove orphan containers unless explicitly planned, because Postgres is currently running and healthy.



\## Files changed recently



Recent important areas:



\* `data/equipment-diagnostics/error-knowledge/gree/gmv6/\*\*`

\* `data/equipment-diagnostics/error-knowledge/packages/\*\*`

\* `docs/equipment-diagnostics/gree-gmv6-manual-import.md`

\* `docs/equipment-diagnostics/error-knowledge-v2-localization.md`

\* `src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Bot/EquipmentDiagnosticBotService.cs`

\* `src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/Conversations/TelegramDiagnosticConversationService.cs`

\* `src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/EquipmentDiagnosticTelegramMessageParser.cs`

\* `src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/EquipmentDiagnosticTelegramResponseFormatter.cs`

\* `src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/History/TelegramDiagnosticHistoryService.cs`

\* `src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Knowledge/Localization/RussianDiagnosticTerminology.cs`

\* `src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Knowledge/Localization/\*\*`

\* `tests/AssistantEngineer.Tests/EquipmentDiagnostics/\*\*`

\* `tools/AssistantEngineer.Tools.EquipmentDiagnosticsVerification/Program.cs`



\## Validation status



Latest known validation for ED-24F.1b:



\* Restore: PASS

\* Build: PASS, 0 warnings, 0 errors

\* Full tests: PASS - 4661

\* EquipmentDiagnostics: PASS - 680

\* Deployment validators: PASS

\* Knowledge validator: PASS - 4 packages, 253 entries, 0 issues

\* Release publish smoke: PASS

\* EF pending model changes: none

\* Migration: none

\* DB changes: none

\* Env changes: none



Previous validation retained for ED-24E.2a:



\* Restore: PASS

\* Build: PASS

\* Full tests: PASS — 4623

\* EquipmentDiagnostics: PASS — 627

\* Deployment validators: PASS

\* Knowledge validator: PASS — 4 packages, 253 entries, 0 issues

\* Release publish smoke: PASS

\* EF pending model changes: none

\* Migration: none

\* DB changes: none

\* Env changes: none



Latest production smoke:



\* API: Up

\* Postgres: healthy

\* Telegram polling: running

\* `Gree C0`: PASS

\* `/last` after C0: PASS

\* `Gree U0`: PASS

\* `Gree H5`: PASS

\* `Gree E1`: PASS

\* `Gree A0`: PASS

\* Logs: clean



\## Next step



Recommended next stage:



`ED-24H.1 Import next reviewed Gree VRF manual from the local backlog`



Scope:



\* select the exact local source manual before coding;

\* no external sources;

\* no cross-series assumptions;

\* no duplicate answer when code, equipment type, and meaning are the same;

\* equipment/series clarification only for genuinely different meanings;

\* no Mini App;

\* no runtime editing;

\* no DB/env changes unless explicitly justified;

\* keep current Telegram diagnostics green.



Before coding:



1\. User provides or identifies the next manual, or selects one from the ED-24H backlog.

2\. Assistant analyzes manual identity:



&#x20;  \* manufacturer;

&#x20;  \* equipment family;

&#x20;  \* series/model scope;

&#x20;  \* document code/version;

&#x20;  \* sections containing diagnostic/error/status/debug tables;

&#x20;  \* whether troubleshooting details are present;

&#x20;  \* candidate package manifests;

&#x20;  \* uncertain/skipped items.

3\. Assistant prepares a Codex prompt for import.



Expected checks after next import:



```powershell

dotnet restore .\\AssistantEngineer.sln

dotnet build .\\AssistantEngineer.sln

dotnet test .\\AssistantEngineer.sln

dotnet run --project tools/AssistantEngineer.Tools.EquipmentDiagnosticsVerification -- verify-knowledge

```



Also run deployment validators and release publish smoke if available.



Production smoke after next import must include:



\* at least one known code from the new manual;

\* `/last`;

\* existing GMV6 smoke:



&#x20; \* `Gree C0`

&#x20; \* `Gree U0`

&#x20; \* `Gree H5`

&#x20; \* `Gree E1`

&#x20; \* `Gree A0`



\## Open plans and roadmap



\### A. Telegram bot / EquipmentDiagnostics



\#### Done



\* Telegram adapter skeleton.

\* Telegram webhook transport.

\* Telegram polling production mode.

\* Telegram command menu.

\* Access roles.

\* Installer role.

\* Service queue.

\* Queue filters.

\* Service request lifecycle UX.

\* Contact/history/action audit.

\* User management audit.

\* Admin audit command.

\* Russian localization foundation.

\* Repo-backed JSON diagnostic knowledge.

\* Knowledge taxonomy.

\* Package manifests.

\* GMV6 manual import.

\* GMV6 debugging/status lookup.

\* Russian terminology normalization.

\* Runtime Docker embedded JSON loading.

\* Safe fallback for diagnostic failures.

\* No-update-loss behavior after processing errors.



\#### In progress / next



\* Continue expanding diagnostic knowledge by manual.

\* Improve import quality and source traceability as new manuals are added.



\#### Planned but not started



\* Import Gree wired controller / remote controller manuals.

\* Import Gree semi-industrial ducted manuals.

\* Import Gree chiller manuals.

\* Import Gree split manuals if needed.

\* Add better ambiguity/disambiguation UI for same code across multiple series/manuals.

\* Add source/manual browsing or trace display for engineers.

\* Add search/help over diagnostic catalog.

\* Add Uzbek locale when product timing is right.

\* Add explicit language selection only later, not now.

\* Add Mini App only at the end, after bot workflow is stable.

\* Add richer attachment/photo support later.

\* Add runtime editing/approval workflow later, if needed.



\#### Known caution



Do not add a huge unreviewed cross-series Gree list. Import by manual.



\### B. Knowledge base / data architecture



\#### Done



\* JSON repo-backed knowledge.

\* Embedded resource loading.

\* Validator.

\* Package manifests.

\* Taxonomy.

\* Release publish smoke.

\* Source manual identity.

\* Manual import report for GMV6.

\* Final response normalization.

\* `/last` normalization.

\* Manual coverage registry and coverage matrix.

\* Future Telegram manual-library access policy.



\#### In progress / next



\* Add series-aware source disambiguation before importing `SERVICE_MANUAL_GMV_IDU.pdf`.

\* Continue improving import tooling only when needed.



\#### Planned but not started



\* Better import tooling from PDF/table extraction.

\* Extend the manual coverage registry after each reviewed import.

\* Knowledge quality reports.

\* Coverage report by manufacturer/series/manual/category.

\* Potential review status workflow.

\* Potential admin-only knowledge review later.

\* Potential NoSQL discussion later, but current decision is repo-backed JSON first.



\### C. Mini App / frontend



\#### Done



\* Frontend test baseline from earlier P2 work.

\* Basic frontend build/test infrastructure exists.



\#### Planned but not started



\* Telegram Mini App.

\* Interactive diagnostic catalog UI.

\* Service request management UI.

\* Knowledge review UI.

\* Apartment/VRF data interactive UI.

\* Dashboard improvements for equipment diagnostics.



Decision:



Mini App is intentionally postponed until Telegram bot workflow and diagnostic knowledge base are mature.



\### D. Backend / Engineering Core



\#### Done / mostly done



Engineering Core V1 formula lane had earlier completed work around:



\* transmission;

\* ventilation;

\* internal gains;

\* window solar;

\* surface irradiance;

\* room load;

\* aggregation;

\* EPW/PVGIS 8760;

\* annual hourly kWh;

\* simplified hourly heat balance;

\* single thermal zone;

\* ground simplified;

\* adjacent zone simplified;

\* DHW;

\* system energy;

\* equipment sizing;

\* CalculationTrace foundation.



Important prior validation/naming work:



\* public forbidden terminology removed;

\* external reference validation language preferred;

\* status endpoint and dashboard panel added earlier;

\* validation documentation added.



\#### Known not done / future



\* Do not claim full EnergyPlus parity.

\* Do not claim full ASHRAE 140 validation.

\* Full external validation remains future work.

\* Long `Verify Engineering Core V1` gate is not to be split/touched casually.

\* Further P3 refactors may remain around calculation pipeline / validation services.



\### E. Workflow / persistence / API hardening



\#### Done



Earlier P0/P1/P2/P3 work included:



\* workflow diagnostics/report/trace services extraction;

\* persistence migrations and docs;

\* API key auth boundary work;

\* queued job worker foundation;

\* workflow input snapshot boundary;

\* API hardening baseline:



&#x20; \* rate limiting;

&#x20; \* health endpoints;

&#x20; \* ready endpoint;

&#x20; \* CORS deny-by-default;

\* payload size gates;

\* deterministic truncation policy;

\* frontend test baseline;

\* atomic queued job claim/lease foundation.



\#### Known not done / future



\* Continue architecture cleanup only stage-by-stage.

\* Avoid broad rewrites.

\* Add tests when changing logic.

\* Keep CI green.

\* Continue reducing controller/service size if specific blockers appear.



\### F. CI/CD / Deployment / Operations



\#### Done



\* Production Docker deployment scaffold.

\* Deployment validators.

\* Dry-run readiness.

\* Runtime observability foundation.

\* Structured request logging/correlation.

\* Incident runbooks and sanitized log review.

\* SQLitePCLRaw NU1903 CI restore hotfix.

\* Production deployment of Telegram bot and diagnostics.



\#### Known production warnings / future hardening



\* DataProtection keys stored inside container path and may not persist outside container.

\* No XML encryptor configured for DataProtection keys.

\* HTTP\_PORTS overridden by URLS warning.

\* Orphan Postgres container warning appears due compose project/service naming history.



These are not current blockers but should be tracked for production hardening.



\### G. Field project / VRF data management



\#### Known domain context



User has a real Gree VRF / energy monitoring project with:



\* multiple building blocks;

\* controllers/gateways;

\* systems;

\* meters;

\* CT ratio 75/5;

\* indoor unit IDs;

\* per-apartment energy allocation needs.



Relevant prior spreadsheet/data structure idea:



\* Controllers

\* Meters

\* Systems

\* IndoorUnits

\* Rooms

\* per-block sheets

\* summary by VRF system:



&#x20; \* outdoor model;

&#x20; \* total indoor capacity;

&#x20; \* loading percentage.



\#### Planned but not started in app



\* Import/track real building/controller/system data.

\* Interactive spreadsheet-like workflow.

\* Apartment/unit management.

\* Energy accounting anomaly diagnostics.

\* Integrate field database with AssistantEngineer product later.



\### H. Other equipment / workshop plans



Known but not active in current software sprint:



\* heat exchanger production equipment support;

\* bending machine segmented matrix sourcing;

\* HVAC field troubleshooting assistance;

\* Gree VRF commissioning support.



These are useful domain inputs but not current AssistantEngineer software stages unless explicitly promoted into the product roadmap.



\## How to continue in a new chat



Start a new chat with:



`Продолжаем AssistantEngineer. Прочитай PROJECT\_STATE.md и продолжай с Current stage / Next step. Не начинай с нуля.`



Expected assistant behavior:



1\. Read `PROJECT\_STATE.md`.

2\. Confirm current stage and production commit.

3\. Continue with the next planned step.

4\. Do not propose unrelated rewrites.

5\. Preserve manual-bound import rules.

6\. Preserve Telegram safety rules.

7\. Preserve public documentation wording restrictions.



\## Commit message suggestion for this file



`ED-STATE Update project state after ED-24E.2a`



