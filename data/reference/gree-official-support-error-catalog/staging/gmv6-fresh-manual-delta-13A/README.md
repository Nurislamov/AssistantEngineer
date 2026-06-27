# ED-24GEC.13A GMV6 Fresh Manual Delta

Fresh manual used: `JF00304129, Export T1_R410A_GMV6 GMV Service Manual (Asia Pacific), D.2.pdf`.

Manual identity: GMV6 DC Inverter VRF Units, `GC202203-IV`, capacity 22.4kW~272.0kW. Chapter 3 Faults / Error Indication was extracted from the local PDF text.

Old comparison manual: `JF00304235, export T3_R410A_GMV6 GMV Service Manual (Saudi Arabia), B .3.pdf`, `GC202005-I`. It was used only for comparison; no runtime rows were imported from it.

## Counts

- Fresh manual inventory rows: 263.
- Existing GMV6 runtime before ED-24GEC.13A: 255.
- Already covered in GMV6 runtime: 255.
- Delta rows confirmed in GC202203-IV: 8.
- Imported: 8.
- Manual review: 0.
- GMV6 runtime after: 263.
- GMV Mini unchanged: 136.
- Total Gree runtime after: 399.

## Imported Delta Codes

- `A9` - Set Back function.
- `n1` - Defrosting cycle K1 setting.
- `qA` - Heat recovery status.
- `qC` - Mainly cooling.
- `qH` - Mainly heating.
- `qP` - Export region setting for PV VRF units.
- `qU` - Grid voltage configuration.
- `Uy` - PV module over-temperature protection.

## Representative Telegram Queries

- `Gree GMV6 A9`
- `Gree GMV6 n1`
- `Gree GMV6 qA`
- `Gree GMV6 Uy`

## Scope Guard

This stage touched only GMV6 runtime, GMV6 package counts, the manual registry, report artifacts, and tests. GMV Mini runtime, X/Flex runtime, deploy, environment, migrations, frontend, Telegram polling, service-request, and phone-flow behavior are out of scope.
