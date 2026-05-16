import { render, screen } from "@testing-library/react";
import { AuthProvider } from "../model/AuthContext";
import { createAuthenticatedAuthState, createAnonymousAuthState } from "../model/authViewModel";
import { AuthStatusBanner } from "./AuthStatusBanner";

describe("AuthStatusBanner", () => {
  it("renders development anonymous mode", () => {
    render(
      <AuthProvider initialState={createAnonymousAuthState(true)}>
        <AuthStatusBanner />
      </AuthProvider>,
    );

    expect(screen.getByText(/Development anonymous mode is active/i)).toBeInTheDocument();
  });

  it("renders authenticated user", () => {
    render(
      <AuthProvider
        initialState={createAuthenticatedAuthState({
          displayName: "Engineer User",
          roles: ["Engineer"],
          permissions: ["WorkflowsRead"],
          isAuthenticated: true,
        })}
      >
        <AuthStatusBanner />
      </AuthProvider>,
    );

    expect(screen.getByText("Authenticated")).toBeInTheDocument();
    expect(screen.getByText("Engineer User")).toBeInTheDocument();
  });

  it("renders unauthorized status", () => {
    const unauthorizedState = {
      ...createAnonymousAuthState(false),
      authRequired: true,
      status: "unauthorized" as const,
      message: "Unauthorized test message.",
    };

    render(
      <AuthProvider initialState={unauthorizedState}>
        <AuthStatusBanner />
      </AuthProvider>,
    );

    expect(screen.getByText("Unauthorized")).toBeInTheDocument();
  });

  it("renders forbidden status", () => {
    const forbiddenState = {
      ...createAuthenticatedAuthState({
        displayName: "Restricted User",
        roles: ["Viewer"],
        permissions: ["ProjectsRead"],
        isAuthenticated: true,
      }),
      status: "forbidden" as const,
      message: "Forbidden test message.",
    };

    render(
      <AuthProvider initialState={forbiddenState}>
        <AuthStatusBanner />
      </AuthProvider>,
    );

    expect(screen.getByText("Forbidden")).toBeInTheDocument();
  });
});
