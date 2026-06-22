# Production Secret Rotation Runbook

Status: ED-SEC.1 operational runbook. This document does not rotate production secrets by itself and must not contain real secret values.

## What Leaked

During production smoke, resolved `docker compose config` output was pasted into a chat/log context. Resolved Compose output can include values from `deploy/.env`, including secrets that are intentionally absent from Git.

Treat these values as exposed:

- production API key: `Authentication__ApiKey__Key`;
- Telegram webhook secret: `AssistantEngineer__EquipmentDiagnostics__Telegram__WebhookSecret`;
- PostgreSQL password and any connection string containing it;
- any other password, token, key, or credential that appeared in the resolved output.

The notification chat ID is operational metadata rather than a password. Review it as sensitive operational context, but rotate only when the operational chat policy changes.

## What Not To Paste

Do not paste full output from:

- `docker compose config`;
- `docker inspect`;
- `env`, `printenv`, or shell variable dumps;
- `cat deploy/.env`;
- any command that expands `${...}` placeholders into real values.

If someone asks for deployment evidence, send only sanitized excerpts: service names, image/build status, health status, mount targets, and non-secret correlation IDs.

## Safe Verification Commands

Prefer commands that do not expand or print `deploy/.env` values:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml ps
docker compose --env-file deploy/.env -f deploy/docker-compose.yml build --no-cache assistantengineer-api
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up -d --no-deps --force-recreate assistantengineer-api
docker compose --env-file deploy/.env -f deploy/docker-compose.yml logs --tail 120 assistantengineer-api
```

For mount verification, avoid full inspect output. Query only the mount fields:

```bash
docker inspect assistantengineer-assistantengineer-api-1 \
  --format '{{range .Mounts}}{{println .Source "->" .Destination}}{{end}}'
```

For committed scaffold validation, this is safe because it uses placeholder-only `.env.example` and `config --quiet`:

```powershell
pwsh ./scripts/deployment/validate-deployment-scaffold.ps1 -RunDockerComposeConfig
```

Do not run `docker compose --env-file deploy/.env -f deploy/docker-compose.yml config` and paste the output. If it must be run locally for troubleshooting, redirect it to a private scratch file, redact it first, and do not commit or share the raw file.

## Generate Replacement Values

Generate candidate values locally without reading current production secrets:

```powershell
pwsh ./scripts/deployment/generate-production-secret-values.ps1
```

The script writes a timestamped file under ignored `artifacts/operations/secret-rotation/` and prints only the path plus instructions. It does not edit `deploy/.env`, does not read existing secret values, and does not print generated secrets to the terminal by default.

## Rotation Checklist

1. Open a private VPS shell. Do not screen-share or paste terminal output containing secrets.
2. Backup the current production env file locally on the server:

   ```bash
   cp deploy/.env "deploy/.env.backup.$(date -u +%Y%m%dT%H%M%SZ)"
   chmod 600 deploy/.env deploy/.env.backup.*
   ```

3. Generate new candidate values with the helper script, or generate equivalent cryptographically strong values using a reviewed password manager.
4. Manually edit `deploy/.env`. Update only the required placeholders/keys. Do not commit this file.
5. Rotate the API key value:

   ```text
   Authentication__ApiKey__Key=<new-api-key>
   ```

6. Rotate the Telegram webhook secret value:

   ```text
   AssistantEngineer__EquipmentDiagnostics__Telegram__WebhookSecret=<new-webhook-secret>
   ```

7. Rotate the PostgreSQL password in the database and in the application connection string. Use placeholders only in notes:

   ```bash
   docker exec -it <postgres-container> psql -U <postgres-admin-user> -d <database-name>
   ```

   ```sql
   ALTER USER <application-db-user> WITH PASSWORD '<new-postgres-password>';
   ```

   Then update the matching `Password=<new-postgres-password>` value in the production connection string in `deploy/.env`.

8. Rotate the Telegram bot token in BotFather only if the bot token itself was exposed. If rotated, update only the production env value for `AssistantEngineer__EquipmentDiagnostics__Telegram__BotToken`.
9. Restart the API with the updated ignored env file:

   ```bash
   docker compose --env-file deploy/.env -f deploy/docker-compose.yml up -d --no-deps --force-recreate assistantengineer-api
   ```

10. Smoke `/health`, `/ready`, deterministic EquipmentDiagnostics, Telegram polling, `/last`, and `/manuals`.
11. Verify logs with bounded tails only. Do not print full env or compose config.
12. Delete or archive any raw leaked transcript outside Git according to the incident policy. Keep only sanitized evidence.

## Post-Rotation Smoke Checklist

- API responds to `/health` and `/ready`.
- Protected API calls accept only the new API key.
- Old API key is rejected.
- Telegram polling or webhook mode starts without printing token or webhook secret.
- Manual delivery still works through `/manuals`.
- `artifacts/operations/equipment-diagnostics-manual-bindings.json` remains on the host and uncommitted.
- Logs do not contain raw API keys, webhook secrets, bot token, Postgres password, connection strings, Telegram file IDs, chat IDs, or user IDs.

## Rollback Notes

Rollback must not reintroduce leaked values unless the service is down and the owner explicitly accepts the risk. Prefer fixing the new values in `deploy/.env`, restarting the API, and keeping the leaked values revoked. If a temporary rollback is unavoidable, record the decision outside Git and rotate again immediately after service recovery.
