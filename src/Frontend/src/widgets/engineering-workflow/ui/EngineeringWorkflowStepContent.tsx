import { Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from "@mui/material";
import type { ProjectWorkflowState, WorkflowStepKind } from "@/entities/engineering-workflow/types";
import { summarizeBuildingMetrics } from "@/entities/engineering-workflow/api/engineeringWorkflowClient";
import { formatNumber } from "@/shared/lib/format";
import { EmptyState } from "@/shared/ui/EmptyState";
import { statusLabel } from "../model/engineeringWorkflowShellViewModel";

interface EngineeringWorkflowStepContentProps {
  step: WorkflowStepKind;
  state: ProjectWorkflowState;
  statusChip: (status: "incomplete" | "valid" | "warnings" | "errors" | "ready") => JSX.Element;
}

export function EngineeringWorkflowStepContent({
  step,
  state,
  statusChip,
}: EngineeringWorkflowStepContentProps): JSX.Element {
  if (step === "Project" || step === "Building") {
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Project: {state.buildingMetadata.projectName ?? "n/a"}</Typography>
        <Typography variant="body2">Building: {state.buildingMetadata.buildingName ?? "n/a"}</Typography>
        <Typography variant="body2">Location: {state.buildingMetadata.locationText ?? "n/a"}</Typography>
        <Typography variant="body2">Floor area: {formatNumber(state.buildingMetadata.floorAreaM2, 1)} m2</Typography>
        <Typography variant="body2">Volume: {formatNumber(state.buildingMetadata.volumeM3, 0)} m3</Typography>
      </Stack>
    );
  }

  if (step === "Zones") {
    return state.zones.length === 0 ? (
      <EmptyState title="No zones" description="Create zones in Building workspace before running full engineering workflow." />
    ) : (
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Zone</TableCell>
              <TableCell>Kind</TableCell>
              <TableCell>Area</TableCell>
              <TableCell>Volume</TableCell>
              <TableCell>Status</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {state.zones.map((zone) => (
              <TableRow key={String(zone.id)}>
                <TableCell>{zone.name}</TableCell>
                <TableCell>{zone.zoneKind}</TableCell>
                <TableCell>{formatNumber(zone.floorAreaM2, 1)} m2</TableCell>
                <TableCell>{formatNumber(zone.airVolumeM3, 0)} m3</TableCell>
                <TableCell>{statusChip(zone.status)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    );
  }

  if (step === "Envelope") {
    return state.boundaries.length === 0 ? (
      <EmptyState title="No boundaries" description="Add wall boundaries in building workspace envelope panel." />
    ) : (
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Room / zone</TableCell>
              <TableCell>Exposure</TableCell>
              <TableCell>Area</TableCell>
              <TableCell>U-value</TableCell>
              <TableCell>Adjacent</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {state.boundaries.slice(0, 60).map((boundary) => (
              <TableRow key={String(boundary.id)}>
                <TableCell>{boundary.zoneOrRoomName}</TableCell>
                <TableCell>{boundary.exposureKind}</TableCell>
                <TableCell>{formatNumber(boundary.areaM2, 2)} m2</TableCell>
                <TableCell>{formatNumber(boundary.uValue, 3)} W/(m2*K)</TableCell>
                <TableCell>{boundary.adjacentZoneReference ?? "-"}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    );
  }

  if (step === "WeatherSolar") {
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Weather source status: {state.weatherSolarSettings.weatherSourceStatus}</Typography>
        <Typography variant="body2">Location/timezone summary: {state.weatherSolarSettings.locationTimezoneSummary}</Typography>
        <Typography variant="body2">Solar chain readiness: {state.weatherSolarSettings.solarChainReadinessSummary}</Typography>
      </Stack>
    );
  }

  if (step === "Ventilation") {
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Openings: {state.ventilationSettings.openingCount}</Typography>
        <Typography variant="body2">Control mode: {state.ventilationSettings.controlModeSummary}</Typography>
        <Typography variant="body2">Airflow/Hve summary: {state.ventilationSettings.airflowSummary}</Typography>
      </Stack>
    );
  }

  if (step === "Ground") {
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Ground boundary count: {state.groundSettings.groundBoundaryCount}</Typography>
        <Typography variant="body2">Ground profile mode: {state.groundSettings.groundProfileMode}</Typography>
        <Typography variant="body2">Status: {statusLabel(state.groundSettings.summaryStatus)}</Typography>
      </Stack>
    );
  }

  if (step === "DomesticHotWater") {
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Demand basis: {state.domesticHotWaterSettings.demandBasis}</Typography>
        <Typography variant="body2">Useful demand summary: {state.domesticHotWaterSettings.usefulDemandSummary}</Typography>
        <Typography variant="body2">Losses summary: {state.domesticHotWaterSettings.lossesSummary}</Typography>
        <Typography variant="body2">Ownership policy: {state.domesticHotWaterSettings.ownershipPolicy}</Typography>
      </Stack>
    );
  }

  if (step === "SystemEnergy") {
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Uses: {state.systemEnergySettings.usesSummary}</Typography>
        <Typography variant="body2">Carriers: {state.systemEnergySettings.carriersSummary}</Typography>
        <Typography variant="body2">Final/Primary/CO2: {state.systemEnergySettings.finalPrimaryCarbonSummary}</Typography>
      </Stack>
    );
  }

  if (step === "Validation") {
    const errors = state.validationDiagnostics.filter((item) => item.severity === "error").length;
    const warnings = state.validationDiagnostics.filter((item) => item.severity === "warning").length;
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Diagnostics summary: {state.validationDiagnostics.length}</Typography>
        <Typography variant="body2">Errors: {errors}</Typography>
        <Typography variant="body2">Warnings: {warnings}</Typography>
      </Stack>
    );
  }

  if (step === "CalculationTrace") {
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Trace modules: {state.calculationTraceSummary?.modules.join(", ") ?? "n/a"}</Typography>
        <Typography variant="body2">Assumptions: {state.calculationTraceSummary?.assumptions.length ?? 0}</Typography>
        <Typography variant="body2">Warnings: {state.calculationTraceSummary?.warnings.length ?? 0}</Typography>
      </Stack>
    );
  }

  if (step === "Reports") {
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Preview title: {state.reportSummary?.title ?? "n/a"}</Typography>
        <Typography variant="body2">Sections: {state.reportSummary?.sections.length ?? 0}</Typography>
        <Typography variant="body2">Available formats: {state.reportSummary?.exportFormatsAvailable.join(", ") ?? "Json, Markdown"}</Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={0.5}>
      <Typography variant="body2">Review summary: {summarizeBuildingMetrics(state)}</Typography>
      <Typography variant="body2">Current mode: {state.workflowModeLabel}</Typography>
    </Stack>
  );
}
