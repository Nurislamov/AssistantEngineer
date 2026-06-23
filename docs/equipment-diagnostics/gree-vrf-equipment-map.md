# Gree VRF Equipment Catalog Map

## Purpose

ED-24H.0 adds a catalog-level map of Gree VRF/GMV equipment so future manual intake can target the right service manuals, controller manuals, and gateway manuals.

This is not a service-manual import. No diagnostic codes, packages, entries, Telegram lookup logic, role policy, database schema, or runtime behavior changed.

## Source Catalogues

| Source ID | Local file | Language | Git committed |
| --- | --- | --- | --- |
| `gmv6-catalogue-en` | `artifacts/manual-intake/sources/gree/GMV6 Catalouge.pdf` | English | No |
| `gree-vrf-catalog-141367-ru` | `artifacts/manual-intake/sources/gree/141367.pdf` | Russian | No |
| `gmv6-2023-ru` | `artifacts/manual-intake/sources/gree/GMV6 2023 РУС.pdf` | Russian | No |

## Series Coverage

| Series | Equipment type | Source catalogues | Current coverage | Next manual needed |
| --- | --- | --- | --- | --- |
| GMV6 | Outdoor unit | `gmv6-catalogue-en`, `gmv6-2023-ru` | Imported, partial manual-backed knowledge exists | Already imported GMV6 service manual; continue reviewed source-bound imports only |
| GMV6 Anti-corrosion Series | Outdoor unit variant | `gmv6-catalogue-en` | CatalogIdentified | Review whether GMV6 service manual covers this variant |
| GMV6 HR | Heat-recovery outdoor, hydromodule, mode exchanger, branch selector | `gree-vrf-catalog-141367-ru`, `gmv6-2023-ru`, `gmv6-catalogue-en` | CatalogIdentified | GMV6 HR service manual |
| GMV X | Outdoor unit | `gmv6-catalogue-en`, `gree-vrf-catalog-141367-ru`, `gmv6-2023-ru` | CatalogIdentified | GMV X service manual |
| GMV X PRO | Outdoor unit | `gree-vrf-catalog-141367-ru` | CatalogIdentified | GMV X PRO service manual |
| GMV9 Flex | Outdoor unit | `gree-vrf-catalog-141367-ru` | CatalogIdentified | GMV9 Flex service manual |
| GMV5 MAX | Outdoor unit | `gmv6-2023-ru` | CatalogIdentified | GMV5 MAX service manual |
| GMV Mini Star | Outdoor unit | `gree-vrf-catalog-141367-ru`, `gmv6-2023-ru` | CatalogIdentified | GMV Mini Star service manual |
| GMV5 Mini | Outdoor unit | `gree-vrf-catalog-141367-ru`, `gmv6-2023-ru` | CatalogIdentified | GMV5 Mini service manual |
| GMV5 Slim | Outdoor unit | `gree-vrf-catalog-141367-ru`, `gmv6-2023-ru` | CatalogIdentified | GMV5 Slim service manual |
| GMV5 Home | Outdoor unit, hydromodule | `gree-vrf-catalog-141367-ru`, `gmv6-2023-ru` | CatalogIdentified | GMV5 Home service/hydromodule manual |

## Indoor Unit Types

| Indoor type | Source catalogues | Expected diagnostic source | Notes |
| --- | --- | --- | --- |
| Duct / concealed duct | All three | Yes | Includes generic duct/concealed duct references |
| High static duct | `gmv6-catalogue-en`, `gmv6-2023-ru` | Yes | Distinct high-static duct subtype |
| Medium static duct | `gmv6-catalogue-en` | Yes | Identified in the English GMV6 catalogue |
| Low static duct | `gmv6-catalogue-en`, `gmv6-2023-ru` | Yes | Distinct low-static duct subtype |
| Vertical duct | `gmv6-2023-ru` | Yes | Russian GMV6 catalogue identifies vertical duct units |
| Cassette | All three | Yes | Parent cassette type |
| 360 / round-flow cassette | `gmv6-catalogue-en`, `gmv6-2023-ru` | Yes | 360-degree or 8-flow cassette |
| Compact cassette | `gmv6-catalogue-en`, `gmv6-2023-ru` | Yes | Compact 360 cassette subtype |
| Two-way cassette | `gmv6-catalogue-en`, `gmv6-2023-ru` | Yes | Two-way cassette subtype |
| One-way cassette | `gmv6-catalogue-en`, `gmv6-2023-ru` | Yes | One-way cassette subtype |
| Wall-mounted | All three | Yes | Wall-mounted indoor units |
| Floor-ceiling | All three | Yes | Floor-ceiling indoor units |
| Console | All three | Yes | Console indoor units |
| Column / floor-standing | All three | Yes | Floor-standing/column units |
| Concealed floor-standing | `gmv6-catalogue-en` | Yes | Exact Russian naming needs review |
| Fresh air processing indoor unit | All three | Yes | Fresh-air GMV indoor unit |
| AHU-kit | All three | Yes | AHU connection kit |
| ERV with evaporator / DX coil | All three | Yes | Heat-recovery ventilation connected to GMV |

## Controls And Accessories

| Device/category | Examples | Display | Query | Log | Forward | Source catalogues |
| --- | --- | --- | --- | --- | --- | --- |
| Infrared remote controllers | `YAP1F`, `YAP1F7` | No | No | No | No | `gmv6-catalogue-en`, `gmv6-2023-ru` |
| Infrared receivers | `JS05`, `JS13` | Unknown | No | No | No | `gmv6-catalogue-en`, `gmv6-2023-ru` |
| Wired controllers | `XK46`, `XE7A-*`, `XE70-33/H`, `XE73-23/HC` | Yes | Yes | Mixed/unknown | Mixed/unknown | All three |
| Central controllers | `CE52-24/F(C)`, `CE55-24/F(C)`, `CE58-00/EF(CM)` | Yes | Yes | Yes | Unknown | All three |
| E-Smart zone controller | `CE54-24/F(C)` | Yes | Yes | Unknown | Unknown | `gmv6-catalogue-en` |
| Portable commissioning tools | `CE42-24/F(C)`, `DE43-00/EF(CM)` | Yes | Yes | Yes | Unknown | All three |
| PC debugging software | `DE42-33/A(C)` | Yes | Yes | Yes | Unknown | `gmv6-catalogue-en`, `gmv6-2023-ru` |
| Protocol converters / building protocol gateway | Building protocol gateway | Unknown | Yes | Unknown | Yes | `gmv6-catalogue-en`, `gree-vrf-catalog-141367-ru` |
| BACnet/Modbus BMS gateways | `ME30-24/D1(BM)`, `ME30-24/E6(M)`, `ME31-33/EH1(M)` | Unknown | Yes | Unknown | Yes | All three |
| KNX-capable remote dispatching path | KNX mention | Unknown | Unknown | Unknown | Yes | `gmv6-2023-ru` |
| G-Cloud Wi-Fi/cloud controller | `G-Cloud`, `ME31-00/F13` | Unknown | Yes | Unknown | Yes | All three |
| Intelligent Remote Eudemon | Intelligent Remote Eudemon | Yes | Yes | Yes | Yes | All three |
| Intelligent Billing Eudemon / energy billing | Intelligent Billing | Unknown | Yes | Yes | Yes | `gmv6-catalogue-en`, `gree-vrf-catalog-141367-ru` |
| Dry-contact, linkage, and key-card modules | `LE60-24/H1`, `DQ01/A` | No | No | No | Yes | `gmv6-catalogue-en` |

## Manual Search Backlog

| Priority | Target | Desired manual type | Reason |
| --- | --- | --- | --- |
| 1 | GMV X and GMV X PRO service manuals | ServiceManual | Separate outdoor series with no imported diagnostic package |
| 1 | GMV6 HR service manual | ServiceManual | Heat-recovery/hydromodule scope is not covered by imported GMV6 knowledge |
| 1 | GMV IDU multi-source diagnostic reference model | ServiceManual | ED-24F.1 stopped because 38 indoor codes overlap GMV6 indoor entries |
| 1 | Wired, infrared, central, and zone controller manuals | ControllerManual | Controllers can display/query/log errors |
| 1 | Portable commissioning tool and PC debugging software manuals | CommissioningToolManual | Tools/software are diagnostic and commissioning surfaces |
| 2 | GMV9 Flex service manual | ServiceManual | Separate outdoor series |
| 2 | GMV5 MAX service manual | ServiceManual | Separate outdoor series in GMV6 2023 Russian catalogue |
| 2 | GMV Mini Star, GMV5 Mini, GMV5 Slim, and GMV5 Home service manuals | ServiceManual | Compact/non-modular/home VRF series are catalog-identified only |
| 2 | BMS, BACnet/Modbus/KNX, Wi-Fi/cloud, remote monitoring, and billing gateway manuals | GatewayManual | Gateways can query, forward, or log system status/errors |

Future manual library policy remains plan-only: Installer, Engineer, Admin, and Owner may receive reviewed manuals; Consumer must not receive manuals.

## Next Recommended Stages

- ED-24H.1: completed the next-manual selection analysis without importing entries.
- ED-24H.2: inspect and import `SERVICE_MANUAL_GMV_MINI.pdf` after duplicate review against `SERVICE_MANUAL_GMV_MINI (1).pdf`.
- Future: import GMV X / GMV X PRO only after an exact service manual is available or the generic VRF service manual is proven to be that source.
- Future: import GMV6 HR, GMV9 Flex, GMV5 MAX, controller, commissioning, BMS, cloud, billing, and remote-monitoring manuals only after exact manual identity and actual equipment need are confirmed.

No new diagnostic codes were imported.
