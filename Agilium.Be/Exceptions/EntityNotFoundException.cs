namespace Eng.Agilium.Be.Exceptions;

public class EntityNotFoundException(Type entityType, string identifier)
  : BadRequestException($"Entity of type '{entityType.Name}' with identifier '{identifier}' was not found.")
{
  public Type EntityType { get; } = entityType;
  public string Identifier { get; } = identifier;

  public EntityNotFoundException(Type entityType, int identifier)
    : this(entityType, identifier.ToString()) { }
}
