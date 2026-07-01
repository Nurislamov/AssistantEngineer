-- ED-24MAN.4 idempotent metadata correction for the uploaded Gree U-Match R32 service manual.
-- Run from production psql with operator-owned credentials; this script contains no secrets.

with matched as (
    select "Id"
    from "TelegramManualBindings"
    where "IsActive" = true
      and (
          "OriginalFileName" = 'JF00305212, export T1 R32 Eastern Europe New Generation Zero Firewire Communication Full DC Inverter u-match Service Manual, A.3.pdf'
          or "OriginalFileName" = 'Gree U-Match R32 Service Manual EN 3.5-16kW.pdf'
          or "Title" = 'JF00305212, export T1 R32 Eastern Europe New Generation Zero Firewire Communication Full DC Inverter u-match Service Manual, A.3.pdf'
          or "Title" = 'Gree U-Match R32 Service Manual EN 3.5-16kW.pdf'
      )
),
updated as (
    update "TelegramManualBindings" b
    set "OriginalFileName" = 'Gree U-Match R32 Service Manual EN 3.5-16kW.pdf',
        "Title" = 'Gree U-Match R32 Service Manual EN',
        "Brand" = 'Gree',
        "Series" = 'U-Match R32',
        "DocumentType" = 'ServiceManual',
        "MinRole" = 'Engineer',
        "CanUseForDiagnostics" = false,
        "IsLibraryVisible" = true,
        "IsActive" = true,
        "UpdatedAtUtc" = now()
    from matched
    where b."Id" = matched."Id"
    returning b."Id", b."ManualId", b."OriginalFileName", b."Title", b."Brand", b."Series", b."DocumentType", b."MinRole", b."CanUseForDiagnostics", b."IsLibraryVisible", b."IsActive"
)
select * from updated
order by "Id";
