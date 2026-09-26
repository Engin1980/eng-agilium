import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as projectsApi from "./projects-api";
import type { CreateProjectCommand } from "./projects-api";

const projectsKey = ["projects"] as const;

export function useProjects() {
  return useQuery({
    queryKey: projectsKey,
    queryFn: () => projectsApi.listProjects(),
  });
}

export function useProject(id: number) {
  const { data, ...rest } = useProjects();
  return { ...rest, data: data?.projects.find((p) => p.id === id) };
}

export function useCreateProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (command: CreateProjectCommand) => projectsApi.createProject(command),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: projectsKey }),
  });
}
