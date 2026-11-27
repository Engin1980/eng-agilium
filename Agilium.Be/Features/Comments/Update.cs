using Eng.Agilium.Be.Services;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Comments.Update;

public record Command([property: XRegex(@"(?s).*\S.*", false, XValidationErrorKey.INVALID_TITLE)] string Text);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, IdParameters, EmptyResult>
{
  public override async Task<EmptyResult> HandleAsync(
    Command command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var comment =
      await dbContext.ItemComments.Include(c => c.Item).FirstOrDefaultAsync(c => c.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(ItemComment), parameters.Id);

    await Access.EnsureCanAsync(comment.Item.ProjectId, ProjectPermission.EditItems, cancellationToken);
    var userId = LoggedUser?.AppUserId ?? throw new InvalidOperationException("Login is required.");

    // only the author edits a comment
    if (comment.AuthorId != userId)
      throw new AuthorizationFailedException();

    var text = command.Text.Trim();
    if (text.Length > ItemComment.MaxTextLength)
      throw new BadRequestException($"A comment can have at most {ItemComment.MaxTextLength} characters");

    comment.Text = text;
    comment.EditedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);
    return new EmptyResult();
  }
}

[EndpointSummary("Edits a comment (only its author)")]
public class Endpoint : GenericOkEndpoint<Command, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Patch;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "items/comments/{id}";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.EditItems);
}
