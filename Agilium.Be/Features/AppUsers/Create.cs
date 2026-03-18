using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;

namespace Eng.Agilium.Be.Features.AppUsers;

public record Command(string Email, string Name, string Surname);

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
    if (dbContext.AppUsers.Any(u => u.Email == command.Email))
      throw new EntityAlreadyExistsException(typeof(AppUser), command.Email);

    var user = new AppUser
    {
      Email = command.Email,
      Name = command.Name,
      Surname = command.Surname,
      PasswordHash = string.Empty,
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
