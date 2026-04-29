import { FormControl, InputLabel, MenuItem, Select, Stack } from "@mui/material";
import { useState } from "react";
import { useBuildings } from "@/entities/building/model/useBuildingsQueries";
import { useProjectSelection } from "@/features/projects/project-selection/model/ProjectSelectionProvider";
import { EmptyState } from "@/shared/ui/EmptyState";
import { PageContainer } from "@/shared/ui/PageContainer";
import { PageHeader } from "@/shared/ui/PageHeader";
import { QueryState } from "@/shared/ui/QueryState";
import { ReportsPanel } from "@/widgets/building-workspace/ui/BuildingWorkspace";

export function ReportsPage(): JSX.Element {
  const { selectedProjectId } = useProjectSelection();
  const buildingsQuery = useBuildings(selectedProjectId ?? 0);
  const buildings = buildingsQuery.data ?? [];
  const [buildingId, setBuildingId] = useState(0);
  const selectedBuildingId = buildingId || buildings[0]?.id || 0;

  return (
    <PageContainer>
      <PageHeader title="Reports" description="Download Excel reports and inspect JSON reports." />
      <Stack spacing={2}>
        <QueryState
          isLoading={buildingsQuery.isLoading}
          error={buildingsQuery.error}
          onRetry={() => void buildingsQuery.refetch()}
        />
        {buildings.length === 0 ? (
          <EmptyState title="No buildings" description="Create a building before running reports." />
        ) : (
          <>
            <FormControl size="small" sx={{ maxWidth: 360 }}>
              <InputLabel>Building</InputLabel>
              <Select
                label="Building"
                value={selectedBuildingId}
                onChange={(event) => setBuildingId(Number(event.target.value))}
              >
                {buildings.map((building) => (
                  <MenuItem key={building.id} value={building.id}>
                    {building.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <ReportsPanel buildingId={selectedBuildingId} />
          </>
        )}
      </Stack>
    </PageContainer>
  );
}
