-- ED-24MAN.3 data correction for the known Gree Indoor service manual upload.
-- Run on the production PostgreSQL database after deploy; do not run against local data by default.
-- The update is idempotent and targets only the exact known active library binding.

select
    "Id",
    "Brand",
    "Series",
    "DocumentType",
    "FileName",
    "MinRole",
    "CanUseForDiagnostics",
    "IsLibraryVisible",
    "IsActive"
from "TelegramManualBindings"
where "Brand" = 'Gree'
  and "Series" = 'Indoor'
  and "FileName" = 'Gree_GMV_Indoor_Units_Service_Manual_EN_GC202603_I_1_5_79kW_R410A.pdf';

update "TelegramManualBindings"
set "DocumentType" = 'ServiceManual',
    "MinRole" = 'Engineer',
    "CanUseForDiagnostics" = false,
    "IsLibraryVisible" = true,
    "UpdatedAt" = now()
where "Brand" = 'Gree'
  and "Series" = 'Indoor'
  and "FileName" = 'Gree_GMV_Indoor_Units_Service_Manual_EN_GC202603_I_1_5_79kW_R410A.pdf'
  and (
      "DocumentType" <> 'ServiceManual'
      or "MinRole" <> 'Engineer'
      or "CanUseForDiagnostics" <> false
      or "IsLibraryVisible" <> true
  );

select
    "Id",
    "Brand",
    "Series",
    "DocumentType",
    "FileName",
    "MinRole",
    "CanUseForDiagnostics",
    "IsLibraryVisible",
    "IsActive"
from "TelegramManualBindings"
where "Brand" = 'Gree'
  and "Series" = 'Indoor'
  and "FileName" = 'Gree_GMV_Indoor_Units_Service_Manual_EN_GC202603_I_1_5_79kW_R410A.pdf';
