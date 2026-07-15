# Lab Runbook

This is a recovery runbook for future offline/lab work. It is not permission to run a new experiment in this documentation stage.

## Boundaries

Do not run GREE+, Frida, ADB, observers, launchers, proxy changes, network requests, or HVAC commands during documentation recovery.

For any future separately approved lab stage:

- no power on/off;
- no temperature, fan, mode, scene, schedule, settings, bind/unbind, or firmware changes;
- no production API, Telegram, database, migrations, deployment, or runtime wiring;
- no raw payload, raw ByteBuffer, credentials, cookies, tokens, MACs, emails, or account identifiers in git.

## Recovery checklist

1. Confirm the evidence root exists:

```text
D:\AssistantEngineer-live-evidence\GREE-ALICE-API-DISCOVERY
```

2. Confirm package and device identity from existing evidence before any future device operation:

```text
package: com.gree.greeplus
device: SM-A715F / Android 13 / RZ8N92EZGHD
Frida: 17.15.4
```

3. Check package PID state only in a separately approved live stage.

4. Prefer reusing an existing bypassed foreground process if the preparation evidence says it is alive and clean.

5. If the app is not already prepared, use the approved launcher/preparation path only. Do not manually start GREE+ and assume it is observer-ready.

6. Confirm preparation markers:

```text
original PID observed
replacement PID observed
replacement attach-success
patch-set-ready
patch-watchdog-heartbeat
CLEANUP marker
foreground PID selected
```

7. Treat two package PIDs as expected during replacement/preparation. Choose the foreground PID by the launcher/run evidence, not by guesswork.

8. Require cleanup of the preparation Frida session before main observer attach.

9. Do not force-stop GREE+ after successful preparation unless the run explicitly requires rollback. The main observer needs the prepared foreground process to remain alive.

10. If `CLEANUP` is missing, stop and record the run as preparation-incomplete. Do not treat the observer as started.

## Observer phases

Expected safe observer phases for a future target correlation run:

- attach to foreground PID;
- script loaded;
- VM perform entered;
- exact shared `ExecuteNterpImpl` address validated;
- hook ready;
- armed/baseline/interaction/cooldown;
- listener detached;
- post-detach classification if designed;
- sanitized result, statuses, errors, summary, and leak check exported.

## Safe UI actions

Only safe navigation actions are allowed in a future approved lab stage, such as opening GREE+ and staying on the device list. Do not open controls that send or risk sending HVAC changes.

## Expected report artifacts

A complete report should include:

- `summary.txt`;
- `result.json`;
- `statuses.json`;
- `errors.json`;
- `leak-check.txt`;
- optional host result and console transcript;
- ZIP integrity and SHA256;
- source agent/host/supervisor hashes where available.

## After failure

Collect only safe artifacts:

- host error type;
- detach reason;
- PID/session information;
- hook-ready/gate-complete flags;
- status count and last status kind;
- error count and sanitized error type;
- leak check result;
- whether any payload/register/raw values were exported.

Do not collect raw payloads, credentials, or arbitrary register dumps.
