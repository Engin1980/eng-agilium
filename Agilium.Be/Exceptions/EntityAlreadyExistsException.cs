namespace Eng.Agilium.Be.Exceptions;

public class EntityAlreadyExistsException(Type entityType, string identifier)
  : BadRequestException($"Entity of type '{entityType.Name}' already exists with value '{identifier}'")
{
  public Type EntityType { get; } = entityType;
  public string Identifier { get; } = identifier;
}