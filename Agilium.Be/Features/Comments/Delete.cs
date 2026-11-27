using Eng.Agilium.Be.Services;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Comments.Delete;

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, IdParameters, EmptyResult>
{
  public override async Task<EmptyResult> HandleAsync(
    EmptyCommand command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var comment =
      await dbContext.ItemComments.Include(c => c.Item).FirstOrDefaultAsync(c => c.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(ItemComment), parameters.Id);

    var access = await Access.EnsureCanAsync(comment.Item.ProjectId, ProjectPermission.ViewProject, cancellationToken);
    var userId = LoggedUser?.AppUserId ?? throw new InvalidOperationException("Login is required.");

    // the author may always delete their own comment, otherwise ManageProject (owner, manager) is needed
    if (comment.AuthorId != userId && !access.CanManageProject)
      throw new AuthorizationFailedException();

    dbContext.ItemComments.Remove(comment);
    await dbContext.SaveChangesAsync(cancellationToken);
    return new EmptyResult();
  }
}

[EndpointSummary("Deletes a comment (its author, or a member who can manage the project)")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Delete;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "items/comments/{id}";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.ViewProject);
}
