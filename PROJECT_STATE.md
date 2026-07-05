# AssistantEngineer Project State

## Current stage

ED-24GMVMINI CLOSED / production PASS.

GMV Mini diagnostics audit, function-setting wording hotfix, CI and production Telegram smoke are completed.

## Current branch

master

## Last completed work

ED-24GMVMINI.1-2 was merged through PR #52.

- Merge commit: b781d872
- Implementation commit: 27ec74a0
- GMV Mini cards: 136 -> 148
- Indoor: 27
- Outdoor: 62
- Status: 47 -> 59
- Added GMV Mini function/status codes:
  - qd
  - n3
  - n5
  - nL
  - nU
  - q7
  - q8
  - q9
  - qF
  - qL
  - qn
  - qU
- Added model applicability:
  - GMV-180WL/C-X(D)
  - GMV-280WL/C1-X
  - GMV-335WL/C1-X
  - GMV-280WL/C1-X(S)
  - GMV-335WL/C1-X(S)

ED-24GMVMINI.3 production smoke passed.

ED-24GMVMINI.4 fixed visible wording for GMV Mini q/n function-setting cards.

- PR: #53
- Merge commit: 4de47413
- Implementation commit: f6e04698
- Replaced confusing hardcoded GMV-141WL/C-T visible instruction with model-safe wording.
- qL/qF/q/n codes remain service/function settings, not component faults.
- Production smoke passed for:
  - Gree GMV Mini GMV-224WL/C1-X qL
  - Gree GMV Mini GMV-280WL/C1-X qL
  - Gree GMV Mini GMV-141WL/C-T qL

## Current blocker

None.

## Important decisions

GMV Mini function-setting/status codes must stay non-alarming.

For q/n service function codes, visible wording must not imply that the requested model is wrong just because the source function list was associated with GMV-141WL/C-T.

Preferred wording:

"Для некоторых моделей GMV Mini набор сервисных функций отличается. Перед изменением настройки проверьте доступность этой функции для конкретной модели наружного блока."

Do not use public/user-visible provenance wording such as:

- manual
- source
- packageId
- карточка неисправности
- по таблице
- основание
- руководство

## Files changed recently

ED-24GMVMINI.1-2:

- data/equipment-diagnostics/error-knowledge/gree/gmv-mini/**
- data/equipment-diagnostics/error-knowledge/packages/gree-gmv-mini-vrf-*.json
- data/equipment-diagnostics/equipment-catalog/gree-vrf-equipment-map.json
- data/equipment-diagnostics/manual-library/manuals.json
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/**

ED-24GMVMINI.4:

- data/equipment-diagnostics/error-knowledge/gree/gmv-mini/status/n3.json
- data/equipment-diagnostics/error-knowledge/gree/gmv-mini/status/n5.json
- data/equipment-diagnostics/error-knowledge/gree/gmv-mini/status/nl.json
- data/equipment-diagnostics/error-knowledge/gree/gmv-mini/status/nu.json
- data/equipment-diagnostics/error-knowledge/gree/gmv-mini/status/q7.json
- data/equipment-diagnostics/error-knowledge/gree/gmv-mini/status/q8.json
- data/equipment-diagnostics/error-knowledge/gree/gmv-mini/status/q9.json
- data/equipment-diagnostics/error-knowledge/gree/gmv-mini/status/qd.json
- data/equipment-diagnostics/error-knowledge/gree/gmv-mini/status/qf.json
- data/equipment-diagnostics/error-knowledge/gree/gmv-mini/status/ql.json
- data/equipment-diagnostics/error-knowledge/gree/gmv-mini/status/qn.json
- data/equipment-diagnostics/error-knowledge/gree/gmv-mini/status/qu.json
- tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvMiniVisibleWording12_2Tests.cs

No production env changes.
No EF migrations.
No generated artifacts committed.

## Validation status

ED-24GMVMINI.1-2:

- Focused diagnostics/Telegram: 1352/1352 PASS
- Branch readiness: PASS
- Restore: PASS
- Build: PASS, 0 warnings
- Full suite: 5314/5314 PASS
- Engineering Core V1: PASS
- PR checks: 7/7 PASS
- Master CI after merge: PASS
- Production smoke: PASS

ED-24GMVMINI.4:

- GMV Mini focused: 91/91 PASS
- Restore: PASS
- Build: PASS, 0 warnings
- Full suite: 5319/5319 PASS
- Engineering Core V1: PASS
- git diff --check: PASS
- PR checks: 7/7 PASS
- Production smoke: PASS
- Production logs: clean
- GMV Mini counts unchanged: 148 total, 27 indoor, 62 outdoor, 59 status
- Gree runtime count unchanged: 1308
- GMV6, GMV X and GMV9 Flex runtime JSON unchanged

## Known backlog

ED-24UX.LAST — Improve /last display to preserve matched series/model label.

Current behavior:

- /last works and preserves the latest code.
- It may display `Gree C0` instead of preserving the matched series/model label such as `Gree GMV Mini C0`.

This is not a production blocker.

ED-24OPS.CLEANUP — Move VPS env backups outside repo or ignore safe backup pattern.

CI maintenance:

- Node.js 20 deprecation warning remains future maintenance.

## Next step

Recommended next stage:

ED-24UX.LAST — improve /last display to preserve matched series/model label.

Alternative:

ED-24OPS.CLEANUP — cleanup VPS env backup handling.
