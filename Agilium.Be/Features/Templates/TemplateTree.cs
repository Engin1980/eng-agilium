using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Templates;

/// <summary>Helpers for the template tree (Template → Table → Column → Section → Item).</summary>
public static class TemplateTree
{
  /// <summary>Eagerly loads the whole tree of the templates in the query.</summary>
  public static IQueryable<Template> IncludeTree(this IQueryable<Template> query) =>
    query.Include(t => t.Tables).ThenInclude(tb => tb.Columns).ThenInclude(c => c.Sections).ThenInclude(s => s.Items);

  /// <summary>Deep-copies tables (with columns, sections and items) as new, untracked entities - used to give a project its own copy of a global template.</summary>
  public static List<TemplateTable> CloneTables(IEnumerable<TemplateTable> source) =>
    source
      .OrderBy(tb => tb.OrderIndex)
      .Select(tb => new TemplateTable
      {
        OrderIndex = tb.OrderIndex,
        Columns = tb
          .Columns.OrderBy(c => c.OrderIndex)
          .Select(c => new TemplateColumn
          {
            OrderIndex = c.OrderIndex,
            Width = c.Width,
            Sections = c
              .Sections.OrderBy(s => s.OrderIndex)
              .Select(s => new TemplateSection
              {
                OrderIndex = s.OrderIndex,
                Title = s.Title,
                Items = s
                  .Items.OrderBy(i => i.OrderIndex)
                  .Select(i => new TemplateItem
                  {
                    OrderIndex = i.OrderIndex,
                    Key = i.Key,
                    Title = i.Title,
                    Type = i.Type,
                    ValidatingRegex = i.ValidatingRegex,
                  })
                  .ToList(),
              })
              .ToList(),
          })
          .ToList(),
      })
      .ToList();

  /// <summary>
  /// Makes sure every project has a template for every <see cref="ItemType"/> (copied from the global
  /// one, or from the built-in default when that is missing) - idempotent. Used on startup, e.g. after
  /// the template model migration dropped the old project templates.
  /// </summary>
  public static async Task EnsureProjectTemplatesAsync(AppDbContext db, CancellationToken cancellationToken = default)
  {
    var globals = await db.Templates.AsNoTracking().IncludeTree().Where(t => t.ProjectId == null).ToListAsync(cancellationToken);
    var existing = await db
      .Templates.Where(t => t.ProjectId != null)
      .Select(t => new { ProjectId = t.ProjectId!.Value, t.Type })
      .ToListAsync(cancellationToken);
    var have = existing.Select(e => (e.ProjectId, e.Type)).ToHashSet();
    var projectIds = await db.Projects.Select(p => p.Id).ToListAsync(cancellationToken);

    foreach (var projectId in projectIds)
    foreach (var type in Enum.GetValues<ItemType>())
    {
      if (have.Contains((projectId, type)))
        continue;
      db.Templates.Add(NewProjectTemplate(globals.FirstOrDefault(g => g.Type == type), type, projectId: projectId));
    }
  }

  /// <summary>Builds a project template of the given type by copying <paramref name="global"/> (or the built-in default when null).</summary>
  public static Template NewProjectTemplate(Template? global, ItemType type, Project? project = null, int? projectId = null) =>
    new()
    {
      Project = project,
      ProjectId = projectId,
      Type = type,
      Tables = CloneTables(global?.Tables ?? DefaultTemplates.BuildTablesFor(type)),
    };
}
