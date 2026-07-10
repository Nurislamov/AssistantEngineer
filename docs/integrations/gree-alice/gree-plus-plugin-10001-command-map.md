# Gree Plus Plugin 10001 Command Map

Stage: GREE-ALICE-TRACE-2
Scope: Gree Plus Webplugin, Appliance_mid 10001
Safety: no secrets, no real MAC, no raw logs
Timers: out of scope

## Summary

This document fixes the redacted command map captured from controlled Gree Plus traces.

Main fields:

- Power: Pow
- Temperature: SetTem, TemUn, TemRec
- Mode: Mod
- Fan: WdSpd, Quiet, Tur
- Features: Lig, Blo, Health, SvSt, SwhSlp, SlpMod
- Swing: SwUpDn, SwingLfRig

## Mode values

- Mod 0: Auto
- Mod 1: Cool
- Mod 2: Dry
- Mod 3: Fan
- Mod 4: Heat

## Fan values

- WdSpd 0: Auto
- WdSpd 1: Low
- WdSpd 2: Medium-low
- WdSpd 3: Medium
- WdSpd 4: Medium-high
- WdSpd 5: High
- Quiet 2: Quiet
- Tur 1: Turbo

## Swing values

Vertical SwUpDn:

- 1: swing
- 2: angle 1
- 3: angle 2
- 4: angle 3
- 5: angle 4
- 6: angle 5

Horizontal SwingLfRig:

- 1: swing
- 2: angle 1
- 3: angle 2
- 4: angle 3
- 5: angle 4
- 6: angle 5

## JSON source

Canonical machine-readable map:

docs/integrations/gree-alice/gree-plus-plugin-10001-command-map.json

## Next step

Implement an isolated command builder in a later stage:

GREE-ALICE-CMD-1 Add isolated Gree Plus command builder