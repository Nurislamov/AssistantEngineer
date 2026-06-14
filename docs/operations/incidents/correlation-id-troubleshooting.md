# Correlation ID Troubleshooting

`X-Correlation-ID` is a non-secret troubleshooting identifier. Generate a compact value containing only letters,
digits, `_`, `-`, or `.`, up to 128 characters.

```powershell
$correlationId = "incident-$([Guid]::NewGuid().ToString('N'))"
Invoke-WebRequest -Uri "http://localhost:8080/health" -Headers @{ "X-Correlation-ID" = $correlationId }
```

Confirm the response echoes the same ID. Search only sanitized logs:

```powershell
.\scripts\operations\collect-sanitized-logs.ps1 `
  -IncludeDockerComposeLogs `
  -ServiceName assistantengineer-api `
  -CorrelationId $correlationId
```

Record correlation IDs in the incident report so operators can connect safe request start/completion and Telegram
outbound events. A correlation ID is not secret, but it does not authorize access and is not an audit record.

Never include raw Telegram messages, chat IDs, BotToken, WebhookSecret, Authorization or Telegram secret headers,
raw request/response bodies, or internal artifact paths when sharing results.
