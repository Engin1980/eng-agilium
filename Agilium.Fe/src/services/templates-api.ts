import { apiRequest } from "./http-client";
import type { ItemType } from "./items-api";

export const TemplateItemType = {
  InlineText: 1,
  NextlineText: 2,
  NextlineTextArea: 3,
  InlineInt: 4,
  NextlineInt: 5,
  InlineDouble: 6,
  NNextlineDouble: 7,
  Comments: 8,
  Untemplated: 9,
  Checkbox: 10,
  LabelOnly: 11,
} as const;
export type TemplateItemType = (typeof TemplateItemType)[keyof typeof TemplateItemType];

export type TemplateFieldDto = {
  id: number;
  key: string;
  title: string;
  type: TemplateItemType;
  validatingRegex: string | null;
  orderIndex: number;
  columnStart: number;
  columnSpan: number;
  rowStart: number;
  rowSpan: number;
};

export type TemplateDto = {
  templateId: number;
  columnCount: number;
  items: TemplateFieldDto[];
};

export type TemplateFieldInput = {
  id: number | null;
  key: string;
  title: string;
  type: TemplateItemType;
  validatingRegex: string | null;
  orderIndex: number;
  columnStart: number;
  columnSpan: number;
  rowStart: number;
  rowSpan: number;
};

export function getProjectTemplate(projectId: number, itemType: ItemType): Promise<TemplateDto> {
  return apiRequest(`/projects/${projectId}/templates/${itemType}`);
}

export function updateProjectTemplate(
  projectId: number,
  itemType: ItemType,
  columnCount: number,
  items: TemplateFieldInput[],
): Promise<void> {
  return apiRequest(`/projects/${projectId}/templates/${itemType}`, {
    method: "PUT",
    body: { columnCount, items },
  });
}

export function getGlobalTemplate(itemType: ItemType): Promise<TemplateDto> {
  return apiRequest(`/templates/${itemType}`);
}

export function updateGlobalTemplate(
  itemType: ItemType,
  columnCount: number,
  items: TemplateFieldInput[],
): Promise<void> {
  return apiRequest(`/templates/${itemType}`, { method: "PUT", body: { columnCount, items } });
}
