namespace Arc.Domain.Models;


/// <summary>
/// Defines the available user roles in the system.
/// Roles determine what actions a user can perform.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Standard user with basic access to execute tasks.
    /// </summary>
    User = 0,

    /// <summary>
    /// Administrator with full system access.
    /// </summary>
    Admin = 1
}

/// <summary>
/// Extension methods for UserRole enum.
/// </summary>
public static class UserRoleExtensions
{
    /// <summary>
    /// Converts UserRole to string representation.
    /// </summary>
    public static string ToRoleString(this UserRole role)
    {
        return role switch
        {
            UserRole.User => "User",
            UserRole.Admin => "Admin",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }

    /// <summary>
    /// Parses string to UserRole.
    /// </summary>
    public static UserRole FromRoleString(string roleString)
    {
        return roleString switch
        {
            "User" => UserRole.User,
            "Admin" => UserRole.Admin,
            _ => throw new ArgumentException($"Invalid role string: {roleString}", nameof(roleString))
        };
    }
}