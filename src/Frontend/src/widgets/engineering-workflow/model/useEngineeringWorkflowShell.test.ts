import { act, renderHook, waitFor } from "@testing-library/react";
import { useEngineeringWorkflowShell } from "./useEngineeringWorkflowShell";
import type { ProjectWorkflowState } from "@/entities/engineering-workflow/types";

const useEngineeringWorkflowMock = vi.fn();

vi.mock("@/entities/engineering-workflow/model/useEngineeringWorkflow", () => ({
  useEngineeringWorkflow: (...args: unknown[]) => useEngineeringWorkflowMock(...args),
}));

function createState(): ProjectWorkflowState {
  return {
    projectId: 42,
    projectName: "Workflow Hook Project",
    buildingId: 420,
    currentStep: "Review",
    completionByStep: [{ step: "Review", status: "ready" }],
    availableModules: ["SystemEnergy"],
    buildingMetadata: {
      projectName: "Workflow Hook Project",
      buildingName: "Building",
      locationText: "Tashkent",
      floorAreaM2: 100,
      volumeM3: 250,
      numberOfZones: 1,
    },
    zones: [],
    boundaries: [],
    weatherSolarSettings: {
      weatherSourceStatus: "Ready",
      locationTimezoneSummary: "UTC+5",
      solarChainReadinessSummary: "Ready",
    },
    ventilationSettings: {
      openingCount: 0,
      controlModeSummary: "n/a",
      airflowSummary: "n/a",
      warnings: [],
    },
    groundSettings: {
      groundBoundaryCount: 0,
      groundProfileMode: "n/a",
      summaryStatus: "incomplete",
    },
    domesticHotWaterSettings: {
      demandBasis: "PerPerson",
      usefulDemandSummary: "n/a",
      lossesSummary: "n/a",
      ownershipPolicy: "NoDoubleCounting",
    },
    systemEnergySettings: {
      usesSummary: "n/a",
      carriersSummary: "n/a",
      finalPrimaryCarbonSummary: "n/a",
    },
    validationDiagnostics: [],
    assumptions: [],
    links: [],
    workflowMode: "api",
    workflowModeLabel: "API workflow mode",
    metadata: {},
  };
}

function createWorkflowMock() {
  const state = createState();
  return {
    mode: "api" as const,
    state,
    isLoading: false,
    error: null as Error | null,
    refresh: vi.fn().mockResolvedValue(undefined),
    validate: vi.fn().mockResolvedValue([]),
    prepareCalculation: vi.fn().mockResolvedValue({
      requestId: "req-1",
      status: "prepared",
      diagnostics: [],
      metadata: {},
    }),
    runCalculation: vi.fn(),
    createCalculationJob: vi.fn().mockResolvedValue({
      jobId: "job-1",
      projectId: state.projectId,
      scenarioId: "scenario-1",
      status: "CompletedWithWarnings",
      progressPercent: 100,
      currentStep: "Completed",
      queuedAtUtc: "2026-05-11T00:00:00Z",
      diagnostics: [],
      assumptions: [],
      warnings: [],
      persistedArtifactReferences: [],
      historyEvents: [],
      metadata: {},
      scenarioResultSummary: {
        scenarioId: "scenario-1",
        status: "CompletedWithWarnings",
        executed: true,
        executedModules: ["SystemEnergy"],
        skippedModules: [],
        unavailableModules: [],
        validationDiagnostics: [],
        assumptions: [],
        warnings: [],
        moduleSummaries: {},
        moduleResults: [],
        timings: [],
        metadata: {},
      },
    }),
    getCalculationJob: vi.fn().mockResolvedValue(null),
    listProjectJobs: vi.fn().mockResolvedValue([]),
    getCalculationJobEvents: vi.fn().mockResolvedValue([]),
    cancelCalculationJob: vi.fn().mockResolvedValue(null),
    listScenarios: vi.fn().mockResolvedValue([]),
    getScenarioResult: vi.fn().mockResolvedValue(null),
    getScenarioArtifacts: vi.fn().mockResolvedValue([]),
    getScenarioArtifact: vi.fn().mockResolvedValue(null),
    loadTracePreview: vi.fn().mockResolvedValue(undefined),
    generateReport: vi.fn(),
    exportReportJson: vi.fn(),
    exportReportMarkdown: vi.fn(),
  };
}

describe("useEngineeringWorkflowShell", () => {
  afterEach(() => {
    useEngineeringWorkflowMock.mockReset();
  });

  it("loads initial workflow state and history deterministically", async () => {
    const workflow = createWorkflowMock();
    useEngineeringWorkflowMock.mockReturnValue(workflow);

    const { result } = renderHook(() => useEngineeringWorkflowShell(42, 420));

    await waitFor(() => {
      expect(workflow.listScenarios).toHaveBeenCalled();
      expect(workflow.listProjectJobs).toHaveBeenCalled();
    });

    expect(result.current.selectedStep).toBe("Review");
    expect(result.current.stepStatus.get("Review")).toBe("ready");
  });

  it("runAvailableModules calls workflow job endpoint and updates current job/result", async () => {
    const workflow = createWorkflowMock();
    useEngineeringWorkflowMock.mockReturnValue(workflow);

    const { result } = renderHook(() => useEngineeringWorkflowShell(42, 420));

    await act(async () => {
      await result.current.runAvailableModules();
    });

    expect(workflow.createCalculationJob).toHaveBeenCalledWith("Synchronous");
    expect(result.current.currentJob?.jobId).toBe("job-1");
    expect(result.current.scenarioResult?.scenarioId).toBe("scenario-1");
  });

  it("keeps underlying workflow error visible through hook facade", async () => {
    const workflow = createWorkflowMock();
    workflow.error = new Error("workflow failed");
    useEngineeringWorkflowMock.mockReturnValue(workflow);

    const { result } = renderHook(() => useEngineeringWorkflowShell(42, 420));
    await waitFor(() => {
      expect(workflow.listScenarios).toHaveBeenCalled();
      expect(workflow.listProjectJobs).toHaveBeenCalled();
    });
    expect(result.current.workflow.error).toBeInstanceOf(Error);
  });
});
