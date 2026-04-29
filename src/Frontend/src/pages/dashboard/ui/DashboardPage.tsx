import ApartmentIcon from "@mui/icons-material/Apartment";
import CalculateIcon from "@mui/icons-material/Calculate";
import PrecisionManufacturingIcon from "@mui/icons-material/PrecisionManufacturing";
import { Button, Stack, Typography } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import { paths } from "@/app/router/paths";
import { useProjectSelection } from "@/features/projects/project-selection/model/ProjectSelectionProvider";
import { DataCard } from "@/shared/ui/DataCard";
import { PageContainer } from "@/shared/ui/PageContainer";
import { PageHeader } from "@/shared/ui/PageHeader";

export function DashboardPage(): JSX.Element {
  const { selectedProjectId } = useProjectSelection();

  return (
    <PageContainer>
      <PageHeader
        title="Dashboard"
        description={
          selectedProjectId
            ? `Selected project: #${selectedProjectId}`
            : "No project selected. Create or select a project to start."
        }
      />
      <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
        <DashboardCard
          icon={<ApartmentIcon color="primary" />}
          title="Buildings"
          description="Create projects, buildings, floors, rooms, envelope elements, and model inputs."
          to={paths.buildings}
          action="Open buildings"
        />
        <DashboardCard
          icon={<CalculateIcon color="primary" />}
          title="Calculations"
          description="Run building and room load calculations from a building workspace."
          to={paths.buildings}
          action="Choose building"
        />
        <DashboardCard
          icon={<PrecisionManufacturingIcon color="primary" />}
          title="Equipment"
          description="Maintain cooling equipment catalog items and select equipment for rooms."
          to={paths.equipmentSelection}
          action="Open equipment"
        />
      </Stack>
    </PageContainer>
  );
}

interface DashboardCardProps {
  icon: JSX.Element;
  title: string;
  description: string;
  to: string;
  action: string;
}

function DashboardCard({ icon, title, description, to, action }: DashboardCardProps): JSX.Element {
  return (
    <DataCard>
      <Stack spacing={2} sx={{ minWidth: { md: 260 } }}>
        {icon}
        <Stack spacing={0.5}>
          <Typography variant="h6" sx={{ fontWeight: 700 }}>
            {title}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {description}
          </Typography>
        </Stack>
        <Button component={RouterLink} to={to} variant="outlined">
          {action}
        </Button>
      </Stack>
    </DataCard>
  );
}
