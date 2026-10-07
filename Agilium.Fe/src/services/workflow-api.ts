import { apiRequest } from "./http-client";
import type { WorkflowStateType } from "./sprints-api";

export type WorkflowStateDto = { id: number; title: string; type: WorkflowStateType; itemCount: number };

/** Update payload: an `id` of `null` creates the column; the array order is the column order. */
export type WorkflowStateInput = { id: number | null; title: string; type: WorkflowStateType };

export type UpdateWorkflowResult = { movedItems: number };

export function getWorkflow(projectId: number): Promise<{ states: WorkflowStateDto[] }> {
  return apiRequest(`/projects/${projectId}/workflow`);
}

export function updateWorkflow(projectId: number, states: WorkflowStateInput[]): Promise<UpdateWorkflowResult> {
  return apiRequest(`/projects/${projectId}/workflow`, { method: "PUT", body: { states } });
}
