using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;

namespace Eng.Agilium.Be.Features.AppUsers;

public record Command(
  [property: XEmailValidation] string Email,
  string Name,
  string Surname,
  [property: XRegex(".{8,}", false, XValidationErrorKey.INVALID_PASSWORD_FORMAT)] string Password
);

public class Handler : GenericHandler<Command, EmptyParameters, IdResult>
{
  private readonly AppDbContext dbContext;

  public Handler(AppDbContext dbContext) => this.dbContext = dbContext;

  public override async Task<IdResult> HandleAsync(
    Command command,
    EmptyParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var email = command.Email.Trim().ToLowerInvariant();

    if (dbContext.AppUsers.Any(u => u.Email == email))
      throw new EntityAlreadyExistsException(typeof(AppUser), email);

    var user = new AppUser
    {
      Email = email,
      Name = command.Name,
      Surname = command.Surname,
      PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password),
      IsActive = true,
    };

    dbContext.AppUsers.Add(user);
    await dbContext.SaveChangesAsync(cancellationToken);

    return new IdResult(user.Id);
  }
}

[EndpointSummary("Creates a new application user and returns its id")]
public class Endpoint : GenericCreatedEndpoint<Command, EmptyParameters, Handler, IdResult>
{
  public override HttpMethod Method => HttpMethod.Post;
  public override BaseRoute BaseRoute => BaseRoute.Auth; // TODO: confirm correct base route for user management
  public override string EndpointRoute => "users";
  public override string[] RequiredRoles => Array.Empty<string>();
}
