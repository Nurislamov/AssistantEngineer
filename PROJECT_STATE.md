# AssistantEngineer Project State

## Current stage

ED-24GEC.15.1 - CLOSED / production PASS.

Next recommended steps:

1. Start with this project-state checkpoint after ED-24GEC.15 / ED-24GEC.15.1.
2. Then do small technical hygiene stage ED-24TD.4:
   - add or update .gitattributes;
   - exclude .ae-tools/** from GitHub language stats;
   - keep helper Python scripts from appearing as project language.
3. After that discuss the next Gree diagnostics direction: GMV-W, Versati, chillers, fan coils, or another priority.

## Current branch

master

## Last completed work

ED-24GEC.15.1 cleaned visible manual/document references and fixed n2 ambiguity behavior after GMV9 Flex import.

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

Full baseline after ED-24GEC.15.1:

4907/4907 passed

Production smoke after ED-24GEC.15.1:

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

No active production blocker after ED-24GEC.15.1.

Pending technical housekeeping:

- ED-24TD.4 should exclude .ae-tools/** from GitHub language stats.

## Next step

Commit this PROJECT_STATE.md update first.

Then start:

ED-24TD.4 Exclude helper tooling from GitHub language stats

