import { renderHook } from "@testing-library/react";
import type { PropsWithChildren } from "react";
import { AuthProvider } from "./AuthContext";
import { createAuthenticatedAuthState } from "./authViewModel";
import { useAuth } from "./useAuth";

describe("AuthContext", () => {
  it("provides default anonymous state", () => {
    const wrapper = ({ children }: PropsWithChildren) => (
      <AuthProvider>{children}</AuthProvider>
    );

    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.authState.status).toBe("anonymous");
    expect(result.current.authState.principal.isAuthenticated).toBe(false);
  });

  it("accepts initial authenticated state", () => {
    const initialState = createAuthenticatedAuthState({
      userId: 10,
      displayName: "Jane",
      roles: ["Engineer"],
      permissions: ["WorkflowsRead"],
      isAuthenticated: true,
    });

    const wrapper = ({ children }: PropsWithChildren) => (
      <AuthProvider initialState={initialState}>{children}</AuthProvider>
    );

    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.authState.status).toBe("authenticated");
    expect(result.current.authState.principal.displayName).toBe("Jane");
  });

  it("useAuth hasPermission returns expected value", () => {
    const initialState = createAuthenticatedAuthState({
      roles: ["Engineer"],
      permissions: ["ReportsRead"],
      isAuthenticated: true,
    });

    const wrapper = ({ children }: PropsWithChildren) => (
      <AuthProvider initialState={initialState}>{children}</AuthProvider>
    );

    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.hasPermission("ReportsRead")).toBe(true);
    expect(result.current.hasPermission("ReportsWrite")).toBe(false);
  });
});
