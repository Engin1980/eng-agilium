using Eng.Agilium.Be.Services;

namespace Eng.Agilium.Be.Features;

/// <summary>The authenticated caller (identity only - what the caller may do is decided by <see cref="IProjectAccess"/>).</summary>
public record LoggedUser(int AppUserId, string Email);

public interface IHandler
{
  /// <summary>Called by the endpoint before <c>HandleAsync</c>.</summary>
  public void SetUpContext(LoggedUser? loggedUser, IProjectAccess access);
}

public abstract class GenericHandler<TCommand, TParameters, TResult> : IHandler
{
  private IProjectAccess? access;

  protected LoggedUser? LoggedUser { get; private set; }

  /// <summary>
  /// Authorization of the current request: call <c>Access.EnsureCanAsync(projectId, ...)</c> as soon as the
  /// project of the addressed entity is known (never take the project id from a different source than the entity).
  /// </summary>
  protected IProjectAccess Access =>
    access ?? throw new InvalidOperationException("The handler was not set up by an endpoint (no access context).");

  public abstract Task<TResult> HandleAsync(
    TCommand request,
    TParameters parameters,
    CancellationToken cancellationToken
  );

  public void SetUpContext(LoggedUser? loggedUser, IProjectAccess access)
  {
    LoggedUser = loggedUser;
    this.access = access;
  }
}
