namespace Eng.Agilium.Be.Exceptions;

/// <summary>Too many attempts of a sensitive operation (e.g. guessing invitation keys); mapped to HTTP 429.</summary>
public class TooManyRequestsException(string message) : BadRequestException(message) { }
