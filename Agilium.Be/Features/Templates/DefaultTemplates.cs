using Eng.Agilium.Be.Model.Db;

namespace Eng.Agilium.Be.Features.Templates;

/// <summary>
/// The built-in layout used to seed the global default template for each <see cref="ItemType"/>
/// (see <c>AppInitializer</c>). Global templates are not editable through the API; every project gets
/// its own copy of them (see <see cref="TemplateTree.CloneTables"/>) which is then edited per project.
/// Layout: one table with a wide left column (description, comments) and a narrow right column
/// (priority, complexity and - for task/bug - time tracking); widths sum to 12.
/// </summary>
public static class DefaultTemplates
{
  public static List<TemplateTable> BuildTablesFor(ItemType type)
  {
    var details = new List<TemplateItem>
    {
      Item(1, "priority", "Priority", TemplateItemType.Integer),
      Item(2, "complexity", "Complexity", TemplateItemType.Integer),
    };

    if (type is ItemType.Task or ItemType.Bug)
    {
      details.Add(Item(3, "time-expected", "Time Expected (h)", TemplateItemType.Decimal));
      details.Add(Item(4, "time-spent", "Time Spent (h)", TemplateItemType.Decimal));
    }

    return
    [
      new TemplateTable
      {
        OrderIndex = 1,
        Columns =
        [
          new TemplateColumn
          {
            OrderIndex = 1,
            Width = 8,
            Sections =
            [
              Section(1, "Description", Item(1, "description", "Description", TemplateItemType.MultiLineText)),
              Section(2, "Discussion", Item(1, "comments", "Comments", TemplateItemType.Comments)),
            ],
          },
          new TemplateColumn
          {
            OrderIndex = 2,
            Width = 4,
            Sections = [Section(1, "Details", [.. details])],
          },
        ],
      },
    ];
  }

  private static TemplateSection Section(int orderIndex, string title, params TemplateItem[] items) =>
    new()
    {
      OrderIndex = orderIndex,
      Title = title,
      Items = items,
    };

  private static TemplateItem Item(int orderIndex, string key, string title, TemplateItemType type) =>
    new()
    {
      OrderIndex = orderIndex,
      Key = key,
      Title = title,
      Type = type,
    };
}
