import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as api from "./members-api";
import type { CreateInvitationInput, RoleInput } from "./members-api";

const membersKey = (projectId: number) => ["projects", projectId, "members"] as const;
const rolesKey = (projectId: number) => ["projects", projectId, "roles"] as const;
const invitationsKey = (projectId: number) => ["projects", projectId, "invitations"] as const;

export function useMembers(projectId: number) {
  return useQuery({ queryKey: membersKey(projectId), queryFn: () => api.listMembers(projectId) });
}

export function useRoles(projectId: number) {
  return useQuery({ queryKey: rolesKey(projectId), queryFn: () => api.listRoles(projectId) });
}

export function useInvitations(projectId: number) {
  return useQuery({ queryKey: invitationsKey(projectId), queryFn: () => api.listInvitations(projectId) });
}

/** Members, roles and the caller's own permissions (a role change may concern the caller) all go stale together. */
function useInvalidateAccess(projectId: number) {
  const queryClient = useQueryClient();
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: membersKey(projectId) }),
      queryClient.invalidateQueries({ queryKey: rolesKey(projectId) }),
      queryClient.invalidateQueries({ queryKey: ["projects", projectId] }),
      queryClient.invalidateQueries({ queryKey: ["projects"], exact: true }),
    ]);
}

export function useChangeMemberRole(projectId: number) {
  const invalidate = useInvalidateAccess(projectId);
  return useMutation({
    mutationFn: ({ membershipId, roleId }: { membershipId: number; roleId: number }) =>
      api.changeMemberRole(membershipId, roleId),
    onSuccess: invalidate,
  });
}

export function useRemoveMember(projectId: number) {
  const invalidate = useInvalidateAccess(projectId);
  return useMutation({ mutationFn: (membershipId: number) => api.removeMember(membershipId), onSuccess: invalidate });
}

export function useCreateRole(projectId: number) {
  const invalidate = useInvalidateAccess(projectId);
  return useMutation({ mutationFn: (input: RoleInput) => api.createRole(projectId, input), onSuccess: invalidate });
}

export function useUpdateRole(projectId: number) {
  const invalidate = useInvalidateAccess(projectId);
  return useMutation({
    mutationFn: ({ id, ...input }: RoleInput & { id: number }) => api.updateRole(id, input),
    onSuccess: invalidate,
  });
}

export function useDeleteRole(projectId: number) {
  const invalidate = useInvalidateAccess(projectId);
  return useMutation({ mutationFn: (id: number) => api.deleteRole(id), onSuccess: invalidate });
}

export function useCreateInvitation(projectId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateInvitationInput) => api.createInvitation(projectId, input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: invitationsKey(projectId) }),
  });
}

export function useRevokeInvitation(projectId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => api.revokeInvitation(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: invitationsKey(projectId) }),
  });
}

export function useJoinProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (key: string) => api.joinProject(key),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["projects"] }),
  });
}
