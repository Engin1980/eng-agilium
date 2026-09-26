import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as itemsApi from "./items-api";
import type { CreateItemCommand, ItemType } from "./items-api";

const itemsKey = (projectId: number) => ["projects", projectId, "items"] as const;

export function useProjectItems(projectId: number) {
  return useQuery({
    queryKey: itemsKey(projectId),
    queryFn: () => itemsApi.listProjectItems(projectId),
  });
}

export function useCreateItem(projectId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (command: CreateItemCommand) => itemsApi.createItem(command),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: itemsKey(projectId) }),
  });
}

export function useUpdateItem(projectId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, title, type }: { id: number; title: string; type: ItemType }) =>
      itemsApi.updateItem(id, { title, type }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: itemsKey(projectId) }),
  });
}

export function useUpdateItemParent(projectId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, parentId }: { id: number; parentId: number | null }) =>
      itemsApi.updateItemParent(id, parentId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: itemsKey(projectId) }),
  });
}

export function useUpdateItemAssignee(projectId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, assigneeId }: { id: number; assigneeId: number | null }) =>
      itemsApi.updateItemAssignee(id, assigneeId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: itemsKey(projectId) }),
  });
}

export function useDeleteItem(projectId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => itemsApi.deleteItem(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: itemsKey(projectId) }),
  });
}
