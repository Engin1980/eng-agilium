import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as projectsApi from "./projects-api";
import type { CreateProjectCommand, ProjectPermissions } from "./projects-api";
import { ApiError } from "./http-client";

const projectsKey = ["projects"] as const;

export function useProjects() {
  return useQuery({
    queryKey: projectsKey,
    queryFn: () => projectsApi.listProjects(),
  });
}

export function useProject(id: number) {
  return useQuery({
    queryKey: [...projectsKey, id],
    queryFn: () => projectsApi.getProject(id),
    // 403/404 mean "no access to this project" - retrying would not help
    retry: (count, error) => !(error instanceof ApiError && [401, 403, 404].includes(error.status)) && count < 2,
  });
}

const noPermissions: ProjectPermissions = {
  canViewProject: false,
  canManageProject: false,
  canViewMembers: false,
  canManageMembers: false,
  canManageSprints: false,
  canEditItems: false,
  isOwner: false,
};

/** What the logged-in user may do in the project (all false while loading). The BE enforces it anyway. */
export function useProjectPermissions(id: number): ProjectPermissions {
  const { data } = useProject(id);
  return data?.permissions ?? noPermissions;
}

export function useCreateProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (command: CreateProjectCommand) => projectsApi.createProject(command),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: projectsKey }),
  });
}

export function useDeleteProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => projectsApi.deleteProject(id),
    onSuccess: (_data, id) => {
      queryClient.removeQueries({ queryKey: [...projectsKey, id] });
      return queryClient.invalidateQueries({ queryKey: projectsKey });
    },
  });
}
