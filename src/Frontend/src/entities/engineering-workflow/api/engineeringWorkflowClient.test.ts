import { createEngineeringWorkflowClient } from "./engineeringWorkflowClient";
import type {
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
});
