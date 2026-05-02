import { createTheme } from "@mui/material/styles";

export const appTheme = createTheme({
  palette: {
    mode: "light",
    primary: {
      main: "#1f5f8b",
      dark: "#174a6d",
      light: "#4f86ad",
    },
    secondary: {
      main: "#547064",
    },
    background: {
      default: "#f4f6f8",
      paper: "#ffffff",
    },
  },
  shape: {
    borderRadius: 8,
  },
  typography: {
    fontFamily: "Inter, Roboto, Arial, sans-serif",
    h4: {
      fontSize: "1.75rem",
      letterSpacing: 0,
    },
    h5: {
      fontSize: "1.25rem",
      letterSpacing: 0,
    },
    button: {
      textTransform: "none",
      letterSpacing: 0,
    },
  },
  components: {
    MuiButton: {
      defaultProps: {
        size: "medium",
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundImage: "none",
        },
      },
    },
  },
});
