import FileDownloadIcon from "@mui/icons-material/FileDownload";
import { Button } from "@mui/material";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";
import { useDownloadBuildingReport } from "../model/useDownloadBuildingReport";

interface DownloadBuildingReportButtonProps {
  buildingId: number;
  onError?: (message: string) => void;
}

export function DownloadBuildingReportButton({
  buildingId,
  onError,
}: DownloadBuildingReportButtonProps): JSX.Element {
  const downloadReport = useDownloadBuildingReport(buildingId);

  return (
    <Button
      variant="outlined"
      startIcon={<FileDownloadIcon />}
      disabled={downloadReport.isPending}
      onClick={() =>
        downloadReport.mutate(undefined, {
          onError: (error) => onError?.(getErrorMessage(error)),
        })
      }
    >
      {downloadReport.isPending ? "Скачивание..." : "Скачать Excel-отчёт"}
    </Button>
  );
}
