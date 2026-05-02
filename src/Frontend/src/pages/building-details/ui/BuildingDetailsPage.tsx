import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button } from "@mui/material";
import { Link as RouterLink, useParams } from "react-router-dom";
import { paths } from "@/app/router/paths";
import { parsePositiveIntParam } from "@/shared/lib/routeParams";
import { ErrorState } from "@/shared/ui/ErrorState";
import { PageContainer } from "@/shared/ui/PageContainer";
import { PageHeader } from "@/shared/ui/PageHeader";
import { BuildingWorkspace } from "@/widgets/building-workspace/ui/BuildingWorkspace";

export function BuildingDetailsPage(): JSX.Element {
  const params = useParams();
  const buildingId = parsePositiveIntParam(params.buildingId);

  if (!buildingId) {
    return (
      <PageContainer>
        <ErrorState message="Invalid building ID in route" />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <PageHeader
        title="Building workspace"
        actions={
          <Button component={RouterLink} to={paths.buildings} startIcon={<ArrowBackIcon />}>
            Buildings
          </Button>
        }
      />
      <BuildingWorkspace buildingId={buildingId} />
    </PageContainer>
  );
}
