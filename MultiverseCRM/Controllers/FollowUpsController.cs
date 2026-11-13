using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiverseCRM.Data;
using MultiverseCRM.Services;
using MultiverseCRM.ViewModels;

namespace MultiverseCRM.Controllers;

[Authorize]
public class FollowUpsController : Controller
{
    private readonly AppDbContext _context;
    private readonly IActivityLogger _activity;
    private readonly INotificationService _notifications;

    public FollowUpsController(
        AppDbContext context,
        IActivityLogger activity,
        INotificationService notifications)
    {
        _context = context;
        _activity = activity;
        _notifications = notifications;
    }

    private string CurrentUserDisplay =>
        User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "System";

    // ─── IST helper ─────────────────────────────────────────
    private static TimeZoneInfo GetIstTz()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
        catch
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
            catch { return TimeZoneInfo.Utc; }
        }
    }

    private static DateTime TodayIst() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetIstTz()).Date;

    // ─── Index ──────────────────────────────────────────────
    public async Task<IActionResult> Index(
        string tab = "today",
        string? search = null,
        string? staff = null,
        string sortBy = "RemarkDate",
        string sortOrder = "asc",
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        tab = (tab ?? "today").ToLowerInvariant();
        if (tab != "today" && tab != "overdue" && tab != "upcoming" && tab != "all")
            tab = "today";

        pageSize = new[] { 10, 25, 50, 100 }.Contains(pageSize) ? pageSize : 25;
        page = Math.Max(1, page);

        var today = TodayIst();

        var allLeadsWithFollowUp = await _context.Leads
            .Where(l => l.RemarkDate != null)
            .AsNoTracking()
            .Select(l => new
            {
                l.Id, l.FullName, l.Phone, l.Email, l.Status, l.Priority,
                l.Staff, l.Project, l.City, l.LatestRemark, l.RemarkDate
            })
            .ToListAsync(ct);

        // Counts
        var overdueRows  = allLeadsWithFollowUp.Where(l => l.RemarkDate!.Value.Date < today).ToList();
        var todayRows    = allLeadsWithFollowUp.Where(l => l.RemarkDate!.Value.Date == today).ToList();
        var upcomingRows = allLeadsWithFollowUp.Where(l => l.RemarkDate!.Value.Date > today).ToList();

        var staffOptions = allLeadsWithFollowUp
            .Where(l => !string.IsNullOrWhiteSpace(l.Staff))
            .Select(l => l.Staff!)
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        // Filter for current tab
        List<dynamic> sourceRows = tab switch
        {
            "overdue"  => overdueRows.Cast<dynamic>().ToList(),
            "today"    => todayRows.Cast<dynamic>().ToList(),
            "upcoming" => upcomingRows.Cast<dynamic>().ToList(),
            _          => allLeadsWithFollowUp.Cast<dynamic>().ToList()
        };

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLowerInvariant();
            sourceRows = sourceRows.Where(l =>
                ((string?)l.FullName)?.ToLowerInvariant().Contains(q) == true ||
                ((string?)l.Phone)?.Contains(q) == true ||
                ((string?)l.Email)?.ToLowerInvariant().Contains(q) == true ||
                ((string?)l.Status)?.ToLowerInvariant().Contains(q) == true
            ).ToList();
        }

        // Staff filter
        if (!string.IsNullOrWhiteSpace(staff))
        {
            sourceRows = sourceRows.Where(l =>
                string.Equals((string?)l.Staff, staff, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        // Sort
        bool asc = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);
        sourceRows = sortBy switch
        {
            "Name"     => asc ? sourceRows.OrderBy(l => (string?)l.FullName).ToList()
                              : sourceRows.OrderByDescending(l => (string?)l.FullName).ToList(),
            "Status"   => asc ? sourceRows.OrderBy(l => (string?)l.Status).ToList()
                              : sourceRows.OrderByDescending(l => (string?)l.Status).ToList(),
            "Priority" => asc ? sourceRows.OrderBy(l => (string?)l.Priority).ToList()
                              : sourceRows.OrderByDescending(l => (string?)l.Priority).ToList(),
            _ /* RemarkDate */ => asc
                ? sourceRows.OrderBy(l => (DateTime?)l.RemarkDate).ToList()
                : sourceRows.OrderByDescending(l => (DateTime?)l.RemarkDate).ToList()
        };

        // ─── Pagination ────────────────────────────────────
        int totalFiltered = sourceRows.Count;
        int totalPages = Math.Max(1, (int)Math.Ceiling(totalFiltered / (double)pageSize));
        page = Math.Min(page, totalPages);

        var pageRows = sourceRows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Map
        var vmRows = pageRows.Select(l =>
        {
            DateTime rd = (DateTime)l.RemarkDate!;
            int diff = (rd.Date - today).Days;
            string bucket = diff < 0 ? "overdue" : diff == 0 ? "today" : "upcoming";

            return new FollowUpRow
            {
                LeadId = (int)l.Id,
                FullName = (string?)l.FullName ?? "—",
                Phone = (string?)l.Phone,
                Email = (string?)l.Email,
                Status = (string?)l.Status ?? "—",
                Priority = (string?)l.Priority,
                Staff = (string?)l.Staff,
                Project = (string?)l.Project,
                City = (string?)l.City,
                LatestRemark = (string?)l.LatestRemark,
                RemarkDate = rd,
                DaysFromToday = diff,
                Bucket = bucket
            };
        }).ToList();

        var vm = new FollowUpsViewModel
        {
            ActiveTab = tab,
            Search = search,
            SortBy = sortBy,
            SortOrder = sortOrder,
            StaffFilter = staff,
            StaffOptions = staffOptions,
            OverdueCount = overdueRows.Count,
            TodayCount = todayRows.Count,
            UpcomingCount = upcomingRows.Count,
            AllCount = allLeadsWithFollowUp.Count,
            Page = page,
            PageSize = pageSize,
            TotalFiltered = totalFiltered,
            Rows = vmRows
        };

        return View(vm);
    }

    // ─── Mark Done (single) ─────────────────────────────────
    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> MarkDone(int id, CancellationToken ct)
{
    try
    {
        var lead = await _context.Leads.FindAsync(new object[] { id }, ct);
        if (lead == null)
            return Json(new { ok = false, error = "Lead not found" });

        var today = TodayIst();
        
        // ✅ FIX: Clear RemarkDate → lead follow-up list se completely hat jayegi
        lead.RemarkDate = null;
        lead.LatestRemark = $"Follow-up completed on {today:dd MMM yyyy}";
        lead.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        await _activity.LogAsync(
            entity: "Lead", action: "FollowUpCompleted",
            title: lead.FullName,
            detail: $"Follow-up closed on {today:dd MMM yyyy}",
            url: Url.Action("Index", "FollowUps"),
            entityId: lead.Id);

        return Json(new { ok = true, closed = true });
    }
    catch (Exception ex)
    {
        return Json(new { ok = false, error = ex.Message });
    }
}

    // ─── Reschedule ─────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int id, DateTime? newDate, CancellationToken ct)
    {
        try
        {
            var lead = await _context.Leads.FindAsync(new object[] { id }, ct);
            if (lead == null)
                return Json(new { ok = false, error = "Lead not found" });

            var target = (newDate ?? TodayIst()).Date;
            if (target < TodayIst()) target = TodayIst();

            lead.RemarkDate = target;
            lead.LatestRemark = $"Follow-up rescheduled to {target:dd MMM yyyy}";
            lead.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            await _activity.LogAsync(
                entity: "Lead", action: "FollowUpRescheduled",
                title: lead.FullName,
                detail: $"New date: {target:dd MMM yyyy}",
                url: Url.Action("Index", "FollowUps"),
                entityId: lead.Id);

            return Json(new { ok = true, newDate = target });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = ex.Message });
        }
    }

    // ─── Bulk actions ───────────────────────────────────────
    public record BulkRequest(List<int> Ids, string Action, DateTime? NewDate);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAction(
        [FromBody] BulkRequest req, CancellationToken ct)
    {
        try
        {
            if (req?.Ids == null || req.Ids.Count == 0)
                return Json(new { ok = false, error = "No leads selected" });

            var leads = await _context.Leads
                .Where(l => req.Ids.Contains(l.Id))
                .ToListAsync(ct);

            if (leads.Count == 0)
                return Json(new { ok = false, error = "Leads not found" });

            var today = TodayIst();

            if (req.Action == "done")
            {
                foreach (var l in leads)
                {
                    l.RemarkDate = null;
                    l.LatestRemark = $"Bulk follow-up completed on {today:dd MMM yyyy}";
                    l.UpdatedAt = DateTime.UtcNow;
                }
            }
            else if (req.Action == "reschedule")
            {
                var target = (req.NewDate ?? today).Date;
                if (target < today) target = today;
                foreach (var l in leads)
                {
                    l.RemarkDate = target;
                    l.LatestRemark = $"Follow-up rescheduled to {target:dd MMM yyyy}";
                    l.UpdatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                return Json(new { ok = false, error = "Unknown action" });
            }

            await _context.SaveChangesAsync(ct);

            await _activity.LogAsync(
                entity: "Lead", action: req.Action == "done" ? "FollowUpCompleted" : "FollowUpRescheduled",
                title: $"{leads.Count} follow-up(s) updated",
                detail: $"Bulk action · {leads.Count} leads",
                url: Url.Action("Index", "FollowUps"));

            return Json(new { ok = true, count = leads.Count });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = ex.Message });
        }
    }
}