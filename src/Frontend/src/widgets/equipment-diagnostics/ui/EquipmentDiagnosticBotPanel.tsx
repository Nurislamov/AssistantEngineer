import SearchIcon from "@mui/icons-material/Search";
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  FormControl,
  InputLabel,
  List,
  ListItem,
  ListItemText,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useRef, useState } from "react";
import {
  diagnoseEquipmentBot,
  EquipmentDiagnosticBotClientError,
} from "@/entities/equipment-diagnostics/api/equipmentDiagnosticBotClient";
import type {
  EquipmentDiagnosticBotDisplayContext,
  EquipmentDiagnosticBotEquipmentSide,
  EquipmentDiagnosticBotRequest,
  EquipmentDiagnosticBotResponse,
} from "@/entities/equipment-diagnostics/types";
import { DataCard } from "@/shared/ui/DataCard";

const equipmentSides: EquipmentDiagnosticBotEquipmentSide[] = [
  "Unknown", "Indoor", "Outdoor", "Chiller", "Controller", "CommissioningTool",
];
const displayContexts: EquipmentDiagnosticBotDisplayContext[] = [
  "Unknown", "WiredController", "OduMainBoardLed", "IduDisplay",
  "CentralizedController", "PortableCommissioningTool", "MobileAppOrGateway",
];
const responseStatusNames = [
  "Answer", "ClarificationRequired", "NotFound", "ReferenceOnly", "Unsupported", "UnsafeOrOutOfScope",
] as const;

export function EquipmentDiagnosticBotPanel(): JSX.Element {
  const [manufacturer, setManufacturer] = useState("Gree");
  const [code, setCode] = useState("");
  const [series, setSeries] = useState("");
  const [modelCode, setModelCode] = useState("");
  const [equipmentSide, setEquipmentSide] = useState<EquipmentDiagnosticBotEquipmentSide>("Unknown");
  const [displayContext, setDisplayContext] = useState<EquipmentDiagnosticBotDisplayContext>("Unknown");
  const [response, setResponse] = useState<EquipmentDiagnosticBotResponse | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const abortRef = useRef<AbortController | null>(null);

  const submit = async (): Promise<void> => {
    if (loading) return;
    setError("");
    setResponse(null);
    setLoading(true);
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    const request: EquipmentDiagnosticBotRequest = {
      manufacturer,
      code,
      ...(series.trim() ? { series } : {}),
      ...(modelCode.trim() ? { modelCode } : {}),
      ...(equipmentSide !== "Unknown" ? { equipmentSide: equipmentSides.indexOf(equipmentSide) - 1 } : {}),
      ...(displayContext !== "Unknown" ? { displayContext: displayContexts.indexOf(displayContext) - 1 } : {}),
    };

    try {
      setResponse(await diagnoseEquipmentBot(request, { signal: controller.signal }));
    } catch (requestError) {
      if (!(requestError instanceof DOMException && requestError.name === "AbortError")) {
        setError(requestError instanceof EquipmentDiagnosticBotClientError
          ? requestError.message
          : "The diagnostic request could not be completed.");
      }
    } finally {
      if (abortRef.current === controller) {
        setLoading(false);
      }
    }
  };

  return (
    <Stack spacing={2}>
      <DataCard>
        <Stack spacing={2}>
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <TextField
              required
              label="Manufacturer"
              value={manufacturer}
              onChange={(event) => setManufacturer(event.target.value)}
              inputProps={{ maxLength: 80 }}
              fullWidth
            />
            <TextField
              required
              label="Displayed code"
              value={code}
              onChange={(event) => setCode(event.target.value)}
              inputProps={{ maxLength: 32 }}
              fullWidth
            />
          </Stack>
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <TextField label="Series" value={series} onChange={(event) => setSeries(event.target.value)} inputProps={{ maxLength: 120 }} fullWidth />
            <TextField label="Model code" value={modelCode} onChange={(event) => setModelCode(event.target.value)} inputProps={{ maxLength: 120 }} fullWidth />
          </Stack>
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <Selection label="Equipment side" value={equipmentSide} values={equipmentSides} onChange={(value) => setEquipmentSide(value as EquipmentDiagnosticBotEquipmentSide)} />
            <Selection label="Display context" value={displayContext} values={displayContexts} onChange={(value) => setDisplayContext(value as EquipmentDiagnosticBotDisplayContext)} />
          </Stack>
          <Button
            variant="contained"
            startIcon={loading ? <CircularProgress size={18} color="inherit" /> : <SearchIcon />}
            disabled={loading || !manufacturer.trim() || !code.trim()}
            onClick={() => void submit()}
            sx={{ alignSelf: "flex-start" }}
          >
            {loading ? "Checking runtime catalog..." : "Diagnose"}
          </Button>
        </Stack>
      </DataCard>

      {error ? <Alert severity="error">{error}</Alert> : null}
      {response ? (
        <DiagnosticResponse
          response={response}
          onSelectOption={(option) => {
            setManufacturer(option.manufacturer);
            setCode(option.code);
            setSeries(option.series ?? "");
            setEquipmentSide(sideFromValue(option.equipmentSide));
            setDisplayContext(displayFromValue(option.displayContext));
            setResponse(null);
          }}
        />
      ) : null}
    </Stack>
  );
}

function Selection({ label, value, values, onChange }: {
  label: string;
  value: string;
  values: readonly string[];
  onChange: (value: string) => void;
}): JSX.Element {
  return (
    <FormControl fullWidth>
      <InputLabel>{label}</InputLabel>
      <Select label={label} value={value} onChange={(event) => onChange(event.target.value)}>
        {values.map((item) => <MenuItem key={item} value={item}>{item}</MenuItem>)}
      </Select>
    </FormControl>
  );
}

function DiagnosticResponse({ response, onSelectOption }: {
  response: EquipmentDiagnosticBotResponse;
  onSelectOption: (option: NonNullable<EquipmentDiagnosticBotResponse["clarificationQuestion"]>["options"][number]) => void;
}): JSX.Element {
  const status = typeof response.status === "number" ? responseStatusNames[response.status] : response.status;
  const isAnswer = status === "Answer";

  return (
    <DataCard>
      <Stack spacing={2}>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={1} alignItems={{ sm: "center" }}>
          <Typography variant="h6" sx={{ flex: 1 }}>{response.title}</Typography>
          <Chip label={status} color={isAnswer ? "success" : "warning"} />
          <Chip label={`Confidence: ${response.confidence}`} variant="outlined" />
        </Stack>
        <Typography variant="body2">{response.message}</Typography>

        {response.verificationRequired ? (
          <Alert severity="warning">
            {response.answerCard?.verificationBanner ?? "Verification is required before final conclusion."}
          </Alert>
        ) : null}

        {response.clarificationQuestion ? (
          <Box>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>{response.clarificationQuestion.prompt}</Typography>
            <Stack direction={{ xs: "column", sm: "row" }} spacing={1} flexWrap="wrap" useFlexGap>
              {response.clarificationQuestion.options.map((option) => (
                <Button key={`${option.label}-${option.code}`} variant="outlined" onClick={() => onSelectOption(option)}>
                  {option.label}
                </Button>
              ))}
            </Stack>
          </Box>
        ) : null}

        {response.sourceCard ? (
          <Box>
            <Typography variant="subtitle2">Source and provenance</Typography>
            <Typography variant="body2" color="text.secondary">{response.sourceCard.summary}</Typography>
          </Box>
        ) : null}

        <Alert severity="info">
          <Typography variant="subtitle2">Safety boundary</Typography>
          {response.safetyCard.boundary}
        </Alert>

        <Divider />
        <Box>
          <Typography variant="subtitle2">Operator next steps</Typography>
          <List dense disablePadding>
            {response.operatorNextSteps.map((step) => (
              <ListItem key={step} disableGutters><ListItemText primary={step} /></ListItem>
            ))}
          </List>
        </Box>
      </Stack>
    </DataCard>
  );
}

function sideFromValue(value: number): EquipmentDiagnosticBotEquipmentSide {
  return (["Indoor", "Outdoor", "Chiller", "Controller", "CommissioningTool", "Unknown"] as const)[value] ?? "Unknown";
}

function displayFromValue(value: number): EquipmentDiagnosticBotDisplayContext {
  return (["WiredController", "OduMainBoardLed", "IduDisplay", "CentralizedController", "PortableCommissioningTool", "MobileAppOrGateway", "Unknown"] as const)[value] ?? "Unknown";
}
