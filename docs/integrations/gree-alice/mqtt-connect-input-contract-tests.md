# GREE-ALICE CONNECT-only input contract tests

## Stage

`GREE-ALICE-18` adds guard tests for the MQTT CONNECT-only input contract and safety documentation.

This stage does not implement MQTT `CONNECT`, does not open TCP/TLS/MQTT connections, does not subscribe, does not publish, and does not control a device.

This stage does not name or cite third-party protocol sources.

## What is tested

The tests verify:

```text
MqttConnectInputValidationCommand defines the expected fail-closed environment contract.
Offline validation/evidence commands do not contain live MQTT/network implementation markers.
GREE-ALICE docs and PROJECT_STATE.md do not contain third-party protocol source references.
CONNECT-only safety specification keeps subscribe, publish, and control blocked.
```

## Test file

```text
AssistantEngineer.Tests/GreeAlice/MqttConnectInputContractSafetyTests.cs
```

The exact test project path may differ by repository layout, but the script places the file under the discovered `AssistantEngineer.Tests` project.

## Safety

```text
MQTT CONNECT implementation included: no
Network connection opened: no
MQTT CONNECT sent: no
MQTT SUBSCRIBE sent: no
MQTT PUBLISH sent: no
Device control sent: no
```

## Next stage proposal

```text
GREE-ALICE-19 — CONNECT-only dry-run command contract
```

That future stage should still be offline unless explicitly approved otherwise.

## GREE-ALICE-19 follow-up

`GREE-ALICE-19` adds an offline dry-run command for the future CONNECT-only input contract.

The dry-run command validates inputs and writes masked output only. It still does not implement MQTT `CONNECT`, open network connections, subscribe, publish, or control a device.
