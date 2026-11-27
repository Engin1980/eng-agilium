using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.AppUsers;

public record Command(
  [property: XEmailValidation] string Email,
  string Name,
  string Surname,
  [property: XRegex(".{8,}", false, XValidationErrorKey.INVALID_PASSWORD_FORMAT)] string Password
);

public class Handler(AppDbContext dbContext, AccountMailer mailer) : GenericHandler<Command, EmptyParameters, EmptyResult>
{
  public override async Task<EmptyResult> HandleAsync(
    Command command,
    EmptyParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var email = command.Email.Trim().ToLowerInvariant();

    // The answer is the same whether or not the e-mail is already registered (no account enumeration);
    // an existing owner of the address just gets a notice by e-mail.
    var existing = await dbContext.AppUsers.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    if (existing is not null)
    {
      await mailer.SendAccountExistsAsync(existing, cancellationToken);
      return new EmptyResult();
    }

    var user = new AppUser
    {
      Email = email,
      Name = command.Name,
      Surname = command.Surname,
      PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password),
      IsActive = true,
      EmailVerifiedAt = null,
    };

    dbContext.AppUsers.Add(user);
    try
    {
      await dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException)
    {
      // a concurrent registration of the same address won the unique index
      return new EmptyResult();
    }

    await mailer.SendVerificationAsync(user, cancellationToken);
    return new EmptyResult();
  }
}

[EndpointSummary("Registers a new user and sends an e-mail with a verification link (the answer never reveals whether the e-mail exists)")]
public class Endpoint : GenericOkEndpoint<Command, EmptyParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Post;
  public override BaseRoute BaseRoute => BaseRoute.Auth;
  public override string EndpointRoute => "users";
  public override EndpointPolicy Policy => EndpointPolicy.Anonymous;
}
