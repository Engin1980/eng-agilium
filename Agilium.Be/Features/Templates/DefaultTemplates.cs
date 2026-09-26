using Eng.Agilium.Be.Model.Db;

namespace Eng.Agilium.Be.Features.Templates;

/// <summary>
/// The built-in field layout used to seed the global default template for each <see cref="ItemType"/>
/// (see <c>AppInitializer</c>) and as a fallback in <c>Projects.Create</c> if that seed is somehow
/// missing. A 4-column grid: a wide left column (description/comments) and a narrow right column
/// (numeric fields), mirroring the old fixed-column layout this replaces.
/// </summary>
public static class DefaultTemplates
{
  public const int DefaultColumnCount = 4;

  public static List<TemplateItem> BuildFieldsFor(ItemType type) =>
    type switch
    {
      ItemType.Task or ItemType.Bug => TaskOrBugFields(),
      ItemType.UserStory or ItemType.Feature => FeatureOrUserStoryFields(),
      _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

  private static List<TemplateItem> TaskOrBugFields() =>
    [
      DescriptionField(),
      CommentsField(),
      PriorityField(),
      ComplexityField(),
      new()
      {
        OrderIndex = 5,
        Key = "time-expected",
        Title = "Time Expected (h)",
        Type = TemplateItemType.InlineDouble,
        ColumnStart = 4,
        ColumnSpan = 1,
        RowStart = 3,
        RowSpan = 1,
      },
      new()
      {
        OrderIndex = 6,
        Key = "time-spent",
        Title = "Time Spent (h)",
        Type = TemplateItemType.InlineDouble,
        ColumnStart = 4,
        ColumnSpan = 1,
        RowStart = 4,
        RowSpan = 1,
      },
    ];

  private static List<TemplateItem> FeatureOrUserStoryFields() => [DescriptionField(), CommentsField(), PriorityField(), ComplexityField()];

  private static TemplateItem DescriptionField() =>
    new()
    {
      OrderIndex = 1,
      Key = "description",
      Title = "Description",
      Type = TemplateItemType.NextlineTextArea,
      ColumnStart = 1,
      ColumnSpan = 3,
      RowStart = 1,
      RowSpan = 1,
    };

  private static TemplateItem CommentsField() =>
    new()
    {
      OrderIndex = 2,
      Key = "comments",
      Title = "Comments",
      Type = TemplateItemType.Comments,
      ColumnStart = 1,
      ColumnSpan = 3,
      RowStart = 2,
      RowSpan = 1,
    };

  private static TemplateItem PriorityField() =>
    new()
    {
      OrderIndex = 3,
      Key = "priority",
      Title = "Priority",
      Type = TemplateItemType.InlineInt,
      ColumnStart = 4,
      ColumnSpan = 1,
      RowStart = 1,
      RowSpan = 1,
    };

  private static TemplateItem ComplexityField() =>
    new()
    {
      OrderIndex = 4,
      Key = "complexity",
      Title = "Complexity",
      Type = TemplateItemType.InlineInt,
      ColumnStart = 4,
      ColumnSpan = 1,
      RowStart = 2,
      RowSpan = 1,
    };
}
