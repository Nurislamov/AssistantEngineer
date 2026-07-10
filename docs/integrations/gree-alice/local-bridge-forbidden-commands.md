# Local Bridge Forbidden Commands

This stage is offline/local only. Do not run commands that leave the local isolated bridge boundary.

Forbidden on GREE-ALICE-51:

- `curl` or `Invoke-RestMethod` to real Yandex production endpoints
- `curl` or `Invoke-RestMethod` to real Gree+ Cloud endpoints
- MQTT CONNECT
- MQTT SUBSCRIBE
- MQTT PUBLISH
- Running production deployment scripts
- Adding production env files
- Adding OAuth client secrets
- Adding access/refresh tokens
- Adding Gree credentials
- Adding real account/device IDs
- Running commands that send device control
- Running scripts outside offline/local scope
- Committing smoke evidence with secrets

Also forbidden:

- Real OAuth endpoint implementation
- Production OAuth callback
- Real provider registration data
- Production provider credentials
- Live HTTP calls to Yandex
- Live HTTP calls to Gree+ Cloud
- Device command execution
- Production runtime wiring
