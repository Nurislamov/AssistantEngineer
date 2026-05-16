import {
  canAccess,
  createAnonymousAuthState,
  createAuthenticatedAuthState,
  getSecurityNonClaims,
  hasPermission,
} from "./authViewModel";

describe("authViewModel", () => {
  it("anonymous state is not authenticated", () => {
    const state = createAnonymousAuthState(true);

    expect(state.status).toBe("anonymous");
    expect(state.principal.isAuthenticated).toBe(false);
  });

  it("authenticated state keeps permissions", () => {
    const state = createAuthenticatedAuthState({
      userId: 12,
      organizationId: 7,
      displayName: "Engineer",
      roles: ["Engineer"],
      permissions: ["WorkflowsRead"],
      isAuthenticated: true,
    });

    expect(state.status).toBe("authenticated");
    expect(state.principal.permissions).toContain("WorkflowsRead");
  });

  it("hasPermission returns true and false correctly", () => {
    const state = createAuthenticatedAuthState({
      roles: [],
      permissions: ["ReportsRead"],
      isAuthenticated: true,
    });

    expect(hasPermission(state, "ReportsRead")).toBe(true);
    expect(hasPermission(state, "ReportsWrite")).toBe(false);
  });

  it("canAccess allows anonymous when requirement allows anonymous", () => {
    const state = createAnonymousAuthState(true);
    state.authRequired = true;

    expect(canAccess(state, { allowAnonymous: true })).toBe(true);
  });

  it("canAccess denies missing permission when auth required", () => {
    const state = createAuthenticatedAuthState({
      roles: [],
      permissions: ["ProjectsRead"],
      isAuthenticated: true,
    });

    expect(
      canAccess(state, {
        permission: "ProjectsWrite",
      }),
    ).toBe(false);
  });

  it("non-claims include required phrases", () => {
    const nonClaims = getSecurityNonClaims();

    expect(nonClaims.some((item) => item.includes("No production security certification claim."))).toBe(true);
    expect(nonClaims.some((item) => item.includes("No SOC 2 / ISO 27001 compliance claim."))).toBe(true);
    expect(nonClaims.some((item) => item.includes("No full multi-tenant isolation claim yet."))).toBe(true);
    expect(nonClaims.some((item) => item.includes("No external identity provider integration claim."))).toBe(true);
    expect(nonClaims.some((item) => item.includes("No certified/certification claim."))).toBe(true);
  });
});
