export interface AuthConfig {
  authEnabled: boolean;
  developmentMode: boolean;
  loginRoute: string;
  defaultRedirectAfterLogin: string;
  apiKeyHeaderName: string;
  externalProviderEnabled: boolean;
}

export const defaultAuthConfig: AuthConfig = {
  authEnabled: false,
  developmentMode: true,
  loginRoute: "/login",
  defaultRedirectAfterLogin: "/",
  apiKeyHeaderName: "X-AssistantEngineer-Api-Key",
  externalProviderEnabled: false,
};
