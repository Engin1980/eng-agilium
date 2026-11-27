namespace Eng.Agilium.Be.Exceptions;

/// <summary>The account exists and the password is right, but the e-mail address was not confirmed yet.</summary>
public class EmailNotVerifiedException() : BadRequestException("E-mail address is not verified.");

/// <summary>An e-mail verification / password reset token is unknown, used or expired (the cases are not distinguished).</summary>
public class InvalidTokenException() : BadRequestException("The link is invalid or has expired.");
