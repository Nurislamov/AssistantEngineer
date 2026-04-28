import { useMutation } from "@tanstack/react-query";
import { reportsApi } from "@/entities/calculation/api/reportsApi";
import { downloadBlob } from "@/shared/lib/downloadFile";

export function useDownloadBuildingReport(buildingId: number) {
  return useMutation({
    mutationFn: () => reportsApi.downloadBuildingEnergyBalanceExcel(buildingId),
    onSuccess: (blob) => {
      downloadBlob(blob, `building-${buildingId}-energy-balance.xlsx`);
    },
  });
}
