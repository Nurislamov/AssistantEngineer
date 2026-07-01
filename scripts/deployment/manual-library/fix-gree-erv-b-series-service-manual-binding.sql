-- ED-24MAN.4 idempotent metadata correction for the uploaded Gree ERV B Series service manual.
-- Run from production psql with operator-owned credentials; this script contains no secrets.

with matched as (
    select "Id"
    from "TelegramManualBindings"
    where "IsActive" = true
      and (
          "OriginalFileName" = 'JF00305089, export B series Energy Recovery Ventilation System technical data, A.3.pdf'
          or "OriginalFileName" = 'Gree ERV B Series Service Manual EN FHBQG-D3.5B-D60B.pdf'
          or "Title" = 'JF00305089, export B series Energy Recovery Ventilation System technical data, A.3.pdf'
          or "Title" = 'Gree ERV B Series Service Manual EN FHBQG-D3.5B-D60B.pdf'
      )
),
updated as (
    update "TelegramManualBindings" b
    set "OriginalFileName" = 'Gree ERV B Series Service Manual EN FHBQG-D3.5B-D60B.pdf',
        "Title" = 'Gree ERV B Series Service Manual EN',
        "Brand" = 'Gree',
        "Series" = 'ERV B Series',
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
