using Microsoft.EntityFrameworkCore;
using VacationManager.Data;

namespace VacationManager.Middleware;

/// <summary>
/// Зарежда текущия потребител (по claim NameIdentifier) в HttpContext.Items
/// за употреба от RequireRolesAttribute и контролерите.
/// </summary>
public class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && int.TryParse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            var user = await db.Users
                .Include(u => u.Role)
                .Include(u => u.Team)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                context.Items["CurrentUser"] = user;
            }
        }

        await _next(context);
    }
}
