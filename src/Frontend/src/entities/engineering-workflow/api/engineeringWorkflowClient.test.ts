import { apiRoutes } from "@/shared/api/apiRoutes";
import {
  createEngineeringWorkflowClient,
  describeWorkflowStepStatus,
  summarizeBuildingMetrics,
} from "./engineeringWorkflowClient";
import type {
  EngineeringWorkflowApiStateResponse,
  EngineeringWorkflowCalculationPreparationResult,
  EngineeringWorkflowCalculationRequest,
  ProjectWorkflowState,
} from "../types";

function createState(): ProjectWorkflowState {
  return {
    projectId: 10,
    projectName: "Project 10",
    buildingId: 100,
    availableModules: ["ThermalTopology", "SystemEnergy"],
    assumptions: ["baseline assumption"],
    links: [],
    metadata: {},
    buildingMetadata: {
      buildingName: "Building",
      locationText: "Tashkent",
      floorAreaM2: 100,
      volumeM3: 250,
      numberOfZones: 1,
      notes: "note",
    },
    zones: [{ id: "z1", name: "Zone 1", zoneKind: "Conditioned", floorAreaM2: 100, airVolumeM3: 250, status: "valid" }],
    boundaries: [{
      id: "b1",
      zoneOrRoomName: "Zone 1",
      exposureKind: "External",
      areaM2: 30,
      uValue: 0.45,
      adjacentZoneReference: undefined,
      indicator: "exterior",
      validationStatus: "valid",
    }],
    weatherSolarSettings: {
      weatherSourceStatus: "Ready",
      locationTimezoneSummary: "UTC+05",
      solarChainReadinessSummary: "Ready",
    },
    ventilationSettings: {
      openingCount: 1,
      controlModeSummary: "Auto",
      airflowSummary: "Configured",
      warnings: [],
    },
    groundSettings: {
      groundBoundaryCount: 1,
      groundProfileMode: "Constant",
      summaryStatus: "valid",
    },
    domesticHotWaterSettings: {
      demandBasis: "PerPerson",
      usefulDemandSummary: "1200",
      lossesSummary: "200",
      ownershipPolicy: "NoDoubleCounting",
    },
    systemEnergySettings: {
      usesSummary: "Heating,DHW",
      carriersSummary: "Electricity",
      finalPrimaryCarbonSummary: "Ready",
    },
    validationDiagnostics: [],
    currentStep: "Review",
    completionByStep: [{ step: "Review", status: "ready" }],
    workflowMode: "api",
    workflowModeLabel: "API workflow mode",
  };
}

function createWorkflowStateResponse(): EngineeringWorkflowApiStateResponse {
  const state = createState();
  return {
    projectId: 10,
    projectName: "Project 10",
    buildingId: 100,
    availableModules: state.availableModules,
    assumptions: state.assumptions,
    links: state.links,
    metadata: state.metadata,
    buildingMetadata: state.buildingMetadata,
    zones: state.zones,
    boundaries: state.boundaries,
    weatherSolarSettings: state.weatherSolarSettings,
    ventilationSettings: state.ventilationSettings,
    groundSettings: state.groundSettings,
    domesticHotWaterSettings: state.domesticHotWaterSettings,
    systemEnergySettings: state.systemEnergySettings,
    diagnostics: [],
    steps: [{ kind: "Review", status: "ready", isComplete: true }],
    currentStep: "Review",
    calculationTraceSummary: undefined,
    reportSummary: undefined,
  };
}

describe("engineeringWorkflowClient", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("buildCalculationRequest returns deterministic workflow payload shape", () => {
    const client = createEngineeringWorkflowClient("api");
    const state = createState();

    const request = client.buildCalculationRequest(state);

    expect(request).toEqual({
      projectId: 10,
      buildingId: 100,
      workflowState: state,
    });
  });

  it("getWorkflowState generates expected endpoint URL with query", async () => {
    const client = createEngineeringWorkflowClient("api");
    const responsePayload = createWorkflowStateResponse();
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(
      new Response(JSON.stringify(responsePayload), {
        status: 200,
        headers: { "content-type": "application/json" },
      }),
    );

    const result = await client.getWorkflowState(10, 100);

    expect(result.projectId).toBe(10);
    expect(fetchSpy).toHaveBeenCalledTimes(1);
    const [url] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain(apiRoutes.engineeringWorkflow.state(10));
    expect(String(url)).toContain("buildingId=100");
  });

  it("validateWorkflow returns controlled error diagnostic when fetch fails", async () => {
    const client = createEngineeringWorkflowClient("api");
    const state = createState();
    vi.spyOn(globalThis, "fetch").mockRejectedValueOnce(new Error("network down"));

    const diagnostics = await client.validateWorkflow(state);

    expect(diagnostics).toHaveLength(1);
    expect(diagnostics[0].severity).toBe("error");
    expect(diagnostics[0].code).toBe("WORKFLOW_API_ERROR");
    expect(diagnostics[0].message).toContain("network down");
  });

  it("prepareCalculation posts request in deterministic shape", async () => {
    const client = createEngineeringWorkflowClient("api");
    const state = createState();
    const preparation: EngineeringWorkflowCalculationPreparationResult = {
      requestId: "wf-prepare-10",
      status: "prepared",
      diagnostics: [],
      metadata: { mode: "api" },
    };

    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(
      new Response(JSON.stringify(preparation), {
        status: 200,
        headers: { "content-type": "application/json" },
      }),
    );

    const request: EngineeringWorkflowCalculationRequest = client.buildCalculationRequest(state);
    const response = await client.prepareCalculation(request);

    expect(response.requestId).toBe("wf-prepare-10");
    expect(fetchSpy).toHaveBeenCalledTimes(1);

    const [, init] = fetchSpy.mock.calls[0];
    expect(init?.method).toBe("POST");
    expect(JSON.parse(String(init?.body))).toEqual({
      state,
      executeCalculation: false,
    });
  });

  it("runCalculation posts to run endpoint with idempotency key", async () => {
    const client = createEngineeringWorkflowClient("api");
    const state = createState();
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(
      new Response(JSON.stringify({
        scenarioId: "scenario-10",
        status: "Completed",
        executed: true,
        executedModules: [],
        skippedModules: [],
        unavailableModules: [],
        validationDiagnostics: [],
        assumptions: [],
        warnings: [],
        moduleSummaries: {},
        moduleResults: [],
        timings: [],
        calculationTraceSummary: null,
        reportPreview: null,
        reportJson: null,
        reportMarkdown: null,
        metadata: {},
      }), {
        status: 200,
        headers: { "content-type": "application/json" },
      }),
    );

    await client.runCalculation({
      scenarioId: "scenario-10",
      projectId: 10,
      buildingId: 100,
      scenarioKind: "FullEngineeringCore",
      executionMode: "ExecuteAvailableModules",
      state,
      requestedModules: ["ThermalTopology"],
      detailLevel: "Summary",
      includeTrace: false,
      includeReport: false,
      reportFormats: ["Json"],
      diagnosticsMode: "Deterministic",
    });

    const [url, init] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain(apiRoutes.engineeringWorkflow.runCalculation());
    expect(init?.method).toBe("POST");
    expect((init?.headers as Record<string, string>)["Idempotency-Key"]).toContain("wf-run-10-scenario-10");
    expect(JSON.parse(String(init?.body)).scenarioId).toBe("scenario-10");
  });

  it("runCalculation returns deterministic failed execution fallback on request failure", async () => {
    const client = createEngineeringWorkflowClient("api");
    vi.spyOn(globalThis, "fetch").mockRejectedValueOnce(new Error("runner unavailable"));
    const state = createState();

    const response = await client.runCalculation({
      scenarioId: "scenario-10",
      projectId: 10,
      buildingId: 100,
      scenarioKind: "FullEngineeringCore",
      executionMode: "ExecuteAvailableModules",
      state,
      requestedModules: ["ThermalTopology"],
      detailLevel: "Summary",
      includeTrace: false,
      includeReport: false,
      reportFormats: ["Json"],
      diagnosticsMode: "Deterministic",
    });

    expect(response.status).toBe("FailedExecution");
    expect(response.executed).toBe(false);
    expect(response.validationDiagnostics[0].code).toBe("WORKFLOW_API_ERROR");
  });

  it("getTracePreview returns deterministic fallback when request fails", async () => {
    const client = createEngineeringWorkflowClient("api");
    vi.spyOn(globalThis, "fetch").mockRejectedValueOnce(new Error("trace endpoint unavailable"));
    const state = createState();

    const trace = await client.getTracePreview(state, "Detailed");

    expect(trace.traceId).toBe("workflow-trace-preview-failed");
    expect(trace.detailLevel).toBe("Detailed");
    expect(trace.modules).toEqual(["Validation"]);
  });

  it("keeps backward-compatible exports and step labels", () => {
    const client = createEngineeringWorkflowClient("dev");

    expect(client.mode).toBe("dev");
    expect(typeof client.getWorkflowState).toBe("function");
    expect(typeof client.generateReport).toBe("function");
    expect(describeWorkflowStepStatus("ready")).toBe("Ready");
    expect(describeWorkflowStepStatus("warnings")).toBe("Warnings");
    expect(summarizeBuildingMetrics(createState())).toContain("1 zone(s), 1 boundaries");
  });
});
