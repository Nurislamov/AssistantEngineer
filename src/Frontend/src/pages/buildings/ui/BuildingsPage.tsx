import AddIcon from "@mui/icons-material/Add";
import { Alert, Button, Stack, TextField } from "@mui/material";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { FormEvent, useEffect, useState } from "react";
import { buildingsApi } from "@/entities/building/api/buildingsApi";
import { useBuildings, useProjects } from "@/entities/building/model/useBuildingsQueries";
import type { BuildingDto, UpdateBuildingRequest } from "@/entities/building/types";
import { BuildingList } from "@/features/buildings/building-list/ui/BuildingList";
import { CreateBuildingForm } from "@/features/buildings/create-building/ui/CreateBuildingForm";
import { CreateProjectForm } from "@/features/projects/create-project/ui/CreateProjectForm";
import { useProjectSelection } from "@/features/projects/project-selection/model/ProjectSelectionProvider";
import { queryKeys } from "@/shared/api/queryKeys";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";
import { FormDialog } from "@/shared/ui/FormDialog";
import { PageContainer } from "@/shared/ui/PageContainer";
import { PageHeader } from "@/shared/ui/PageHeader";
import { QueryState } from "@/shared/ui/QueryState";

export function BuildingsPage(): JSX.Element {
  const [createBuildingOpen, setCreateBuildingOpen] = useState(false);
  const [createProjectOpen, setCreateProjectOpen] = useState(false);
  const [editingBuilding, setEditingBuilding] = useState<BuildingDto | null>(null);
  const [operationError, setOperationError] = useState<string | null>(null);
  const { selectedProjectId, setSelectedProjectId } = useProjectSelection();
  const queryClient = useQueryClient();
  const projectsQuery = useProjects();
  const projects = projectsQuery.data ?? [];

  useEffect(() => {
    if (!projectsQuery.isSuccess) return;
    if (projects.length === 0) {
      setSelectedProjectId(null);
      return;
    }
    if (!selectedProjectId || !projects.some((project) => project.id === selectedProjectId)) {
      setSelectedProjectId(projects[0].id);
    }
  }, [projects, projectsQuery.isSuccess, selectedProjectId, setSelectedProjectId]);

  const activeProject = projects.find((project) => project.id === selectedProjectId);
  const projectId = activeProject?.id ?? 0;
  const buildingsQuery = useBuildings(projectId);
  const deleteBuilding = useMutation({
    mutationFn: (buildingId: number) => buildingsApi.delete(buildingId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.buildings.byProject(projectId) });
    },
    onError: (error) => setOperationError(getErrorMessage(error)),
  });

  const queryError = projectsQuery.error ?? buildingsQuery.error;

  return (
    <PageContainer>
      <PageHeader
        title="Buildings"
        description={
          activeProject
            ? `Buildings in ${activeProject.name}`
            : "Create a project before adding buildings."
        }
        actions={
          <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
            <Button variant="outlined" startIcon={<AddIcon />} onClick={() => setCreateProjectOpen(true)}>
              New project
            </Button>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              disabled={!activeProject}
              onClick={() => setCreateBuildingOpen(true)}
            >
              New building
            </Button>
          </Stack>
        }
      />

      {operationError ? (
        <Alert severity="error" onClose={() => setOperationError(null)} sx={{ mb: 2 }}>
          {operationError}
        </Alert>
      ) : null}

      <QueryState
        isLoading={projectsQuery.isLoading || buildingsQuery.isLoading}
        error={queryError}
        onRetry={() => {
          void projectsQuery.refetch();
          void buildingsQuery.refetch();
        }}
      />

      {!projectsQuery.isLoading && !queryError && projects.length === 0 ? (
        <EmptyState
          title="No projects yet"
          description="Create a project to store buildings, rooms, calculations, and reports."
          actions={
            <Button variant="contained" onClick={() => setCreateProjectOpen(true)}>
              Create project
            </Button>
          }
        />
      ) : null}

      {buildingsQuery.data && activeProject ? (
        <DataCard>
          <BuildingList
            buildings={buildingsQuery.data}
            onEdit={setEditingBuilding}
            onDelete={(building) => {
              if (window.confirm(`Delete building "${building.name}"?`)) {
                deleteBuilding.mutate(building.id);
              }
            }}
          />
        </DataCard>
      ) : null}

      <FormDialog
        open={createBuildingOpen}
        title="Create building"
        onClose={() => setCreateBuildingOpen(false)}
      >
        <CreateBuildingForm
          projectId={projectId}
          onCreated={() => setCreateBuildingOpen(false)}
          onCancel={() => setCreateBuildingOpen(false)}
        />
      </FormDialog>

      <FormDialog
        open={createProjectOpen}
        title="Create project"
        onClose={() => setCreateProjectOpen(false)}
      >
        <CreateProjectForm
          onCreated={(project) => {
            setSelectedProjectId(project.id);
            setCreateProjectOpen(false);
          }}
          onCancel={() => setCreateProjectOpen(false)}
        />
      </FormDialog>

      <FormDialog
        open={Boolean(editingBuilding)}
        title="Edit building"
        onClose={() => setEditingBuilding(null)}
      >
        {editingBuilding ? (
          <EditBuildingDialog
            building={editingBuilding}
            onSaved={() => setEditingBuilding(null)}
            onCancel={() => setEditingBuilding(null)}
          />
        ) : null}
      </FormDialog>
    </PageContainer>
  );
}

interface EditBuildingDialogProps {
  building: BuildingDto;
  onSaved: () => void;
  onCancel: () => void;
}

function EditBuildingDialog({ building, onSaved, onCancel }: EditBuildingDialogProps): JSX.Element {
  const [form, setForm] = useState<UpdateBuildingRequest>({
    name: building.name,
    climateZoneId: building.climateZoneId ?? null,
  });
  const queryClient = useQueryClient();
  const updateBuilding = useMutation({
    mutationFn: (request: UpdateBuildingRequest) => buildingsApi.update(building.id, request),
    onSuccess: async (updated) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.buildings.byProject(updated.projectId) }),
        queryClient.invalidateQueries({ queryKey: queryKeys.buildings.detail(updated.id) }),
      ]);
      onSaved();
    },
  });

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    updateBuilding.mutate({ ...form, name: form.name.trim() });
  };

  return (
    <Stack component="form" spacing={2} onSubmit={handleSubmit}>
      {updateBuilding.isError ? (
        <Alert severity="error">{getErrorMessage(updateBuilding.error)}</Alert>
      ) : null}
      <TextField
        label="Name"
        value={form.name}
        required
        onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
      />
      <TextField
        label="Climate zone ID"
        type="number"
        value={form.climateZoneId ?? ""}
        onChange={(event) =>
          setForm((current) => ({
            ...current,
            climateZoneId: event.target.value ? Number(event.target.value) : null,
          }))
        }
      />
      <Stack direction="row" spacing={1} justifyContent="flex-end">
        <Button type="button" color="inherit" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" variant="contained" disabled={updateBuilding.isPending}>
          Save
        </Button>
      </Stack>
    </Stack>
  );
}
