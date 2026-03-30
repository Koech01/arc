namespace Arc.Domain.Exceptions;


/// <summary>
/// Exception thrown when authentication operations fail.
/// </summary>
public sealed class AuthenticationException : DomainException
{
    public AuthenticationException(string message) : base(message)
    {
    }

    public AuthenticationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates an exception for invalid credentials.
    /// </summary>
    public static AuthenticationException InvalidCredentials()
    {
        return new AuthenticationException("Invalid email or password");
    }

    /// <summary>
    /// Creates an exception for inactive user account.
    /// </summary>
    public static AuthenticationException InactiveAccount()
    {
        return new AuthenticationException("User account is inactive");
    }

    /// <summary>
    /// Creates an exception for duplicate email registration.
    /// </summary>
    public static AuthenticationException DuplicateEmail(string email)
    {
        return new AuthenticationException($"User with email '{email}' already exists");
    }

    /// <summary>
    /// Creates an exception for user not found.
    /// </summary>
    public static AuthenticationException UserNotFound()
    {
        return new AuthenticationException("User not found");
    }

    /// <summary>
    /// Creates an exception for a temporarily locked account.
    /// </summary>
    public static AuthenticationException AccountLocked(DateTime lockedUntilUtc)
    {
        var remaining = (int)Math.Ceiling((lockedUntilUtc - DateTime.UtcNow).TotalMinutes);
        return new AuthenticationException($"Account is temporarily locked. Try again in {remaining} minute(s)");
    }

    /// <summary>
    /// Creates an exception for a soft-deleted account.
    /// </summary>
    public static AuthenticationException AccountDeleted()
    {
        return new AuthenticationException("This account has been removed");
    }
}