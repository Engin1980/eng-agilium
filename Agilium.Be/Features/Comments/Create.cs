using Eng.Agilium.Be.Services;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Comments.Create;

public record Command(
  int TemplateItemId,
  [property: XRegex(@"(?s).*\S.*", false, XValidationErrorKey.INVALID_TITLE)] string Text
);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, IdParameters, IdResult>
{
  public override async Task<IdResult> HandleAsync(
    Command command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var item =
      await dbContext.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Item), parameters.Id);

    await Access.EnsureCanAsync(item.ProjectId, ProjectPermission.EditItems, cancellationToken);
    var userId = LoggedUser?.AppUserId ?? throw new InvalidOperationException("Login is required.");

    var text = command.Text.Trim();
    if (text.Length > ItemComment.MaxTextLength)
      throw new BadRequestException($"A comment can have at most {ItemComment.MaxTextLength} characters");

    // the element must be a Comments element of the template of this item (its project and type)
    var isCommentsElement = await dbContext.TemplateItems.AnyAsync(
      ti =>
        ti.Id == command.TemplateItemId
        && ti.Type == TemplateItemType.Comments
        && ti.Section.Column.Table.Template.ProjectId == item.ProjectId
        && ti.Section.Column.Table.Template.Type == item.Type,
      cancellationToken
    );
    if (!isCommentsElement)
      throw new BadRequestException(
        $"Template item {command.TemplateItemId} is not a Comments element of the template of this item"
      );

    var comment = new ItemComment
    {
      ItemId = item.Id,
      TemplateItemId = command.TemplateItemId,
      AuthorId = userId,
      Text = text,
      CreatedAt = DateTime.UtcNow,
    };
    dbContext.ItemComments.Add(comment);
    await dbContext.SaveChangesAsync(cancellationToken);
    return new IdResult(comment.Id);
  }
}

[EndpointSummary("Adds a comment (Markdown text) to a Comments element of an item")]
public class Endpoint : GenericCreatedEndpoint<Command, IdParameters, Handler, IdResult>
{
  public override HttpMethod Method => HttpMethod.Post;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "items/{id}/comments";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.EditItems);
}
