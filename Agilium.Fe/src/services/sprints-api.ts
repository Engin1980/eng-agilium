import { apiRequest } from "./http-client";

export const SprintState = { Planned: 1, Active: 2, Completed: 3 } as const;
export type SprintState = (typeof SprintState)[keyof typeof SprintState];

export const SPRINT_STATE_LABELS: Record<SprintState, string> = {
  [SprintState.Planned]: "Plánovaný",
  [SprintState.Active]: "Aktivní",
  [SprintState.Completed]: "Dokončený",
};

/** Type of a workflow state (kanban column); also the derived status of a feature / user story. */
export const WorkflowStateType = { ToDo: 1, Active: 2, Done: 3 } as const;
export type WorkflowStateType = (typeof WorkflowStateType)[keyof typeof WorkflowStateType];

export const WORKFLOW_STATUS_LABELS: Record<WorkflowStateType, string> = {
  [WorkflowStateType.ToDo]: "TODO",
  [WorkflowStateType.Active]: "ACTIVE",
  [WorkflowStateType.Done]: "DONE",
};

export type SprintListItemDto = {
  id: number;
  title: string;
  startDateTime: string | null;
  endDateTime: string | null;
  state: SprintState;
  itemCount: number;
  doneCount: number;
};

export type SprintInput = {
  title: string;
  startDateTime: string | null;
  endDateTime: string | null;
};

export type BacklogItemDto = {
  id: number;
  title: string;
  /** ItemType: 1 = Task, 2 = Bug. */
  type: number;
  userStoryId: number | null;
  userStoryTitle: string | null;
  sprintId: number | null;
  sprintTitle: string | null;
};

export type BoardCardDto = {
  itemId: number;
  title: string;
  type: number;
  assigneeName: string | null;
  userStoryId: number | null;
  userStoryTitle: string | null;
  featureId: number | null;
  featureTitle: string | null;
};

export type BoardColumnDto = { id: number; title: string; type: WorkflowStateType; cards: BoardCardDto[] };

export type BoardParentDto = {
  itemId: number;
  title: string;
  type: number;
  isGeneric: boolean;
  status: WorkflowStateType;
  children: BoardParentDto[];
};

export type BoardDto = {
  sprint: { id: number; title: string; startDateTime: string | null; endDateTime: string | null; state: SprintState };
  columns: BoardColumnDto[];
  features: BoardParentDto[];
};

export function listSprints(projectId: number): Promise<{ sprints: SprintListItemDto[] }> {
  return apiRequest(`/projects/${projectId}/sprints`);
}

export function createSprint(projectId: number, input: SprintInput): Promise<{ id: number }> {
  return apiRequest(`/projects/sprints`, { method: "POST", body: { projectId, ...input } });
}

export function updateSprint(id: number, input: SprintInput & { state: SprintState }): Promise<void> {
  return apiRequest(`/projects/sprints/${id}`, { method: "PATCH", body: input });
}

export function deleteSprint(id: number): Promise<void> {
  return apiRequest(`/projects/sprints/${id}`, { method: "DELETE" });
}

export function listBacklog(projectId: number): Promise<{ items: BacklogItemDto[] }> {
  return apiRequest(`/projects/${projectId}/backlog`);
}

export function assignItem(sprintId: number, itemId: number): Promise<void> {
  return apiRequest(`/projects/sprints/${sprintId}/items`, { method: "POST", body: { itemId } });
}

export function unassignItem(sprintId: number, itemId: number): Promise<void> {
  return apiRequest(`/projects/sprints/${sprintId}/items/${itemId}`, { method: "DELETE" });
}

export function updateItemState(sprintId: number, itemId: number, workflowStateId: number): Promise<void> {
  return apiRequest(`/projects/sprints/${sprintId}/items/${itemId}/state`, {
    method: "PATCH",
    body: { workflowStateId },
  });
}

export function getBoard(sprintId: number): Promise<BoardDto> {
  return apiRequest(`/projects/sprints/${sprintId}/board`);
}
