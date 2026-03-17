namespace Eng.Agilium.Be.Exceptions.Validation;

public class XStudyNumberValidationAttribute : XRegexAttribute
{
  private const string StudyNumberPattern = @"^[a-zA-Z][0-9]{5}$";

  public XStudyNumberValidationAttribute()
    : base(StudyNumberPattern, true, XValidationErrorKey.INVALID_STUDY_NUMBER_FORMAT) { }
}
