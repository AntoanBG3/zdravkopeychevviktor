using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using VacationManager.Attributes;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Controllers;

[RequireRoles]
public class VacationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public VacationsController(ApplicationDbContext context) => _context = context;

    private User user() => (User)HttpContext.Items["CurrentUser"]!;

    // GET: Vacations?from=&page=&pageSize= – моите заявки
    public async Task<IActionResult> Index(DateTime? from, int page = 1, int pageSize = 10)
    {
        var me = user();
        var query = _context.VacationRequests
            .Include(v => v.Requester)
            .Where(v => v.RequesterId == me.Id);

        if (from.HasValue)
        {
            query = query.Where(v => v.CreatedOn >= from.Value.Date);
        }

        var items = await query
            .OrderByDescending(v => v.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var total = await query.CountAsync();

        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        return View(items);
    }

    // GET: Vacations/Approve – заявки за одобрение (CEO: всички; Team Lead: на своя екип)
    public async Task<IActionResult> Approve(DateTime? from, int page = 1, int pageSize = 10)
    {
        var me = user();
        var roleName = me.Role.Name;

        if (roleName != Role.Ceo && roleName != Role.TeamLead)
        {
            return Forbid();
        }

        var query = _context.VacationRequests
            .Include(v => v.Requester)
            .ThenInclude(u => u.Team)
            .Where(v => !v.Approved && v.RequesterId != me.Id);

        if (roleName == Role.TeamLead)
        {
            query = query.Where(v => v.Requester.TeamId == me.TeamId);
        }

        if (from.HasValue)
        {
            query = query.Where(v => v.CreatedOn >= from.Value.Date);
        }

        var items = await query
            .OrderByDescending(v => v.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var total = await query.CountAsync();

        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        return View(items);
    }

    // GET: Vacations/Create
    public IActionResult Create() => View(new VacationForm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VacationForm form)
    {
        // болничният отпуск не поддържа половин ден
        if (form.Type == VacationType.Sick && form.HalfDay)
        {
            ModelState.AddModelError(nameof(VacationForm.HalfDay), "За болничен отпуск не се поддържа половин ден.");
        }

        // болничен отпуск изисква прикачен файл
        if (form.Type == VacationType.Sick)
        {
            if (form.SickNote is null || form.SickNote.Length == 0)
            {
                ModelState.AddModelError(nameof(VacationForm.SickNote), "За болничен отпуск е задължителен прикачен болничен лист.");
            }
        }

        if (form.FromDate > form.ToDate)
        {
            ModelState.AddModelError(nameof(VacationForm.ToDate), "Крайната дата не може да е преди началната.");
        }

        if (ModelState.IsValid)
        {
            var request = new VacationRequest
            {
                Type = form.Type,
                FromDate = form.FromDate,
                ToDate = form.ToDate,
                CreatedOn = DateTime.Today,
                HalfDay = form.HalfDay,
                Approved = false,
                RequesterId = user().Id,
            };

            if (form.Type == VacationType.Sick && form.SickNote is { Length: > 0 })
            {
                using var ms = new MemoryStream();
                await form.SickNote.CopyToAsync(ms);
                request.SickNoteData = ms.ToArray();
                request.SickNoteFileName = form.SickNote.FileName;
                request.SickNoteContentType = form.SickNote.ContentType;
            }

            _context.Add(request);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(form);
    }

    // GET: Vacations/Edit/5 – само собственик, докато не е одобрена
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var request = await _context.VacationRequests.FindAsync(id);
        if (request == null) return NotFound();
        if (!CanModify(request)) return Forbid();

        var form = new VacationForm
        {
            Type = request.Type,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            HalfDay = request.HalfDay,
        };
        ViewBag.Id = request.Id;
        ViewBag.HasSickNote = request.SickNoteData != null;
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VacationForm form)
    {
        var request = await _context.VacationRequests.FindAsync(id);
        if (request == null) return NotFound();
        if (!CanModify(request)) return Forbid();

        if (form.Type == VacationType.Sick && form.HalfDay)
        {
            ModelState.AddModelError(nameof(VacationForm.HalfDay), "За болничен отпуск не се поддържа половин ден.");
        }

        if (form.FromDate > form.ToDate)
        {
            ModelState.AddModelError(nameof(VacationForm.ToDate), "Крайната дата не може да е преди началната.");
        }

        if (ModelState.IsValid)
        {
            request.Type = form.Type;
            request.FromDate = form.FromDate;
            request.ToDate = form.ToDate;
            request.HalfDay = form.HalfDay;

            if (form.Type == VacationType.Sick && form.SickNote is { Length: > 0 })
            {
                using var ms = new MemoryStream();
                await form.SickNote.CopyToAsync(ms);
                request.SickNoteData = ms.ToArray();
                request.SickNoteFileName = form.SickNote.FileName;
                request.SickNoteContentType = form.SickNote.ContentType;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Id = id;
        ViewBag.HasSickNote = request.SickNoteData != null;
        return View(form);
    }

    // GET: Vacations/Delete/5 – само собственик, докато не е одобрена
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var request = await _context.VacationRequests
            .Include(v => v.Requester)
            .FirstOrDefaultAsync(v => v.Id == id);
        if (request == null) return NotFound();
        if (!CanModify(request)) return Forbid();
        return View(request);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var request = await _context.VacationRequests.FindAsync(id);
        if (request == null) return NotFound();
        if (!CanModify(request)) return Forbid();

        _context.VacationRequests.Remove(request);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: Vacations/DownloadNote/5
    public async Task<IActionResult> DownloadNote(int id)
    {
        var request = await _context.VacationRequests.FindAsync(id);
        if (request == null || request.SickNoteData == null) return NotFound();
        return File(request.SickNoteData, request.SickNoteContentType ?? "application/octet-stream", request.SickNoteFileName ?? "sick-note");
    }

    // POST: Vacations/ApproveRequest/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(int id)
    {
        var me = user();
        var request = await _context.VacationRequests
            .Include(v => v.Requester)
            .FirstOrDefaultAsync(v => v.Id == id);
        if (request == null) return NotFound();

        var roleName = me.Role.Name;
        var isCeo = roleName == Role.Ceo;
        var isTeamLeadOfRequester = roleName == Role.TeamLead
            && me.TeamId != null
            && request.Requester.TeamId == me.TeamId;

        // лидерът не може да одобрява собствената си заявка
        if (request.RequesterId == me.Id)
        {
            return Forbid();
        }

        if (!isCeo && !isTeamLeadOfRequester)
        {
            return Forbid();
        }

        request.Approved = true;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Approve));
    }

    private bool CanModify(VacationRequest request)
        => request.RequesterId == user().Id && !request.Approved;
}

public class VacationForm
{
    [Required]
    [Display(Name = "Тип")]
    public VacationType Type { get; set; } = VacationType.Paid;

    [Required, DataType(DataType.Date)]
    [Display(Name = "От")]
    public DateTime FromDate { get; set; } = DateTime.Today;

    [Required, DataType(DataType.Date)]
    [Display(Name = "До")]
    public DateTime ToDate { get; set; } = DateTime.Today;

    [Display(Name = "Половин ден")]
    public bool HalfDay { get; set; }

    [Display(Name = "Болничен лист")]
    public IFormFile? SickNote { get; set; }

    public static IEnumerable<SelectListItem> TypeOptions => new[]
    {
        new SelectListItem("Платен", ((int)VacationType.Paid).ToString()),
        new SelectListItem("Неплатен", ((int)VacationType.Unpaid).ToString()),
        new SelectListItem("Болничен", ((int)VacationType.Sick).ToString()),
    };
}
