# Closed Beta Operator Quickstart

This quickstart is for a controlled EquipmentDiagnostics closed beta, not production or public release.

1. Run `.\scripts\dev\verify-and-prepare-pr.ps1 -BaseRef origin/master -Scope EquipmentDiagnostics`.
2. Review the branch readiness report, PR body, beta report, and beta summary under ignored `artifacts/verification/`.
3. Run the ED-22C deployment activation dry-run, ED-22D activation checklist, ED-22E final go/no-go evidence, and ED-22F release tag/handoff checklist, then review the activation runbook and handoff before deterministic bot scenario smoke tests.
4. Choose a VPS and domain later. Create the BotFather credential last: no real secrets in Git.
5. Configure one reviewed `BootstrapOwnerChatId`, apply the Telegram user/session/phone/history migrations, retain the `DeniedChatIds` policy, and verify access before enabling transport.
6. Use polling mode by default; run `deleteWebhook`/`getWebhookInfo`, then smoke-test bootstrap owner, Consumer Russian UX, contact sharing button, role promotion, block/unblock, and phone sharing. Use `setWebhook` only for reviewed webhook fallback.
7. Keep chat identifier discovery disabled after setup and collect sanitized logs only.
8. Give every beta operator `telegram-closed-beta-operator-limitation-card.md`, complete `telegram-closed-beta-smoke-matrix.md`, and record only sanitized results in `telegram-closed-beta-smoke-evidence-template.md`.

Consumer users see short Russian public-safe messages and a Telegram contact button when their phone is not saved. Owner/Admin can see admin commands; Engineer receives technical diagnostics without admin commands. ED-23A adds private `/history` and `/last` diagnostic history, but still does not add ServiceLead/CRM, admin global history browsing, web-admin, photo/OCR, external monitoring, audit persistence, AI/RAG, or full vendor manual coverage claim.
