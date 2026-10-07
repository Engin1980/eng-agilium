using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Sprints.ListBacklog;

public record BacklogItemResult(
  int Id,
  string Title,
  int Type,
  int? UserStoryId,
  string? UserStoryTitle,
  int? SprintId,
  string? SprintTitle
);

public record Result(List<BacklogItemResult> Items);

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

    var sprintByItem = await dbContext
      .SprintItems //
      .AsNoTracking()
      .Where(si => si.Sprint.ProjectId == parameters.Id)
      .Select(si => new
      {
        si.ItemId,
        si.SprintId,
        si.Sprint.Title,
      })
      .ToDictionaryAsync(x => x.ItemId, cancellationToken);

    var items = await dbContext
      .Items //
      .AsNoTracking()
      .Where(i => i.ProjectId == parameters.Id && (i.Type == ItemType.Task || i.Type == ItemType.Bug))
      .Select(i => new
      {
        i.Id,
        i.Title,
        i.Type,
        i.ParentId,
        ParentTitle = i.Parent == null ? null : i.Parent.Title,
      })
      .OrderBy(i => i.Title)
      .ToListAsync(cancellationToken);

    var result = items
      .Select(i =>
      {
        sprintByItem.TryGetValue(i.Id, out var sprint);
        return new BacklogItemResult(
          i.Id,
          i.Title,
          (int)i.Type,
          i.ParentId,
          i.ParentTitle,
          sprint?.SprintId,
          sprint?.Title
        );
      })
      .ToList();

    return new Result(result);
  }
}

[EndpointSummary("Returns all tasks/bugs of a project with the sprint (if any) they are currently assigned to")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}/backlog";
  public override string[] RequiredRoles => [];
}
