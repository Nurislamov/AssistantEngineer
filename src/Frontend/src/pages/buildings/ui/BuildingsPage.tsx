import AddIcon from "@mui/icons-material/Add";
import { Button } from "@mui/material";
import { useState } from "react";
import { useBuildings, useProjects } from "@/entities/building/model/useBuildingsQueries";
import { BuildingList } from "@/features/buildings/building-list/ui/BuildingList";
import { CreateBuildingForm } from "@/features/buildings/create-building/ui/CreateBuildingForm";
import { CreateProjectForm } from "@/features/projects/create-project/ui/CreateProjectForm";
import { appConfig } from "@/shared/config/env";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";
import { FormDialog } from "@/shared/ui/FormDialog";
import { PageContainer } from "@/shared/ui/PageContainer";
import { PageHeader } from "@/shared/ui/PageHeader";
import { QueryState } from "@/shared/ui/QueryState";

export function BuildingsPage(): JSX.Element {
  const [createBuildingOpen, setCreateBuildingOpen] = useState(false);
  const [createProjectOpen, setCreateProjectOpen] = useState(false);
  const projectsQuery = useProjects();
  const activeProject =
    projectsQuery.data?.find((project) => project.id === appConfig.defaultProjectId) ??
    projectsQuery.data?.[0];
  const projectId = activeProject?.id ?? 0;
  const buildingsQuery = useBuildings(projectId);
  const queryError = projectsQuery.error ?? buildingsQuery.error;

  return (
    <PageContainer>
      <PageHeader
        title="Здания"
        description={
          activeProject
            ? `Список зданий проекта "${activeProject.name}" (#${activeProject.id})`
            : "Здания привязаны к проекту backend"
        }
        actions={
          activeProject ? (
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => setCreateBuildingOpen(true)}
            >
              Создать здание
            </Button>
          ) : (
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => setCreateProjectOpen(true)}
            >
              Создать проект
            </Button>
          )
        }
      />

      <QueryState
        isLoading={projectsQuery.isLoading || buildingsQuery.isLoading}
        error={queryError}
        onRetry={() => {
          void projectsQuery.refetch();
          void buildingsQuery.refetch();
        }}
      />
      {!projectsQuery.isLoading && !queryError && !activeProject ? (
        <EmptyState
          title="Проектов пока нет"
          description="Создайте проект, чтобы backend смог сохранять здания."
          actions={
            <Button variant="contained" onClick={() => setCreateProjectOpen(true)}>
              Создать проект
            </Button>
          }
        />
      ) : null}
      {buildingsQuery.data && activeProject ? (
        <DataCard>
          <BuildingList buildings={buildingsQuery.data} />
        </DataCard>
      ) : null}

      <FormDialog
        open={createBuildingOpen}
        title="Создать здание"
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
        title="Создать проект"
        onClose={() => setCreateProjectOpen(false)}
      >
        <CreateProjectForm
          onCreated={() => setCreateProjectOpen(false)}
          onCancel={() => setCreateProjectOpen(false)}
        />
      </FormDialog>
    </PageContainer>
  );
}
