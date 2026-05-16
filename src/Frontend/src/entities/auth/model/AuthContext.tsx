import { createContext, useMemo, useState } from "react";
import type { Dispatch, PropsWithChildren, SetStateAction } from "react";
import { createAnonymousAuthState } from "./authViewModel";
import type { AuthStateViewModel } from "./authTypes";

export interface AuthContextValue {
  authState: AuthStateViewModel;
  setAuthState: Dispatch<SetStateAction<AuthStateViewModel>>;
}

const defaultContextValue: AuthContextValue = {
  authState: createAnonymousAuthState(true),
  setAuthState: () => undefined,
};

export const AuthContext = createContext<AuthContextValue>(defaultContextValue);

interface AuthProviderProps extends PropsWithChildren {
  initialState?: AuthStateViewModel;
}

export function AuthProvider({
  children,
  initialState,
}: AuthProviderProps): JSX.Element {
  const [authState, setAuthState] = useState<AuthStateViewModel>(
    initialState ?? createAnonymousAuthState(true),
  );

  const value = useMemo(
    () => ({
      authState,
      setAuthState,
    }),
    [authState],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
