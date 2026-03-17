using System.Text.Json.Serialization;
using Eng.Agilium.Be.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Eng.Agilium.Be;

public enum ErrorKeys
{
  INTERNAL_SERVER_ERROR,
  ENTITY_ALREADY_EXISTS,
  ENTITY_NOT_FOUND,
  AUTHENTICATION_FAILED,
  AUTHORIZATION_FAILED,
  ACCOUNT_LOCKED,
  ACCOUNT_DEACTIVATED,
  INVALID_STATE_OPERATION,
  GENERIC_BAD_REQUEST,
  COMMAND_VALIDATION_EXCEPTION,
  PARAMETERS_VALIDATION_EXCEPTION,
  SERVICE_UNREACHABLE,
}

public class ExtendedProblemDetails : ProblemDetails
{
  [JsonConverter(typeof(JsonStringEnumConverter))]
  public ErrorKeys ErrorKey { get; set; }
  public string[] Params { get; set; } = [];
}

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
  private readonly ILogger<GlobalExceptionHandler> logger = logger;

  public async ValueTask<bool> TryHandleAsync(
    HttpContext httpContext,
    Exception exception,
    CancellationToken cancellationToken
  )
  {
    const bool IS_HANDLED = true;
    ExtendedProblemDetails pd;

    if (exception is BadRequestException bre)
      pd = ConvertBadRequestException(bre);
    else if (exception is ServerException se)
      pd = ConvertServerException(se);
    else
      pd = ConvertUnhandledException(exception);

    httpContext.Response.StatusCode = pd.Status == null ? StatusCodes.Status500InternalServerError : pd.Status.Value;
    await httpContext.Response.WriteAsJsonAsync(pd, cancellationToken);
    return IS_HANDLED;
  }

  private ExtendedProblemDetails ConvertBadRequestException(BadRequestException bre)
  {
    ExtendedProblemDetails ret;

    // if (bre is AccountDeactivatedException ade)
    // {
    //   ret = new ExtendedProblemDetails
    //   {
    //     Status = StatusCodes.Status400BadRequest,
    //     Title = "Bad Request",
    //     Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
    //     Detail = ade.Message,
    //     ErrorKey = ErrorKeys.ACCOUNT_DEACTIVATED,
    //   };
    // }
    // else if (bre is AccountLockedException ale)
    // {
    //   ret = new ExtendedProblemDetails
    //   {
    //     Status = StatusCodes.Status400BadRequest,
    //     Title = "Bad Request",
    //     Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
    //     Detail = ale.Message,
    //     ErrorKey = ErrorKeys.ACCOUNT_LOCKED,
    //   };
    // }
    // else if (bre is AuthenticationFailedException afe)
    // {
    //   ret = new ExtendedProblemDetails
    //   {
    //     Status = StatusCodes.Status401Unauthorized,
    //     Title = "Unauthorized",
    //     Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
    //     Detail = afe.Message,
    //     ErrorKey = ErrorKeys.AUTHENTICATION_FAILED,
    //     Params =
    //     [
    //       afe.Entity?.GetType().Name.ToString() ?? "",
    //       afe.Reason == AuthenticationFailedException.FailureReason.EmailNotFound
    //       || afe.Reason == AuthenticationFailedException.FailureReason.CredentialsInvalid
    //       || afe.Reason == AuthenticationFailedException.FailureReason.StudyNumberNotFound
    //         ? AuthenticationFailedException.FailureReason.CredentialsInvalid.ToString()
    //         : afe.Reason.ToString(),
    //     ],
    //     // we don't want to leak whether the email was not found or the credentials were invalid, so we return "CredentialsInvalid" for both cases. For other failure reasons, we return the actual reason.
    //   };
    // }
    // else if (bre is AuthorizationFailedException aufe)
    // {
    //   ret = new ExtendedProblemDetails
    //   {
    //     Status = StatusCodes.Status403Forbidden,
    //     Title = "Forbidden",
    //     Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
    //     Detail = aufe.Message,
    //     ErrorKey = ErrorKeys.AUTHORIZATION_FAILED,
    //   };
    // }
    // else if (bre is EntityAlreadyExistsException eaee)
    // {
    //   ret = new ExtendedProblemDetails
    //   {
    //     Status = StatusCodes.Status409Conflict,
    //     Title = "Conflict",
    //     Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
    //     Detail = eaee.Message,
    //     ErrorKey = ErrorKeys.ENTITY_ALREADY_EXISTS,
    //     Params = [eaee.EntityType.Name],
    //   };
    // }
    // else if (bre is EntityNotFoundException enfe)
    // {
    //   ret = new ExtendedProblemDetails
    //   {
    //     Status = StatusCodes.Status404NotFound,
    //     Title = "Not Found",
    //     Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
    //     Detail = enfe.Message,
    //     ErrorKey = ErrorKeys.ENTITY_NOT_FOUND,
    //     Params = [enfe.EntityType.Name, enfe.Identifier],
    //   };
    // }
    // else if (bre is InvalidStateException ise)
    // {
    //   ret = new ExtendedProblemDetails
    //   {
    //     Status = StatusCodes.Status422UnprocessableEntity,
    //     Title = "Unprocessable Entity",
    //     Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
    //     Detail = ise.Message,
    //     ErrorKey = ErrorKeys.INVALID_STATE_OPERATION,
    //     Params = [ise.Error.ToString()],
    //   };
    // }
    // else if (bre is TurnstileVerificationFailedException tvfe)
    // {
    //   ret = new ExtendedProblemDetails
    //   {
    //     Status = StatusCodes.Status403Forbidden,
    //     Title = "Forbidden",
    //     Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
    //     Detail = tvfe.Message,
    //     ErrorKey = ErrorKeys.GENERIC_BAD_REQUEST,
    //     Params = tvfe.ErrorCodes,
    //   };
    // }
    // else if (bre is ValidationException ve)
    // {
    //   ret = new ExtendedProblemDetails
    //   {
    //     Status = StatusCodes.Status400BadRequest,
    //     Title = "Bad Request",
    //     Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
    //     Detail = ve.Message,
    //     ErrorKey = ve.ValidatedObject switch
    //     {
    //       ValidatedObject.Command => ErrorKeys.COMMAND_VALIDATION_EXCEPTION,
    //       ValidatedObject.Parameters => ErrorKeys.PARAMETERS_VALIDATION_EXCEPTION,
    //       _ => ErrorKeys.GENERIC_BAD_REQUEST,
    //     },
    //     Params = ve.ValidationErrors,
    //   };
    // }
    // else
    ret = new ExtendedProblemDetails
    {
      Status = StatusCodes.Status400BadRequest,
      Title = "Bad Request",
      Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
      Detail = bre.Message,
      ErrorKey = ErrorKeys.GENERIC_BAD_REQUEST,
    };

    return ret;
  }

  private static ExtendedProblemDetails ConvertServerException(ServerException se)
  {
    ExtendedProblemDetails ret;

    //   if (se is ServiceUnreachableException sue)
    //   {
    //     ret = new ExtendedProblemDetails
    //     {
    //       Status = StatusCodes.Status503ServiceUnavailable,
    //       Title = "Service Unavailable",
    //       Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.4",
    //       Detail = sue.Message,
    //       ErrorKey = ErrorKeys.SERVICE_UNREACHABLE,
    //       Params = [sue.ServiceType.ToString()],
    //     };
    //   }
    //   else
    ret = new ExtendedProblemDetails
    {
      Status = StatusCodes.Status500InternalServerError,
      Title = "Server Error",
      Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
      Detail = se.Message,
      ErrorKey = ErrorKeys.INTERNAL_SERVER_ERROR,
    };

    return ret;
  }

  private ExtendedProblemDetails ConvertUnhandledException(Exception exception)
  {
    ExtendedProblemDetails ret;
    if (exception is NotImplementedException nie)
    {
      ret = new ExtendedProblemDetails
      {
        Status = StatusCodes.Status501NotImplemented,
        Title = "Not Implemented",
        Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.2",
        Detail = nie.Message,
        ErrorKey = ErrorKeys.INTERNAL_SERVER_ERROR,
      };
    }
    else
    {
      logger.LogError(exception, "Došlo k neošetřené chybě: {Message}", exception.Message);

      ret = new ExtendedProblemDetails
      {
        Status = StatusCodes.Status500InternalServerError,
        Title = "Server Error",
        Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
        Detail = exception.Message,
        ErrorKey = ErrorKeys.INTERNAL_SERVER_ERROR,
      };
    }

    return ret;
  }
}
