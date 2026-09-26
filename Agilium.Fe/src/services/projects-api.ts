import { apiRequest } from "./http-client";

export type ProjectStatus = 1 | 2; // 1 = Active, 2 = Inactive

export type Project = {
  id: number;
  title: string;
  description: string;
  status: ProjectStatus;
  memberCount: number;
};

export type ListProjectsResult = { projects: Project[] };

export function listProjects(memberId?: number): Promise<ListProjectsResult> {
  const query = memberId !== undefined ? `?memberId=${memberId}` : "";
  return apiRequest<ListProjectsResult>(`/projects${query}`);
}

export type CreateProjectCommand = { title: string; description: string };

export function createProject(command: CreateProjectCommand): Promise<{ id: number }> {
  return apiRequest<{ id: number }>("/projects", { method: "POST", body: command });
}
