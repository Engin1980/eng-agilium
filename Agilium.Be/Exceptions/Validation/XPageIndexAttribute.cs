namespace Eng.Agilium.Be.Exceptions.Validation;

public class XPageIndexAttribute : XValidationAttribute
{
  public XPageIndexAttribute()
    : base(q => q is null || (q is int intValue && intValue >= 0), XValidationErrorKey.INVALID_PAGE_INDEX) { }
}
