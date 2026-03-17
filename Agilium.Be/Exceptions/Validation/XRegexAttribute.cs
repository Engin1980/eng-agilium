using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Eng.Agilium.Be.Exceptions.Validation;

public class XRegexAttribute(string pattern, bool ignoreCase, XValidationErrorKey validationErrorKey)
  : XValidationAttribute(q => DoValidation(q, pattern, ignoreCase), validationErrorKey)
{
  private static bool DoValidation(object? value, string pattern, bool ignoreCase)
  {
    if (value == null)
      return false;
    if (value is not string s)
      return false;
    var regexOptions = ignoreCase ? RegexOptions.Compiled | RegexOptions.IgnoreCase : RegexOptions.Compiled;
    return new Regex(pattern, regexOptions).IsMatch(s);
  }
}
