using System.ComponentModel.DataAnnotations;
using System.Net;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.AspNetCore.Mvc;

namespace Eng.Agilium.Be.Features;

public enum HttpMethod
{
  Get,
  Post,
  Patch,
  Put,
  Delete,
}

public abstract class GenericOkEndpoint<TCommand, TParameters, THandler, TResult>
  : GenericEndpoint<TCommand, TParameters, THandler>
  where THandler : GenericHandler<TCommand, TParameters, TResult>
  where TParameters : notnull
{
  protected override async Task<IResult> ProcessRequestAsync(
    TCommand command,
    TParameters parameters,
    HttpContext httpContext,
    THandler handler,
    CancellationToken cancellationToken
  )
  {
    var result = await handler.HandleAsync(command, parameters, cancellationToken);
    if (result is EmptyResult)
      return Results.NoContent();
    else
      return Results.Ok(result);
  }
}

public abstract class GenericCreatedEndpoint<TCommand, TParameters, THandler, TResult>
  : GenericEndpoint<TCommand, TParameters, THandler>
  where THandler : GenericHandler<TCommand, TParameters, TResult>
  where TParameters : notnull
{
  protected override async Task<IResult> ProcessRequestAsync(
    TCommand command,
    TParameters parameters,
    HttpContext httpContext,
    THandler handler,
    CancellationToken cancellationToken
  )
  {
    var id = await handler.HandleAsync(command, parameters, cancellationToken);
    return Results.Created($"{FullRoute}/{id}", id);
  }
}

public interface IEndpoint
{
  void Map(WebApplication app);
}

public abstract class GenericEndpoint<TCommand, TParameters, THandler> : IEndpoint
  where THandler : IHandler
  where TParameters : notnull
{
  public abstract HttpMethod Method { get; }
  public abstract BaseRoute BaseRoute { get; }
  public abstract string EndpointRoute { get; }
  public abstract string[] RequiredRoles { get; }
  public string FullRoute => $"{BaseRoute.ToRouteString().TrimEnd('/')}/{EndpointRoute.TrimStart('/')}";

  protected LoggedUser? LoggedUser { get; private set; }

  private Task<IResult> InvokeProcessRequestAsync(
    TCommand command,
    TParameters parameters,
    HttpContext httpContext,
    THandler handler,
    CancellationToken cancellationToken
  )
  {
    ValidateObject(command, typeof(TCommand), ValidatedObject.Command);
    ValidateObject(parameters, typeof(TParameters), ValidatedObject.Parameters);
    SetUpLoggedUser(httpContext, handler);
    //ValidateRequiredRoles();
    var result = ProcessRequestAsync(command, parameters, httpContext, handler, cancellationToken);
    return result;
  }

  // private void ValidateRequiredRoles()
  // {
  //   if (RequiredRoles.Length > 0)
  //   {
  //     if (LoggedUser == null)
  //       throw new AuthorizationFailedException();

  //     if (!RequiredRoles.Any(q => q == LoggedUser.RoleName))
  //       throw new AuthorizationFailedException();
  //   }
  // }

  private static void ValidateObject(object? obj, Type objType, ValidatedObject objectType)
  {
    if (obj == null)
    {
      string[] props = [.. objType.GetProperties().Select(p => p.Name)];
      if (props.Length == 0)
        return;
      else
      {
        string propNames = string.Join(", ", props);
        throw new Exceptions.ValidationException(
          objectType,
          [$"The {objectType.ToString().ToLower()} cannot be empty."]
        );
      }
    }

    var context = new ValidationContext(obj);
    var results = new List<ValidationResult>();

    Validator.TryValidateObject(obj, context, results, validateAllProperties: true);

    var errors = results.Select(r => r.ErrorMessage ?? "Validation error.").ToList();
    if (errors.Count > 0)
    {
      throw new Exceptions.ValidationException(objectType, errors.ToArray());
    }
  }

  private void SetUpLoggedUser(HttpContext httpContext, THandler handler)
  {
    var user = httpContext.User;
    if (user.Identity is not null && user.Identity.IsAuthenticated)
    {
      var idClaim = user.FindFirst("sub")?.Value;
      var email = user.FindFirst("email")?.Value ?? "";
      var roleNames = user.FindAll("role").Select(c => c.Value).ToArray();
      bool isSuperAdmin = roleNames.Contains("SuperAdmin");

      var roleAssignments = roleNames
        .Where(q => q.StartsWith('P'))
        .Select(q => IRoleAssignment.FromRoleString(q))
        .ToArray();

      if (!int.TryParse(idClaim, out var appUserId))
        appUserId = 0;

      LoggedUser = new LoggedUser(appUserId, email, isSuperAdmin, roleAssignments);
      handler.SetUpLoggedUser(this.LoggedUser);
    }
  }

  protected abstract Task<IResult> ProcessRequestAsync(
    TCommand command,
    TParameters parameters,
    HttpContext httpContext,
    THandler handler,
    CancellationToken cancellationToken
  );

  public void Map(WebApplication webApp)
  {
    Delegate handlerFunc = (
      [FromBody] TCommand command,
      [AsParameters] TParameters parameters,
      HttpContext httpContext,
      [FromServices] THandler handler,
      CancellationToken cancellationToken
    ) => InvokeProcessRequestAsync(command, parameters, httpContext, handler, cancellationToken);

    var groupName = this.BaseRoute.ToRouteString();
    var group = webApp.MapGroup("").WithTags(groupName);
    RouteHandlerBuilder rhb = Method switch
    {
      HttpMethod.Get => group.MapGet(FullRoute, handlerFunc),
      HttpMethod.Post => group.MapPost(FullRoute, handlerFunc),
      HttpMethod.Put => group.MapPut(FullRoute, handlerFunc),
      HttpMethod.Delete => group.MapDelete(FullRoute, handlerFunc),
      HttpMethod.Patch => group.MapPatch(FullRoute, handlerFunc),
      _ => throw new ArgumentOutOfRangeException(),
    };
    if (
      this.GetType().GetCustomAttributes(typeof(EndpointSummaryAttribute), true).FirstOrDefault()
      is EndpointSummaryAttribute endpointSummaryAttribute
    )
    {
      rhb.WithSummary(endpointSummaryAttribute.Summary);
    }

    if (
      this.GetType().GetCustomAttributes(typeof(EndpointDescriptionAttribute), true).FirstOrDefault()
      is EndpointDescriptionAttribute endpointDescriptionAttribute
    )
    {
      rhb.WithDescription(endpointDescriptionAttribute.Description);
    }
  }

  protected static bool IsLocalhost(HttpContext httpContext)
  {
    ArgumentNullException.ThrowIfNull(httpContext);

    var connection = httpContext.Connection;
    if (connection.RemoteIpAddress != null)
    {
      return connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
        || IPAddress.IsLoopback(connection.RemoteIpAddress);
    }
    return false;
  }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
sealed class EndpointSummaryAttribute(string summary) : Attribute
{
  public string Summary { get; } = summary;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
sealed class EndpointDescriptionAttribute(string description) : Attribute
{
  public string Description { get; } = description;
}

public enum BaseRoute
{
  Health = 0,
  Admins = 1,
  Auth = 2,
  Projects = 3,
  Items = 4,
}

public static class BaseRouteExtensions
{
  private const string rootRoute = "/api/v1";

  public static string ToRouteString(this BaseRoute baseRoute)
  {
    return baseRoute switch
    {
      BaseRoute.Health => $"{rootRoute}/health",
      BaseRoute.Admins => $"{rootRoute}/admins",
      BaseRoute.Auth => $"{rootRoute}/auth",
      BaseRoute.Projects => $"{rootRoute}/projects",
      BaseRoute.Items => $"{rootRoute}/items",
      _ => throw new ArgumentOutOfRangeException(nameof(baseRoute), baseRoute, null),
    };
  }
}
