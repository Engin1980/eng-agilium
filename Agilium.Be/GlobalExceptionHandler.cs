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
  TOO_MANY_REQUESTS,
}

public class ExtendedProblemDetails : ProblemDetails
{
  [JsonConverter(typeof(JsonStringEnumConverter))]
  public ErrorKeys ErrorKey { get; set; }
  public string[] Params { get; set; } = [];
}

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
  private const string TYPE_400 = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1";
  private const string TYPE_403 = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3";
  private const string TYPE_404 = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4";
  private const string TYPE_409 = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8";

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

  /// <remarks>
  /// The order of the checks matters: <see cref="AuthenticationFailedException"/>,
  /// <see cref="AuthorizationFailedException"/>, <see cref="EntityNotFoundException"/> and the others derive
  /// from <see cref="BadRequestException"/>, so the specific types are matched first.
  /// </remarks>
  private static ExtendedProblemDetails ConvertBadRequestException(BadRequestException bre)
  {
    return bre switch
    {
      AccountDeactivatedException ade => Create(
        StatusCodes.Status400BadRequest,
        "Bad Request",
        TYPE_400,
        ade.Message,
        ErrorKeys.ACCOUNT_DEACTIVATED
      ),
      AccountLockedException ale => Create(
        StatusCodes.Status400BadRequest,
        "Bad Request",
        TYPE_400,
        ale.Message,
        ErrorKeys.ACCOUNT_LOCKED
      ),
      AuthenticationFailedException afe => Create(
        StatusCodes.Status401Unauthorized,
        "Unauthorized",
        TYPE_400,
        afe.Message,
        ErrorKeys.AUTHENTICATION_FAILED,
        [
          // we don't want to leak whether the email was not found or the credentials were invalid
          afe.Reason == AuthenticationFailedException.FailureReason.EmailNotFound
            ? AuthenticationFailedException.FailureReason.CredentialsInvalid.ToString()
            : afe.Reason.ToString(),
        ]
      ),
      AuthorizationFailedException aufe => Create(
        StatusCodes.Status403Forbidden,
        "Forbidden",
        TYPE_403,
        aufe.Message,
        ErrorKeys.AUTHORIZATION_FAILED
      ),
      EntityAlreadyExistsException eaee => Create(
        StatusCodes.Status409Conflict,
        "Conflict",
        TYPE_409,
        eaee.Message,
        ErrorKeys.ENTITY_ALREADY_EXISTS,
        [eaee.EntityType.Name]
      ),
      EntityNotFoundException enfe => Create(
        StatusCodes.Status404NotFound,
        "Not Found",
        TYPE_404,
        enfe.Message,
        ErrorKeys.ENTITY_NOT_FOUND,
        [enfe.EntityType.Name, enfe.Identifier]
      ),
      TooManyRequestsException tmre => Create(
        StatusCodes.Status429TooManyRequests,
        "Too Many Requests",
        "https://datatracker.ietf.org/doc/html/rfc6585#section-4",
        tmre.Message,
        ErrorKeys.TOO_MANY_REQUESTS
      ),
      TurnstileVerificationFailedException tvfe => Create(
        StatusCodes.Status403Forbidden,
        "Forbidden",
        TYPE_403,
        tvfe.Message,
        ErrorKeys.GENERIC_BAD_REQUEST,
        tvfe.ErrorCodes
      ),
      ValidationException ve => Create(
        StatusCodes.Status400BadRequest,
        "Bad Request",
        TYPE_400,
        ve.Message,
        ve.ValidatedObject switch
        {
          ValidatedObject.Command => ErrorKeys.COMMAND_VALIDATION_EXCEPTION,
          ValidatedObject.Parameters => ErrorKeys.PARAMETERS_VALIDATION_EXCEPTION,
          _ => ErrorKeys.GENERIC_BAD_REQUEST,
        },
        ve.ValidationErrors
      ),
      _ => Create(StatusCodes.Status400BadRequest, "Bad Request", TYPE_400, bre.Message, ErrorKeys.GENERIC_BAD_REQUEST),
    };
  }

  private static ExtendedProblemDetails Create(
    int status,
    string title,
    string type,
    string detail,
    ErrorKeys errorKey,
    string[]? parameters = null
  ) =>
    new()
    {
      Status = status,
      Title = title,
      Type = type,
      Detail = detail,
      ErrorKey = errorKey,
      Params = parameters ?? [],
    };

  private static ExtendedProblemDetails ConvertServerException(ServerException se)
  {
    if (se is ServiceUnreachableException sue)
      return Create(
        StatusCodes.Status503ServiceUnavailable,
        "Service Unavailable",
        "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.4",
        sue.Message,
        ErrorKeys.SERVICE_UNREACHABLE,
        [sue.ServiceType.ToString()]
      );

    return Create(
      StatusCodes.Status500InternalServerError,
      "Server Error",
      "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
      se.Message,
      ErrorKeys.INTERNAL_SERVER_ERROR
    );
  }

  private ExtendedProblemDetails ConvertUnhandledException(Exception exception)
  {
    if (exception is NotImplementedException nie)
      return Create(
        StatusCodes.Status501NotImplemented,
        "Not Implemented",
        "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.2",
        nie.Message,
        ErrorKeys.INTERNAL_SERVER_ERROR
      );

    logger.LogError(exception, "Došlo k neošetřené chybě: {Message}", exception.Message);
    return Create(
      StatusCodes.Status500InternalServerError,
      "Server Error",
      "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
      exception.Message,
      ErrorKeys.INTERNAL_SERVER_ERROR
    );
  }
}
