import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as workflowApi from "./workflow-api";
import type { WorkflowStateInput } from "./workflow-api";

const workflowKey = (projectId: number) => ["projects", projectId, "workflow"] as const;

export function useWorkflow(projectId: number) {
  return useQuery({
    queryKey: workflowKey(projectId),
    queryFn: () => workflowApi.getWorkflow(projectId),
  });
}

export function useUpdateWorkflow(projectId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (states: WorkflowStateInput[]) => workflowApi.updateWorkflow(projectId, states),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: workflowKey(projectId) });
      // Boards and sprint counters are built from the workflow states.
      queryClient.invalidateQueries({ queryKey: ["sprints"] });
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "sprints"] });
    },
  });
}
