using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationManager.Attributes;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Controllers;

[RequireRoles]
public class ProjectsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProjectsController(ApplicationDbContext context) => _context = context;

    // GET: Projects?search=&searchBy=&page=&pageSize=
    public async Task<IActionResult> Index(string? search, string? searchBy, int page = 1, int pageSize = 10)
    {
        var query = _context.Projects.Include(p => p.Teams).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = searchBy switch
            {
                "description" => query.Where(p => p.Description.Contains(search)),
                _ => query.Where(p => p.Name.Contains(search)),
            };
        }

        var projects = await query
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var total = await query.CountAsync();

        ViewBag.Search = search;
        ViewBag.SearchBy = searchBy ?? "name";
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        ViewBag.IsCeo = user().Role.Name == Role.Ceo;
        return View(projects);
    }

    private User user() => (User)HttpContext.Items["CurrentUser"]!;

    // GET: Projects/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var project = await _context.Projects
            .Include(p => p.Teams)
            .ThenInclude(t => t.Leader)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) return NotFound();

        ViewBag.AllTeams = await _context.Teams.OrderBy(t => t.Name).ToListAsync();
        return View(project);
    }

    // POST: Projects/Details/5 – добавяне на екип към проекта
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTeam(int id, int teamId)
    {
        var project = await _context.Projects
            .Include(p => p.Teams)
            .FirstOrDefaultAsync(p => p.Id == id);
        var team = await _context.Teams.FindAsync(teamId);
        if (project == null || team == null) return NotFound();

        project.Teams.Add(team);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Projects/RemoveTeam/5?teamId=2
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveTeam(int id, int teamId)
    {
        var project = await _context.Projects
            .Include(p => p.Teams)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) return NotFound();

        var team = project.Teams.FirstOrDefault(t => t.Id == teamId);
        if (team != null)
        {
            project.Teams.Remove(team);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: Projects/Create  (само CEO)
    [RequireRoles(RoleName.CEO)]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireRoles(RoleName.CEO)]
    public async Task<IActionResult> Create([Bind("Name,Description")] Project project)
    {
        if (await _context.Projects.AnyAsync(p => p.Name == project.Name))
        {
            ModelState.AddModelError(nameof(Project.Name), "Проект с това име вече съществува.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(project);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(project);
    }

    // GET: Projects/Edit/5  (само CEO)
    [RequireRoles(RoleName.CEO)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var project = await _context.Projects.FindAsync(id);
        if (project == null) return NotFound();
        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireRoles(RoleName.CEO)]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Project project)
    {
        if (id != project.Id) return NotFound();

        if (await _context.Projects.AnyAsync(p => p.Name == project.Name && p.Id != id))
        {
            ModelState.AddModelError(nameof(Project.Name), "Проект с това име вече съществува.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _context.Projects.FindAsync(id);
                if (existing == null) return NotFound();
                existing.Name = project.Name;
                existing.Description = project.Description;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Projects.AnyAsync(p => p.Id == id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(project);
    }

    // GET: Projects/Delete/5  (само CEO)
    [RequireRoles(RoleName.CEO)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var project = await _context.Projects
            .Include(p => p.Teams)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) return NotFound();
        return View(project);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [RequireRoles(RoleName.CEO)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project != null)
        {
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
