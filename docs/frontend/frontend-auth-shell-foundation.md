# Frontend Auth Shell Foundation

## Purpose

This document defines the frontend auth-shell foundation for staged SaaS security rollout without introducing real login/provider integration in this step.

## Scope

This foundation covers:

- auth state model;
- principal and organization context view models;
- protected content wrapper behavior;
- unauthorized and forbidden UI states;
- development compatibility mode;
- API transport placeholder strategy for future auth headers.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No external identity provider integration claim.
- No certified/certification claim.

## Auth state model

- `unknown`
- `anonymous`
- `authenticated`
- `unauthorized`
- `forbidden`

The default app behavior remains anonymous-compatible for local development while backend auth enforcement is still staged.

## Principal model

Frontend principal view model carries lightweight fields:

- `userId`
- `organizationId`
- `externalSubjectId`
- `displayName`
- `email`
- `roles`
- `permissions`
- `isAuthenticated`

No secrets, tokens, or API keys are stored in this model.

## Organization context model

Organization context is placeholder-only in this step:

- `organizationId`
- `organizationSlug`
- `organizationName`
- `isActive`

Tenant-aware navigation and ownership UX remain future work.

## Protected content model

`ProtectedContent` supports staged behavior:

- when auth compatibility mode is active, content remains accessible;
- when auth is required and principal is anonymous, render unauthorized state;
- when principal is authenticated but missing permission, render forbidden state;
- when permission checks pass, render protected children.

## Development compatibility

- Auth shell defaults to development-friendly anonymous mode.
- Forced login is not enabled by default.
- Existing workflow UI remains compatible without backend auth-route changes.

## API client strategy

- Frontend includes `createAuthHeaders` placeholder returning empty headers by default.
- No token persistence and no API key hardcoding are introduced.
- Future stages can map authenticated session context to secure headers.

## Current limitations

- No real login flow.
- No JWT/OIDC SDK integration.
- No tenant-scoped navigation enforcement.
- No authoritative frontend session source from backend yet.
- Frontend source guardrails (including secret/token checks) are tracked in `docs/security/security-regression-guardrails.md`.

## Future login/provider integration

Planned future work:

- connect frontend auth state to backend authenticated principal endpoint;
- add provider-specific login/session handling;
- add secure token lifecycle handling and rotation UX.

## Future tenant-aware navigation

Planned future work:

- organization switcher;
- permission-aware navigation and action guards;
- tenant-scoped route and resource presentation.
