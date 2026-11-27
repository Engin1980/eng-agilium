using Eng.Agilium.Be.Services;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Comments.List;

/// <summary>Without <c>TemplateItemId</c> the comments of all Comments elements of the item are returned.</summary>
public record Parameters(int Id, int? TemplateItemId);

public record CommentRecord(
  int Id,
  int TemplateItemId,
  int AuthorId,
  string AuthorName,
  string Text,
  DateTime CreatedAt,
  DateTime? EditedAt,
  bool CanEdit,
  bool CanDelete
);

public record Result(List<CommentRecord> Comments);

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, Parameters, Result>
{
  public override async Task<Result> HandleAsync(
    EmptyCommand command,
    Parameters parameters,
    CancellationToken cancellationToken
  )
  {
    var item =
      await dbContext.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Item), parameters.Id);

    var access = await Access.EnsureCanAsync(item.ProjectId, ProjectPermission.ViewProject, cancellationToken);
    var userId = LoggedUser?.AppUserId ?? throw new InvalidOperationException("Login is required.");

    var rows = await dbContext.ItemComments //
      .AsNoTracking()
      .Where(c => c.ItemId == item.Id && (parameters.TemplateItemId == null || c.TemplateItemId == parameters.TemplateItemId))
      .OrderBy(c => c.CreatedAt)
      .ThenBy(c => c.Id)
      .Select(c => new { c.Id, c.TemplateItemId, c.AuthorId, c.Author.Name, c.Author.Surname, c.Text, c.CreatedAt, c.EditedAt })
      .ToListAsync(cancellationToken);

    return new Result(
      rows.Select(c => new CommentRecord(
          c.Id,
          c.TemplateItemId,
          c.AuthorId,
          $"{c.Name} {c.Surname}".Trim(),
          c.Text,
          c.CreatedAt,
          c.EditedAt,
          CanEdit: c.AuthorId == userId && access.CanEditItems,
          CanDelete: c.AuthorId == userId || access.CanManageProject
        ))
        .ToList()
    );
  }
}

[EndpointSummary("Lists the comments of one Comments element of an item (oldest first)")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, Parameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "items/{id}/comments";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.ViewProject);
}
