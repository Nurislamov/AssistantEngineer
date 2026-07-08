# GREE-ALICE CONNECT-only dry-run contract tests

## Stage

`GREE-ALICE-20` adds guard tests for the offline MQTT CONNECT-only dry-run command.

This stage does not implement MQTT `CONNECT`, does not open TCP/TLS/MQTT connections, does not subscribe, does not publish, and does not control a device.

This stage does not name or cite third-party protocol sources.

## What is tested

The tests verify:

```text
dry-run command defines the expected environment contract;
dry-run command rejects topic/payload/control arguments;
dry-run command masks provided values and stores no raw credentials;
dry-run command does not contain live MQTT/network implementation markers;
dry-run documentation keeps CONNECT/SUBSCRIBE/PUBLISH/control blocked;
Program.cs registers --mqtt-connect-dry-run.
```

## Test file

```text
tests/AssistantEngineer.Tests/GreeAlice/MqttConnectDryRunContractSafetyTests.cs
```

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
GREE-ALICE-21 — Dry-run sample matrix and operator instructions
```

That future stage should document safe operator scenarios and still avoid live MQTT CONNECT.
