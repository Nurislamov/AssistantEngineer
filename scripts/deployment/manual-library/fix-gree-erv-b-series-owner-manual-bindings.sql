-- ED-24MAN.4b idempotent metadata correction for Gree ERV B Series diagnostic guides.
-- Uses only TelegramManualBindings columns, never deletes files, and keeps generic
-- controller documents outside the ERV diagnostic-guide binding.

with correct_erv_guide as (
    select "Id"
    from "TelegramManualBindings"
    where regexp_replace(
        lower(coalesce("FileName", '') || ' ' || coalesce("Title", '')),
        '[^a-z0-9]+',
        '',
        'g') like '%gree%erv%bseries%installation%startup%maintenance%manual%fhbqg%d35b%d60b%'
    order by "Id" desc
    limit 1
),
updated_guide as (
    update "TelegramManualBindings" binding
    set "Brand" = 'Gree',
        "Series" = 'ERV B Series',
        "DocumentType" = 'OwnerManual',
        "FileName" = 'Gree ERV B Series Installation Startup Maintenance Manual EN FHBQG-D3.5B-D60B.pdf',
        "Title" = 'Gree ERV B Series Installation Startup Maintenance Manual EN FHBQG-D3.5B-D60B.pdf',
        "MinRole" = 'Consumer',
        "CanUseForDiagnostics" = true,
        "IsLibraryVisible" = true,
        "IsActive" = true,
        "UpdatedAt" = now()
    from correct_erv_guide
    where binding."Id" = correct_erv_guide."Id"
      and (
          binding."Brand" is distinct from 'Gree'
          or binding."Series" is distinct from 'ERV B Series'
          or binding."DocumentType" is distinct from 'OwnerManual'
          or binding."FileName" is distinct from 'Gree ERV B Series Installation Startup Maintenance Manual EN FHBQG-D3.5B-D60B.pdf'
          or binding."Title" is distinct from 'Gree ERV B Series Installation Startup Maintenance Manual EN FHBQG-D3.5B-D60B.pdf'
          or binding."MinRole" is distinct from 'Consumer'
          or binding."CanUseForDiagnostics" is distinct from true
          or binding."IsLibraryVisible" is distinct from true
          or binding."IsActive" is distinct from true
    )
    returning binding."Id"
),
disabled_duplicate_guides as (
    update "TelegramManualBindings" binding
    set "CanUseForDiagnostics" = false,
        "IsActive" = false,
        "UpdatedAt" = now()
    where regexp_replace(
        lower(coalesce(binding."FileName", '') || ' ' || coalesce(binding."Title", '')),
        '[^a-z0-9]+',
        '',
        'g') like '%gree%erv%bseries%installation%startup%maintenance%manual%fhbqg%d35b%d60b%'
      and not exists (
          select 1
          from correct_erv_guide
          where correct_erv_guide."Id" = binding."Id"
      )
      and (
          binding."CanUseForDiagnostics" is distinct from false
          or binding."IsActive" is distinct from false
      )
    returning binding."Id"
),
disabled_secondary_erv_guides as (
    update "TelegramManualBindings" binding
    set "CanUseForDiagnostics" = false,
        "UpdatedAt" = now()
    where lower(coalesce(binding."Brand", '')) = 'gree'
      and lower(coalesce(binding."Series", '')) = 'erv b series'
      and binding."DocumentType" = 'OwnerManual'
      and binding."CanUseForDiagnostics" is distinct from false
      and regexp_replace(
          lower(coalesce(binding."FileName", '') || ' ' || coalesce(binding."Title", '')),
          '[^a-z0-9]+',
          '',
          'g') not like '%gree%erv%bseries%installation%startup%maintenance%manual%fhbqg%d35b%d60b%'
    returning binding."Id"
),
reclassified_controllers as (
    update "TelegramManualBindings" binding
    set "Series" = 'Controllers',
        "DocumentType" = 'ControllerGuide',
        "CanUseForDiagnostics" = false,
        "UpdatedAt" = now()
    where lower(coalesce(binding."Brand", '')) = 'gree'
      and lower(coalesce(binding."Series", '')) = 'erv b series'
      and (
          lower(coalesce(binding."FileName", '') || ' ' || coalesce(binding."Title", '')) like '%xk46%'
          or lower(coalesce(binding."FileName", '') || ' ' || coalesce(binding."Title", '')) like '%xe7a%'
          or lower(coalesce(binding."FileName", '') || ' ' || coalesce(binding."Title", '')) like '%yap1f%'
          or lower(coalesce(binding."FileName", '') || ' ' || coalesce(binding."Title", '')) like '%yv1l1%'
          or lower(coalesce(binding."FileName", '') || ' ' || coalesce(binding."Title", '')) like '%erv wired controller%'
      )
      and (
          binding."Series" is distinct from 'Controllers'
          or binding."DocumentType" is distinct from 'ControllerGuide'
          or binding."CanUseForDiagnostics" is distinct from false
      )
    returning binding."Id"
)
select
    (select count(*) from updated_guide) as "UpdatedGuideRows",
    (select count(*) from disabled_duplicate_guides) as "DisabledDuplicateGuideRows",
    (select count(*) from disabled_secondary_erv_guides) as "DisabledSecondaryGuideRows",
    (select count(*) from reclassified_controllers) as "ReclassifiedControllerRows";

select "Id", "Brand", "Series", "DocumentType", "FileName", "MinRole",
       "CanUseForDiagnostics", "IsLibraryVisible", "IsActive", "UpdatedAt"
from "TelegramManualBindings"
where "Brand" = 'Gree'
  and "Series" = 'ERV B Series'
order by "DocumentType", "FileName";

select "Id", "Brand", "Series", "DocumentType", "FileName", "MinRole",
       "CanUseForDiagnostics", "IsLibraryVisible", "IsActive", "UpdatedAt"
from "TelegramManualBindings"
where "Brand" = 'Gree'
  and "Series" = 'ERV B Series'
  and "DocumentType" = 'OwnerManual'
  and "CanUseForDiagnostics" = true
  and "IsActive" = true
order by "FileName";
