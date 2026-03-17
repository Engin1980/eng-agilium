namespace Eng.Agilium.Be.Exceptions.Validation;

public class XEmailValidationAttribute : XRegexAttribute
{
  private const string EmailPattern = @"^.+@.+\..+$";

  public XEmailValidationAttribute()
    : base(EmailPattern, true, XValidationErrorKey.INVALID_EMAIL_FORMAT) { }
}
