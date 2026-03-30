using Arc.Application.Identity;
namespace Arc.Infrastructure.Identity;


/// <summary>
/// BCrypt-based password hashing service that provides secure password storage.
/// Uses BCrypt algorithm with deterministic salt generation for consistent hashing.
/// </summary>
public sealed class BCryptPasswordHashingService : IPasswordHashingService
{
    private const int WorkFactor = 12; // BCrypt work factor for security vs performance balance

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;
        
        if (string.IsNullOrWhiteSpace(hashedPassword))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            // Invalid hash format or other BCrypt errors
            return false;
        }
    }
}