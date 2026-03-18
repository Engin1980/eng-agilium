namespace Eng.Agilium.Be.Exceptions;

public class AccountLockedException() : BadRequestException("Account is locked due to too many failed login attempts.");
