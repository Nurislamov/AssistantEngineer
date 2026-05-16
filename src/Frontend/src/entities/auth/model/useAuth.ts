import { useContext } from "react";
import { AuthContext } from "./AuthContext";
import { canAccess, hasPermission } from "./authViewModel";
import type { AuthRequirement, AuthStateViewModel } from "./authTypes";

export function useAuth() {
  const context = useContext(AuthContext);

  return {
    authState: context.authState,
    setAuthState: context.setAuthState,
    hasPermission: (permission: string): boolean =>
      hasPermission(context.authState, permission),
    canAccess: (requirement: AuthRequirement): boolean =>
      canAccess(context.authState, requirement),
  };
}

export function useAuthState(): AuthStateViewModel {
  return useContext(AuthContext).authState;
}
