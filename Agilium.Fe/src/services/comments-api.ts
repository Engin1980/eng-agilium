import { apiRequest } from "./http-client";

export type ItemComment = {
  id: number;
  templateItemId: number;
  authorId: number;
  authorName: string;
  /** Markdown source. */
  text: string;
  createdAt: string;
  editedAt: string | null;
  canEdit: boolean;
  canDelete: boolean;
};

export const MAX_COMMENT_LENGTH = 4000;

export async function listComments(itemId: number, templateItemId: number): Promise<ItemComment[]> {
  const result = await apiRequest<{ comments: ItemComment[] }>(
    `/projects/items/${itemId}/comments?templateItemId=${templateItemId}`,
  );
  return result.comments;
}

export function createComment(itemId: number, templateItemId: number, text: string): Promise<{ id: number }> {
  return apiRequest<{ id: number }>(`/projects/items/${itemId}/comments`, {
    method: "POST",
    body: { templateItemId, text },
  });
}

export function updateComment(commentId: number, text: string): Promise<void> {
  return apiRequest<void>(`/projects/items/comments/${commentId}`, { method: "PATCH", body: { text } });
}

export function deleteComment(commentId: number): Promise<void> {
  return apiRequest<void>(`/projects/items/comments/${commentId}`, { method: "DELETE" });
}
