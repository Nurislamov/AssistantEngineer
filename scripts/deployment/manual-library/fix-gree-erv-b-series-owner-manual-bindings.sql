-- ED-24MAN.4a idempotent metadata correction for the Gree ERV wired-controller owner manual.
-- Uses only columns that exist in TelegramManualBindings and never inserts rows.

with candidates as (
    select "Id"
    from "TelegramManualBindings"
    where regexp_replace(
        lower(coalesce("FileName", '') || ' ' || coalesce("Title", '')),
        '[^a-z0-9]+',
        '',
        'g') like '%gree%erv%wiredcontroller%owner%manual%'
       or regexp_replace(
        lower(coalesce("FileName", '') || ' ' || coalesce("Title", '')),
        '[^a-z0-9]+',
        '',
        'g') like '%wiredcontroller%owner%manual%'
       or (
           lower(coalesce("Brand", '')) = 'gree'
           and lower(coalesce("Series", '')) = 'erv b series'
           and "DocumentType" = 'OwnerManual'
           and lower(coalesce("FileName", '') || ' ' || coalesce("Title", '')) like '%controller%'
       )
),
updated as (
    update "TelegramManualBindings" binding
    set "Brand" = 'Gree',
        "Series" = 'ERV B Series',
        "DocumentType" = 'OwnerManual',
        "FileName" = 'Gree ERV Wired Controller Owner Manual EN.pdf',
        "Title" = 'Gree ERV Wired Controller Owner Manual EN.pdf',
        "MinRole" = 'Consumer',
        "CanUseForDiagnostics" = true,
        "IsLibraryVisible" = true,
        "IsActive" = true,
        "UpdatedAt" = now()
    from candidates
    where binding."Id" = candidates."Id"
      and (
          binding."Brand" is distinct from 'Gree'
          or binding."Series" is distinct from 'ERV B Series'
          or binding."DocumentType" is distinct from 'OwnerManual'
          or binding."FileName" is distinct from 'Gree ERV Wired Controller Owner Manual EN.pdf'
          or binding."Title" is distinct from 'Gree ERV Wired Controller Owner Manual EN.pdf'
          or binding."MinRole" is distinct from 'Consumer'
          or binding."CanUseForDiagnostics" is distinct from true
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
  and "Series" = 'ERV B Series'
  and "DocumentType" = 'OwnerManual'
  and "FileName" = 'Gree ERV Wired Controller Owner Manual EN.pdf'
order by "Id";
