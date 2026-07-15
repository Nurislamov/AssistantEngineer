# Evidence Index

Live evidence remains outside git and is treated as read-only.

Root:

```text
D:\AssistantEngineer-live-evidence\GREE-ALICE-API-DISCOVERY
```

## Top-level roots

| Path | Artifact type | Timestamp | Safe to commit | Purpose |
| --- | --- | --- | --- | --- |
| `D:\AssistantEngineer-live-evidence\GREE-ALICE-API-DISCOVERY\00-setup` | setup logs | 2026-07-10 | no | Host/lab setup evidence. |
| `D:\AssistantEngineer-live-evidence\GREE-ALICE-API-DISCOVERY\01-local-ui-check` | local UI/proxy logs | 2026-07-10 | no | Initial local UI and proxy readiness. |
| `D:\AssistantEngineer-live-evidence\GREE-ALICE-API-DISCOVERY\02-wireguard` | WireGuard/proxy material | 2026-07-10 | no | Contains CA/config material; index only. |
| `D:\AssistantEngineer-live-evidence\GREE-ALICE-API-DISCOVERY\03-android-lab` | Android lab evidence | 2026-07-11 to 2026-07-15 | no | Main GREE+ Android discovery stream. |
| `D:\AssistantEngineer-live-evidence\GREE-ALICE-API-DISCOVERY\04-analysis` | offline analysis evidence | 2026-07-13 | no | Google/Alexa/AppFlip/API side analysis. |

## Key inspected artifacts

| Path | Type | Version | Size | SHA256 | Leak check | Safe to commit | Related decision |
| --- | --- | --- | ---: | --- | --- | --- | --- |
| `...\static-apk-inventory\run-20260712-103917\06-reports\summary.txt` | summary | v1.0.4 | 2503 | `9BCAB6D27692A3B2F6900DCF1F739ECA89BFB021C61883AC2FCD2866E54FB305` | sanitized summary | no | `GREE_APK_STATIC_INVENTORY_PASS` |
| `...\flutter-plugin-contract\run-20260712-111123\06-reports\summary.txt` | summary | v1.0.4 | 3662 | `3025DEEB216309E1CC6F0FE63C7525F46C26C0A7F9EF03DDB45392246233E09A` | PASS via paired leak check | no | `GREE_FLUTTER_PLUGIN_CONTRACT_PASS` |
| `...\flutter-plugin-contract\run-20260712-111123\06-reports\leak-check.txt` | leak check | v1.0.4 | 95 | `B9A94B2413D8BFBD839526E7A1E43466B4AF7C75EBBD2EE10F619868955143D1` | PASS | no | plugin extraction safe summary |
| `...\flutter-aot\run-20260714-122159\report-v1.0.5\summary-v1.0.5.txt` | summary | v1.0.5 | 1078 | `79EBEA4713CFC860DD6220829045E8F04DC3CDEE3BC7CB61EAEC64A42AEE4919` | static-only | no | `GREE_FLUTTER_AOT_REPORT_PASS` |
| `...\flutter-aot\run-20260714-122159\channel-proof-v1.0.10\report\channel-proof-summary-v1.0.10.txt` | summary | v1.0.10 | 345 | `52341CF287AFA0F8F64E1CBCB49E57573BCFDC4A6BBE8AE6CD96E726E5BEF0DD` | static-only | no | channel candidate proof |
| `...\gree-live-methodchannel-inventory-after-v48\run-20260714-210522\sanitized-report\summary.txt` | summary | v1.0.23 | 824 | `50290EB8CCE2C1966057C1514724306A48EB4854FFC6EDDFB80ACFD29A503E95` | PASS | no | live MethodChannel inventory |
| `...\managed-methodchannel-handler-mapping\run-20260715-093048\sanitized-report\summary.md` | summary | v1.0.40 | 416 | `73876FC9ECDAD629CDCC255212DCD8D02E8283538C93E4D4EFD333708B8BB7E7` | PASS | no | `no-managed-handler-match-found` |
| `...\post-bypass-direct-jni-methodchannel-gate\run-20260715-144607\sanitized-report\summary.txt` | summary | v1.0.44c | 636 | `DF9D8FCC39E304920F5AC410EF661640EC5B494EC90AB0ACD290810C929321FB` | PASS | no | direct JNI entrypoints confirmed |
| `...\art-method-anchor-feasibility-gate\run-20260715-151505\sanitized-report\summary.txt` | summary | v1.0.45 | 832 | `EA939D9415B45CC957F0FE70328953827D5A62E22A4684C894EE1AD20A356A1A` | PASS | no | shared ART stub candidate |
| `...\artmethod-slot-discrimination-gate\run-20260715-154903\sanitized-report\summary.txt` | summary | v1.0.46 | 842 | `D90EAD2FE862CF775F13690AC0DD53287B63CFF5D375EBFD3956E0CE87E410F7` | PASS | no | slot16 unique / slot24 shared |
| `...\slot16-code-kind-diagnostic\run-20260715-162142\sanitized-report\summary.txt` | summary | v1.0.47a | 770 | `0FBDC4E64589D586AC1B2D3C14242CFD3BFC87317C04D74B020F5C5FAB8F8C44` | PASS | no | slot16 CodeItem-like |
| `...\executenterp-register-filter-feasibility\run-20260715-164229\sanitized-report\summary.txt` | summary | v1.0.48 | 936 | `18839BD14753C8054EA74A47B06C327DDF36F97A5131EED2B281C8E6695E7427` | PASS | no | x0 ArtMethod candidate |
| `...\executenterp-target-artmethod-correlation\run-20260715-213420\sanitized-report\summary.txt` | summary | v1.0.49 | 1026 | `82682AA08B6598CF93064ED03E9F610C2DD5CE9B94C6DE85F0066E8AF1558794` | PASS | no | invalid jmethodID equality gate |
| `...\executenterp-target-artmethod-correlation\run-20260715-222613\sanitized-report\summary.txt` | summary | v1.0.49a | 1332 | `86CFA6BADE4CF1B01AD851AC5478A4C96F8C5B602DE63C77BF8A04FBD72AE39D` | PASS | no | invalid live CodeItem deref gate |
| `...\executenterp-target-artmethod-correlation\run-20260715-230048\sanitized-report\summary.txt` | summary | v1.0.49b | 1506 | `20D071FD0AEF88A77DA4E7029EAC517740D29601E995F8407F0A541E04FC561A` | PASS | no | invalid deferred gate attempt |
| `...\executenterp-target-artmethod-correlation\run-20260715-230048\GREE-executenterp-target-artmethod-correlation-v1.0.49b-report.zip` | ZIP report | v1.0.49b | 3167 | `C90004A9ABA44DE118F8EDE1121390D36DC3377B4A243BBB4C364957C0748DEC` | PASS inside ZIP | no | provided archive analysis |

## Notable non-committable areas

- `02-wireguard` includes CA/config material and must not be copied.
- `03-android-lab\tools` includes local runtimes/venvs and is not evidence to commit.
- Raw JSON/JSONL, PCAP-like, proxy, app data, and private folders are local-only unless explicitly sanitized and leak-checked.
- At least one early sanitized network/static branch reported `Leak check: FAIL`; those artifacts are local evidence only and are not imported into git.

## ZIP copies

Only one v1.0.49b report ZIP was found at the user-provided path during this pass:

```text
D:\AssistantEngineer-live-evidence\GREE-ALICE-API-DISCOVERY\03-android-lab\executenterp-target-artmethod-correlation\run-20260715-230048\GREE-executenterp-target-artmethod-correlation-v1.0.49b-report.zip
```

SHA256: `C90004A9ABA44DE118F8EDE1121390D36DC3377B4A243BBB4C364957C0748DEC`.
