namespace Eng.Agilium.Be.Exceptions;

public abstract class ServerException(string message, Exception cause) : Exception(message, cause) { }
