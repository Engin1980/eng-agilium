import { apiRequest } from "./http-client";

export const ItemType = {
  Task: 1,
  Bug: 2,
  UserStory: 3,
  Feature: 4,
} as const;
export type ItemType = (typeof ItemType)[keyof typeof ItemType];

export type ItemAssignee = {
  id: number;
  name: string;
  surname: string;
  roleName: string;
};

export type ItemNode = {
  id: number;
  title: string;
  type: ItemType;
  isGeneric: boolean;
  assignee: ItemAssignee | null;
  parentId: number | null;
  subItems: ItemNode[];
};

export function listProjectItems(projectId: number): Promise<{ items: ItemNode[] }> {
  return apiRequest(`/projects/${projectId}/items`);
}

export type CreateItemCommand = {
  title: string;
  type: ItemType;
  projectId: number;
  parentId: number | null;
};

export function createItem(command: CreateItemCommand): Promise<{ id: number }> {
  return apiRequest("/projects/items", { method: "POST", body: command });
}

export function updateItem(id: number, command: { title: string; type: ItemType }): Promise<void> {
  return apiRequest(`/projects/items/${id}`, { method: "PATCH", body: command });
}

export function updateItemParent(id: number, parentId: number | null): Promise<void> {
  return apiRequest(`/projects/items/${id}/parent`, { method: "PATCH", body: { parentId } });
}

export function updateItemAssignee(id: number, assigneeId: number | null): Promise<void> {
  return apiRequest(`/projects/items/${id}/assignee`, { method: "PATCH", body: { assigneeId } });
}

export function deleteItem(id: number): Promise<void> {
  return apiRequest(`/projects/items/${id}`, { method: "DELETE" });
}
