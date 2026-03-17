using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Eng.Agilium.Be.Exceptions;

public enum ValidatedObject
{
  Command,
  Parameters,
}

public class ValidationException(ValidatedObject validatedObject, string[] validationErrors)
  : BadRequestException("Validation errors occurred in " + validatedObject + ".")
{
  public string[] ValidationErrors { get; } = validationErrors;
  public ValidatedObject ValidatedObject { get; } = validatedObject;
}
