import { apiRequest } from "./http-client";
import type { TemplateItemType } from "./templates-api";

export type ItemFieldDto = {
  templateItemId: number;
  key: string;
  title: string;
  type: TemplateItemType;
  validatingRegex: string | null;
  value: string | null;
};

export type ItemFieldSectionDto = { id: number; title: string; items: ItemFieldDto[] };
export type ItemFieldColumnDto = { id: number; width: number; sections: ItemFieldSectionDto[] };
export type ItemFieldTableDto = { id: number; columns: ItemFieldColumnDto[] };
export type ItemFieldsDto = { tables: ItemFieldTableDto[] };

export function getItemFields(itemId: number): Promise<ItemFieldsDto> {
  return apiRequest(`/projects/items/${itemId}/fields`);
}

export function setItemFields(
  itemId: number,
  values: { templateItemId: number; value: string | null }[],
): Promise<void> {
  return apiRequest(`/projects/items/${itemId}/fields`, { method: "PUT", body: { values } });
}
