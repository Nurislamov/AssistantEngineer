import DownloadIcon from "@mui/icons-material/Download";
import { Alert, Button, Stack, Typography } from "@mui/material";
import { downloadBlob } from "@/shared/lib/downloadFile";
import { DataCard } from "@/shared/ui/DataCard";
import { EngineeringCoreDisclosurePanel } from "@/widgets/engineering-core-disclosure/ui/EngineeringCoreDisclosurePanel";
import { useBuildingReports } from "../model/useBuildingReports";
import { JsonBlock } from "./JsonBlock";

export function ReportsPanel({ buildingId }: { buildingId: number }): JSX.Element {
  const { report, error, runReport, downloadReport } = useBuildingReports(buildingId);

  const download = async (kind: "cooling" | "energy") => {
    const blob = await downloadReport(kind);
    if (!blob) return;

    downloadBlob(blob, `building-${buildingId}-${kind}.xlsx`);
  };

  return (
    <DataCard>
      <Stack spacing={2}>
        <Typography variant="h6">Reports</Typography>
        {error ? <Alert severity="error">{error}</Alert> : null}
        <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
          <Button variant="outlined" onClick={() => void runReport("cooling")}>Show cooling JSON</Button>
          <Button variant="outlined" onClick={() => void runReport("heating")}>Show heating JSON</Button>
          <Button variant="contained" startIcon={<DownloadIcon />} onClick={() => void download("cooling")}>Cooling Excel</Button>
          <Button variant="contained" startIcon={<DownloadIcon />} onClick={() => void download("energy")}>Energy balance Excel</Button>
        </Stack>
        {report ? <EngineeringCoreDisclosurePanel report={report} /> : null}
        {report ? <JsonBlock title="Report" value={report} /> : null}
      </Stack>
    </DataCard>
  );
}
