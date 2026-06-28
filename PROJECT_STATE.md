# AssistantEngineer Project State

## Current stage

ED-24UX.4a - CLOSED / production PASS.

Next recommended steps:

1. Keep the ED-24QA.1 quality baseline and ED-24OPS.1 local smoke runner green.
2. Use `.\scripts\diagnostics\run-gree-diagnostics-smoke.ps1` before deploy or after Gree diagnostics changes.
3. Discuss either ED-24UX.5 (shorten Gree diagnostic safety boilerplate) or ED-24SRC.1 (add a separate source reference action for diagnostics).

## Current branch

master

## Last completed work

ED-24UX.4a polished live-reviewed Gree Telegram answer UX with safe HTML formatting, clearer labels, and no runtime knowledge or routing changes.

Commit: `e24ae712`.

## Current working point

- ED-24GEC.15.1 - CLOSED / production PASS.
- ED-24QA.1 - CLOSED / pushed.
- ED-24OPS.1 - CLOSED / pushed.
- ED-24UX.4 - CLOSED / pushed.
- ED-24UX.4a - CLOSED / production PASS.

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

## Current blocker

No active production blocker after ED-24UX.4a.

## Next step

Discuss one of:

- ED-24UX.5 Shorten Gree diagnostic safety boilerplate.
- ED-24SRC.1 Add separate source reference action for diagnostics.

