export type AuthStatus =
  | "unknown"
  | "anonymous"
  | "authenticated"
  | "unauthorized"
  | "forbidden";

export interface AuthPrincipalViewModel {
  userId?: number;
  organizationId?: number;
  externalSubjectId?: string;
  displayName?: string;
  email?: string;
  roles: string[];
  permissions: string[];
  isAuthenticated: boolean;
}

export interface OrganizationContextViewModel {
  organizationId?: number;
  organizationSlug?: string;
  organizationName?: string;
  isActive?: boolean;
}

export interface AuthStateViewModel {
  status: AuthStatus;
  principal: AuthPrincipalViewModel;
  organization?: OrganizationContextViewModel;
  developmentMode: boolean;
  authRequired: boolean;
  message?: string;
}

export interface AuthRequirement {
  permission?: string;
  resourceType?: string;
  allowAnonymous?: boolean;
}
