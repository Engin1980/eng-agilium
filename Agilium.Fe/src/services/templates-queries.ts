import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as templatesApi from "./templates-api";
import type { TemplateFieldInput } from "./templates-api";
import type { ItemType } from "./items-api";

const projectTemplateKey = (projectId: number, itemType: ItemType) =>
  ["templates", "project", projectId, itemType] as const;
const globalTemplateKey = (itemType: ItemType) => ["templates", "global", itemType] as const;

export function useProjectTemplate(projectId: number, itemType: ItemType) {
  return useQuery({
    queryKey: projectTemplateKey(projectId, itemType),
    queryFn: () => templatesApi.getProjectTemplate(projectId, itemType),
  });
}

export function useUpdateProjectTemplate(projectId: number, itemType: ItemType) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ columnCount, items }: { columnCount: number; items: TemplateFieldInput[] }) =>
      templatesApi.updateProjectTemplate(projectId, itemType, columnCount, items),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: projectTemplateKey(projectId, itemType) }),
  });
}

export function useGlobalTemplate(itemType: ItemType) {
  return useQuery({
    queryKey: globalTemplateKey(itemType),
    queryFn: () => templatesApi.getGlobalTemplate(itemType),
  });
}

export function useUpdateGlobalTemplate(itemType: ItemType) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ columnCount, items }: { columnCount: number; items: TemplateFieldInput[] }) =>
      templatesApi.updateGlobalTemplate(itemType, columnCount, items),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: globalTemplateKey(itemType) }),
  });
}
