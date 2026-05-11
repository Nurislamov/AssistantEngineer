import { expect, test, type Page, type Route } from "@playwright/test";

const projectId = 1;
const buildingId = 101;
const scenarioId = "scenario-e2e-001";
const jobId = "job-e2e-001";

test.describe("Engineering workflow smoke", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      window.localStorage.clear();
    });
    await mockWorkflowApi(page);
  });

  test("app boots and renders engineering workflow shell", async ({ page }) => {
    await page.goto("/engineering-workflow");

    await expect(page.getByRole("heading", { name: "Engineering workflow" })).toBeVisible();
    await expect(page.getByText("Workflow steps")).toBeVisible();
    await expect(page.getByRole("heading", { name: "Validation diagnostics" })).toBeVisible();
    await expect(page.getByText("Persistence provider:", { exact: false })).toBeVisible();
  });

  test("workflow run smoke uses mocked job endpoint and shows result status", async ({ page }) => {
    let runCallCount = 0;
    await page.route("**/api/v1/engineering-workflow/jobs", async (route) => {
      if (route.request().method() !== "POST") {
        await route.fallback();
        return;
      }

      runCallCount += 1;
      const idempotency = route.request().headers()["idempotency-key"];
      await expect(idempotency).toBeTruthy();

      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(buildJobResponse()),
      });
    });

    await page.goto("/engineering-workflow");
    await expect(page.getByRole("button", { name: "Run available modules" })).toBeVisible();

    await page.getByRole("button", { name: "Run available modules" }).click();

    await expect(page.getByText(`Scenario ID: ${scenarioId}.`)).toBeVisible();
    await expect(page.getByText(`Job ID: ${jobId}. Status: CompletedWithWarnings.`)).toBeVisible();
    await expect.poll(() => runCallCount).toBe(1);
  });
});

async function mockWorkflowApi(page: Page): Promise<void> {
  await page.route("**/api/v1/projects**", async (route) => {
    if (route.request().method() !== "GET") {
      await route.fallback();
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(
        paged([
          {
            id: projectId,
            name: "E2E Project",
          },
        ]),
      ),
    });
  });

  await page.route(`**/api/v1/projects/${projectId}/buildings**`, async (route) => {
    if (route.request().method() !== "GET") {
      await route.fallback();
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(
        paged([
          {
            id: buildingId,
            projectId,
            name: "E2E Building",
            climateZoneId: 3,
            climateZoneName: "Zone 3",
          },
        ]),
      ),
    });
  });

  await page.route(`**/api/v1/engineering-workflow/${projectId}/state**`, async (route) => {
    if (route.request().method() !== "GET") {
      await route.fallback();
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(buildWorkflowStateResponse()),
    });
  });

  await page.route(`**/api/v1/engineering-workflow/${projectId}/scenarios**`, async (route) => {
    if (route.request().method() !== "GET") {
      await route.fallback();
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(paged([])),
    });
  });

  await page.route(`**/api/v1/engineering-workflow/${projectId}/jobs**`, async (route) => {
    if (route.request().method() !== "GET") {
      await route.fallback();
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(paged([])),
    });
  });

  await page.route("**/api/v1/engineering-workflow/**", async (route: Route) => {
    await route.fallback();
  });
}

function buildWorkflowStateResponse() {
  return {
    projectId,
    projectName: "E2E Project",
    buildingId,
    currentStep: "Project",
    steps: [
      { kind: "Project", status: "valid", isComplete: true },
      { kind: "Building", status: "valid", isComplete: true },
      { kind: "Zones", status: "warnings", isComplete: false },
      { kind: "Envelope", status: "warnings", isComplete: false },
      { kind: "WeatherSolar", status: "valid", isComplete: true },
      { kind: "Ventilation", status: "warnings", isComplete: false },
      { kind: "Ground", status: "incomplete", isComplete: false },
      { kind: "DomesticHotWater", status: "valid", isComplete: true },
      { kind: "SystemEnergy", status: "valid", isComplete: true },
      { kind: "Validation", status: "warnings", isComplete: false },
      { kind: "CalculationTrace", status: "valid", isComplete: true },
      { kind: "Reports", status: "ready", isComplete: true },
      { kind: "Review", status: "warnings", isComplete: false },
    ],
    availableModules: [
      "Weather",
      "Solar",
      "ThermalTopology",
      "Iso52016",
      "Ventilation",
      "Ground",
      "DomesticHotWater",
      "SystemEnergy",
      "Validation",
      "Reporting",
    ],
    buildingMetadata: {
      projectName: "E2E Project",
      buildingName: "E2E Building",
      locationText: "Tashkent",
      floorAreaM2: 120.5,
      volumeM3: 320.0,
      numberOfZones: 1,
    },
    zones: [
      {
        id: 1,
        name: "Zone 1",
        zoneKind: "Conditioned",
        floorAreaM2: 120.5,
        airVolumeM3: 320,
        status: "valid",
      },
    ],
    boundaries: [
      {
        id: 1,
        zoneOrRoomName: "Zone 1",
        exposureKind: "Exterior",
        areaM2: 40,
        uValue: 0.35,
        adjacentZoneReference: null,
        indicator: "exterior",
        validationStatus: "valid",
      },
    ],
    weatherSolarSettings: {
      weatherSourceStatus: "Loaded",
      locationTimezoneSummary: "UTC+5",
      solarChainReadinessSummary: "Ready",
    },
    ventilationSettings: {
      openingCount: 2,
      controlModeSummary: "WindowControl",
      airflowSummary: "Hve approx 0.45 ACH",
      warnings: [],
    },
    groundSettings: {
      groundBoundaryCount: 1,
      groundProfileMode: "AnnualMean",
      summaryStatus: "valid",
    },
    domesticHotWaterSettings: {
      demandBasis: "ResidentialPerPerson",
      usefulDemandSummary: "1.2 MWh/year",
      lossesSummary: "0.4 MWh/year",
      ownershipPolicy: "NoDoubleCounting",
    },
    systemEnergySettings: {
      usesSummary: "Heating, Cooling, DHW",
      carriersSummary: "Electricity, Gas",
      finalPrimaryCarbonSummary: "Final 6.4 MWh, Primary 8.1 MWh, CO2 1.6 t",
    },
    diagnostics: [
      {
        severity: "warning",
        code: "VENT_CONTROL_ASSUMPTION",
        message: "Ventilation control mode uses default lockout rule.",
        sourceStep: "Ventilation",
      },
    ],
    assumptions: ["Internal engineering foundation snapshot"],
    links: [],
    calculationTraceSummary: {
      traceId: "trace-e2e-001",
      calculationId: "calc-e2e-001",
      detailLevel: "Standard",
      modules: ["Weather", "Solar", "Iso52016", "SystemEnergy"],
      assumptions: ["Solar chain uses internal engineering defaults."],
      warnings: [],
      steps: [
        {
          stepId: "weather",
          moduleKind: "Weather",
          stepName: "Weather readiness",
          sequence: 1,
          assumptions: [],
          warnings: [],
          diagnosticsCount: 0,
        },
      ],
    },
    reportSummary: {
      reportKind: "FullEngineeringCore",
      title: "Engineering Report Preview",
      sections: ["ExecutiveSummary", "ValidationDiagnostics", "CalculationTraceAppendix"],
      warningsCount: 1,
      diagnosticsCount: 1,
      exportFormatsAvailable: ["Json", "Markdown"],
      generatedTimestamp: "2026-05-11T00:00:00Z",
      limitations: ["Foundation-level report preview."],
    },
    metadata: {
      persistenceProvider: "InMemory",
      durablePersistenceEnabled: "false",
    },
  };
}

function buildJobResponse() {
  return {
    jobId,
    projectId,
    scenarioId,
    status: "CompletedWithWarnings",
    progressPercent: 100,
    currentStep: "Completed",
    queuedAtUtc: "2026-05-11T00:00:00Z",
    startedAtUtc: "2026-05-11T00:00:01Z",
    completedAtUtc: "2026-05-11T00:00:03Z",
    durationMilliseconds: 2000,
    diagnostics: [],
    assumptions: ["Smoke scenario used mocked API response."],
    warnings: ["Weather fallback was not required."],
    persistedArtifactReferences: [
      {
        artifactId: "artifact-scenario-json",
        scenarioId,
        artifactKind: "ScenarioResultJson",
        contentType: "application/json",
        content: "{\"status\":\"CompletedWithWarnings\"}",
        createdAtUtc: "2026-05-11T00:00:03Z",
        sizeBytes: 37,
        checksumSha256: null,
      },
    ],
    historyEvents: [
      {
        eventId: "job-event-001",
        jobId,
        scenarioId,
        status: "CompletedWithWarnings",
        message: "Calculation finished.",
        moduleKind: null,
        progressPercent: 100,
        diagnostics: [],
        createdAtUtc: "2026-05-11T00:00:03Z",
      },
    ],
    metadata: {
      mode: "Synchronous",
    },
    scenarioResultSummary: {
      scenarioId,
      status: "CompletedWithWarnings",
      executed: true,
      executedModules: ["Validation", "DomesticHotWater", "SystemEnergy"],
      skippedModules: ["Iso52016"],
      unavailableModules: [],
      validationDiagnostics: [],
      assumptions: ["Smoke scenario uses deterministic mocked result."],
      warnings: ["Heating/cooling module intentionally skipped in smoke fixture."],
      moduleSummaries: {
        domesticHotWaterSummary: "1.6 MWh/year system load",
        systemEnergySummary: "Final 6.4 MWh, Primary 8.1 MWh, CO2 1.6 t",
      },
      moduleResults: [
        {
          moduleKind: "DomesticHotWater",
          status: "Executed",
          summaryValues: [],
          diagnostics: [],
          assumptions: [],
          warnings: [],
          durationMilliseconds: 80,
          sourceServiceName: "MockDhwService",
        },
      ],
      timings: [
        {
          moduleKind: "DomesticHotWater",
          durationMilliseconds: 80,
        },
      ],
      calculationTraceSummary: {
        traceId: "trace-e2e-run-001",
        calculationId: "calc-e2e-run-001",
        detailLevel: "Standard",
        modules: ["Validation", "DomesticHotWater", "SystemEnergy"],
        assumptions: [],
        warnings: [],
        steps: [],
      },
      reportPreview: {
        reportKind: "FullEngineeringCore",
        title: "Engineering Report Preview",
        sections: ["ExecutiveSummary", "DomesticHotWater", "SystemEnergy"],
        warningsCount: 1,
        diagnosticsCount: 0,
        exportFormatsAvailable: ["Json", "Markdown"],
        generatedTimestamp: "2026-05-11T00:00:03Z",
        limitations: ["Smoke response with mocked backend route."],
      },
      reportJson: "{\n  \"report\": \"mock\"\n}",
      reportMarkdown: "# Mock report\n",
      metadata: {
        mode: "Synchronous",
      },
    },
  };
}

function paged<T>(items: T[]) {
  return {
    items,
    page: 1,
    pageSize: 50,
    totalCount: items.length,
    totalPages: 1,
    sortBy: "id",
    sortDescending: false,
  };
}
