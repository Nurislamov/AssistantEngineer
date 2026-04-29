import { FormControl, InputLabel, MenuItem, Select, Stack, Typography } from "@mui/material";
import { useEffect } from "react";
import { useProjects } from "@/entities/building/model/useBuildingsQueries";
import { useProjectSelection } from "../model/ProjectSelectionProvider";

export function ProjectSelector(): JSX.Element {
  const projectsQuery = useProjects();
  const { selectedProjectId, setSelectedProjectId } = useProjectSelection();
  const projects = projectsQuery.data ?? [];

  useEffect(() => {
    if (!projectsQuery.isSuccess) return;

    if (projects.length === 0) {
      setSelectedProjectId(null);
      return;
    }

    if (!selectedProjectId || !projects.some((project) => project.id === selectedProjectId)) {
      setSelectedProjectId(projects[0].id);
    }
  }, [projects, projectsQuery.isSuccess, selectedProjectId, setSelectedProjectId]);

  return (
    <Stack direction="row" spacing={1.5} alignItems="center" sx={{ minWidth: { xs: 180, sm: 280 } }}>
      <Typography variant="caption" color="text.secondary" sx={{ display: { xs: "none", sm: "block" } }}>
        Project
      </Typography>
      <FormControl size="small" fullWidth disabled={projectsQuery.isLoading || projects.length === 0}>
        <InputLabel id="project-selector-label">Project</InputLabel>
        <Select
          labelId="project-selector-label"
          label="Project"
          value={selectedProjectId ?? ""}
          onChange={(event) => setSelectedProjectId(Number(event.target.value) || null)}
        >
          {projects.map((project) => (
            <MenuItem key={project.id} value={project.id}>
              {project.name}
            </MenuItem>
          ))}
        </Select>
      </FormControl>
    </Stack>
  );
}
