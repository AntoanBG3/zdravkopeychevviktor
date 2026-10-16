using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using VacationManager.Models;

namespace VacationManager.Attributes;

/// <summary>
/// Разрешава достъп само до потребители с някоя от посочените роли.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireRolesAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public RequireRolesAttribute(params RoleName[] roles)
    {
        _roles = roles.Select(RoleNameToDbName).ToArray();
    }

    private static string RoleNameToDbName(RoleName role) => role switch
    {
        RoleName.CEO => Role.Ceo,
        RoleName.TeamLead => Role.TeamLead,
        RoleName.Developer => Role.Developer,
        _ => Role.Unassigned,
    };

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.Items["CurrentUser"] as User;
        if (user is null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = context.HttpContext.Request.Path });
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(user.Role.Name))
        {
            context.Result = new ForbidResult();
        }
    }
}
