# Telegram Closed Beta Activation Runbook

## Purpose

This runbook defines the human-reviewed activation and smoke sequence for the EquipmentDiagnostics Telegram closed beta only; not for production or public launch. It does not activate Telegram by itself.

## When To Use

Use only after ED-22A release evidence, ED-22B release-candidate review, and ED-22C deployment dry-run evidence pass. Stop when a prerequisite, safety boundary, or access-policy review is incomplete.

## Prerequisites

- Approved closed-beta operators and activation reviewer.
- Reviewed HTTPS deployment prepared outside this repository.
- Passing activation checklist, deployment dry-run, release evidence, and smoke matrix.
- No real secrets in Git, no real domains in Git, and no real chat IDs in Git.
- Telegram disabled by default and chat ID discovery disabled by default.

## Activation Roles

- **Activation operator:** prepares server-side configuration and performs reviewed manual actions.
- **Safety reviewer:** confirms access policy, diagnostic boundaries, and smoke evidence.
- **Rollback operator:** remains ready to stop delivery and confirm readiness after rollback.

## Secrets Handling

1. Create the BotFather token manually outside Git and store it in the approved server secret store.
2. Generate a unique webhook secret manually outside Git.
3. Never place token, webhook secret, domain, chat ID, or raw message body in committed files or generated evidence.
4. Keep sanitized evidence limited to statuses, timestamps, commit SHA, and redacted references.

## Server-Side Environment Preparation

1. Copy the committed placeholder environment example to the ignored server environment file.
2. Set real values only on the reviewed server or secret store; do not paste or print them in this runbook.
3. Keep Telegram transport disabled while validating health, readiness, allow/deny policy, and rollback access.
4. Confirm no long polling is configured.

## Temporary Chat ID Discovery Phase

Chat ID discovery may be enabled only temporarily during setup.

1. Enable discovery on the reviewed server only.
2. Restart the reviewed service and use the existing identity command from the approved beta chat.
3. Record the identifier only in the server-side allowlist; evidence must use `<redacted-chat-id>`.
4. Disable discovery immediately after the approved chat is identified.
5. Restart and confirm chat ID discovery disabled by default behavior is restored.

## Allowlist And Denylist Review

- Require a non-empty reviewed allowlist for the closed beta.
- Review the denylist; deny must win over allow.
- Confirm discovery is off before transport activation.
- Confirm no real chat IDs appear in Git or evidence templates.

## Enabling Transport

After all reviewers approve the access policy, enable Telegram transport only in the reviewed server environment. Confirm health and readiness before webhook setup. The committed defaults remain disabled.

## Manual Webhook Setup And Verification

The activation operator may manually run the existing `set-telegram-webhook.ps1` script using server-side secret inputs, then manually run `get-telegram-webhook-info.ps1`. Do not paste command output containing sensitive values into Git. ED-22D does not execute either script and makes no Telegram network call.

## Manual Smoke Sequence

Complete every row in `telegram-closed-beta-smoke-matrix.md` and record only sanitized results in `telegram-closed-beta-smoke-evidence-template.md`:

1. Health/readiness and access-policy checks.
2. `/start` and `/help`.
3. Known, ambiguous, and unknown diagnostic codes.
4. Too-long and unsupported/free-text handling.
5. Safe outbound-failure handling and sanitized logs.
6. Rollback drill.

Runtime catalog remains the only final-answer source. Manual-codebook/staging/preview are not final diagnosis. Do not claim complete vendor manual coverage. There is no AI/RAG/vector-search capability.

## Safety Boundary

- Do not bypass protections or safety devices.
- No hazardous electrical/refrigerant instructions through the bot.
- A qualified technician and exact installed-equipment service manual remain required for hazardous or final diagnostic work.

## Rollback

1. The rollback operator may manually run the existing `delete-telegram-webhook.ps1`.
2. Set `TELEGRAM_IS_ENABLED=false` in the reviewed server environment.
3. Restart the stack and confirm health/readiness.
4. Confirm delivery stopped and discovery remains disabled.
5. Retain only sanitized evidence.

## Sanitized Evidence Collection

- Use the smoke evidence template and `<sanitized-evidence-path>` references.
- Do not retain real secrets, real domains, real chat IDs, raw Telegram payloads, or raw message bodies.
- There is no DB/audit persistence and no external monitoring in this closed-beta workflow.

## Post-Activation Review

- Review blockers, warnings, access-list behavior, safety/provenance display, rollback result, and sanitized logs.
- Confirm generated artifacts remain ignored and uncommitted.
- Confirm Telegram and chat ID discovery defaults in Git remain disabled.

## Go / No-Go States

- **GO for controlled closed beta:** every prerequisite and smoke check passes; blockers are zero; reviewers sign off.
- **HOLD for review:** warnings or incomplete sanitized evidence require human review.
- **NO-GO:** any blocker, unsafe response, access-policy failure, secret exposure, discovery-left-on condition, or rollback failure.
