using System.Diagnostics.CodeAnalysis;

namespace Eng.Agilium.Be.Features;

public record LoggedUser(int AppUserId, string Email, string RoleName);

public interface IHandler
{
  public void SetUpLoggedUser(LoggedUser? loggedUser);
}

public abstract class GenericHandler<TCommand, TParameters, TResult> : IHandler
{
  protected LoggedUser? LoggedUser { get; private set; }
  public abstract Task<TResult> HandleAsync(
    TCommand request,
    TParameters parameters,
    CancellationToken cancellationToken
  );

  public void SetUpLoggedUser(LoggedUser? loggedUser) => this.LoggedUser = loggedUser;
}

// public class AuthValContext()
// {
//   internal LoggedUser LoggedUser { get; init; } = null!;
//   internal List<IdAndRole> Combinations { get; } = [];

//   internal void Evaluate()
//   {
//     if (Combinations.Count == 0)
//       return;

//     foreach (var combination in Combinations)
//     {
//       var hasId = combination.Ids.Length == 0 || combination.Ids.Contains(LoggedUser.AppUserId);
//       var hasRole =
//         combination.RoleNames.Length == 0 || combination.RoleNames.Any(r => LoggedUser.RoleNames.Contains(r));
//       if (hasId && hasRole)
//         return;
//     }

//     throw new AuthorizationFailedException();
//   }
// }

// public record IdAndRole(int[] Ids, string[] RoleNames);

// public class Authorizer(LoggedUser loggedUser)
// {
//   private readonly AuthValContext ctx = new AuthValContext() { LoggedUser = loggedUser };

//   public IdSelected HavingId(int id) => new(ctx, [id]);

//   public IdSelected HavingIds(IEnumerable<int> ids) => new(ctx, ids.ToArray());

//   public IdSelected HavingAnyId() => new(ctx, []);

//   public void Evaluate() => ctx.Evaluate();
// }

// public class IdSelected(AuthValContext ctx, int[] ids)
// {
//   private readonly AuthValContext ctx = ctx;
//   private readonly int[] ids = ids;

//   public IdRoleSelected WithRole(string role) => new(ctx, ids, [role]);

//   public IdRoleSelected WithRole(params string[] roles) => new(ctx, ids, roles.ToArray());

//   public IdRoleSelected WithAnyRole() => new(ctx, ids, []);
// }

// public class IdRoleSelected
// {
//   private readonly AuthValContext ctx;

//   public IdRoleSelected(AuthValContext ctx, int[] ids, string[] roles)
//   {
//     this.ctx = ctx;
//     this.ctx.Combinations.Add(new(ids, roles));
//   }

//   public IdSelected HavingId(int id) => new(ctx, [id]);

//   public IdSelected HavingIds(IEnumerable<int> ids) => new(ctx, ids.ToArray());

//   public IdSelected HavingAnyId() => new(ctx, []);

//   public void Validate() => ctx.Evaluate();
// }
