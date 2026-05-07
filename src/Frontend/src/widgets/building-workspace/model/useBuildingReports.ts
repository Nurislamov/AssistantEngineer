import { useState } from "react";
import { reportsApi } from "@/entities/calculation/api/reportsApi";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";

export function useBuildingReports(buildingId: number) {
  const [report, setReport] = useState<unknown>(null);
  const [error, setError] = useState<string | null>(null);

  const runReport = async (kind: "cooling" | "heating") => {
    setError(null);
    try {
      setReport(kind === "cooling" ? await reportsApi.getBuildingCoolingReport(buildingId) : await reportsApi.getBuildingHeatingReport(buildingId));
    } catch (caught) {
      setError(getErrorMessage(caught));
    }
  };

  const downloadReport = async (kind: "cooling" | "energy") => {
    setError(null);
    try {
      return kind === "cooling"
        ? await reportsApi.downloadBuildingCoolingExcel(buildingId)
        : await reportsApi.downloadBuildingEnergyBalanceExcel(buildingId);
    } catch (caught) {
      setError(getErrorMessage(caught));
      return null;
    }
  };

  return {
    report,
    error,
    runReport,
    downloadReport,
  };
}
