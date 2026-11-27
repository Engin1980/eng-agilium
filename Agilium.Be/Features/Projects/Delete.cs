using System.Data;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Projects.Delete;

public class Handler(AppDbContext dbContext, IAccessCache accessCache)
  : GenericHandler<EmptyCommand, IdParameters, EmptyResult>
{
  public override async Task<EmptyResult> HandleAsync(
    EmptyCommand command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    // deleting the project is reserved for the owner (see .docs/development/roles.md)
    await Access.EnsureOwnerAsync(parameters.Id, cancellationToken);
    var projectId = parameters.Id;

    using (var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
    {
      // the model uses DeleteBehavior.Restrict, so the rows are removed explicitly, from leaves to the root
      // (the template tree Template -> Table -> Column -> Section -> Item cascades in the database)
      await dbContext.ItemComments.Where(c => c.Item.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
      await dbContext.ItemFieldValues.Where(v => v.Item.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
      await dbContext.SprintItems.Where(s => s.Sprint.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
      await dbContext.Items.Where(i => i.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
      await dbContext.Sprints.Where(s => s.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
      await dbContext.Templates.Where(t => t.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
      await dbContext.WorkflowStates.Where(w => w.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
      await dbContext.ProjectInvitations.Where(i => i.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
      await dbContext.Memberships.Where(m => m.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
      await dbContext.Roles.Where(r => r.ProjectId == projectId).ExecuteDeleteAsync(cancellationToken);
      var deleted = await dbContext.Projects.Where(p => p.Id == projectId).ExecuteDeleteAsync(cancellationToken);
      if (deleted == 0)
        throw new EntityNotFoundException(typeof(Project), projectId);

      await tx.CommitAsync(cancellationToken);
    }

    // after the commit, so a concurrent request cannot cache the old state again
    accessCache.InvalidateProject(projectId);

    return new EmptyResult();
  }
}

[EndpointSummary("Permanently deletes a project with all its data (owner only)")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Delete;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}";
  public override EndpointPolicy Policy => EndpointPolicy.ProjectOwner;
}
