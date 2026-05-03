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

interface CalculationDisclosureApiResponse {
  coreStatus: string;
  calculationScope: string;
  calculationMethod: string;
  actualMethod: string;
  warnings: string[];
  assumptions: string[];
  explicitNonClaims: string[];
  outOfScopeV1: string[];
  documentationFiles: string[];
}

interface CalculationDiagnosticApiResponse {
  severity: "Info" | "Warning" | "Error" | string | number;
  code: string;
  message: string;
  context?: string | null;
}

interface ReportWithEngineeringCoreVisibility {
  calculationDisclosure?: CalculationDisclosureApiResponse | null;
  diagnostics?: CalculationDiagnosticApiResponse[] | null;
}

interface EngineeringCoreDisclosurePanelProps {
  report: unknown;
}

export function EngineeringCoreDisclosurePanel({
  report,
}: EngineeringCoreDisclosurePanelProps): JSX.Element | null {
  const disclosure = extractCalculationDisclosure(report);
  const diagnostics = extractDiagnostics(report);

  if (!disclosure && diagnostics.length === 0) {
    return null;
  }

  return (
    <Alert severity={getPanelSeverity(diagnostics)} icon={<InfoOutlinedIcon />}>
      <Stack spacing={1.5}>
        {disclosure ? <CalculationDisclosureSection disclosure={disclosure} /> : null}

        {disclosure && diagnostics.length > 0 ? <Divider /> : null}

        {diagnostics.length > 0 ? <DiagnosticsSection diagnostics={diagnostics} /> : null}
      </Stack>
    </Alert>
  );
}

function CalculationDisclosureSection({
  disclosure,
}: {
  disclosure: CalculationDisclosureApiResponse;
}): JSX.Element {
  return (
    <Stack spacing={1.5}>
      <Stack direction={{ xs: "column", md: "row" }} spacing={1} alignItems={{ md: "center" }}>
        <Box sx={{ flex: 1 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            Engineering Core disclosure
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {disclosure.calculationScope}
          </Typography>
        </Box>

        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
          <Chip
            icon={<CheckCircleIcon />}
            color={disclosure.coreStatus === "ClosedV1" ? "success" : "warning"}
            label={disclosure.coreStatus}
            size="small"
          />
          <Chip label={`method: ${disclosure.calculationMethod}`} size="small" variant="outlined" />
          <Chip label={`actual: ${disclosure.actualMethod}`} size="small" variant="outlined" />
        </Stack>
      </Stack>

      <Divider />

      <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
        <DisclosureList
          title="Warnings"
          items={disclosure.warnings}
          icon={<WarningAmberIcon color="warning" fontSize="small" />}
        />

        <DisclosureList
          title="Assumptions"
          items={disclosure.assumptions}
          icon={<InfoOutlinedIcon color="info" fontSize="small" />}
        />
      </Stack>

      <Box>
        <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
          Explicit non-claims
        </Typography>
        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
          {disclosure.explicitNonClaims.map((claim) => (
            <Chip key={claim} label={claim} size="small" variant="outlined" />
          ))}
        </Stack>
      </Box>

      <Box>
        <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
          Out of scope v1
        </Typography>
        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
          {disclosure.outOfScopeV1.map((item) => (
            <Chip key={item} label={item} color="warning" size="small" variant="outlined" />
          ))}
        </Stack>
      </Box>

      <Box>
        <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
          Documentation
        </Typography>
        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
          {disclosure.documentationFiles.map((file) => (
            <Chip key={file} label={file} size="small" />
          ))}
        </Stack>
      </Box>
    </Stack>
  );
}

function DiagnosticsSection({
  diagnostics,
}: {
  diagnostics: CalculationDiagnosticApiResponse[];
}): JSX.Element {
  const solarPath = getSolarPathLabel(diagnostics);

  return (
    <Box>
      <Stack direction={{ xs: "column", md: "row" }} spacing={1} alignItems={{ md: "center" }}>
        <Box sx={{ flex: 1 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            Calculation diagnostics
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Method/source diagnostics returned by the calculation API.
          </Typography>
        </Box>

        {solarPath ? <Chip label={solarPath} color={solarPath.includes("fallback") ? "warning" : "success"} size="small" /> : null}
      </Stack>

      <List dense disablePadding sx={{ mt: 1 }}>
        {diagnostics.map((diagnostic) => (
          <ListItem key={`${diagnostic.code}-${diagnostic.message}-${diagnostic.context ?? ""}`} disableGutters alignItems="flex-start">
            <ListItemIcon sx={{ minWidth: 32 }}>{getDiagnosticIcon(diagnostic)}</ListItemIcon>
            <ListItemText
              primary={
                <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap alignItems="center">
                  <Chip
                    label={getSeverityLabel(diagnostic.severity)}
                    color={getDiagnosticChipColor(diagnostic)}
                    size="small"
                    variant="outlined"
                  />
                  <Chip label={diagnostic.code} size="small" />
                </Stack>
              }
              secondary={
                <>
                  {diagnostic.message}
                  {diagnostic.context ? ` Context: ${diagnostic.context}` : ""}
                </>
              }
            />
          </ListItem>
        ))}
      </List>
    </Box>
  );
}

function DisclosureList({
  title,
  items,
  icon,
}: {
  title: string;
  items: string[];
  icon: JSX.Element;
}): JSX.Element {
  return (
    <Box sx={{ flex: 1 }}>
      <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
        {title}
      </Typography>
      <List dense disablePadding>
        {items.map((item) => (
          <ListItem key={item} disableGutters alignItems="flex-start">
            <ListItemIcon sx={{ minWidth: 32 }}>{icon}</ListItemIcon>
            <ListItemText primary={item} />
          </ListItem>
        ))}
      </List>
    </Box>
  );
}

function extractCalculationDisclosure(report: unknown): CalculationDisclosureApiResponse | null {
  if (!isObject(report)) {
    return null;
  }

  const candidate = (report as ReportWithEngineeringCoreVisibility).calculationDisclosure;

  if (!isCalculationDisclosure(candidate)) {
    return null;
  }

  return candidate;
}

function extractDiagnostics(report: unknown): CalculationDiagnosticApiResponse[] {
  if (!isObject(report)) {
    return [];
  }

  const candidate = (report as ReportWithEngineeringCoreVisibility).diagnostics;

  if (!Array.isArray(candidate)) {
    return [];
  }

  return candidate.filter(isCalculationDiagnostic);
}

function isCalculationDisclosure(value: unknown): value is CalculationDisclosureApiResponse {
  if (!isObject(value)) {
    return false;
  }

  const candidate = value as Partial<CalculationDisclosureApiResponse>;

  return (
    typeof candidate.coreStatus === "string" &&
    typeof candidate.calculationScope === "string" &&
    typeof candidate.calculationMethod === "string" &&
    typeof candidate.actualMethod === "string" &&
    isStringArray(candidate.warnings) &&
    isStringArray(candidate.assumptions) &&
    isStringArray(candidate.explicitNonClaims) &&
    isStringArray(candidate.outOfScopeV1) &&
    isStringArray(candidate.documentationFiles)
  );
}

function isCalculationDiagnostic(value: unknown): value is CalculationDiagnosticApiResponse {
  if (!isObject(value)) {
    return false;
  }

  const candidate = value as Partial<CalculationDiagnosticApiResponse>;

  return (
    (typeof candidate.severity === "string" || typeof candidate.severity === "number") &&
    typeof candidate.code === "string" &&
    typeof candidate.message === "string" &&
    (candidate.context === undefined || candidate.context === null || typeof candidate.context === "string")
  );
}

function getPanelSeverity(
  diagnostics: CalculationDiagnosticApiResponse[],
): "error" | "warning" | "info" {
  if (diagnostics.some((diagnostic) => normalizeSeverity(diagnostic.severity) === "Error")) {
    return "error";
  }

  if (diagnostics.some((diagnostic) => normalizeSeverity(diagnostic.severity) === "Warning")) {
    return "warning";
  }

  return "info";
}

function getDiagnosticIcon(diagnostic: CalculationDiagnosticApiResponse): JSX.Element {
  const severity = normalizeSeverity(diagnostic.severity);

  if (severity === "Error" || severity === "Warning") {
    return <WarningAmberIcon color={severity === "Error" ? "error" : "warning"} fontSize="small" />;
  }

  return <InfoOutlinedIcon color="info" fontSize="small" />;
}

function getDiagnosticChipColor(
  diagnostic: CalculationDiagnosticApiResponse,
): "default" | "info" | "warning" | "error" {
  const severity = normalizeSeverity(diagnostic.severity);

  if (severity === "Error") {
    return "error";
  }

  if (severity === "Warning") {
    return "warning";
  }

  if (severity === "Info") {
    return "info";
  }

  return "default";
}

function getSeverityLabel(severity: CalculationDiagnosticApiResponse["severity"]): string {
  return normalizeSeverity(severity);
}

function normalizeSeverity(severity: CalculationDiagnosticApiResponse["severity"]): string {
  if (severity === 1) {
    return "Info";
  }

  if (severity === 2) {
    return "Warning";
  }

  if (severity === 3) {
    return "Error";
  }

  if (typeof severity === "string" && severity.length > 0) {
    return severity;
  }

  return "Info";
}

function getSolarPathLabel(diagnostics: CalculationDiagnosticApiResponse[]): string | null {
  if (diagnostics.some((diagnostic) => diagnostic.code === "Iso52016.WeatherSolarContextUsed")) {
    return "ISO 52016 weather-solar context";
  }

  if (diagnostics.some((diagnostic) => diagnostic.code === "Iso52016.SolarGainComponentPathUsed")) {
    return "ISO 52016 component solar gains";
  }

  if (diagnostics.some((diagnostic) => diagnostic.code === "Iso52016.MatrixSolarRadiationFallbackUsed")) {
    return "matrix solar radiation fallback";
  }

  return null;
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function isStringArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.every((item) => typeof item === "string");
}
