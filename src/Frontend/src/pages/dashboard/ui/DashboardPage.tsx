import ApartmentIcon from "@mui/icons-material/Apartment";
import CalculateIcon from "@mui/icons-material/Calculate";
import PrecisionManufacturingIcon from "@mui/icons-material/PrecisionManufacturing";
import { Button, Stack, Typography } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import { appConfig } from "@/shared/config/env";
import { paths } from "@/app/router/paths";
import { DataCard } from "@/shared/ui/DataCard";
import { PageContainer } from "@/shared/ui/PageContainer";
import { PageHeader } from "@/shared/ui/PageHeader";

export function DashboardPage(): JSX.Element {
  return (
    <PageContainer>
      <PageHeader
        title="Dashboard"
        description={`Текущий проект для API-запросов: #${appConfig.defaultProjectId}`}
      />
      <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
        <DashboardCard
          icon={<ApartmentIcon color="primary" />}
          title="Здания"
          description="Список зданий, создание и переход в карточку."
          to={paths.buildings}
          action="Открыть здания"
        />
        <DashboardCard
          icon={<CalculateIcon color="primary" />}
          title="Расчёты"
          description="Расчёты запускаются из карточки здания или строки помещения."
          to={paths.calculations}
          action="Открыть результаты"
        />
        <DashboardCard
          icon={<PrecisionManufacturingIcon color="primary" />}
          title="Оборудование"
          description="Заготовка под будущий подбор оборудования."
          to={paths.equipmentSelection}
          action="Открыть модуль"
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
