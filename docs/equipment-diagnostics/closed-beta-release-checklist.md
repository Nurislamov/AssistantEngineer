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
- [ ] `AllowedChatIds` and `DeniedChatIds` policy is reviewed.
- [ ] Webhook setup and status checks pass before inviting closed-beta operators.
- [ ] Runtime catalog remains the only final-answer source.
- [ ] Partial manual-backed coverage and every other known limitation are communicated.
- [ ] ED-22A Telegram closed-beta release evidence pack is generated and manually reviewed before activation.

Before any later production release, add an approved hosting/domain plan, secret management, external monitoring, audit persistence, and a separate production security review.
