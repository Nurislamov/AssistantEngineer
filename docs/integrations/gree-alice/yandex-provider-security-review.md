# Yandex Provider Security Review

## Purpose

Define the security review gate for future Yandex Smart Home provider readiness.

## Current Status

Security review status: NOT APPROVED

Provider readiness is NOT READY by default.

Provider registration is NOT APPROVED.

## Secrets Policy

No secrets in repository.

Production secrets must be stored outside repository.

## Token Policy

Real tokens are not issued in this stage.

No real access tokens or refresh tokens may be committed.

Token revocation plan is required before production readiness.

## Yandex Credential Policy

Real Yandex credentials/tokens must not be stored in repo.

Real OAuth is not implemented.

Real provider registration is NOT APPROVED.

## Gree Credential Policy

No real Gree credentials, device keys, account identifiers, device identifiers, or MAC-like identifiers may be committed.

## Registry Scope Policy

Yandex user must map to bridge account and explicit registry scope.

The bridge must never return the global registry by default.

## Masked Evidence Policy

Evidence must be masked. Real account, device, token, credential, and identifier material is forbidden.

## Unknown/Unlinked User Policy

Unknown/unlinked users must receive no devices or fail-closed behavior.

## Action Fail-Closed Policy

Device control remains fail-closed until a separate explicit approval stage.

## MQTT Blocked Policy

MQTT is blocked.

## Production Wiring Policy

Production endpoint is not configured.

Production deploy is not enabled.

Production runtime wiring is disabled.

## Audit Requirements

Audit logging plan is required before any production readiness decision.

## Monitoring Requirements

Monitoring plan is required before any production readiness decision.

## Rollback/Kill-Switch Requirements

Rollback owner and kill-switch owner must be assigned before any provider publication decision.

## Approval Status

Security review status: NOT APPROVED

Provider publication remains NOT APPROVED.
