# AssistantEngineer Project State

## Current stage

ED-24UX.7 - CLOSED / pushed.

Next recommended steps:

1. Run production VPS deployment/live-check for ED-24UX.7 when ready; do not mark production PASS until the live-check is done.
2. Keep the ED-24QA.1 quality baseline and ED-24OPS.1 local smoke runner green.
3. Keep ED-24MAN.3 exact model-family matching, GMV9 Flex/GMV6 HR OwnerManual acquisition, and ED-24EF.1 remaining EF enum sentinel warning cleanup as future candidates.

## Current branch

master

## Last completed work

ED-24UX.7 fixed Gree series refinement coverage and keyboard layout; the stage is local validation PASS and pushed,
with production live-check still pending.

Implementation commit: current commit (`ED-24UX.7 Fix Gree series refinement layout`).

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
- Production PASS is not marked for ED-24UX.7.

ED-24E.3 added GMV6 HR diagnostic runtime coverage from local GMV6 HR Service/Owner manuals; the stage is local validation PASS and pushed, with production live-check still pending.

Implementation commit: current commit (`ED-24E.3 Add Gree GMV6 HR diagnostics`).

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
- GMV6 HR OwnerManual binding is pending unless production DB already has one; no Telegram upload or `/manual_bind` was performed.
- No migration was added.
- JSON/cards/sourceReferences changed only for the new HR runtime series plus HR manual registry metadata.
- Routing changed only to recognize explicit `GMV6 HR` hints and preserve GMV6/HR separation.
- Deploy scripts are unchanged.
- No PDF files were committed.
- Restore/build/focused validation/smoke/full suite/diff-check: PASS locally for ED-24E.3; full solution suite 5027/5027 passed.
- Production PASS is not marked for ED-24E.3.

ED-24MAN.2b fixed diagnostic multi OwnerManual selection for GMV Mini / Slim so long OwnerManual filenames no longer break Telegram inline keyboard payload limits; the stage is local validation PASS and pushed, with production live-check still pending.

Implementation commit: current commit (`ED-24MAN.2b Fix OwnerManual selection buttons`).

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
- Known EF sentinel warnings for `TelegramLibraryAccessRequestEntity.RequestedRole`, `TelegramManualBindingEntity.DocumentType`, and `TelegramManualBindingEntity.MinRole` are unrelated/non-blocking and remain future cleanup candidate ED-24EF.1.
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
- ED-24MAN.2b - CLOSED / pushed.
- ED-24E.3 - CLOSED / pushed.

## Gree diagnostics runtime status

### GMV6

- Runtime: 263 cards.
- Fresh delta from GMV6 manual GC202203-IV was imported earlier.
- Production smoke passed.

### GMV6 HR

- Runtime: 262 cards.
- Imported from local GMV6 HR service manual in ED-24E.3.
- OwnerManual source audited; diagnostic OwnerManual binding remains pending unless present in production DB.
- Production smoke not marked PASS for ED-24E.3.

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
- EF enum default/sentinel warnings were observed for `TelegramLibraryAccessRequestEntity.RequestedRole`, `TelegramManualBindingEntity.DocumentType`, and `TelegramManualBindingEntity.MinRole`; these are unrelated/non-blocking for ED-24OPS.2b and tracked as future cleanup candidate ED-24EF.1.

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

- ED-24OPS.2a - production PASS.
- ED-24OPS.2 - production PASS.
- ED-24MAN.1 - production PASS.
- ED-24USR.3 - production PASS.
- ED-24SRC.1a - production PASS.

Latest pushed local point:

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
- General Gree n2 must show only real series where n2 exists: GMV Mini, GMV6, GMV X.
- GMV9 Flex n2 must not be added unless runtime/manual confirms it.
- Keep visible answers readable Russian: no mixed translation, no question-mark placeholders.
- Guard grammar: no 'к наружного блока', no 'к внутреннего блока', no 'к наладки системы'.
- Do not give Codex prompts automatically before discussing the next stage.

## Future candidates

- ED-24MAN.1 follow-up - Production library finalization / bind GMV Mini after ED-24SRC.2 audit, if still pending.
- ED-24MAN.3 - Manual variants by model family / exact model matching.
- ED-24EF.1 - Fix remaining EF enum default/sentinel warnings for Telegram library/manual entities.

## Current blocker

No active blocker after ED-24MAN.2 production PASS.

## Next step

Discuss one of the next possible small follow-ups: bind the audited Gree GMV Mini/Slim broad manual in production, ED-24MAN.3 manual variants by model family / exact model matching, ED-24EF.1 remaining EF enum sentinel warning hygiene, EF warning hygiene for `HourlySchedule.Factors`, or the next Gree diagnostics direction.

