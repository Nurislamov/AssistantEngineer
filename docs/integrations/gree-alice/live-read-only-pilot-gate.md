# GREE-ALICE-42: live read-only pilot gate

## Purpose

This stage adds a formal offline gate for a possible future live read-only pilot.

The gate is not live adapter implementation and is not a live connection stage.

## Default status

```text
Pilot gate status: NOT APPROVED
Pilot gate open: false
Live read-only pilot allowed: false
Live read-only adapter enabled: false
Live control allowed: false
MQTT allowed: false
Production wiring allowed: false
```

The live read-only pilot remains blocked by default. Any future pilot requires a separate explicit manual approval and a separate stage.

## Manual requirements

The gate cannot open until all requirements are manually closed:

```text
RepositoryCleanAndSynced
AllTestsPass
BridgeRemainsIsolated
ControlAdapterBlocked
MqttBlocked
NoProductionDeploymentWiring
NoSecretsInRepository
CredentialsStoredOutsideRepository
EvidenceMasksAccountAndDeviceIdentifiers
OperatorApprovesExactAccountAndDeviceScope
KillSwitchPlanDocumented
RollbackPlanDocumented
PilotLimitedToReadOnly
ReadOnlyAdapterImplementationReviewed
ManualApprovalRecorded
```

## Safety boundary

Control remains forbidden.

MQTT CONNECT, MQTT SUBSCRIBE, and MQTT PUBLISH remain forbidden.

Production runtime wiring and production deployment wiring remain forbidden.

Secrets, credentials, real account identifiers, real device identifiers, MAC addresses, PCAP, CSV, and raw artifacts must not be stored in the repository.

The offline evaluator must not read environment variables, credentials, files with secrets, network resources, Gree+ Cloud, MQTT, or runtime state. It returns `not-approved` and blocked by default.

## Decision record

Use `live-read-only-pilot-decision-record-template.md` for any future manual decision record.

The template remains `NOT APPROVED` by default and must use masked account/device identifiers only.

## Next stage

The next stage is:

```text
GREE-ALICE-43 — add control safety approval package
```

GREE-ALICE-43 may prepare a control safety approval package, but it still does not include live control implementation, live MQTT, production wiring, deployment changes, or migrations.
