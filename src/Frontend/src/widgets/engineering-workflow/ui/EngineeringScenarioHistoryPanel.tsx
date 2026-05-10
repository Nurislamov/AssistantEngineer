import { Button, Chip, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from "@mui/material";
import type {
  EngineeringCalculationArtifactRecord,
  EngineeringCalculationScenarioRecord,
} from "@/entities/engineering-workflow/types";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";

interface EngineeringScenarioHistoryPanelProps {
  scenarios: EngineeringCalculationScenarioRecord[];
  selectedScenarioId?: string;
  onViewResult: (scenarioId: string) => void;
  onLoadArtifacts: (scenarioId: string) => void;
  artifacts: EngineeringCalculationArtifactRecord[];
  onViewArtifact: (scenarioId: string, artifactKind: EngineeringCalculationArtifactRecord["artifactKind"]) => void;
}

export function EngineeringScenarioHistoryPanel({
  scenarios,
  selectedScenarioId,
  onViewResult,
  onLoadArtifacts,
  artifacts,
  onViewArtifact,
}: EngineeringScenarioHistoryPanelProps): JSX.Element {
  return (
    <DataCard>
      <Stack spacing={1.5}>
        <Typography variant="h6">Scenario history</Typography>
        {scenarios.length === 0 ? (
          <EmptyState
            title="No persisted scenarios yet"
            description="Run or prepare a calculation scenario to create persisted history artifacts."
          />
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Scenario</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Created</TableCell>
                <TableCell>Diagnostics</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {scenarios.slice(0, 10).map((item) => (
                <TableRow key={item.scenarioId} selected={selectedScenarioId === item.scenarioId}>
                  <TableCell sx={{ fontFamily: "monospace", fontSize: 12 }}>{item.scenarioId}</TableCell>
                  <TableCell>
                    <Chip label={item.status} size="small" color={item.status.includes("Failed") ? "error" : "default"} />
                  </TableCell>
                  <TableCell>{new Date(item.createdAtUtc).toLocaleString()}</TableCell>
                  <TableCell>{countDiagnostics(item.diagnosticsJson)}</TableCell>
                  <TableCell>
                    <Stack direction="row" spacing={1}>
                      <Button size="small" variant="text" onClick={() => onViewResult(item.scenarioId)}>
                        View result
                      </Button>
                      <Button size="small" variant="text" onClick={() => onLoadArtifacts(item.scenarioId)}>
                        Artifacts
                      </Button>
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}

        {artifacts.length > 0 ? (
          <Stack spacing={1}>
            <Typography variant="body2" sx={{ fontWeight: 600 }}>
              Artifacts for selected scenario
            </Typography>
            <Stack direction="row" spacing={1} flexWrap="wrap">
              {artifacts.map((item) => (
                <Button
                  key={item.artifactId}
                  size="small"
                  variant="outlined"
                  sx={{ mb: 1 }}
                  onClick={() => onViewArtifact(item.scenarioId, item.artifactKind)}
                >
                  {item.artifactKind}
                </Button>
              ))}
            </Stack>
          </Stack>
        ) : null}
      </Stack>
    </DataCard>
  );
}

function countDiagnostics(raw?: string | null): number {
  if (!raw) {
    return 0;
  }

  try {
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed.length : 0;
  } catch {
    return 0;
  }
}
