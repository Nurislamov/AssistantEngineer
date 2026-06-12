# Deployment Environment Variables

`deploy/.env.example` is a placeholder-only template. Copy it to ignored `deploy/.env` and configure values outside
Git. Do not put real bot tokens, webhook secrets, domains, or credentials in source-controlled files.

## Scaffold Variables

| Variable | Purpose | Default/example |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | API environment | `Production` |
| `API_LOCAL_PORT` | Loopback API smoke port | `8080` |
| `FRONTEND_LOCAL_PORT` | Loopback frontend smoke port | `8081` |
| `HTTP_PORT` / `HTTPS_PORT` | Reverse proxy ports | `80` / `443` |
| `VITE_API_BASE_URL` | Existing frontend build-time API base URL | `http://localhost/` |
| `VITE_API_VERSION` | API version used by frontend routes | `1` |
| `TELEGRAM_IS_ENABLED` | Explicit transport enable switch | `false` |
| `TELEGRAM_ENABLE_CHAT_ID_DISCOVERY` | Temporary `/id` setup switch | `false` |

Telegram `BotToken`, `WebhookSecret`, `AllowedChatIds`, and `DeniedChatIds` use the existing nested ASP.NET
environment variable names shown as comments in `.env.example`. They intentionally have no committed values.

No database variables are defined because ED-18A adds no database service or persistence stage.
