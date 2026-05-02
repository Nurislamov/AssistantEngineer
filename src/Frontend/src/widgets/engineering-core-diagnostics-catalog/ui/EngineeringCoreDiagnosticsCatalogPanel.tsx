import ErrorOutlineIcon from "@mui/icons-material/ErrorOutline";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import RuleIcon from "@mui/icons-material/Rule";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import {
  Alert,
  Box,
  Chip,
  Divider,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Stack,
  Typography,
} from "@mui/material";
import { useEngineeringCoreDiagnosticsCatalog } from "@/entities/calculation/model/useEngineeringCoreDiagnosticsCatalog";
import type { EngineeringCoreV1DiagnosticCatalogItemApiResponse } from "@/entities/calculation/types";
import { DataCard } from "@/shared/ui/DataCard";
import { QueryState } from "@/shared/ui/QueryState";

const ANNUAL_8760_DIAGNOSTIC_CODES = new Set([
  "AnnualEnergy.Not8760",
  "AnnualEnergy.SyntheticWeather",
  "SolarWeather.SyntheticWeatherUsed",
  "AnnualEnergy.MonthlyBalanceAdapter",
  "AnnualEnergy.TrueHourlySimulationPartial",
]);

export function EngineeringCoreDiagnosticsCatalogPanel(): JSX.Element {
  const query = useEngineeringCoreDiagnosticsCatalog();

  const queryState = (
    <QueryState
      isLoading={query.isLoading}
      error={query.error}
      loadingLabel="Загружаем каталог диагностик..."
      onRetry={() => query.refetch()}
    />
  );

  if (queryState) {
    return <DataCard>{queryState}</DataCard>;
  }

  const catalog = query.data;

  if (!catalog) {
    return (
      <DataCard>
        <Alert severity="warning">Каталог диагностик Engineering Core V1 недоступен.</Alert>
      </DataCard>
    );
  }

  const errorDiagnostics = catalog.diagnostics.filter((item) => item.severity === "Error");
  const warningDiagnostics = catalog.diagnostics.filter((item) => item.severity === "Warning");
  const infoDiagnostics = catalog.diagnostics.filter((item) => item.severity === "Info");
  const annual8760Diagnostics = catalog.diagnostics.filter((item) =>
    ANNUAL_8760_DIAGNOSTIC_CODES.has(item.code),
  );

  return (
    <DataCard>
      <Stack spacing={2}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={1.5} alignItems={{ md: "center" }}>
          <Stack spacing={0.5} sx={{ flex: 1 }}>
            <Typography variant="h6" sx={{ fontWeight: 700 }}>
              Engineering Core diagnostics catalog
            </Typography>
            <Typography variant="body2" color="text.secondary">
              User-facing Error / Warning / Info rules, actions and annual 8760 safeguards.
            </Typography>
          </Stack>

          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <Chip color="success" label={catalog.status} variant="filled" />
            <Chip color="error" label={`${errorDiagnostics.length} Error`} variant="outlined" />
            <Chip color="warning" label={`${warningDiagnostics.length} Warning`} variant="outlined" />
            <Chip color="info" label={`${infoDiagnostics.length} Info`} variant="outlined" />
          </Stack>
        </Stack>

        <Alert severity="info" icon={<RuleIcon />}>
          {catalog.rules.successRule}
        </Alert>

        <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
          <RuleSummary
            title="Error"
            description={catalog.rules.error}
            icon={<ErrorOutlineIcon color="error" fontSize="small" />}
          />

          <RuleSummary
            title="Warning"
            description={catalog.rules.warning}
            icon={<WarningAmberIcon color="warning" fontSize="small" />}
          />

          <RuleSummary
            title="Info"
            description={catalog.rules.info}
            icon={<InfoOutlinedIcon color="info" fontSize="small" />}
          />
        </Stack>

        <Divider />

        <DiagnosticsSection
          title="Annual 8760 safeguards"
          description="These diagnostics prevent adapted, synthetic or partial results from being presented as true hourly annual 8760 simulation."
          diagnostics={annual8760Diagnostics}
          emptyLabel="No annual 8760 diagnostics found."
        />

        <DiagnosticsSection
          title="Blocking Error diagnostics"
          description="Error diagnostics represent invalid mandatory input. Calculation must fail."
          diagnostics={errorDiagnostics.slice(0, 8)}
          emptyLabel="No Error diagnostics found."
        />

        <DiagnosticsSection
          title="Warnings and user actions"
          description="Warnings must stay visible near results and report disclosures."
          diagnostics={warningDiagnostics.slice(0, 10)}
          emptyLabel="No Warning diagnostics found."
        />
      </Stack>
    </DataCard>
  );
}

function RuleSummary({
  title,
  description,
  icon,
}: {
  title: string;
  description: string;
  icon: JSX.Element;
}): JSX.Element {
  return (
    <Box sx={{ flex: 1 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
        {icon}
        <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
          {title}
        </Typography>
      </Stack>
      <Typography variant="body2" color="text.secondary">
        {description}
      </Typography>
    </Box>
  );
}

function DiagnosticsSection({
  title,
  description,
  diagnostics,
  emptyLabel,
}: {
  title: string;
  description: string;
  diagnostics: EngineeringCoreV1DiagnosticCatalogItemApiResponse[];
  emptyLabel: string;
}): JSX.Element {
  return (
    <Box>
      <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
        {title}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
        {description}
      </Typography>

      {diagnostics.length === 0 ? (
        <Alert severity="warning">{emptyLabel}</Alert>
      ) : (
        <List dense disablePadding>
          {diagnostics.map((diagnostic) => (
            <ListItem key={diagnostic.code} disableGutters alignItems="flex-start">
              <ListItemIcon sx={{ minWidth: 34 }}>{severityIcon(diagnostic.severity)}</ListItemIcon>
              <ListItemText
                primary={
                  <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap alignItems="center">
                    <Typography variant="body2" sx={{ fontWeight: 700 }}>
                      {diagnostic.code}
                    </Typography>
                    <Chip label={diagnostic.severity} size="small" variant="outlined" />
                    <Chip label={diagnostic.closedV1Gate} size="small" />
                  </Stack>
                }
                secondary={
                  <Stack spacing={0.5} sx={{ mt: 0.5 }}>
                    <Typography variant="body2" color="text.secondary">
                      {diagnostic.userMessage}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Action: {diagnostic.userAction}
                    </Typography>
                  </Stack>
                }
              />
            </ListItem>
          ))}
        </List>
      )}
    </Box>
  );
}

function severityIcon(severity: string): JSX.Element {
  if (severity === "Error") {
    return <ErrorOutlineIcon color="error" fontSize="small" />;
  }

  if (severity === "Warning") {
    return <WarningAmberIcon color="warning" fontSize="small" />;
  }

  return <InfoOutlinedIcon color="info" fontSize="small" />;
}
