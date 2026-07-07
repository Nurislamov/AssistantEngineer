# GREE-ALICE-01 Cloud Probe Plan

## Goal

Prepare the first technical check for controlling Gree devices through Gree+ Cloud from the VPS side.

The goal of the probe is to confirm whether the cloud path can see and describe the user's real devices before any Yandex Smart Home runtime is added.

## Target flow

```text
Gree+ account
        ↓
Gree Cloud login
        ↓
Homes and rooms
        ↓
Split AC devices and VRF devices
        ↓
Diagnostic output for the next implementation stage
```

## What the probe must check

1. The selected Gree Cloud region can be reached.
2. The Gree+ account can authenticate.
3. The account returns at least one home.
4. Homes contain rooms or device groups.
5. The account returns real devices.
6. Device entries include stable identifiers for future mapping.
7. Split AC devices can be identified as individual controllable devices.
8. VRF systems can be identified either as individual indoor units or as gateway/child-unit structures.
9. Online/offline status is visible if the cloud returns it.
10. Device keys, tokens, or command metadata are available if the cloud returns them.

## Required input values

The probe must accept input through environment variables or command-line arguments.

Do not store secrets in committed files.

Expected inputs:

```text
GREE_ALICE_GREE_USERNAME
GREE_ALICE_GREE_PASSWORD
GREE_ALICE_GREE_REGION
GREE_ALICE_OUTPUT_DIR
```

Optional inputs:

```text
GREE_ALICE_TIMEOUT_SECONDS
GREE_ALICE_SAVE_RAW_RESPONSE
GREE_ALICE_MASK_SECRETS
```

## Required output

The probe should print a readable summary:

```text
Region
Login result
User/account id if available
Homes count
Rooms count
Devices count
Split candidates count
VRF gateway candidates count
VRF child-unit candidates count
Offline devices count
Unknown devices count
```

The probe should also save a local diagnostic JSON file when requested:

```text
artifacts/gree-alice/probe/gree-cloud-probe-<timestamp>.json
```

The saved file must mask sensitive values by default.

## Data that must be masked

The probe must not print or save full secrets by default.

Mask these fields if present:

```text
password
token
refresh_token
access_token
key
device_key
secret
authorization
```

Allowed masking format:

```text
abcd...wxyz
```

## Device fields to capture

For every discovered device, capture the fields that are available:

```text
home_id
home_name
room_id
room_name
device_id
device_name
device_type
device_model
mac
parent_id
parent_mac
child_id
child_mac
online
power
mode
target_temperature
current_temperature
fan_speed
raw_capability_names
```

If a field is not available, leave it empty instead of guessing.

## Split support check

A split AC is considered usable for the next stage when the cloud output contains:

```text
stable device id or MAC
device name
online status or command target
enough metadata to send a power command later
```

## VRF support check

A VRF system is considered usable for the next stage when at least one of these is true:

```text
each indoor unit appears as a separate controllable device
```

or

```text
a VRF gateway appears with child indoor units or child identifiers
```

or

```text
the cloud output contains enough gateway/device metadata to test a command against one selected indoor unit
```

## Success criteria

GREE-ALICE-01 is successful when the next stage can be implemented with clear inputs and expected output.

Minimum acceptable result:

```text
The probe design is documented.
Secrets handling is documented.
Split and VRF validation rules are documented.
No runtime service is added.
No production API or Telegram runtime files are changed.
```

## Failure cases to handle in the future probe

The future probe must produce clear messages for these cases:

```text
invalid region
network timeout
invalid username/password
two-factor or captcha requirement
no homes returned
no devices returned
all devices offline
unknown device format
VRF visible only as a gateway
cloud returns devices but no command metadata
```

## Next stage

The next implementation stage is:

```text
GREE-ALICE-02 — Gree Cloud probe tool scaffold
```

That stage should create a small console/tool project inside the repository, but it must still remain disconnected from production runtime.
