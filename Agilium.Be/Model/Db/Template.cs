namespace Eng.Agilium.Be.Model.Db;

public class Template
{
  public int Id { get; set; }

  /// <summary>Null marks the single global default template for this <see cref="Type"/>.</summary>
  public int? ProjectId { get; set; }
  public Project? Project { get; set; }

  public ItemType Type { get; set; }

  /// <summary>Tables stacked below each other (ordered by <see cref="TemplateTable.OrderIndex"/>).</summary>
  public ICollection<TemplateTable> Tables { get; set; } = [];
}

/// <summary>One table of a template; it holds columns side by side (bootstrap-like, widths ideally sum to 12).</summary>
public class TemplateTable
{
  public int Id { get; set; }
  public int TemplateId { get; set; }
  public Template Template { get; set; } = null!;
  public int OrderIndex { get; set; }
  public ICollection<TemplateColumn> Columns { get; set; } = [];
}

/// <summary>A column of a <see cref="TemplateTable"/>; <see cref="Width"/> is at least 1 (sum of a table's widths is expected to be 12, not enforced).</summary>
public class TemplateColumn
{
  public int Id { get; set; }
  public int TemplateTableId { get; set; }
  public TemplateTable Table { get; set; } = null!;
  public int Width { get; set; }
  public int OrderIndex { get; set; }
  public ICollection<TemplateSection> Sections { get; set; } = [];
}

/// <summary>A titled group of attributes stacked inside a <see cref="TemplateColumn"/> (title may be empty).</summary>
public class TemplateSection
{
  public int Id { get; set; }
  public int TemplateColumnId { get; set; }
  public TemplateColumn Column { get; set; } = null!;
  public string Title { get; set; } = string.Empty;
  public int OrderIndex { get; set; }
  public ICollection<TemplateItem> Items { get; set; } = [];
}
