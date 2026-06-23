# Gree VRF next manual selection

## Current imported coverage

ED-24H.1 was an analysis-only planning stage. ED-24H.2 has since partially imported the selected GMV Mini source without using the `(1)` duplicate/revision candidate.

Post ED-24H.2 repository diagnostic knowledge:

- Packages: 7.
- Entries: 262.
- Validator issues: 0.
- Imported diagnostic source: `Service Manual for GMV6 v_2020.09.pdf`, document code `GC202001-I`.
- Runtime package scope:
  - `gree-gmv6-indoor-fault-codes`: 60 entries.
  - `gree-gmv6-outdoor-fault-protection-codes`: 120 entries.
  - `gree-gmv6-debugging-codes`: 37 entries.
  - `gree-gmv6-status-codes`: 36 entries.
  - `gree-gmv-mini-vrf-indoor-controller-codes`: 2 entries.
  - `gree-gmv-mini-vrf-outdoor-protection-codes`: 1 entry.
  - `gree-gmv-mini-vrf-status-codes`: 6 entries.

`SERVICE_MANUAL_GMV_IDU.pdf`, document code `GC202004-X`, is partially imported as additional `sourceReferences[]` on 38 existing GMV6 indoor entries. It added no package and no diagnostic entry because every reviewed code overlapped an existing GMV6 indoor answer with the same equipment type and meaning.

`SERVICE_MANUAL_GMV_MINI.pdf` is partially imported by ED-24H.2 with 9 new entries, 31 exact source-reference merges, and 90 NeedsReview contexts. `SERVICE_MANUAL_GMV_MINI (1).pdf` was not used.

The current imported knowledge is GMV6-bound and manual-backed. New Gree VRF imports must stay manual-bound and must not use catalog text, owner manuals, sales guides, external sources, OCR, or model memory to fill missing fault meanings.

## Current Telegram manual delivery coverage

The Telegram manual-library foundation is live in production and manual delivery works.

Connected production manuals:

| Manual | Document code | Delivery state |
| --- | --- | --- |
| `Service Manual for GMV6 v_2020.09.pdf` | `GC202001-I` | Connected |
| `SERVICE_MANUAL_GMV_IDU.pdf` | `GC202004-X` | Connected |

`/manuals` after `Gree d1` sends both PDF manuals because the selected diagnostic answer carries both reviewed source references.

Runtime Telegram `file_id` bindings remain outside Git under the ignored operations artifact path. This planning document does not add, expose, or require any real Telegram file binding.

## VRF series/device map summary

The equipment map identifies these outdoor VRF series:

- GMV6.
- GMV6 Anti-corrosion Series.
- GMV6 HR.
- GMV X.
- GMV X PRO.
- GMV9 Flex.
- GMV5 MAX.
- GMV Mini Star.
- GMV5 Mini.
- GMV5 Slim.
- GMV5 Home.

GMV6 is the only series with imported runtime diagnostic knowledge. GMV6 Anti-corrosion is catalog-identified only and needs review against the GMV6 service-manual scope before it is treated as covered.

Indoor VRF category coverage in the map includes duct/concealed duct, high/medium/low static duct, vertical duct, cassette variants, wall-mounted, floor-ceiling, console, column/floor-standing, concealed floor-standing, fresh-air processing units, AHU-kit, and ERV with evaporator/DX coil. These are catalog-level equipment categories, not imported diagnostic packages.

Controller, commissioning, and integration surfaces found in the map:

- Wired controllers.
- Infrared controllers.
- Infrared receivers.
- Central and zone controllers.
- Portable commissioning tools.
- PC debugging software.
- Protocol converters.
- BACnet/Modbus/KNX BMS gateways.
- G-Cloud Wi-Fi/cloud.
- Intelligent Remote Eudemon.
- Intelligent Billing.
- Dry-contact, key-card, and linkage modules.

These devices can be important diagnostic surfaces, but controller/tool manuals must not be treated as equipment fault-meaning sources unless the manual explicitly defines fault/status meanings for the target equipment context.

## Candidate manual groups

| Candidate group | Current local registry evidence | Probable value | Collision risk | Decision |
| --- | --- | --- | --- | --- |
| GMV Mini / GMV5 Mini / Slim / Home | `SERVICE_MANUAL_GMV_MINI.pdf` was partially imported by ED-24H.2; `(1)` was not used. | High if the installed equipment is compact/non-modular GMV. Service-manual source is more suitable than owner/sales docs. | Medium to high. 90 reviewed contexts still need manual review before import/merge. | Continue only from reviewed remaining contexts or actual equipment need. |
| GMV X / GMV X PRO | Local registry has `Owner's Manual GMV X DC Inverter VRF Units.pdf` and `Technical Sales Guide GMV X DC Inverter VRF Units.pdf`, but no confirmed GMV X/X PRO service manual. | High product value, but current local files are weak diagnostic authorities. | High if owner/sales text is overused; high if broad GMV X meanings overlap GMV6 without series guardrails. | Postpone import until a GMV X/X PRO service manual is available or the generic VRF service manual is proven to be that source. |
| GMV6 HR / heat recovery | Equipment map identifies heat-recovery outdoor units, hydromodule, mode exchanger, and branch selector scope. No GMV6 HR service manual is registered. | High for heat-recovery systems because scope likely differs from GMV6 heat-pump outdoor units. | High. HR/mode-exchanger/hydromodule codes must not be merged into plain GMV6 unless meaning and equipment context match. | Review when the exact GMV6 HR service manual is provided. |
| GMV9 Flex / GMV5 MAX | Catalog-identified series. No local service manual entry is registered for either specific series. | Medium to high depending on installed base. | Medium to high because series-specific outdoor meanings may overlap common GMV codes. | Postpone until exact service manuals are available. |
| Controllers CE41 / CE42 / CE52 | Local controller manuals exist: `CE41-24F(C).pdf`, `CE42-24_F(C)  v2020.10.29.pdf`, `CE52-24F(C).pdf`, and `Manual Portable Commissioning Tool CE41-24F(C).pdf`. CE41 has a known identity mismatch concern. | Medium. Useful for how engineers see/query/log errors, less reliable as primary equipment fault source. | Medium. Tool operation and displayed code lists can be mistaken for equipment fault meanings. | Review after the next equipment service manual, unless the user needs controller workflow first. |
| Commissioning / troubleshooting manuals | `Test Operation & Troubleshooting & Maintenance.pdf` exists but equipment identity is unknown. CE41 portable commissioning tool manual exists. | Potentially high if identity is resolved. | High because an unidentified troubleshooting manual can silently mix equipment families. | Identity review first; no import until equipment family, series, and document code are known. |
| Central controller / Eudemon / billing / gateway manuals | The equipment map identifies these surfaces, but the current manual registry does not contain exact service/diagnostic manuals for Eudemon, billing, BMS, or gateway devices. | Medium for integrations and remote diagnostics, lower for core equipment fault meanings. | Medium. Gateways may forward/query errors without defining the root equipment meaning. | Postpone until exact manuals are available and the target use case is integration diagnostics. |
| Other VRF-related manuals | GMV6 owner manual, C/F series owner manuals, generic Russian `GREE VRF` service manual. | Mixed. Could be useful after identity review. | High for generic or owner-level sources. | Use only after exact identity and diagnostic section review. |

## Collision/deduplication risks

The import rule remains:

`Same code + same equipment type + same meaning = one diagnostic answer`

Practical guardrails for the next import:

1. Compare candidate codes against existing GMV6 entries by code, equipment type, display source/context, signal type, and manual meaning.
2. If the same meaning is confirmed, append a reviewed `sourceReferences[]` item instead of adding a duplicate diagnostic entry.
3. If the meaning differs by series, equipment type, hydromodule, mode exchanger, controller, or display context, keep a separate series-aware answer and require clarification where Telegram cannot safely infer context.
4. Do not merge owner-manual, sales-guide, catalog, controller operation, or gateway forwarding text into equipment fault meanings.
5. Do not promote query/parameter/status screens such as controller setup or commissioning menus into fault diagnostics unless the manual explicitly defines them as diagnostic entries.
6. Keep consumer text safe and keep Installer/Engineer procedure detail inside qualified-service boundaries.

GMV Mini was the best next-source shape and ED-24H.2 imported the safe subset. The remaining risk is the 90 context variants where wording, display context, or equipment meaning did not justify an automatic merge.

## Recommended next import order

1. Continue GMV Mini only if actual equipment need points to one of the 90 NeedsReview contexts.
   - Reason: ED-24H.2 already imported the safe subset from `SERVICE_MANUAL_GMV_MINI.pdf`.
   - Source boundary: keep using only selected reviewed source evidence; do not use `SERVICE_MANUAL_GMV_MINI (1).pdf` unless a separate future identity-review stage explicitly selects it.

2. GMV X / GMV X PRO service manual.
   - Reason: the equipment map ranks GMV X/X PRO high and separate from GMV6.
   - Current blocker: the local registry has an owner manual and a technical sales guide, not a confirmed service manual.
   - Next action: ask the user for the exact GMV X/X PRO service manual, or first identify whether the generic `GREE VRF` service manual is actually this source.

3. CE42 / CE52 / CE41 controller and commissioning manuals.
   - Reason: controllers and commissioning tools are practical diagnostic surfaces for engineers.
   - Current caution: they should shape display/query/workflow support, not become primary equipment fault meanings unless they explicitly define fault semantics.
   - Next action: review model identity and code-display behavior after the next equipment service-manual import, or earlier if the user's actual workflow is controller-centric.

## Proposed next stage after ED-24H.2

`ED-24H.3 Deploy and smoke Gree GMV Mini diagnostic knowledge`

Prompt direction for that stage:

1. Deploy the committed repository state.
2. Smoke `Gree GMV Mini C0`, `Gree GMV Mini EC`, and `Gree GMV Mini A1`.
3. Confirm existing `Gree d1` and `/manuals` delivery still sends the GMV6 and GMV IDU manuals.
4. Do not add real GMV Mini Telegram file bindings until a separate reviewed runtime binding step.

## What not to import yet

- GMV X owner manual and GMV X technical sales guide as primary diagnostic sources.
- GMV X PRO until an exact service manual is available.
- GMV6 HR until the exact heat-recovery service manual is available.
- GMV9 Flex and GMV5 MAX until exact service manuals are available.
- Controller manuals as equipment fault-meaning sources before confirming whether they define faults or only display/query them.
- BMS, gateway, cloud, Eudemon, and billing manuals until the integration use case is chosen and exact manuals are available.
- Chiller, U-Match, split, Versati, FCU, ERV, and spreadsheet sources in this VRF manual-import sequence.
- Generic or Russian-filename service manuals until exact identity, series, document code, and diagnostic sections are known.

## Manual files needed from user

No new file is required to deploy ED-24H.2. Future GMV Mini review should use only explicitly selected source evidence and should not automatically use the `(1)` duplicate/revision candidate.

If the user wants GMV X / GMV X PRO next instead, provide or identify the exact GMV X / GMV X PRO service manual. The existing owner manual and technical sales guide should not be used as primary troubleshooting authority.

If the user wants heat-recovery next, provide or identify the exact GMV6 HR service manual.

## Validation expectations for the next import

If ED-24H.2 changes runtime knowledge, run:

```powershell
dotnet restore .\AssistantEngineer.sln
dotnet build .\AssistantEngineer.sln
dotnet test .\AssistantEngineer.sln
dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter EquipmentDiagnostics
dotnet run --project tools/AssistantEngineer.Tools.EquipmentDiagnosticsVerification -- verify-knowledge
```

Also run deployment validators and release publish smoke when embedded resource paths, publish behavior, manual-library metadata, or production deployment docs are touched.

Expected invariants:

- No PDF/DOC/DOCX/XLS/XLSX committed.
- No `deploy/.env` committed.
- No runtime Telegram binding JSON committed.
- No real Telegram `file_id` committed.
- No Telegram runtime behavior change unless explicitly scoped.
- No DB schema or EF migration change.
- Package and entry counts must be reported exactly.
