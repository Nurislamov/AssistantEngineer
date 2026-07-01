-- ED-24MAN.4a idempotent metadata correction for the Gree ERV B Series service manual.
-- Uses only columns that exist in TelegramManualBindings and never inserts rows.

with candidates as (
    select "Id"
    from "TelegramManualBindings"
    where
        regexp_replace(
            lower(coalesce("FileName", '') || ' ' || coalesce("Title", '')),
            '[^a-z0-9]+',
            '',
            'g') like '%jf00305089%'
        or regexp_replace(
            lower(coalesce("FileName", '') || ' ' || coalesce("Title", '')),
            '[^a-z0-9]+',
            '',
            'g') like '%erv%bseries%servicemanual%'
        or regexp_replace(
            lower(coalesce("FileName", '') || ' ' || coalesce("Title", '')),
            '[^a-z0-9]+',
            '',
            'g') like '%bseries%energyrecoveryventilation%technicaldata%'
        or (
            lower(coalesce("Brand", '')) = 'gree'
            and lower(coalesce("Series", '')) = 'erv b series'
            and "DocumentType" = 'ServiceManual'
        )
),
updated as (
    update "TelegramManualBindings" binding
    set "Brand" = 'Gree',
        "Series" = 'ERV B Series',
        "DocumentType" = 'ServiceManual',
        "FileName" = 'Gree ERV B Series Service Manual EN FHBQG-D3.5B-D60B.pdf',
        "Title" = 'Gree ERV B Series Service Manual EN FHBQG-D3.5B-D60B.pdf',
        "MinRole" = 'Engineer',
        "CanUseForDiagnostics" = false,
        "IsLibraryVisible" = true,
        "IsActive" = true,
        "UpdatedAt" = now()
    from candidates
    where binding."Id" = candidates."Id"
      and (
          binding."Brand" is distinct from 'Gree'
          or binding."Series" is distinct from 'ERV B Series'
          or binding."DocumentType" is distinct from 'ServiceManual'
          or binding."FileName" is distinct from 'Gree ERV B Series Service Manual EN FHBQG-D3.5B-D60B.pdf'
          or binding."Title" is distinct from 'Gree ERV B Series Service Manual EN FHBQG-D3.5B-D60B.pdf'
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
  and "Series" = 'ERV B Series'
  and "DocumentType" = 'ServiceManual'
  and "FileName" = 'Gree ERV B Series Service Manual EN FHBQG-D3.5B-D60B.pdf'
order by "Id";
