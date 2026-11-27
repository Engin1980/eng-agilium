import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as commentsApi from "./comments-api";

const commentsKey = (itemId: number, templateItemId: number) => ["items", itemId, "comments", templateItemId] as const;

export function useComments(itemId: number, templateItemId: number) {
  return useQuery({
    queryKey: commentsKey(itemId, templateItemId),
    queryFn: () => commentsApi.listComments(itemId, templateItemId),
  });
}

export function useAddComment(itemId: number, templateItemId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (text: string) => commentsApi.createComment(itemId, templateItemId, text),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: commentsKey(itemId, templateItemId) }),
  });
}

export function useEditComment(itemId: number, templateItemId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: { id: number; text: string }) => commentsApi.updateComment(input.id, input.text),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: commentsKey(itemId, templateItemId) }),
  });
}

export function useDeleteComment(itemId: number, templateItemId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => commentsApi.deleteComment(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: commentsKey(itemId, templateItemId) }),
  });
}
