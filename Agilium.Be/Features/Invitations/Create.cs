using System.Security.Cryptography;
using System.Text;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Invitations.Create;

/// <param name="ExpiresAt">Default: 7 days from now; at most 90 days ahead.</param>
/// <param name="MaxUses">Default: 10; 1-1000.</param>
public record Command(int RoleId, DateTime? ExpiresAt, int? MaxUses);

/// <summary>The only response that ever contains the key.</summary>
public record Result(int Id, string Key, string KeyPrefix, DateTime ExpiresAt, int MaxUses);

public static class InvitationKeys
{
  public const int PrefixLength = 6;

  public static string Generate() => Base64Url(RandomNumberGenerator.GetBytes(24));

  public static string Hash(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key.Trim())));

  private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

public class Handler(AppDbContext dbContext, TimeProvider clock) : GenericHandler<Command, IdParameters, Result>
{
  public override async Task<Result> HandleAsync(
    Command command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var userId = LoggedUser?.AppUserId ?? throw new InvalidOperationException("Login is required.");
    await Access.EnsureCanAsync(parameters.Id, ProjectPermission.ManageMembers, cancellationToken);

    var role =
      await dbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == command.RoleId, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Role), command.RoleId);
    if (role.ProjectId != parameters.Id)
      throw new BadRequestException("Role does not belong to the project");
    if (role.SystemKind == RoleSystemKind.Owner)
      throw new BadRequestException("An owner cannot be invited; change the role of an existing member instead");

    var now = clock.GetUtcNow().UtcDateTime;
    var expiresAt = command.ExpiresAt?.ToUniversalTime() ?? now.AddDays(ProjectInvitation.DefaultValidityDays);
    if (expiresAt <= now)
      throw new BadRequestException("The expiration must be in the future");
    if (expiresAt > now.AddDays(ProjectInvitation.MaxValidityDays))
      throw new BadRequestException($"The expiration can be at most {ProjectInvitation.MaxValidityDays} days ahead");

    var maxUses = command.MaxUses ?? ProjectInvitation.DefaultMaxUses;
    if (maxUses < 1 || maxUses > ProjectInvitation.MaxMaxUses)
      throw new BadRequestException($"The number of uses must be between 1 and {ProjectInvitation.MaxMaxUses}");

    var key = InvitationKeys.Generate();
    var invitation = new ProjectInvitation
    {
      ProjectId = parameters.Id,
      RoleId = role.Id,
      KeyHash = InvitationKeys.Hash(key),
      KeyPrefix = key[..InvitationKeys.PrefixLength],
      ExpiresAt = expiresAt,
      MaxUses = maxUses,
      CreatedById = userId,
      CreatedAt = now,
    };
    dbContext.ProjectInvitations.Add(invitation);
    await dbContext.SaveChangesAsync(cancellationToken);

    return new Result(invitation.Id, key, invitation.KeyPrefix, invitation.ExpiresAt, invitation.MaxUses);
  }
}

[EndpointSummary("Creates an invitation key; the key is returned only in this response")]
public class Endpoint : GenericCreatedEndpoint<Command, IdParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Post;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}/invitations";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.ManageMembers);
}
