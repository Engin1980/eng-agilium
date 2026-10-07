import * as React from "react";

export type LayoutSection<A> = { id: number | string; title: string; items: A[] };
export type LayoutColumn<A> = { id: number | string; width: number; sections: LayoutSection<A>[] };
export type LayoutTable<A> = { id: number | string; columns: LayoutColumn<A>[] };

/**
 * Renders a template layout: tables stacked below each other, each with columns side by side (a
 * column width is its share of the row, ideally out of 12), sections stacked inside a column and
 * attributes across the full width of their section. Shared by the item detail form and the template
 * editor preview so both always look the same; what an attribute looks like is up to `renderAttribute`.
 */
export function TemplateLayout<A>({
  tables,
  getAttributeKey,
  renderAttribute,
}: {
  tables: LayoutTable<A>[];
  getAttributeKey: (attribute: A) => React.Key;
  renderAttribute: (attribute: A) => React.ReactNode;
}) {
  return (
    <div className="space-y-6">
      {tables.map((table) => (
        <div
          key={table.id}
          className="grid gap-6"
          style={{ gridTemplateColumns: table.columns.map((c) => `minmax(0, ${Math.max(c.width, 1)}fr)`).join(" ") }}
        >
          {table.columns.map((column) => (
            <div key={column.id} className="space-y-4">
              {column.sections.map((section) => (
                <section key={section.id}>
                  {section.title && <h3 className="mb-2 font-bold text-gray-800">{section.title}</h3>}
                  <div className="space-y-3">
                    {section.items.map((attribute) => (
                      <div key={getAttributeKey(attribute)}>{renderAttribute(attribute)}</div>
                    ))}
                  </div>
                </section>
              ))}
            </div>
          ))}
        </div>
      ))}
    </div>
  );
}
