import {
  Alert,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
} from "@mui/material";
import { useEffect, useState } from "react";
import { useBuildings, useProjects } from "@/entities/building/model/useBuildingsQueries";
import { useProjectSelection } from "@/features/projects/project-selection/model/ProjectSelectionProvider";
import { EmptyState } from "@/shared/ui/EmptyState";
import { PageContainer } from "@/shared/ui/PageContainer";
import { PageHeader } from "@/shared/ui/PageHeader";
import { QueryState } from "@/shared/ui/QueryState";
import { EngineeringWorkflowShell } from "@/widgets/engineering-workflow/ui/EngineeringWorkflowShell";

export function EngineeringWorkflowPage(): JSX.Element {
  const projectsQuery = useProjects();
  const { selectedProjectId, setSelectedProjectId } = useProjectSelection();
  const [projectId, setProjectId] = useState<number>(selectedProjectId ?? 0);

  const buildingsQuery = useBuildings(projectId);
  const buildings = buildingsQuery.data ?? [];
  const [buildingId, setBuildingId] = useState<number>(0);

  useEffect(() => {
    if (selectedProjectId && selectedProjectId > 0) {
      setProjectId(selectedProjectId);
    }
  }, [selectedProjectId]);

  useEffect(() => {
    if (buildings.length === 0) {
      setBuildingId(0);
      return;
    }

    setBuildingId((current) => (buildings.some((item) => item.id === current) ? current : buildings[0].id));
  }, [buildings]);

  return (
    <PageContainer>
      <PageHeader
        title="Engineering workflow"
        description="Foundation workflow for project/building setup, validation diagnostics, trace summary, and report preview exports (JSON/Markdown)."
      />

      <Stack spacing={2}>
        <Alert severity="info">
          Frontend workflow is an internal engineering foundation. It aggregates backend contracts and does not execute calculation physics in browser.
        </Alert>

        <QueryState
          isLoading={projectsQuery.isLoading || buildingsQuery.isLoading}
          error={projectsQuery.error ?? buildingsQuery.error}
          onRetry={() => {
            void projectsQuery.refetch();
            void buildingsQuery.refetch();
          }}
        />

        <Stack direction={{ xs: "column", md: "row" }} spacing={1.5}>
          <FormControl size="small" sx={{ minWidth: 260 }}>
            <InputLabel>Project</InputLabel>
            <Select
              label="Project"
              value={projectId > 0 ? projectId : ""}
              onChange={(event) => {
                const next = Number(event.target.value);
                setProjectId(next);
                setSelectedProjectId(next > 0 ? next : null);
              }}
            >
              {(projectsQuery.data ?? []).map((project) => (
                <MenuItem key={project.id} value={project.id}>
                  {project.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl size="small" sx={{ minWidth: 260 }} disabled={buildings.length === 0}>
            <InputLabel>Building</InputLabel>
            <Select
              label="Building"
              value={buildingId > 0 ? buildingId : ""}
              onChange={(event) => setBuildingId(Number(event.target.value))}
            >
              {buildings.map((building) => (
                <MenuItem key={building.id} value={building.id}>
                  {building.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Stack>

        {projectId <= 0 ? (
          <EmptyState title="Select project" description="Choose a project to start engineering workflow." />
        ) : null}

        {projectId > 0 && buildings.length === 0 ? (
          <EmptyState
            title="No buildings in project"
            description="Create a building first, then reopen this workflow for staged engineering setup and validation."
          />
        ) : null}

        {projectId > 0 && buildingId > 0 ? (
          <EngineeringWorkflowShell projectId={projectId} buildingId={buildingId} />
        ) : null}
      </Stack>
    </PageContainer>
  );
}
