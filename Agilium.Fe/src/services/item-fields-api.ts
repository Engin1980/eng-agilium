import { apiRequest } from "./http-client";
import type { TemplateItemType } from "./templates-api";

export type ItemFieldDto = {
  templateItemId: number;
  key: string;
  title: string;
  type: TemplateItemType;
  validatingRegex: string | null;
  columnStart: number;
  columnSpan: number;
  rowStart: number;
  rowSpan: number;
  value: string | null;
};

export type ItemFieldsDto = {
  columnCount: number;
  fields: ItemFieldDto[];
};

export function getItemFields(itemId: number): Promise<ItemFieldsDto> {
  return apiRequest(`/projects/items/${itemId}/fields`);
}

export function setItemFields(
  itemId: number,
  values: { templateItemId: number; value: string | null }[],
): Promise<void> {
  return apiRequest(`/projects/items/${itemId}/fields`, { method: "PUT", body: { values } });
}
