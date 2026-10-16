using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationManager.Attributes;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Controllers;

[RequireRoles(RoleName.CEO)]
public class TeamsController : Controller
{
    private readonly ApplicationDbContext _context;

    public TeamsController(ApplicationDbContext context) => _context = context;

    // GET: Teams?search=&searchBy=&page=&pageSize=
    public async Task<IActionResult> Index(string? search, string? searchBy, int page = 1, int pageSize = 10)
    {
        var query = _context.Teams.Include(t => t.Leader).Include(t => t.Members).Include(t => t.Project).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = searchBy switch
            {
                "project" => query.Where(t => t.Project!.Name.Contains(search)),
                _ => query.Where(t => t.Name.Contains(search)),
            };
        }

        var teams = await query
            .OrderBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var total = await query.CountAsync();

        ViewBag.Search = search;
        ViewBag.SearchBy = searchBy ?? "name";
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        return View(teams);
    }

    // GET: Teams/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var team = await _context.Teams
            .Include(t => t.Leader)
            .Include(t => t.Members)
            .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (team == null) return NotFound();

        ViewBag.UnassignedUsers = await _context.Users
            .Where(u => u.TeamId == null)
            .OrderBy(u => u.Username)
            .ToListAsync();
        return View(team);
    }

    // POST: Teams/Details/5  – добавяне на член
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(int id, int userId)
    {
        var team = await _context.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == id);
        var user = await _context.Users.FindAsync(userId);
        if (team == null || user == null) return NotFound();

        user.TeamId = team.Id;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Teams/RemoveMember/5?userId=2
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.TeamId != id) return NotFound();

        var team = await _context.Teams.FindAsync(id);
        if (team != null && team.LeaderId == userId)
        {
            team.LeaderId = null;
        }
        user.TeamId = null;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Teams/SetLeader/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLeader(int id, int userId)
    {
        var team = await _context.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == id);
        var user = await _context.Users.FindAsync(userId);
        if (team == null || user == null) return NotFound();

        if (user.TeamId != id)
        {
            TempData["Error"] = "Лидерът трябва да е член на екипа.";
        }
        else
        {
            team.LeaderId = user.Id;
            // TeamId на потребителя следва лидерството – така Lead-ът вижда
            // заявките на екипа си в Vacations/Approve
            user.TeamId = team.Id;
            // лидерът на екип трябва да има поне роля Team Lead
            if (user.Role.Name == Role.Developer || user.Role.Name == Role.Unassigned)
            {
                var leadRole = await _context.Roles.SingleAsync(r => r.Name == Role.TeamLead);
                user.RoleId = leadRole.Id;
            }
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: Teams/Create
    public async Task<IActionResult> Create()
    {
        await PopulateProjectsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,ProjectId")] Team team)
    {
        if (await _context.Teams.AnyAsync(t => t.Name == team.Name))
        {
            ModelState.AddModelError(nameof(Team.Name), "Екип с това име вече съществува.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(team);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        await PopulateProjectsAsync();
        return View(team);
    }

    // GET: Teams/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var team = await _context.Teams.FindAsync(id);
        if (team == null) return NotFound();
        await PopulateProjectsAsync(team.ProjectId);
        return View(team);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ProjectId")] Team team)
    {
        if (id != team.Id) return NotFound();

        if (await _context.Teams.AnyAsync(t => t.Name == team.Name && t.Id != id))
        {
            ModelState.AddModelError(nameof(Team.Name), "Екип с това име вече съществува.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _context.Teams.FindAsync(id);
                if (existing == null) return NotFound();
                existing.Name = team.Name;
                existing.ProjectId = team.ProjectId;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Teams.AnyAsync(t => t.Id == id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        await PopulateProjectsAsync(team.ProjectId);
        return View(team);
    }

    private async Task PopulateProjectsAsync(int? projectId = null)
    {
        ViewBag.Projects = await _context.Projects.OrderBy(p => p.Name).ToListAsync();
        ViewBag.SelectedProjectId = projectId;
    }

    // GET: Teams/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var team = await _context.Teams
            .Include(t => t.Members)
            .Include(t => t.Leader)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (team == null) return NotFound();
        return View(team);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var team = await _context.Teams.FindAsync(id);
        if (team != null)
        {
            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
