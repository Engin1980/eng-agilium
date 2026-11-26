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

export function listProjects(): Promise<ListProjectsResult> {
  return apiRequest<ListProjectsResult>("/projects");
}

export type ProjectPermissions = {
  canViewProject: boolean;
  canManageProject: boolean;
  canViewMembers: boolean;
  canManageMembers: boolean;
  canManageSprints: boolean;
  canEditItems: boolean;
  isOwner: boolean;
};

export type ProjectDetail = Project & {
  roleId: number;
  roleTitle: string;
  permissions: ProjectPermissions;
};

export function getProject(id: number): Promise<ProjectDetail> {
  return apiRequest<ProjectDetail>(`/projects/${id}`);
}

export type CreateProjectCommand = { title: string; description: string };

export function createProject(command: CreateProjectCommand): Promise<{ id: number }> {
  return apiRequest<{ id: number }>("/projects", { method: "POST", body: command });
}
