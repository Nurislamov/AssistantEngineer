import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
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
import { useEngineeringCoreStatus } from "@/entities/calculation/model/useEngineeringCoreStatus";
import { DataCard } from "@/shared/ui/DataCard";
import { QueryState } from "@/shared/ui/QueryState";

export function EngineeringCoreStatusPanel(): JSX.Element {
  const query = useEngineeringCoreStatus();

  const queryState = (
    <QueryState
      isLoading={query.isLoading}
      error={query.error}
      loadingLabel="Loading engineering core status..."
      onRetry={() => query.refetch()}
    />
  );

  if (queryState) {
    return <DataCard>{queryState}</DataCard>;
  }

  const status = query.data;

  if (!status) {
    return (
      <DataCard>
        <Alert severity="warning">Engineering Core V1 status is unavailable.</Alert>
      </DataCard>
    );
  }

  const closedGateCount = status.formulaGates.filter((gate) => gate.status === "ClosedV1").length;
  const totalGateCount = status.formulaGates.length;

  return (
    <DataCard>
      <Stack spacing={2}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={1.5} alignItems={{ md: "center" }}>
          <Stack spacing={0.5} sx={{ flex: 1 }}>
            <Typography variant="h6" sx={{ fontWeight: 700 }}>
              Engineering Core {status.version}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Visible status of the engineering core, formula gates, and model limitations.
            </Typography>
          </Stack>

          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <Chip
              icon={<CheckCircleIcon />}
              color={status.status === "ClosedV1" ? "success" : "warning"}
              label={status.status}
              variant="filled"
            />
            <Chip
              color={status.formulaGatesClosed ? "success" : "warning"}
              label={`${closedGateCount}/${totalGateCount} gates`}
              variant="outlined"
            />
            <Chip
              color={status.weather8760GatesClosed ? "success" : "warning"}
              label="EPW/PVGIS 8760"
              variant="outlined"
            />
            <Chip
              color={status.annualHourly8760GateClosed ? "success" : "warning"}
              label="Annual 8760"
              variant="outlined"
            />
          </Stack>
        </Stack>

        <Alert severity="info" icon={<InfoOutlinedIcon />}>
          ClosedV1 means engineering formula gates are closed with documented limitations. This is
          not a claim of exact EnergyPlus, StandardReference, or ASHRAE 140 equivalence.
        </Alert>

        <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
          <Box sx={{ flex: 1 }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
              Annual 8760 requirements
            </Typography>
            <List dense disablePadding>
              {status.requiredAnnual8760Flags.map((flag) => (
                <ListItem key={flag} disableGutters>
                  <ListItemIcon sx={{ minWidth: 32 }}>
                    <CheckCircleIcon color="success" fontSize="small" />
                  </ListItemIcon>
                  <ListItemText primary={flag} />
                </ListItem>
              ))}
            </List>
          </Box>

          <Box sx={{ flex: 1 }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
              Out of scope v1
            </Typography>
            <List dense disablePadding>
              {status.outOfScopeV1.slice(0, 5).map((item) => (
                <ListItem key={item} disableGutters>
                  <ListItemIcon sx={{ minWidth: 32 }}>
                    <WarningAmberIcon color="warning" fontSize="small" />
                  </ListItemIcon>
                  <ListItemText primary={item} />
                </ListItem>
              ))}
            </List>
          </Box>
        </Stack>

        <Divider />

        <Box>
          <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
            Explicit non-claims
          </Typography>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            {status.explicitNonClaims.slice(0, 6).map((claim) => (
              <Chip key={claim} label={claim} size="small" variant="outlined" />
            ))}
          </Stack>
        </Box>

        <Box>
          <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
            Documentation
          </Typography>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            {status.documentationFiles.map((file) => (
              <Chip key={file} label={file} size="small" />
            ))}
          </Stack>
        </Box>
      </Stack>
    </DataCard>
  );
}
