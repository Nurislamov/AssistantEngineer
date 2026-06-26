# ED-24GEC.11 GMV6 Manual Import Report

Source manual: `Service Manual for GMV6 v_2020.09` (`gree-gmv6-service-manual-2020-09`, document `GC202001-I`).
Source path: `artifacts/manual-intake/sources/gree/Service Manual for GMV6 v_2020.09.pdf`.

## Summary

- GMV6 manual-confirmed codes in inventory: 255
- Already present in GMV6 runtime: 255
- New GMV6 runtime JSON added in ED-24GEC.11: 0
- Manual-review rows retained from tracked support catalog codes: 17

The existing GMV6 runtime already matches the manual-library count (`entriesImported = 255`). ED-24GEC.11 records the full inventory and keeps the add plan empty rather than duplicating entries.

## Counts By Category

| Category | Count | Package |
| --- | ---: | --- |
| outdoor | 121 | `gree-gmv6-outdoor-fault-protection-codes` |
| indoor | 60 | `gree-gmv6-indoor-fault-codes` |
| status | 37 | `gree-gmv6-status-codes` |
| debugging | 37 | `gree-gmv6-debugging-codes` |

## Added Codes

No new GMV6 runtime JSON files were required in this stage; all 255 manual-confirmed GMV6 entries were already present.

## Inventory Files

- `gmv6-manual-code-inventory-11.csv`
- `gmv6-manual-code-inventory-11.json`
- `gmv6-runtime-add-plan-11.csv`
- `gmv6-manual-review-11.csv`

## Codes By Category

### Outdoor (121)

b1, b2, b3, b4, b5, b6, b7, b8, b9, bA, bb, bd, bE, bF, bH, bJ, bn, bP, bU, E0, E1, E2, E3, E4, Ed, F0, F1, F3, F5, F6, F7, F8, F9, FA, Fb, FC, Fd, FE, FF, FH, FJ, FL, Fn, FP, FU, G0, G1, G2, G3, G4, G5, G6, G7, G8, G9, GA, Gb, GC, Gd, GE, GF, GH, GJ, GL, Gn, GP, GU, Gy, H0, H1, H2, H3, H4, H5, H6, H7, H8, H9, HA, HC, HE, HF, HH, HJ, HL, HP, HU, J0, J1, J2, J3, J4, J5, J6, J7, J8, J9, JA, JC, JE, JF, JL, P0, P1, P2, P3, P4, P5, P6, P7, P8, P9, PA, PC, PE, PF, PH, PJ, PL, PP, PU

### Indoor (60)

d1, d2, d3, d4, d5, d6, d7, d8, d9, dA, db, dC, dd, dE, dF, dH, dJ, dL, dn, dP, dU, dy, L0, L1, L2, L3, L4, L5, L6, L7, L8, L9, LA, Lb, LC, LE, LF, LH, LJ, LL, LP, LU, o0, o1, o2, o3, o4, o5, o6, o7, o8, o9, oA, ob, oC, y1, y2, y7, y8, yA

### Status (37)

A0, A2, A3, A4, A6, A7, A8, Ab, AC, Ad, AE, AF, AH, AJ, AL, An, AP, AU, Ay, n0, n2, n3, n4, n5, n6, n7, n8, n9, nA, nb, nC, nE, nF, nH, nJ, nn, nU

### Debugging (37)

C0, C1, C2, C3, C4, C5, C6, C7, C8, C9, CA, Cb, CC, Cd, CE, CF, CH, CJ, CL, Cn, CP, CU, Cy, U0, U2, U3, U4, U5, U6, U8, U9, UC, Ud, UE, UF, UL, Un

## Manual Review

The review CSV keeps tracked support-catalog rows that are not GMV6 runtime entries. They remain outside GMV6 when evidence points to another series, is visually ambiguous, or lacks matching manual evidence.
