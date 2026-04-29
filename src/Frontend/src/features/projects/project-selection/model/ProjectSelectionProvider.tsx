import { createContext, PropsWithChildren, useContext, useMemo, useState } from "react";

interface ProjectSelectionContextValue {
  selectedProjectId: number | null;
  setSelectedProjectId: (projectId: number | null) => void;
}

const storageKey = "assistant-engineer.selectedProjectId";

const ProjectSelectionContext = createContext<ProjectSelectionContextValue | null>(null);

function readInitialProjectId(): number | null {
  const raw = window.localStorage.getItem(storageKey);
  const parsed = Number(raw);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
}

export function ProjectSelectionProvider({ children }: PropsWithChildren): JSX.Element {
  const [selectedProjectId, setSelectedProjectIdState] = useState<number | null>(readInitialProjectId);

  const value = useMemo<ProjectSelectionContextValue>(
    () => ({
      selectedProjectId,
      setSelectedProjectId: (projectId) => {
        if (projectId && projectId > 0) {
          window.localStorage.setItem(storageKey, String(projectId));
          setSelectedProjectIdState(projectId);
          return;
        }

        window.localStorage.removeItem(storageKey);
        setSelectedProjectIdState(null);
      },
    }),
    [selectedProjectId],
  );

  return (
    <ProjectSelectionContext.Provider value={value}>{children}</ProjectSelectionContext.Provider>
  );
}

export function useProjectSelection(): ProjectSelectionContextValue {
  const context = useContext(ProjectSelectionContext);
  if (!context) {
    throw new Error("useProjectSelection must be used inside ProjectSelectionProvider");
  }

  return context;
}
