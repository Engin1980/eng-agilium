using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Projects.ListItems;

public record ProjectAppUserResult(int Id, string Name, string Surname, string RoleName);

public record ItemResult(
  int Id,
  string Title,
  int Type,
  ProjectAppUserResult? Assignee,
  int? ParentId,
  List<ItemResult> SubItems
);

public record Result(List<ItemResult> Items);

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, IdParameters, Result>
{
  private readonly AppDbContext dbContext = dbContext;

  public override async Task<Result> HandleAsync(
    EmptyCommand command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    await dbContext.Projects.EnsureExistsAsync(parameters.Id, cancellationToken);

    var items = await dbContext
      .Items //
      .AsNoTracking()
      .Include(i => i.Assignee)
      .Where(i => i.ProjectId == parameters.Id)
      .ToListAsync(cancellationToken);

    var memberships = await dbContext
      .Memberships //
      .AsNoTracking()
      .Include(m => m.Role)
      .Where(m => m.ProjectId == parameters.Id)
      .ToListAsync(cancellationToken);

    var roleByUser = memberships.ToDictionary(m => m.UserId, m => m.Role.Title);

    var lookup = items.ToDictionary(
      i => i.Id,
      i => new ItemResult(
        i.Id,
        i.Title,
        (int)i.Type,
        i.Assignee is not null
          ? new ProjectAppUserResult(
            i.Assignee.Id,
            i.Assignee.Name,
            i.Assignee.Surname,
            roleByUser.TryGetValue(i.Assignee.Id, out var rn) ? rn : string.Empty
          )
          : null,
        i.ParentId,
        []
      )
    );

    var roots = new List<ItemResult>();

    foreach (var node in lookup.Values)
    {
      if (node.ParentId is int pId && lookup.TryGetValue(pId, out var parent))
      {
        parent.SubItems.Add(node);
      }
      else
      {
        roots.Add(node);
      }
    }

    // Optional: order children and roots by Title
    foreach (var n in lookup.Values)
      n.SubItems.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.Ordinal));

    roots.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.Ordinal));

    return new Result(roots);
  }
}

[EndpointSummary("Returns items for a project arranged as a tree")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}/items";
  public override string[] RequiredRoles => [];
}
