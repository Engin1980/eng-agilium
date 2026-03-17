using System.ComponentModel.DataAnnotations;

namespace Eng.Agilium.Be.Exceptions.Validation;

public delegate bool ValidationCallback(object? value);

public abstract class XValidationAttribute(
  ValidationCallback validationCallback,
  XValidationErrorKey validationErrorKey
) : ValidationAttribute
{
  private readonly XValidationErrorKey validationErrorKey = validationErrorKey;
  private readonly ValidationCallback validatingCallback = validationCallback;

  protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
  {
    bool isValid = validatingCallback(value);
    if (!isValid)
      return new ValidationResult(validationErrorKey.ToString());
    else
      return ValidationResult.Success;
  }
}
