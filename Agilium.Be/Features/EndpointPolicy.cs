using Eng.Agilium.Be.Services;

namespace Eng.Agilium.Be.Features;

public enum EndpointAccess
{
  /// <summary>No login needed (health, login, registration...).</summary>
  Anonymous,

  /// <summary>Any logged-in user (no project involved, or the handler filters by the caller itself).</summary>
  LoggedIn,

  /// <summary>A logged-in member of the addressed project with the given permission (or the owner).</summary>
  Project,
}

/// <summary>
/// What an endpoint demands. The endpoint declares it (<see cref="GenericEndpoint{TCommand,TParameters,THandler}.Policy"/>),
/// the endpoint base class enforces the login part, the handler enforces the project part via
/// <c>Access.EnsureCanAsync/EnsureOwnerAsync</c>; the authorization matrix test checks that both agree.
/// </summary>
public sealed record EndpointPolicy(EndpointAccess Access, ProjectPermission? Permission = null, bool OwnerOnly = false)
{
  public static readonly EndpointPolicy Anonymous = new(EndpointAccess.Anonymous);
  public static readonly EndpointPolicy LoggedIn = new(EndpointAccess.LoggedIn);
  public static readonly EndpointPolicy ProjectOwner = new(EndpointAccess.Project, null, true);

  public static EndpointPolicy Project(ProjectPermission permission) => new(EndpointAccess.Project, permission);

  public bool RequiresAuthentication => Access != EndpointAccess.Anonymous;
}
