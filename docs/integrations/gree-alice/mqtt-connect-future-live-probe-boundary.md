# GREE-ALICE-26: future CONNECT-only live probe boundary

## Purpose

This boundary note defines what a future live CONNECT-only stage may and may not do.

It is not that future stage. It contains no live code, no network operation, and no approval.

## Maximum allowed scope for a later explicit live-safety stage

```text
One manually triggered CONNECT-only attempt.
No SUBSCRIBE.
No PUBLISH.
No device control.
No automatic retry loop.
No background service.
No deployment.
No runtime bridge integration.
Masked output only.
Immediate cleanup of local credentials from process environment after run.
```

## Required preconditions for later stage

```text
Human sign-off result is ready-for-separate-connect-only-safety-stage.
Target account is explicitly selected.
Target device is explicitly selected.
Credentials are provided only through process environment variables.
Output path is under ignored artifacts/ folder.
No raw values are printed.
No raw values are committed.
Rollback is simply no further attempts.
```

## Still forbidden after this document

```text
Live CONNECT is still blocked.
SUBSCRIBE is still blocked.
PUBLISH is still blocked.
Device control is still blocked.
Yandex Smart Home runtime action is still blocked.
AssistantEngineer.Api integration is still blocked.
Telegram integration is still blocked.
Deployment is still blocked.
Migrations are still blocked.
```

## Next possible larger stage

```text
GREE-ALICE-30 — choose live CONNECT-only proof or offline bridge skeleton path
```
