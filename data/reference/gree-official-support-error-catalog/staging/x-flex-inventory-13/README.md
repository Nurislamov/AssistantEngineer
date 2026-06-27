# ED-24GEC.13 X/Flex Inventory

Scope: report-only inventory for Gree X series and Gree 9 series Flex. No runtime JSON, package manifest, Telegram behavior, deploy, environment, migration, frontend, polling, service-request, or phone-flow files are changed.

## Sources Found

- X series: `gree-gmv-x-owner-manual` (`Owner's Manual GMV X DC Inverter VRF Units.pdf`) exists locally, text extraction works, and pages around the error indication section contain indoor, outdoor, debugging, query, and status code tables.
- X series: `gree-gmv-x-technical-sales-guide` (`Technical Sales Guide GMV X DC Inverter VRF Units.pdf`) exists locally and text extraction works, but it is an applicability/engineering guide, not a troubleshooting authority.
- 9 series Flex: `source-missing`. Repository evidence identifies GMV9 Flex as a separate catalog series, but no local service manual/source record or PDF was found.

## Inventory Counts

- X series candidate code rows: 63.
- 9 series Flex candidate code rows: 0.
- Ready for import: 0.
- Needs manual review: 63.
- Source missing: 1 source gap for GMV9 Flex.
- Website-only / support-only rows ready for import: 0.

## Runtime/Package State

- Current Gree runtime folders remain limited to `gmv`, `gmv6`, and `gmv-mini`.
- No `gmv-x`, `x-series`, `gmv9-flex`, `9-series-flex`, or Flex package manifests exist or were created.
- Code-string overlaps with GMV6/GMV Mini are listed in `runtime-overlap-13.csv`; overlaps are not evidence for reusing meanings across series.

## Recommendation

Next stage should not import runtime yet. The best next step is to obtain or identify the exact GMV X / GMV X PRO service or after-sales maintenance manual, because the local GMV X owner manual has useful code tables and explicit source text, but also says specific unit fault and maintenance require engineering debugging and after-sales maintenance material. GMV9 Flex should wait until an exact service manual is added.
