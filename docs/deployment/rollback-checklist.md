# Rollback Checklist

Use this checklist for a reviewed deployment rollback. Never commit secrets or restored environment files.

1. Notify users and operators that rollback is in progress.
2. Stop the current stack with `stop-production-stack.ps1` or reviewed `docker compose down`.
3. If Telegram delivery contributes to the incident, run `delete-telegram-webhook.ps1`.
4. Verify Telegram state with `get-telegram-webhook-info.ps1`.
5. Restore the previous ignored `.env` from the protected operator backup.
6. Select the previous reviewed image tag or digest and run `docker compose up -d`.
7. Run `smoke-production-stack.ps1` against the restored stack.
8. Confirm frontend, API health, bot diagnostics, and expected Telegram-disabled state.
9. Record the incident, deployed version, rollback command, and verification result outside Git secrets.
10. Attach only sanitized log excerpts and correlation IDs; exclude chat IDs, Telegram message bodies, and secrets.

There is no automated rollback controller, database restore workflow, or production audit log in the current
scaffold.
