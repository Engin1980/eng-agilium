import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as templatesApi from "./templates-api";
import type { TemplateTableInput } from "./templates-api";
import type { ItemType } from "./items-api";

const projectTemplateKey = (projectId: number, itemType: ItemType) =>
  ["templates", "project", projectId, itemType] as const;

export function useProjectTemplate(projectId: number, itemType: ItemType) {
  return useQuery({
    queryKey: projectTemplateKey(projectId, itemType),
    queryFn: () => templatesApi.getProjectTemplate(projectId, itemType),
  });
}

export function useUpdateProjectTemplate(projectId: number, itemType: ItemType) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (tables: TemplateTableInput[]) => templatesApi.updateProjectTemplate(projectId, itemType, tables),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: projectTemplateKey(projectId, itemType) });
      // Item detail forms are built from the template, so they are stale now.
      queryClient.invalidateQueries({ queryKey: ["items"] });
    },
  });
}
