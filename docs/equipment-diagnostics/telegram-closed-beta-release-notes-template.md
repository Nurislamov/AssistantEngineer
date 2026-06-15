# Telegram Closed Beta Release Notes Template

Use placeholders only. Closed beta only; not for production or public launch.

## Release Reference

- Release tag: `<release-tag>`
- Commit SHA: `<commit-sha>`
- Release date UTC: `<release-date-utc>`

## Scope

- EquipmentDiagnostics Telegram closed-beta candidate scope: `<evidence-path>`

## Included ED Milestones

- ED-22A through ED-22F evidence and handoff: `<evidence-path>`

## Evidence Artifacts

- Release evidence archive: `<evidence-path>`
- Final go/no-go evidence: `<evidence-path>`
- Sanitized smoke and rollback evidence: `<evidence-path>`

## Known Limitations

- No real secrets in Git, no real domains in Git, and no real chat IDs in Git.
- Telegram disabled by default and chat ID discovery disabled by default remain unchanged.
- No long polling, no DB/audit persistence, and no external monitoring.
- Runtime catalog remains the only final-answer source.
- Manual-codebook/staging/preview are not final diagnosis.
- Do not claim complete vendor manual coverage.
- Do not bypass protections or safety devices.
- No hazardous electrical/refrigerant instructions through the bot.
- Tag/handoff does not call Telegram network.
- Tag/handoff does not execute setWebhook/getWebhookInfo/deleteWebhook.

## Operator Instructions

- Operator initials: `<operator-initials>`
- Approved handoff evidence: `<evidence-path>`
- Proceed to ED-23A only in a manually approved operational window.

## Rollback

- Tagged rollback reference: `<release-tag>`
- Rollback owner: `<rollback-owner>`
- Rollback evidence: `<evidence-path>`

## Sign-Off

- Release reviewer: `<operator-initials>`
- Activation operator: `<operator-initials>`
- Rollback owner: `<rollback-owner>`
