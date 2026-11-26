import { apiRequest } from "./http-client";

export type Member = {
  membershipId: number;
  userId: number;
  name: string;
  surname: string;
  email: string;
  roleId: number;
  roleTitle: string;
};

export type RoleFlags = {
  canViewProject: boolean;
  canManageProject: boolean;
  canViewMembers: boolean;
  canManageMembers: boolean;
  canManageSprints: boolean;
  canEditItems: boolean;
};

export type ProjectRole = RoleFlags & {
  id: number;
  title: string;
  isSystem: boolean;
  /** 0 custom, 1 Owner, 2 Manager, 3 Member, 4 Guest */
  systemKind: number;
  memberCount: number;
};

export type RoleInput = RoleFlags & { title: string };

/** 1 active, 2 expired, 3 exhausted, 4 revoked */
export type InvitationStatus = 1 | 2 | 3 | 4;

export type Invitation = {
  id: number;
  keyPrefix: string;
  roleId: number;
  roleTitle: string;
  expiresAt: string;
  maxUses: number;
  usedCount: number;
  createdAt: string;
  status: InvitationStatus;
};

export type CreateInvitationInput = { roleId: number; expiresAt?: string; maxUses?: number };

/** The only response that contains the key. */
export type CreatedInvitation = {
  id: number;
  key: string;
  keyPrefix: string;
  expiresAt: string;
  maxUses: number;
};

export async function listMembers(projectId: number): Promise<Member[]> {
  return (await apiRequest<{ members: Member[] }>(`/projects/${projectId}/members`)).members;
}

export function changeMemberRole(membershipId: number, roleId: number): Promise<void> {
  return apiRequest<void>(`/projects/members/${membershipId}`, { method: "PATCH", body: { roleId } });
}

export function removeMember(membershipId: number): Promise<void> {
  return apiRequest<void>(`/projects/membership/${membershipId}`, { method: "DELETE" });
}

export async function listRoles(projectId: number): Promise<ProjectRole[]> {
  return (await apiRequest<{ roles: ProjectRole[] }>(`/projects/${projectId}/roles`)).roles;
}

export function createRole(projectId: number, input: RoleInput): Promise<{ id: number }> {
  return apiRequest<{ id: number }>(`/projects/${projectId}/roles`, { method: "POST", body: input });
}

export function updateRole(id: number, input: RoleInput): Promise<void> {
  return apiRequest<void>(`/projects/roles/${id}`, { method: "PATCH", body: input });
}

export function deleteRole(id: number): Promise<void> {
  return apiRequest<void>(`/projects/roles/${id}`, { method: "DELETE" });
}

export async function listInvitations(projectId: number): Promise<Invitation[]> {
  return (await apiRequest<{ invitations: Invitation[] }>(`/projects/${projectId}/invitations`)).invitations;
}

export function createInvitation(projectId: number, input: CreateInvitationInput): Promise<CreatedInvitation> {
  return apiRequest<CreatedInvitation>(`/projects/${projectId}/invitations`, { method: "POST", body: input });
}

export function revokeInvitation(id: number): Promise<void> {
  return apiRequest<void>(`/projects/invitations/${id}`, { method: "DELETE" });
}

export function joinProject(key: string): Promise<{ projectId: number }> {
  return apiRequest<{ projectId: number }>("/projects/join", { method: "POST", body: { key } });
}
