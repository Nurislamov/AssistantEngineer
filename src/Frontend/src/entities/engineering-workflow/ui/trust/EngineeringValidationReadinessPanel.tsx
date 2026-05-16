import CheckCircleOutlineIcon from "@mui/icons-material/CheckCircleOutline";
import RadioButtonUncheckedIcon from "@mui/icons-material/RadioButtonUnchecked";
import {
  Alert,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Stack,
  Typography,
} from "@mui/material";
import type { EngineeringValidationReadinessViewModel } from "@/entities/engineering-workflow/model/engineeringWorkflowTrust";
import { DataCard } from "@/shared/ui/DataCard";

interface EngineeringValidationReadinessPanelProps {
  readiness: EngineeringValidationReadinessViewModel;
}

function CheckItem({
  label,
  checked,
}: {
  label: string;
  checked: boolean;
}): JSX.Element {
  return (
    <ListItem sx={{ py: 0.5, px: 0 }}>
      <ListItemIcon sx={{ minWidth: 32 }}>
        {checked ? <CheckCircleOutlineIcon color="success" fontSize="small" /> : <RadioButtonUncheckedIcon color="disabled" fontSize="small" />}
      </ListItemIcon>
      <ListItemText primaryTypographyProps={{ variant: "body2" }} primary={label} />
    </ListItem>
  );
}

export function EngineeringValidationReadinessPanel({
  readiness,
}: EngineeringValidationReadinessPanelProps): JSX.Element {
  return (
    <DataCard compact>
      <Stack spacing={1.5}>
        <Typography variant="subtitle1">Validation readiness</Typography>

        <List dense sx={{ py: 0 }}>
          <CheckItem label="Manual validation fixtures" checked={readiness.manualFixturesAvailable} />
          <CheckItem label="Validation tolerance policy" checked={readiness.tolerancePolicyAvailable} />
          <CheckItem label="Engineering assumptions registry" checked={readiness.assumptionsRegistryAvailable} />
          <CheckItem label="Units governance" checked={readiness.unitsGovernanceAvailable} />
          <CheckItem label="Input quality checks" checked={readiness.inputQualityAvailable} />
          <CheckItem label="Trace explainability foundation" checked={readiness.traceExplainabilityAvailable} />
        </List>

        <Alert severity="info">
          Foundation readiness items indicate governance and workflow transparency coverage. They do not indicate certification or full external standard validation.
        </Alert>

        <Stack spacing={0.5}>
          <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600 }}>
            Non-claims
          </Typography>
          {readiness.nonClaims.map((line) => (
            <Typography key={line} variant="caption" color="text.secondary">
              - {line}
            </Typography>
          ))}
          <Typography variant="caption" color="text.secondary">This panel does not claim ASHRAE 140 compliance.</Typography>
          <Typography variant="caption" color="text.secondary">This panel does not claim exact EnergyPlus equivalence.</Typography>
          <Typography variant="caption" color="text.secondary">This panel does not claim third-party tool equivalence.</Typography>
          <Typography variant="caption" color="text.secondary">This panel does not claim full ISO/EN compliance.</Typography>
          <Typography variant="caption" color="text.secondary">This panel does not claim certified status.</Typography>
        </Stack>
      </Stack>
    </DataCard>
  );
}

