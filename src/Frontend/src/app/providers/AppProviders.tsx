import { CssBaseline, ThemeProvider } from "@mui/material";
import type { PropsWithChildren } from "react";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "@/entities/auth/model/AuthContext";
import { ProjectSelectionProvider } from "@/features/projects/project-selection/model/ProjectSelectionProvider";
import { QueryProvider } from "./QueryProvider";
import { appTheme } from "./theme";

export function AppProviders({ children }: PropsWithChildren): JSX.Element {
  return (
    <ThemeProvider theme={appTheme}>
      <CssBaseline />
      <QueryProvider>
        <ProjectSelectionProvider>
          <AuthProvider>
            <BrowserRouter>{children}</BrowserRouter>
          </AuthProvider>
        </ProjectSelectionProvider>
      </QueryProvider>
    </ThemeProvider>
  );
}
