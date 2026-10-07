import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as sprintsApi from "./sprints-api";
import type { BoardDto, SprintInput, SprintState } from "./sprints-api";

const sprintsKey = (projectId: number) => ["projects", projectId, "sprints"] as const;
const backlogKey = (projectId: number) => ["projects", projectId, "backlog"] as const;
const boardKey = (sprintId: number) => ["sprints", sprintId, "board"] as const;

export function useSprints(projectId: number) {
  return useQuery({
    queryKey: sprintsKey(projectId),
    queryFn: () => sprintsApi.listSprints(projectId),
  });
}

export function useBacklog(projectId: number) {
  return useQuery({
    queryKey: backlogKey(projectId),
    queryFn: () => sprintsApi.listBacklog(projectId),
  });
}

export function useSprintBoard(sprintId: number) {
  return useQuery({
    queryKey: boardKey(sprintId),
    queryFn: () => sprintsApi.getBoard(sprintId),
  });
}

export function useCreateSprint(projectId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: SprintInput) => sprintsApi.createSprint(projectId, input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: sprintsKey(projectId) }),
  });
}

export function useUpdateSprint(projectId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...input }: SprintInput & { id: number; state: SprintState }) =>
      sprintsApi.updateSprint(id, input),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: sprintsKey(projectId) });
      queryClient.invalidateQueries({ queryKey: boardKey(id) });
    },
  });
}

export function useDeleteSprint(projectId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => sprintsApi.deleteSprint(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: sprintsKey(projectId) }),
  });
}

/** Assigning moves the item out of its previous sprint, so every board of the project is stale. */
export function useAssignSprintItem(projectId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ sprintId, itemId }: { sprintId: number; itemId: number }) =>
      sprintsApi.assignItem(sprintId, itemId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: sprintsKey(projectId) });
      queryClient.invalidateQueries({ queryKey: backlogKey(projectId) });
      queryClient.invalidateQueries({ queryKey: ["sprints"] });
    },
  });
}

export function useUnassignSprintItem(projectId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ sprintId, itemId }: { sprintId: number; itemId: number }) =>
      sprintsApi.unassignItem(sprintId, itemId),
    onSuccess: (_, { sprintId }) => {
      queryClient.invalidateQueries({ queryKey: sprintsKey(projectId) });
      queryClient.invalidateQueries({ queryKey: backlogKey(projectId) });
      queryClient.invalidateQueries({ queryKey: boardKey(sprintId) });
    },
  });
}

/** Moves a card between columns; the card jumps immediately and the board is re-read afterwards. */
export function useUpdateSprintItemState(sprintId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ itemId, workflowStateId }: { itemId: number; workflowStateId: number }) =>
      sprintsApi.updateItemState(sprintId, itemId, workflowStateId),
    onMutate: async ({ itemId, workflowStateId }) => {
      await queryClient.cancelQueries({ queryKey: boardKey(sprintId) });
      const previous = queryClient.getQueryData<BoardDto>(boardKey(sprintId));
      if (previous) {
        const card = previous.columns.flatMap((c) => c.cards).find((c) => c.itemId === itemId);
        if (card) {
          queryClient.setQueryData<BoardDto>(boardKey(sprintId), {
            ...previous,
            columns: previous.columns.map((column) => ({
              ...column,
              cards:
                column.id === workflowStateId
                  ? [...column.cards.filter((c) => c.itemId !== itemId), card]
                  : column.cards.filter((c) => c.itemId !== itemId),
            })),
          });
        }
      }
      return { previous };
    },
    onError: (_error, _variables, context) => {
      if (context?.previous) queryClient.setQueryData(boardKey(sprintId), context.previous);
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: boardKey(sprintId) });
      // Sprint lists show done / total counters.
      queryClient.invalidateQueries({ queryKey: ["projects"], predicate: (q) => q.queryKey[2] === "sprints" });
    },
  });
}
