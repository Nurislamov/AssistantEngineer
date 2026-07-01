-- ED-24MAN.4a idempotent metadata correction for the Gree U-Match R32 service manual.
-- Uses only columns that exist in TelegramManualBindings and never inserts rows.

with candidates as (
    select "Id"
    from "TelegramManualBindings"
    where
        regexp_replace(
            lower(coalesce("FileName", '') || ' ' || coalesce("Title", '')),
            '[^a-z0-9]+',
            '',
            'g') like '%jf00305212%'
        or regexp_replace(
            lower(coalesce("FileName", '') || ' ' || coalesce("Title", '')),
            '[^a-z0-9]+',
            '',
            'g') like '%umatch%servicemanual%'
        or (
            lower(coalesce("Brand", '')) = 'gree'
            and lower(coalesce("Series", '')) in ('u-match r32', 'umatch r32')
            and "DocumentType" = 'ServiceManual'
        )
),
updated as (
    update "TelegramManualBindings" binding
    set "Brand" = 'Gree',
        "Series" = 'U-Match R32',
        "DocumentType" = 'ServiceManual',
        "FileName" = 'Gree U-Match R32 Service Manual EN 3.5-16kW.pdf',
        "Title" = 'Gree U-Match R32 Service Manual EN 3.5-16kW.pdf',
        "MinRole" = 'Engineer',
        "CanUseForDiagnostics" = false,
        "IsLibraryVisible" = true,
        "IsActive" = true,
        "UpdatedAt" = now()
    from candidates
    where binding."Id" = candidates."Id"
      and (
          binding."Brand" is distinct from 'Gree'
          or binding."Series" is distinct from 'U-Match R32'
          or binding."DocumentType" is distinct from 'ServiceManual'
          or binding."FileName" is distinct from 'Gree U-Match R32 Service Manual EN 3.5-16kW.pdf'
          or binding."Title" is distinct from 'Gree U-Match R32 Service Manual EN 3.5-16kW.pdf'
          or binding."MinRole" is distinct from 'Engineer'
          or binding."CanUseForDiagnostics" is distinct from false
          or binding."IsLibraryVisible" is distinct from true
          or binding."IsActive" is distinct from true
      )
    returning binding."Id"
)
select count(*) as "AffectedRows" from updated;

select
    "Id", "ManualId", "Brand", "Series", "DocumentType", "FileName", "Title",
    "MinRole", "CanUseForDiagnostics", "IsLibraryVisible", "IsActive", "UpdatedAt"
from "TelegramManualBindings"
where "Brand" = 'Gree'
  and "Series" = 'U-Match R32'
  and "DocumentType" = 'ServiceManual'
  and "FileName" = 'Gree U-Match R32 Service Manual EN 3.5-16kW.pdf'
order by "Id";
