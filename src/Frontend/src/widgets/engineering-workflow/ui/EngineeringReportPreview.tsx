import DescriptionIcon from "@mui/icons-material/Description";
import {
  Alert,
  Button,
  Chip,
  Divider,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import type { WorkflowDiagnostic, WorkflowReportPreview } from "@/entities/engineering-workflow/types";
import { formatDateTime } from "@/shared/lib/format";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";

interface EngineeringReportPreviewProps {
  preview: WorkflowReportPreview | undefined;
  diagnostics: WorkflowDiagnostic[];
  jsonOutput: string;
  markdownOutput: string;
  onExportJson: () => Promise<void>;
  onExportMarkdown: () => Promise<void>;
}

export function EngineeringReportPreview({
  preview,
  diagnostics,
  jsonOutput,
  markdownOutput,
  onExportJson,
  onExportMarkdown,
}: EngineeringReportPreviewProps): JSX.Element {
  return (
    <DataCard>
      <Stack spacing={2}>
        <Stack direction="row" spacing={1} alignItems="center">
          <DescriptionIcon color="primary" fontSize="small" />
          <Typography variant="h6">Engineering report preview</Typography>
        </Stack>

        {!preview ? (
          <EmptyState
            title="Report preview is unavailable"
            description="Generate report preview from workflow state to inspect sections and export output."
          />
        ) : (
          <>
            <Stack direction={{ xs: "column", sm: "row" }} spacing={1} alignItems={{ xs: "flex-start", sm: "center" }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>{preview.title}</Typography>
              <Chip size="small" label={preview.reportKind} variant="outlined" />
              <Chip size="small" label={`warnings: ${preview.warningsCount}`} color={preview.warningsCount > 0 ? "warning" : "default"} />
              <Chip size="small" label={`diagnostics: ${preview.diagnosticsCount}`} color={preview.diagnosticsCount > 0 ? "info" : "default"} />
            </Stack>

            <Typography variant="body2" color="text.secondary">
              Generated: {formatDateTime(preview.generatedTimestamp)}
            </Typography>

            <Typography variant="body2" sx={{ fontWeight: 600 }}>Sections</Typography>
            <Stack spacing={0.5}>
              {preview.sections.map((section) => (
                <Typography key={section} variant="body2">- {section}</Typography>
              ))}
            </Stack>

            <Divider />

            <Typography variant="body2" sx={{ fontWeight: 600 }}>Limitations</Typography>
            <Stack spacing={0.5}>
              {preview.limitations.map((item) => (
                <Typography key={item} variant="body2">- {item}</Typography>
              ))}
            </Stack>

            {diagnostics.some((item) => item.sourceStep === "Reports") ? (
              <Alert severity="info">
                Report export currently follows workflow foundation mode and may rely on internal dev adapter behavior for pending endpoints.
              </Alert>
            ) : null}

            <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
              <Button variant="contained" onClick={() => void onExportJson()}>Export JSON</Button>
              <Button variant="outlined" onClick={() => void onExportMarkdown()}>Export Markdown</Button>
            </Stack>

            <TextField
              label="JSON output"
              value={jsonOutput}
              multiline
              minRows={8}
              maxRows={16}
              fullWidth
              InputProps={{ readOnly: true }}
            />

            <TextField
              label="Markdown output"
              value={markdownOutput}
              multiline
              minRows={8}
              maxRows={16}
              fullWidth
              InputProps={{ readOnly: true }}
            />
          </>
        )}
      </Stack>
    </DataCard>
  );
}
