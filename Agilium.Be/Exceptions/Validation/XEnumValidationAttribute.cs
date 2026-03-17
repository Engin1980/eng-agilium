using System.ComponentModel.DataAnnotations;

namespace Eng.Agilium.Be.Exceptions.Validation;

public class XEnumValidationAttribute : ValidationAttribute
{
  protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
  {
    if (value == null)
    {
      return ValidationResult.Success; // Consider null as valid. Use [Required] for non-nullable.
    }

    Type valueType = value.GetType();
    if (!valueType.IsEnum)
    {
      return new ValidationResult($"Value must be an enum type, but was {valueType.Name}.");
    }

    //bool isValid = Enum.IsDefined(valueType, value);

    if (value.GetType().IsEnum == false)
    {
      return new ValidationResult($"Value must be of type enum type.");
    }

    if (!Enum.IsDefined(valueType, value))
    {
      return new ValidationResult($"Value '{value}' is not a valid member of {valueType.Name}.");
    }

    return ValidationResult.Success;
  }
}
