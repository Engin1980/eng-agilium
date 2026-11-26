using System.ComponentModel.DataAnnotations;
using System.Net;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Services;
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
  /// <summary>What the endpoint demands from the caller (see <see cref="EndpointPolicy"/>); there is no default on purpose.</summary>
  public abstract EndpointPolicy Policy { get; }
  public string FullRoute => $"{BaseRoute.ToRouteString().TrimEnd('/')}/{EndpointRoute.TrimStart('/')}";

  private async Task<IResult> InvokeProcessRequestAsync(
    TCommand command,
    TParameters parameters,
    HttpContext httpContext,
    THandler handler,
    CancellationToken cancellationToken
  )
  {
    // authentication first, so an anonymous caller gets 401 whatever the request body looks like
    var access = httpContext.RequestServices.GetRequiredService<IProjectAccess>();
    // NOTE: one endpoint instance serves all requests, so per-request state must stay in locals
    var loggedUser = GetLoggedUser(httpContext);
    if (Policy.RequiresAuthentication)
    {
      if (loggedUser == null)
        throw new AuthenticationFailedException(AuthenticationFailedException.FailureReason.TokenNotProvided, null);
      access.Bind(loggedUser);
      await access.EnsureActiveUserAsync(cancellationToken);
    }
    else
      access.Bind(loggedUser);
    handler.SetUpContext(loggedUser, access);

    ValidateObject(command, typeof(TCommand), ValidatedObject.Command);
    ValidateObject(parameters, typeof(TParameters), ValidatedObject.Parameters);
    return await ProcessRequestAsync(command, parameters, httpContext, handler, cancellationToken);
  }

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

  private static LoggedUser? GetLoggedUser(HttpContext httpContext)
  {
    var user = httpContext.User;
    if (user.Identity is not null && user.Identity.IsAuthenticated && int.TryParse(user.FindFirst("sub")?.Value, out var id))
      return new LoggedUser(id, user.FindFirst("email")?.Value ?? "");
    return null;
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
