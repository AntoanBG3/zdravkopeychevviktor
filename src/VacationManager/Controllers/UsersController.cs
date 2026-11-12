using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationManager.Attributes;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Controllers;

[RequireRoles]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;

    public UsersController(ApplicationDbContext context) => _context = context;

    // GET: Users?search=&page=&pageSize=
    public async Task<IActionResult> Index(string? search, string? searchBy, int page = 1, int pageSize = 10)
    {
        var query = _context.Users.Include(u => u.Role).Include(u => u.Team).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = searchBy switch
            {
                "firstName" => query.Where(u => u.FirstName.Contains(search)),
                "lastName" => query.Where(u => u.LastName.Contains(search)),
                "role" => query.Where(u => u.Role.Name.Contains(search)),
                _ => query.Where(u => u.Username.Contains(search)),
            };
        }

        var users = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var total = await query.CountAsync();

        ViewBag.Search = search;
        ViewBag.SearchBy = searchBy ?? "username";
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        ViewBag.IsCeo = user().Role.Name == Role.Ceo;
        return View(users);
    }

    private User user() => (User)HttpContext.Items["CurrentUser"]!;

    // GET: Users/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Team)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        return View(user);
    }

    // GET: Users/Create  (само CEO)
    [RequireRoles(RoleName.CEO)]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireRoles(RoleName.CEO)]
    public async Task<IActionResult> Create([Bind("Username,Password,FirstName,LastName,RoleId,TeamId")] UserForm user)
    {
        if (await _context.Users.AnyAsync(u => u.Username == user.Username))
        {
            ModelState.AddModelError(nameof(UserForm.Username), "Потребителското име вече съществува.");
        }

        if (ModelState.IsValid)
        {
            var entity = new User
            {
                Username = user.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Password),
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleId = user.RoleId,
                TeamId = user.TeamId,
            };
            _context.Add(entity);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdownsAsync(user.RoleId, user.TeamId);
        return View(user);
    }

    // GET: Users/Edit/5  (само CEO)
    [RequireRoles(RoleName.CEO)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        var form = new UserForm
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            RoleId = user.RoleId,
            TeamId = user.TeamId,
        };
        await PopulateDropdownsAsync(form.RoleId, form.TeamId);
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireRoles(RoleName.CEO)]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Password,FirstName,LastName,RoleId,TeamId")] UserForm form)
    {
        if (id != form.Id) return NotFound();

        if (await _context.Users.AnyAsync(u => u.Username == form.Username && u.Id != id))
        {
            ModelState.AddModelError(nameof(UserForm.Username), "Потребителското име вече съществува.");
        }

        if (ModelState.IsValid)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Username = form.Username;
            user.FirstName = form.FirstName;
            user.LastName = form.LastName;
            user.RoleId = form.RoleId;
            user.TeamId = form.TeamId;
            if (!string.IsNullOrEmpty(form.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(form.Password);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdownsAsync(form.RoleId, form.TeamId);
        return View(form);
    }

    // GET: Users/Delete/5  (само CEO)
    [RequireRoles(RoleName.CEO)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Team)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [RequireRoles(RoleName.CEO)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(int? roleId = null, int? teamId = null)
    {
        ViewBag.Roles = await _context.Roles.OrderBy(r => r.Id).ToListAsync();
        ViewBag.Teams = await _context.Teams.OrderBy(t => t.Name).ToListAsync();
        ViewBag.SelectedRoleId = roleId;
        ViewBag.SelectedTeamId = teamId;
    }
}
