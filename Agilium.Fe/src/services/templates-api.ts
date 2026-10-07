import { apiRequest } from "./http-client";
import type { ItemType } from "./items-api";

export const TemplateItemType = {
  SingleLineText: 1,
  MultiLineText: 2,
  Integer: 3,
  Decimal: 4,
  Boolean: 5,
  Comments: 6,
  LabelOnly: 7,
} as const;
export type TemplateItemType = (typeof TemplateItemType)[keyof typeof TemplateItemType];

export const TEMPLATE_ITEM_TYPE_LABELS: Record<TemplateItemType, string> = {
  [TemplateItemType.SingleLineText]: "Jednořádkový text",
  [TemplateItemType.MultiLineText]: "Víceřádkový text",
  [TemplateItemType.Integer]: "Celé číslo",
  [TemplateItemType.Decimal]: "Desetinné číslo",
  [TemplateItemType.Boolean]: "Ano / ne",
  [TemplateItemType.Comments]: "Komentáře",
  [TemplateItemType.LabelOnly]: "Jen popisek",
};

export type TemplateAttributeDto = {
  id: number;
  key: string;
  title: string;
  type: TemplateItemType;
  validatingRegex: string | null;
};

export type TemplateSectionDto = { id: number; title: string; items: TemplateAttributeDto[] };
export type TemplateColumnDto = { id: number; width: number; sections: TemplateSectionDto[] };
export type TemplateTableDto = { id: number; columns: TemplateColumnDto[] };
export type TemplateDto = { templateId: number; tables: TemplateTableDto[] };

/** Update payload: an `id` of `null` creates the element; the array order is the element order. */
export type TemplateAttributeInput = Omit<TemplateAttributeDto, "id"> & { id: number | null };
export type TemplateSectionInput = { id: number | null; title: string; items: TemplateAttributeInput[] };
export type TemplateColumnInput = { id: number | null; width: number; sections: TemplateSectionInput[] };
export type TemplateTableInput = { id: number | null; columns: TemplateColumnInput[] };

export type UpdateTemplateResult = { deletedFieldValues: number };

export function getProjectTemplate(projectId: number, itemType: ItemType): Promise<TemplateDto> {
  return apiRequest(`/projects/${projectId}/templates/${itemType}`);
}

export function updateProjectTemplate(
  projectId: number,
  itemType: ItemType,
  tables: TemplateTableInput[],
): Promise<UpdateTemplateResult> {
  return apiRequest(`/projects/${projectId}/templates/${itemType}`, { method: "PUT", body: { tables } });
}
