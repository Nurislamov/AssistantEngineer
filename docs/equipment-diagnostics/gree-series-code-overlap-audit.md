# Gree series code overlap audit

Stage: `ED-24UX.7`

## Runtime baseline

| Series | Cards |
| --- | ---: |
| GMV6 HR | 262 |
| GMV6 | 263 |
| GMV Mini | 136 |
| GMV X | 263 |
| GMV9 Flex | 260 |
| **Gree total** | **1184** |

The audit found 275 unique normalized codes. Of these, 263 occur in two or more series and 12 occur only in
GMV Mini. All 262 GMV6 HR codes overlap at least one other series, so GMV6 HR must appear whenever one of those
codes is refined. `FH` is a separate overlap that exists only in GMV6 and GMV X.

## Complete overlap matrix

The full matrix is grouped by identical series membership. Within each group, every listed code has exactly the
membership shown in the heading.

### GMV6 HR + GMV6 + GMV Mini + GMV X + GMV9 Flex (123)

`A0, A2, A3, A4, A6, A7, A8, A9, AB, AD, AE, AF, AH, AJ, AL, AP, AU, B1, B2, B3, B4, B5, B6, B7, B9, BH, C0, C2, C3, C4, C5, C6, C8, C9, CA, CB, CC, CF, CH, CJ, CL, CU, D1, D3, D4, D6, D7, D8, D9, DA, DB, DC, DE, DH, DL, E0, E1, E2, E3, E4, ED, F0, F1, F3, F5, FP, J0, J1, J7, J8, J9, JL, L0, L1, L2, L3, L4, L5, L6, L7, L8, L9, LA, LC, LH, N0, N1, N4, N6, N7, N8, NA, NC, NE, NF, NH, P0, P1, P2, P3, P4, P5, P6, P7, P8, P9, PC, PE, PF, PH, PJ, PL, PP, U0, U2, U4, U5, U6, U8, U9, UC, UE, UL`

### GMV6 HR + GMV6 + GMV X + GMV9 Flex (137)

`AC, AN, AY, B8, BA, BB, BD, BE, BF, BJ, BN, BP, BU, C1, C7, CD, CE, CN, CP, CY, D2, D5, DD, DF, DJ, DN, DP, DU, DY, F6, F7, F8, F9, FA, FB, FC, FD, FE, FF, FJ, FL, FN, FU, G0, G1, G2, G3, G4, G5, G6, G7, G8, G9, GA, GB, GC, GD, GE, GF, GH, GJ, GL, GN, GP, GU, GY, H0, H1, H2, H3, H4, H5, H6, H7, H8, H9, HA, HC, HE, HF, HH, HJ, HL, HP, HU, J2, J3, J4, J5, J6, JA, JC, JE, JF, LB, LE, LF, LJ, LL, LP, LU, N3, N5, N9, NB, NJ, NN, NU, O0, O1, O2, O3, O4, O5, O6, O7, O8, O9, OA, OB, OC, PA, PU, QA, QC, QH, QP, QU, U3, UD, UF, UN, Y1, Y2, Y7, Y8, YA`

### GMV6 HR + GMV6 + GMV Mini + GMV X (1)

`N2`

### GMV6 HR + GMV6 + GMV X (1)

`UY`

### GMV6 + GMV X (1)

`FH`

### GMV Mini only (12)

`01, 02, 03, 04, 05, 06, 07, 08, A1, A5, AA, EC`

## Representative checks

| Code | Runtime series |
| --- | --- |
| n2 | GMV6 HR, GMV6, GMV Mini, GMV X |
| E0 | GMV6 HR, GMV6, GMV Mini, GMV X, GMV9 Flex |
| U4 | GMV6 HR, GMV6, GMV Mini, GMV X, GMV9 Flex |
| C2 | GMV6 HR, GMV6, GMV Mini, GMV X, GMV9 Flex |
| A9 / A0 / AJ / C0 / L1 / db | all five series |
| H5 / J5 / o1 | GMV6 HR, GMV6, GMV X, GMV9 Flex |
| 01 | GMV Mini only |

## Refinement decision

- Generic Gree code queries build the series list from the actual searchable runtime candidates.
- The stable order is GMV6 HR, GMV6, GMV Mini, GMV X, GMV9 Flex.
- Only series that contain the code are shown.
- Series buttons use at most two buttons per row and include `Не знаю`.
- Explicit series queries remain separate: GMV6 HR is not treated as plain GMV6.
- No diagnostic cards were added, removed, or edited.
- No manual bindings or manual policy were changed.
- No PDF files were committed.
