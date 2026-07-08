# GREE-ALICE CONNECT-only dry-run sample matrix

## Stage

`GREE-ALICE-21` defines safe offline sample scenarios for the MQTT CONNECT-only dry-run command.

No scenario below opens DNS, TCP, TLS, MQTT, or device-control traffic.

## Matrix

| Scenario | Inputs | Expected status | Expected gates |
|---|---|---|---|
| configuration-only | `--configuration-only` | `configuration-only` | `not-evaluated` |
| no env | no GREE_ALICE_MQTT_* values | `blocked-fail-closed` | all blocked |
| dummy-valid env | dummy client id, username, auth mode, safe flags | `dry-run-ready-for-separate-live-safety-stage` | all blocked |
| invalid auth mode | `GREE_ALICE_MQTT_AUTH_MODE=invalid` | `blocked-fail-closed` | all blocked |
| unsafe subscribe | `GREE_ALICE_MQTT_ALLOW_SUBSCRIBE=true` | `blocked-fail-closed` | all blocked |
| forbidden payload | `--payload forbidden` | `blocked-fail-closed` | all blocked |

## Dummy-valid env example

Use only dummy values in repository validation:

```powershell
$env:GREE_ALICE_MQTT_HOST = "mqtt-hk.gree.com"
$env:GREE_ALICE_MQTT_PORT = "1994"
$env:GREE_ALICE_MQTT_CLIENT_ID = "dummy-client-id"
$env:GREE_ALICE_MQTT_USERNAME = "dummy-username"
$env:GREE_ALICE_MQTT_AUTH_MODE = "token"
$env:GREE_ALICE_MQTT_TOKEN = "dummy-token"
$env:GREE_ALICE_MQTT_KEEP_ALIVE_SECONDS = "60"
$env:GREE_ALICE_MQTT_CONNECT_TIMEOUT_SECONDS = "5"
$env:GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK = "true"
$env:GREE_ALICE_MQTT_ALLOW_SUBSCRIBE = "false"
$env:GREE_ALICE_MQTT_ALLOW_PUBLISH = "false"
```

Expected interpretation:

```text
Dry-run structurally passes, but live CONNECT remains blocked.
```

## Safety assertions

Every sample must preserve:

```text
Output contains raw values: no
Network connection opened: no
MQTT CONNECT implementation included: no
MQTT CONNECT sent: no
MQTT SUBSCRIBE sent: no
MQTT PUBLISH sent: no
Device control sent: no
Raw credentials stored: no
```

## Operator rule

A dry-run pass is only a prerequisite for later review. It is not approval to connect to MQTT and not approval to control a device.
