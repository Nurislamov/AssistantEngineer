import { render, screen } from "@testing-library/react";
import { AuthProvider } from "../model/AuthContext";
import { createAnonymousAuthState, createAuthenticatedAuthState } from "../model/authViewModel";
import { ProtectedContent } from "./ProtectedContent";

describe("ProtectedContent", () => {
  it("renders children when auth disabled compatibility allows", () => {
    const state = createAnonymousAuthState(true);
    state.authRequired = false;

    render(
      <AuthProvider initialState={state}>
        <ProtectedContent requirement={{ permission: "WorkflowsRead" }}>
          <div>Protected body</div>
        </ProtectedContent>
      </AuthProvider>,
    );

    expect(screen.getByText("Protected body")).toBeInTheDocument();
  });

  it("renders unauthorized state when auth required and anonymous", () => {
    const state = createAnonymousAuthState(false);
    state.authRequired = true;

    render(
      <AuthProvider initialState={state}>
        <ProtectedContent requirement={{ permission: "WorkflowsRead" }}>
          <div>Protected body</div>
        </ProtectedContent>
      </AuthProvider>,
    );

    expect(screen.getByText(/Anonymous principal\./i)).toBeInTheDocument();
    expect(screen.queryByText("Protected body")).not.toBeInTheDocument();
  });

  it("renders forbidden state when permission is missing", () => {
    const state = createAuthenticatedAuthState({
      displayName: "Viewer",
      roles: ["Viewer"],
      permissions: ["ProjectsRead"],
      isAuthenticated: true,
    });

    render(
      <AuthProvider initialState={state}>
        <ProtectedContent requirement={{ permission: "ProjectsWrite" }}>
          <div>Protected body</div>
        </ProtectedContent>
      </AuthProvider>,
    );

    expect(screen.getByText(/Forbidden:/i)).toBeInTheDocument();
    expect(screen.queryByText("Protected body")).not.toBeInTheDocument();
  });

  it("renders children when permission is present", () => {
    const state = createAuthenticatedAuthState({
      displayName: "Engineer",
      roles: ["Engineer"],
      permissions: ["WorkflowsRead"],
      isAuthenticated: true,
    });

    render(
      <AuthProvider initialState={state}>
        <ProtectedContent requirement={{ permission: "WorkflowsRead" }}>
          <div>Protected body</div>
        </ProtectedContent>
      </AuthProvider>,
    );

    expect(screen.getByText("Protected body")).toBeInTheDocument();
  });
});
