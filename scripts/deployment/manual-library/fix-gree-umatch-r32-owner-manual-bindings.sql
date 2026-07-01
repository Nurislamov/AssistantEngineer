-- ED-24MAN.4a idempotent metadata correction for Gree U-Match R32 owner manuals.
-- Uses only columns that exist in TelegramManualBindings and never inserts rows.

with candidates as (
    select
        "Id",
        case
            when regexp_replace(
                lower(coalesce("FileName", '') || ' ' || coalesce("Title", '')),
                '[^a-z0-9]+',
                '',
                'g') like '%cassette%owner%manual%'
                then 'Gree U-Match R32 Cassette Type Owner Manual EN 3.5-16kW.pdf'
            when regexp_replace(
                lower(coalesce("FileName", '') || ' ' || coalesce("Title", '')),
                '[^a-z0-9]+',
                '',
                'g') like '%duct%owner%manual%'
                then 'Gree U-Match R32 Duct Type Owner Manual EN 3.5-16kW.pdf'
        end as canonical_name
    from "TelegramManualBindings"
    where regexp_replace(
        lower(coalesce("FileName", '') || ' ' || coalesce("Title", '')),
        '[^a-z0-9]+',
        '',
        'g') like '%umatch%cassette%owner%manual%'
       or regexp_replace(
        lower(coalesce("FileName", '') || ' ' || coalesce("Title", '')),
        '[^a-z0-9]+',
        '',
        'g') like '%umatch%duct%owner%manual%'
       or (
           lower(coalesce("Brand", '')) = 'gree'
           and lower(coalesce("Series", '')) in ('u-match r32', 'umatch r32')
           and "DocumentType" = 'OwnerManual'
           and (
               lower(coalesce("FileName", '') || ' ' || coalesce("Title", '')) like '%cassette%'
               or lower(coalesce("FileName", '') || ' ' || coalesce("Title", '')) like '%duct%'
           )
       )
),
updated as (
    update "TelegramManualBindings" binding
    set "Brand" = 'Gree',
        "Series" = 'U-Match R32',
        "DocumentType" = 'OwnerManual',
        "FileName" = candidates.canonical_name,
        "Title" = candidates.canonical_name,
        "MinRole" = 'Consumer',
        "CanUseForDiagnostics" = true,
        "IsLibraryVisible" = true,
        "IsActive" = true,
        "UpdatedAt" = now()
    from candidates
    where binding."Id" = candidates."Id"
      and candidates.canonical_name is not null
      and (
          binding."Brand" is distinct from 'Gree'
          or binding."Series" is distinct from 'U-Match R32'
          or binding."DocumentType" is distinct from 'OwnerManual'
          or binding."FileName" is distinct from candidates.canonical_name
          or binding."Title" is distinct from candidates.canonical_name
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
  and "Series" = 'U-Match R32'
  and "DocumentType" = 'OwnerManual'
  and "FileName" in (
      'Gree U-Match R32 Cassette Type Owner Manual EN 3.5-16kW.pdf',
      'Gree U-Match R32 Duct Type Owner Manual EN 3.5-16kW.pdf'
  )
order by "FileName", "Id";
