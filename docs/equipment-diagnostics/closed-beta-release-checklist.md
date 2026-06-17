# EquipmentDiagnostics Closed Beta Release Checklist

This checklist authorizes only a reviewed closed beta. It does not authorize production or public release.

- [ ] Branch readiness is `PASS` with zero blockers.
- [ ] Beta readiness report has zero blockers; every warning is reviewed.
- [ ] Backend and frontend tests pass.
- [ ] Deployment scaffold and CI dry-run validation pass.
- [ ] Operations redaction smoke passes; only sanitized logs are retained.
- [ ] Telegram transport remains disabled until intentionally configured.
- [ ] Chat identifier discovery remains disabled after initial access setup.
- [ ] No real secrets in Git; real domains, PDFs, logs, and generated reports are also absent.
- [ ] `BootstrapOwnerChatId` is explicitly configured, `TelegramUsers` migration is applied, and `DeniedChatIds` policy is reviewed.
- [ ] Unknown Telegram users become `Consumer`; admin commands are hidden from Consumer help.
- [ ] ED-21B UX smoke passes: Consumer Russian `/start`/`help`/diagnostic replies, contact sharing button, Russian `/me`, Owner/Admin admin commands, Engineer technical response, no SQL log noise, and no GSSAPI missing-library warning after image rebuild.
- [ ] Webhook setup and status checks pass before inviting closed-beta operators.
- [ ] Runtime catalog remains the only final-answer source.
- [ ] Partial manual-backed coverage and every other known limitation are communicated.
- [ ] ED-22A Telegram closed-beta release evidence pack is generated and manually reviewed before activation.
- [ ] ED-22B release candidate, operator limitation card, and smoke matrix are reviewed.
- [ ] ED-22C deployment activation dry-run passes and its ignored generated evidence is manually reviewed.
- [ ] ED-22D activation checklist passes; the activation runbook and smoke evidence template are reviewed.
- [ ] ED-22E final go/no-go report has zero blockers, its decision and warnings are manually reviewed, and the final handoff remains placeholder-only.
- [ ] ED-22F release tag/handoff checklist passes; after merge into `master`, an authorized operator manually creates the reviewed annotated tag and archives evidence outside Git.

Before any later production release, add an approved hosting/domain plan, secret management, external monitoring, audit persistence, and a separate production security review.
