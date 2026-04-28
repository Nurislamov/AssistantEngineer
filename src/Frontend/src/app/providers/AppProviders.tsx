import { CssBaseline, ThemeProvider } from "@mui/material";
import type { PropsWithChildren } from "react";
import { BrowserRouter } from "react-router-dom";
import { QueryProvider } from "./QueryProvider";
import { appTheme } from "./theme";

export function AppProviders({ children }: PropsWithChildren): JSX.Element {
  return (
    <ThemeProvider theme={appTheme}>
      <CssBaseline />
      <QueryProvider>
        <BrowserRouter>{children}</BrowserRouter>
      </QueryProvider>
    </ThemeProvider>
  );
}
