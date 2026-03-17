namespace Eng.Agilium.Be.Exceptions.Validation;

public class XTokenValidation : XRegexAttribute
{
  private const string TokenPattern = @"^.+$";

  public XTokenValidation()
    : base(TokenPattern, true, XValidationErrorKey.INVALID_TOKEN_FORMAT) { }
}
