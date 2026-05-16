import { defaultAuthConfig } from "./authConfig";
import type {
  AuthPrincipalViewModel,
  AuthRequirement,
  AuthStateViewModel,
  AuthStatus,
  OrganizationContextViewModel,
} from "./authTypes";

const anonymousPrincipal: AuthPrincipalViewModel = {
  roles: [],
  permissions: [],
  isAuthenticated: false,
};

export function createAnonymousAuthState(
  developmentMode = defaultAuthConfig.developmentMode,
): AuthStateViewModel {
  return {
    status: "anonymous",
    principal: anonymousPrincipal,
    developmentMode,
    authRequired: defaultAuthConfig.authEnabled,
    message: developmentMode
      ? "Development anonymous mode is enabled."
      : "Anonymous principal.",
  };
}

export function createAuthenticatedAuthState(
  principal: AuthPrincipalViewModel,
  organization?: OrganizationContextViewModel,
): AuthStateViewModel {
  return {
    status: "authenticated",
    principal: {
      ...principal,
      isAuthenticated: true,
      roles: principal.roles ?? [],
      permissions: principal.permissions ?? [],
    },
    organization,
    developmentMode: false,
    authRequired: true,
  };
}

export function hasPermission(
  authState: AuthStateViewModel,
  permission: string,
): boolean {
  if (!permission.trim()) {
    return true;
  }

  return authState.principal.permissions.some(
    (item) => item.toLowerCase() === permission.toLowerCase(),
  );
}

export function canAccess(
  authState: AuthStateViewModel,
  requirement: AuthRequirement,
): boolean {
  if (requirement.allowAnonymous) {
    return true;
  }

  if (!authState.authRequired) {
    return true;
  }

  if (!authState.principal.isAuthenticated) {
    return false;
  }

  if (!requirement.permission) {
    return true;
  }

  return hasPermission(authState, requirement.permission);
}

export function getAuthStatusLabel(status: AuthStatus): string {
  switch (status) {
    case "authenticated":
      return "Authenticated";
    case "unauthorized":
      return "Unauthorized";
    case "forbidden":
      return "Forbidden";
    case "unknown":
      return "Unknown";
    default:
      return "Anonymous";
  }
}

export function getAuthStatusDescription(authState: AuthStateViewModel): string {
  if (authState.status === "authenticated") {
    return "Authenticated principal context is available for future route protection and tenant-aware UX.";
  }

  if (authState.status === "unauthorized") {
    return buildUnauthorizedMessage();
  }

  if (authState.status === "forbidden") {
    return buildForbiddenMessage();
  }

  if (authState.status === "unknown") {
    return "Authentication status is unknown. Auth shell foundation is present but provider integration is pending.";
  }

  if (authState.developmentMode) {
    return "Development anonymous mode is active. Auth foundation is enabled for staged rollout.";
  }

  return "Anonymous principal context.";
}

export function buildForbiddenMessage(permission?: string): string {
  if (!permission) {
    return "Forbidden: principal does not satisfy current permission requirements.";
  }

  return `Forbidden: missing required permission ${permission}.`;
}

export function buildUnauthorizedMessage(): string {
  return "Unauthorized: sign-in flow is not connected in this foundation step.";
}

export function getSecurityNonClaims(): string[] {
  return [
    "No production security certification claim.",
    "No SOC 2 / ISO 27001 compliance claim.",
    "No full multi-tenant isolation claim yet.",
    "No external identity provider integration claim.",
    "No certified/certification claim.",
  ];
}
