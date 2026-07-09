# GREE-ALICE-30: offline fixture model boundary

## Purpose

This document defines the safe fixture model that later bridge skeleton tests may use.

Fixtures must be dummy-only and must not contain raw values copied from Gree+ Cloud reports.

## Fixture device record

```text
id: dummy-gree-ac-001
name: Demo Gree AC
room: Demo Room
type: devices.types.thermostat.ac
online: true | false
capabilities: on_off, mode, temperature, fan_speed
source: offline-fixture
```

## Fixture state record

```text
device_id: dummy-gree-ac-001
on: true | false
mode: cool | heat | fan_only | dry | auto
target_temperature_c: 16..30
fan_speed: auto | low | medium | high
online: true | false
updated_by: offline-fixture
```

## Forbidden fixture data

```text
No real MAC address.
No real Gree+ account id.
No real token.
No real password.
No real device key.
No real MQTT topic.
No raw cloud response.
No PCAP-derived payload.
```

## Later mapping rule

Real Gree+ Cloud data may be mapped to this model only after a separate masked mapping stage, and the mapping must not commit secrets or raw identifiers.
