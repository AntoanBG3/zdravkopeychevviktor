using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationManager.Attributes;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Controllers;

[RequireRoles(RoleName.CEO)]
public class RolesController : Controller
{
    private readonly ApplicationDbContext _context;

    public RolesController(ApplicationDbContext context) => _context = context;

    // GET: Roles  – всички роли + брой потребители
    public async Task<IActionResult> Index()
    {
        var roles = await _context.Roles
            .Include(r => r.Users)
            .OrderBy(r => r.Id)
            .ToListAsync();
        return View(roles);
    }

    // GET: Roles/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var role = await _context.Roles
            .Include(r => r.Users)
            .ThenInclude(u => u.Team)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return NotFound();
        return View(role);
    }

    // GET: Roles/Create
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name")] Role role)
    {
        if (await _context.Roles.AnyAsync(r => r.Name == role.Name))
        {
            ModelState.AddModelError(nameof(Role.Name), "Роля с това име вече съществува.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(role);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(role);
    }

    // GET: Roles/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var role = await _context.Roles.FindAsync(id);
        if (role == null) return NotFound();
        return View(role);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Role role)
    {
        if (id != role.Id) return NotFound();

        if (await _context.Roles.AnyAsync(r => r.Name == role.Name && r.Id != id))
        {
            ModelState.AddModelError(nameof(Role.Name), "Роля с това име вече съществува.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _context.Roles.FindAsync(id);
                if (existing == null) return NotFound();
                existing.Name = role.Name;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Roles.AnyAsync(r => r.Id == id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(role);
    }

    // GET: Roles/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var role = await _context.Roles
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return NotFound();
        if (role.Users.Count > 0)
        {
            ViewBag.Error = "Не може да се изтрие роля, към която са назначени потребители.";
        }
        return View(role);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var role = await _context.Roles
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (role != null)
        {
            if (role.Users.Count > 0)
            {
                TempData["Error"] = "Не може да се изтрие роля, към която са назначени потребители.";
                return RedirectToAction(nameof(Delete), new { id });
            }
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
