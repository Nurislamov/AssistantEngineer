import { fireEvent, render, screen } from "@testing-library/react";
import { EngineeringWorkflowShell } from "./EngineeringWorkflowShell";

const useEngineeringWorkflowShellMock = vi.fn();

vi.mock("../model/useEngineeringWorkflowShell", () => ({
  useEngineeringWorkflowShell: (...args: unknown[]) => useEngineeringWorkflowShellMock(...args),
}));

function createVm(overrides: Record<string, unknown> = {}) {
  return {
    workflow: {
      mode: "api",
      state: {
        projectId: 1,
        projectName: "Project",
        buildingId: 101,
        currentStep: "Project",
        completionByStep: [{ step: "Project", status: "valid" }],
        availableModules: ["SystemEnergy"],
        buildingMetadata: {
          projectName: "Project",
          buildingName: "Building",
          locationText: "Tashkent",
          floorAreaM2: 100,
          volumeM3: 200,
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
        metadata: { persistenceProvider: "InMemory", durablePersistenceEnabled: "false" },
      },
      isLoading: false,
      error: null,
      refresh: vi.fn().mockResolvedValue(undefined),
    },
    selectedStep: "Project",
    setSelectedStep: vi.fn(),
    traceSummary: undefined,
    reportPreview: undefined,
    reportDiagnostics: [],
    jsonOutput: "",
    markdownOutput: "",
    preparation: null,
    scenarioResult: null,
    scenarioHistory: [],
    selectedScenarioId: undefined,
    scenarioArtifacts: [],
    jobHistory: [],
    currentJob: null,
    jobEvents: [],
    stepStatus: new Map([["Project", "valid"]]),
    allDiagnostics: [],
    prepareRequest: vi.fn().mockResolvedValue(undefined),
    runAvailableModules: vi.fn().mockResolvedValue(undefined),
    generateReport: vi.fn().mockResolvedValue(undefined),
    exportJson: vi.fn().mockResolvedValue(undefined),
    exportMarkdown: vi.fn().mockResolvedValue(undefined),
    refreshCurrentJob: vi.fn().mockResolvedValue(undefined),
    refreshJobEvents: vi.fn().mockResolvedValue(undefined),
    cancelJob: vi.fn().mockResolvedValue(undefined),
    selectJob: vi.fn().mockResolvedValue(undefined),
    loadScenarioResult: vi.fn().mockResolvedValue(undefined),
    loadScenarioArtifacts: vi.fn().mockResolvedValue(undefined),
    openScenarioArtifact: vi.fn().mockResolvedValue(undefined),
    onTraceDetailLevelChange: vi.fn().mockResolvedValue(undefined),
    ...overrides,
  };
}

describe("EngineeringWorkflowShell", () => {
  afterEach(() => {
    useEngineeringWorkflowShellMock.mockReset();
  });

  it("renders loading state when workflow is loading", () => {
    useEngineeringWorkflowShellMock.mockReturnValue(
      createVm({
        workflow: {
          ...createVm().workflow,
          isLoading: true,
          state: undefined,
        },
      }),
    );

    render(<EngineeringWorkflowShell projectId={1} buildingId={101} />);
    expect(screen.getByRole("progressbar")).toBeInTheDocument();
  });

  it("renders error state when workflow query fails", () => {
    useEngineeringWorkflowShellMock.mockReturnValue(
      createVm({
        workflow: {
          ...createVm().workflow,
          state: undefined,
          error: new Error("failed to load workflow"),
        },
      }),
    );

    render(<EngineeringWorkflowShell projectId={1} buildingId={101} />);
    expect(screen.getByText(/failed to load workflow/i)).toBeInTheDocument();
  });

  it("renders workflow shell and triggers run action", () => {
    const vm = createVm();
    useEngineeringWorkflowShellMock.mockReturnValue(vm);

    render(<EngineeringWorkflowShell projectId={1} buildingId={101} />);

    expect(screen.getByText("Workflow steps")).toBeInTheDocument();
    expect(screen.getByText("Validation diagnostics")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Run available modules" }));
    expect(vm.runAvailableModules).toHaveBeenCalled();
  });
});
