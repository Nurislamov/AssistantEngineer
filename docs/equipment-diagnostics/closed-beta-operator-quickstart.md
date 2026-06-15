# Closed Beta Operator Quickstart

This quickstart is for a controlled EquipmentDiagnostics closed beta, not production or public release.

1. Run `.\scripts\dev\verify-and-prepare-pr.ps1 -BaseRef origin/master -Scope EquipmentDiagnostics`.
2. Review the branch readiness report, PR body, beta report, and beta summary under ignored `artifacts/verification/`.
3. Run the ED-22C deployment activation dry-run, ED-22D activation checklist, ED-22E final go/no-go evidence, and ED-22F release tag/handoff checklist, then review the activation runbook and handoff before deterministic bot scenario smoke tests.
4. Choose a VPS and domain later. Create the BotFather credential last: no real secrets in Git.
5. Generate a webhook secret, configure `AllowedChatIds`, retain the `DeniedChatIds` policy, and verify access before enabling transport.
6. Run `setWebhook` and `getWebhookInfo`, then smoke-test the closed beta.
7. Keep chat identifier discovery disabled after setup and collect sanitized logs only.
8. Give every beta operator `telegram-closed-beta-operator-limitation-card.md`, complete `telegram-closed-beta-smoke-matrix.md`, and record only sanitized results in `telegram-closed-beta-smoke-evidence-template.md`.

Telegram transport and chat identifier discovery are disabled by default. There is no production deploy, external monitoring stack, database/audit persistence, AI/RAG, or full vendor manual coverage claim.
