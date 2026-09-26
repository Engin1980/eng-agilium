import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as itemFieldsApi from "./item-fields-api";

const itemFieldsKey = (itemId: number) => ["items", itemId, "fields"] as const;

export function useItemFields(itemId: number) {
  return useQuery({
    queryKey: itemFieldsKey(itemId),
    queryFn: () => itemFieldsApi.getItemFields(itemId),
  });
}

export function useSetItemFields(itemId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (values: { templateItemId: number; value: string | null }[]) =>
      itemFieldsApi.setItemFields(itemId, values),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: itemFieldsKey(itemId) }),
  });
}
