import { apiRoutes } from "@/shared/api/apiRoutes";
import type {
  EngineeringWorkflowReportExportResponse,
  EngineeringWorkflowReportRequest,
  EngineeringWorkflowReportResponse,
  EngineeringWorkflowReportResult,
} from "../types";
import { buildErrorDiagnostic, deduplicateDiagnostics } from "./workflowClientShared";
import { mapTransportError, workflowApiRequest } from "./shared/workflowTransport";

export interface WorkflowReportApi {
  generateReport(request: EngineeringWorkflowReportRequest): Promise<EngineeringWorkflowReportResult>;
  exportReportJson(request: EngineeringWorkflowReportRequest): Promise<string>;
  exportReportMarkdown(request: EngineeringWorkflowReportRequest): Promise<string>;
}

export function createWorkflowReportApi(): WorkflowReportApi {
  return {
    async generateReport(request: EngineeringWorkflowReportRequest): Promise<EngineeringWorkflowReportResult> {
      try {
        const response = await workflowApiRequest<EngineeringWorkflowReportResponse>(
          apiRoutes.engineeringWorkflow.report(),
          {
            method: "POST",
            body: {
              state: request.state,
              reportKind: request.reportKind,
              requestedFormat: request.format,
              detailLevel: request.state.calculationTraceSummary?.detailLevel ?? "Standard",
              includeTraceAppendix: request.includeTraceAppendix,
              includeLimitations: request.includeLimitations,
            },
          },
        );

        return {
          preview: response.preview,
          diagnostics: deduplicateDiagnostics(response.diagnostics),
          json: undefined,
          markdown: undefined,
        };
      } catch (error) {
        const message = mapTransportError(error, "Report generation request failed.");
        return {
          preview: {
            reportKind: request.reportKind,
            title: "Report generation failed",
            sections: [],
            warningsCount: 0,
            diagnosticsCount: 1,
            exportFormatsAvailable: ["Json", "Markdown"],
            generatedTimestamp: new Date().toISOString(),
            limitations: ["Report generation fallback due to API request failure."],
          },
          diagnostics: [buildErrorDiagnostic(message)],
        };
      }
    },

    async exportReportJson(request: EngineeringWorkflowReportRequest): Promise<string> {
      try {
        const response = await workflowApiRequest<EngineeringWorkflowReportExportResponse>(
          apiRoutes.engineeringWorkflow.reportExportJson(),
          {
            method: "POST",
            body: {
              request: {
                state: request.state,
                reportKind: request.reportKind,
                requestedFormat: "Json",
                detailLevel: request.state.calculationTraceSummary?.detailLevel ?? "Standard",
                includeTraceAppendix: request.includeTraceAppendix,
                includeLimitations: request.includeLimitations,
              },
            },
          },
        );

        return response.content;
      } catch (error) {
        return JSON.stringify(
          {
            schemaVersion: "workflow-report-export-error-v1",
            error: mapTransportError(error, "JSON export request failed."),
          },
          null,
          2,
        );
      }
    },

    async exportReportMarkdown(request: EngineeringWorkflowReportRequest): Promise<string> {
      try {
        const response = await workflowApiRequest<EngineeringWorkflowReportExportResponse>(
          apiRoutes.engineeringWorkflow.reportExportMarkdown(),
          {
            method: "POST",
            body: {
              request: {
                state: request.state,
                reportKind: request.reportKind,
                requestedFormat: "Markdown",
                detailLevel: request.state.calculationTraceSummary?.detailLevel ?? "Standard",
                includeTraceAppendix: request.includeTraceAppendix,
                includeLimitations: request.includeLimitations,
              },
            },
          },
        );

        return response.content;
      } catch (error) {
        const message = mapTransportError(error, "Markdown export request failed.");
        return `# Report export error\n\n- ${message}\n`;
      }
    },
  };
}
