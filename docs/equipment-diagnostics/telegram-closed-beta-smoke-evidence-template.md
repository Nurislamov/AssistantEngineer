# Telegram Closed Beta Smoke Evidence Template

Use this committed placeholder template for sanitized manual evidence. Closed beta only; not for production or public launch. Never replace placeholders with real secrets, real domains, real chat IDs, or raw message bodies in Git.

## Release Reference

- Release candidate: `telegram-closed-beta-release-candidate.md`
- Activation date/time UTC: `<utc-date-time>`
- Operator initials: `<operator-initials>`
- Environment: `<environment-placeholder>`
- Commit SHA: `<commit-sha>`
- ED-22A evidence: `<sanitized-evidence-path>`
- ED-22C evidence: `<sanitized-evidence-path>`
- ED-22E final go/no-go and handoff evidence: `<sanitized-evidence-path>`
- Reviewed endpoint placeholder: `https://bot.example.test`

## Activation Boundary

- [ ] Telegram disabled by default was confirmed before activation.
- [ ] Chat ID discovery disabled by default was confirmed.
- [ ] Chat ID discovery may be enabled only temporarily during setup, then was disabled.
- [ ] No real secrets in Git, no real domains in Git, and no real chat IDs in Git.
- [ ] No long polling, no DB/audit persistence, and no external monitoring.

## Webhook And Access Evidence

- Webhook status evidence: `<sanitized-evidence-path>`
- Allowed chat positive path (`<redacted-chat-id>`): `<pass-or-fail>`
- Denied chat negative path (`<redacted-chat-id>`): `<pass-or-fail>`
- Discovery-disabled confirmation: `<sanitized-evidence-path>`

## Command And Diagnostic Smoke

| Check | Result | Sanitized evidence |
|---|---|---|
| `/start` | `<pass-or-fail>` | `<sanitized-evidence-path>` |
| `/help` | `<pass-or-fail>` | `<sanitized-evidence-path>` |
| Known runtime code diagnostic | `<pass-or-fail>` | `<sanitized-evidence-path>` |
| Ambiguous code diagnostic | `<pass-or-fail>` | `<sanitized-evidence-path>` |
| Unknown code diagnostic | `<pass-or-fail>` | `<sanitized-evidence-path>` |
| Too-long message | `<pass-or-fail>` | `<sanitized-evidence-path>` |
| Unsupported/free text | `<pass-or-fail>` | `<sanitized-evidence-path>` |
| Outbound failure safe handling | `<pass-or-fail>` | `<sanitized-evidence-path>` |

## Safety And Knowledge Boundary

- [ ] Runtime catalog remains the only final-answer source.
- [ ] Manual-codebook/staging/preview are not final diagnosis.
- [ ] Do not claim complete vendor manual coverage.
- [ ] Do not bypass protections or safety devices.
- [ ] No hazardous electrical/refrigerant instructions through the bot.
- [ ] There is no AI/RAG/vector-search capability.

## Sanitized Logs And Rollback

- Sanitized logs reference: `<sanitized-evidence-path>`
- Rollback drill result: `<pass-or-fail>`
- Rollback evidence: `<sanitized-evidence-path>`
- Telegram disabled after rollback: `<confirmed-or-not-confirmed>`
- Discovery disabled after rollback: `<confirmed-or-not-confirmed>`

## Review Decision

- Warnings: `<sanitized-warning-summary-or-none>`
- Blockers: `<sanitized-blocker-summary-or-none>`
- Final closed-beta decision: `<go-hold-no-go>`
- Reviewer initials: `<operator-initials>`
