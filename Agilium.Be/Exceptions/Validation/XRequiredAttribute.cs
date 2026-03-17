namespace Eng.Agilium.Be.Exceptions.Validation;

public class XNotNull(XValidationErrorKey validationErrorKey)
  : XValidationAttribute(q => q != null, validationErrorKey);

public class XNonEmpty(XValidationErrorKey validationErrorKey)
  : XValidationAttribute(q => q != null && (q is not string s || !string.IsNullOrWhiteSpace(s)), validationErrorKey);
