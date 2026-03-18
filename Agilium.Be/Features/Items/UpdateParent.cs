using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Items.UpdateParent;

public record Command(int? ParentId);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, IdParameters, EmptyResult>
{
  private readonly AppDbContext dbContext = dbContext;

  public override async Task<EmptyResult> HandleAsync(
    Command command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var item =
      await dbContext.Items.FirstOrDefaultAsync(i => i.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Item), parameters.Id);

    if (command.ParentId is int pId)
    {
      if (pId == item.Id)
        throw new BadRequestException("Item cannot be parent of itself");

      var parent =
        await dbContext.Items.FirstOrDefaultAsync(i => i.Id == pId, cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Item), pId);

      if (parent.ProjectId != item.ProjectId)
        throw new BadRequestException("Parent item does not belong to the same project");

      item.ParentId = pId;
    }
    else
    {
      item.ParentId = null;
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    return new EmptyResult();
  }
}

[EndpointSummary("Updates parent of an item by id")]
public class Endpoint : GenericOkEndpoint<Command, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Patch;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "items/{id}/parent";
  public override string[] RequiredRoles => [];
}
