# AssistantEngineer Project State

## Current stage

ED-24LIB.1 - CLOSED / pushed.

Next recommended steps:

1. Discuss whether the next small follow-up should be ED-24MAN.2 manual taxonomy / owner vs service access levels, ED-24MAN.3 manual variants by model family / exact model matching, ED-24SRC.2 Mini manual comparison, EF warning hygiene for `HourlySchedule.Factors`, or the next Gree diagnostics direction.
2. Keep the ED-24QA.1 quality baseline and ED-24OPS.1 local smoke runner green.
3. Use `.\scripts\diagnostics\run-gree-diagnostics-smoke.ps1` before deploy or after Gree diagnostics changes.

## Current branch

master

## Last completed work

ED-24LIB.1 adds the protected Telegram file library foundation on top of existing manual bindings, with Owner-only library access management and grant-gated library delivery.

Previous implementation commit: `8a3edb6a` (ED-24MAN.1).

Previous project-state commit: `2c842e6d`.

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

## Gree diagnostics runtime status

### GMV6

- Runtime: 263 cards.
- Fresh delta from GMV6 manual GC202203-IV was imported earlier.
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
- GMV Mini: 136
- GMV X: 263
- GMV9 Flex: 260
- Total Gree runtime: 922

## Validation status

ED-24OPS.1 local smoke runner:

`.\scripts\diagnostics\run-gree-diagnostics-smoke.ps1`

ED-24OPS.1 smoke:

9/9 passed

Full baseline after ED-24OPS.1:

4922/4922 passed

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

- ED-24MAN.1 - production PASS.
- ED-24USR.3 - production PASS.
- ED-24SRC.1a - production PASS.

Latest pushed local point:

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
- General Gree n2 must show only real series where n2 exists: GMV Mini, GMV6, GMV X.
- GMV9 Flex n2 must not be added unless runtime/manual confirms it.
- Keep visible answers readable Russian: no mixed translation, no question-mark placeholders.
- Guard grammar: no 'к наружного блока', no 'к внутреннего блока', no 'к наладки системы'.
- Do not give Codex prompts automatically before discussing the next stage.

## Future candidates

- ED-24MAN.2 - Manual taxonomy / owner vs service access levels.
- ED-24MAN.3 - Manual variants by model family / exact model matching.
- ED-24SRC.2 - Compare Mini manuals and decide Mini/Star/Slim handling.

## Current blocker

No active blocker after ED-24LIB.1 pushed.

## Next step

Discuss one of the next possible small follow-ups: ED-24MAN.2 manual taxonomy / owner vs service access levels, ED-24MAN.3 manual variants by model family / exact model matching, ED-24SRC.2 Mini manual comparison, EF warning hygiene for HourlySchedule.Factors, or the next Gree diagnostics direction.

