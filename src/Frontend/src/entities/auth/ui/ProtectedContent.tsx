import { Stack } from "@mui/material";
import type { PropsWithChildren } from "react";
import type { AuthRequirement } from "../model/authTypes";
import { useAuth } from "../model/useAuth";
import { ForbiddenState } from "./ForbiddenState";
import { UnauthorizedState } from "./UnauthorizedState";
import { AuthStatusBanner } from "./AuthStatusBanner";

interface ProtectedContentProps extends PropsWithChildren {
  requirement: AuthRequirement;
  showCompatibilityBanner?: boolean;
}

export function ProtectedContent({
  requirement,
  children,
  showCompatibilityBanner = false,
}: ProtectedContentProps): JSX.Element {
  const { authState, canAccess } = useAuth();
  const accessAllowed = canAccess(requirement);

  if (!authState.authRequired) {
    return (
      <Stack spacing={1}>
        {showCompatibilityBanner ? <AuthStatusBanner /> : null}
        {children}
      </Stack>
    );
  }

  if (requirement.allowAnonymous) {
    return <>{children}</>;
  }

  if (!authState.principal.isAuthenticated || authState.status === "unauthorized") {
    return <UnauthorizedState message={authState.message} />;
  }

  if (!accessAllowed || authState.status === "forbidden") {
    return <ForbiddenState requiredPermission={requirement.permission} />;
  }

  return <>{children}</>;
}
