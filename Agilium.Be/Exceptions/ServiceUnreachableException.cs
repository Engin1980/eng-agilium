namespace Eng.Agilium.Be.Exceptions;

public class ServiceUnreachableException(ServiceType serviceType, Exception cause)
  : ServerException("Service is unreachable", cause)
{
  public ServiceType ServiceType { get; } = serviceType;
  public Exception Cause { get; } = cause;
}

public enum ServiceType
{
  Database,
  Email,
}
