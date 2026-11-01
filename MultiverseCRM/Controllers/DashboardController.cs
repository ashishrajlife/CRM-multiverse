using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiverseCRM.Data;
using MultiverseCRM.Services;

namespace MultiverseCRM.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILookupService _lookups;

    public DashboardController(AppDbContext context, ILookupService lookups)
    {
        _context = context;
        _lookups = lookups;
    }

    public async Task<IActionResult> Index()
    {
        var counts = await _context.Leads
            .GroupBy(l => l.Status)
            .Select(g => new { g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.C);

        var weekAgo = DateTime.UtcNow.AddDays(-7);

        ViewBag.Statuses = await _lookups.GetAsync("Status");
        ViewBag.Counts = counts;
        ViewBag.Total = counts.Values.Sum();
        ViewBag.NewThisWeek = await _context.Leads.CountAsync(l => l.CreatedAt >= weekAgo);
        ViewBag.Unassigned = await _context.Leads.CountAsync(l => l.Staff == null || l.Staff == "");
        ViewBag.Recent = await _context.Leads.OrderByDescending(l => l.CreatedAt).Take(6).ToListAsync();

        return View();
    }
}