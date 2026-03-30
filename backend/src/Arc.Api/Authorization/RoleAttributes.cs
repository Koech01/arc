using Arc.Domain.Models;
using Microsoft.AspNetCore.Authorization;


namespace Arc.Api.Authorization;
/// <summary>
/// Authorization attribute that requires the user to have a specific role.
/// </summary>
public class RequireRoleAttribute : AuthorizeAttribute
{
    public RequireRoleAttribute(UserRole role)
    {
        Roles = role.ToRoleString();
    }

    public RequireRoleAttribute(params UserRole[] roles)
    {
        Roles = string.Join(",", roles.Select(r => r.ToRoleString()));
    }
}

/// <summary>
/// Authorization attribute that requires the user to be an administrator.
/// </summary>
public sealed class RequireAdminAttribute : RequireRoleAttribute
{
    public RequireAdminAttribute() : base(UserRole.Admin)
    {
    }
}

/// <summary>
/// Authorization attribute that allows both users and administrators.
/// </summary>
public sealed class RequireUserOrAdminAttribute : RequireRoleAttribute
{
    public RequireUserOrAdminAttribute() : base(UserRole.User, UserRole.Admin)
    {
    }
}