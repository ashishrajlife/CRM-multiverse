using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiverseCRM.Data;
using MultiverseCRM.Models;
using MultiverseCRM.Services;
using MultiverseCRM.ViewModels;

namespace MultiverseCRM.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILookupService _lookups;
    private readonly INotificationService _notifications;

    public DashboardController(
        AppDbContext context,
        ILookupService lookups,
        INotificationService notifications)
    {
        _context = context;
        _lookups = lookups;
        _notifications = notifications;
    }

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

    private const int OverdueToleranceDays = 30;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userName = User.Identity?.Name ?? "unknown";
        var today = TodayIst();
        var weekAgo = today.AddDays(-7);

        // ═══════════════════════════════════════════════════════
        // QUERY 1 — Status counts (single grouped query)
        // ═══════════════════════════════════════════════════════
        var counts = await _context.Leads.AsNoTracking()
            .GroupBy(l => l.Status)
            .Select(g => new { g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.C, ct);

        // ═══════════════════════════════════════════════════════
        // QUERY 2 — Global counters (single query)
        // ═══════════════════════════════════════════════════════
        var counters = await _context.Leads.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                NewThisWeek = g.Count(l => l.CreatedAt >= weekAgo),
                Unassigned  = g.Count(l => l.Staff == null || l.Staff == "")
            })
            .FirstOrDefaultAsync(ct);

        // ═══════════════════════════════════════════════════════
        // QUERY 3 — Source breakdown (grouped, top 6)
        // ═══════════════════════════════════════════════════════
        var sourceGroups = await _context.Leads.AsNoTracking()
            .Where(l => l.Source != null && l.Source != "")
            .GroupBy(l => l.Source!)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(6)
            .ToListAsync(ct);

        // ═══════════════════════════════════════════════════════
        // QUERY 4 — Follow-ups (only pending, with limits)
        // ═══════════════════════════════════════════════════════
        var followUpsRaw = await _context.Leads.AsNoTracking()
            .Where(l => l.RemarkDate != null)
            .OrderBy(l => l.RemarkDate)
            .Select(l => new
            {
                l.Id, l.FullName, l.Phone, l.Status, l.Staff,
                l.RemarkDate, l.LatestRemark
            })
            .ToListAsync(ct);

        // ═══════════════════════════════════════════════════════
        // QUERY 5 — Recent activity
        // ═══════════════════════════════════════════════════════
        var recentActivity = await _context.ActivityEvents.AsNoTracking()
            .OrderByDescending(e => e.Id)
            .Take(8)
            .Select(e => new ActivityItem(
                e.Id, e.Action, e.Entity, e.Title, e.Detail, e.Url,
                e.ActorDisplayName ?? e.ActorUserName ?? "System",
                e.CreatedUtc,
                MapIcon(e.Action),
                MapColorClass(e.Action)))
            .ToListAsync(ct);

        // ═══════════════════════════════════════════════════════
        // QUERY 6 — Recent leads
        // ═══════════════════════════════════════════════════════
        var recentLeads = await _context.Leads.AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(10)
            .ToListAsync(ct);

        // ═══════════════════════════════════════════════════════
        // QUERY 7 — Statuses lookup
        // ═══════════════════════════════════════════════════════
        var statuses = await _lookups.GetAsync("Status");

        // ═══════════════════════════════════════════════════════
        // QUERY 8 — Unread notifications
        // ═══════════════════════════════════════════════════════
        var unreadNotifs = await _notifications.GetUnreadCountAsync(userName, ct);

        // ─── Compute derived values (pure in-memory) ───────
        var last = statuses.LastOrDefault();
        int closedCount = last != null ? counts.GetValueOrDefault(last.Value) : 0;

        var sourceColors = new[] { "#2F5BEA", "#B08A4A", "#14b8a6", "#f59e0b", "#ef4444", "#8b5cf6" };
        int sourceTotal = sourceGroups.Sum(x => x.Count);
        var sourceSlices = sourceGroups.Select((s, i) => new SourceSlice(
            s.Source, s.Count,
            sourceColors[i % sourceColors.Length],
            sourceTotal == 0 ? 0 : Math.Round(s.Count * 100.0 / sourceTotal, 1)
        )).ToList();

        // ── Follow-ups with tolerance ──────────────────────
        var enriched = followUpsRaw.Select(l =>
        {
            var rd = l.RemarkDate!.Value;
            var actualDiff = (rd.Date - today).Days;
            var displayDiff = (actualDiff < 0 && actualDiff > -OverdueToleranceDays) ? 0 : actualDiff;
            return new { l, rd, actualDiff, displayDiff };
        }).ToList();

        var followUpsToday = enriched
            .Where(x => x.displayDiff == 0)
            .OrderBy(x => x.rd)
            .Take(20)
            .Select(x => new FollowUpItem(
                x.l.Id, x.l.FullName ?? "—", x.l.Phone, x.l.Status ?? "—",
                x.l.Staff, x.rd, x.l.LatestRemark, false))
            .ToList();

        var followUpsOverdue = enriched
            .Where(x => x.displayDiff < 0)
            .OrderBy(x => x.rd)
            .Take(10)
            .Select(x => new FollowUpItem(
                x.l.Id, x.l.FullName ?? "—", x.l.Phone, x.l.Status ?? "—",
                x.l.Staff, x.rd, x.l.LatestRemark, true))
            .ToList();

        var overdueCount = enriched.Count(x => x.displayDiff < 0);

        var vm = new DashboardViewModel
        {
            Statuses = statuses,
            StatusCounts = counts,
            TotalLeads = counts.Values.Sum(),
            NewThisWeek = counters?.NewThisWeek ?? 0,
            Unassigned = counters?.Unassigned ?? 0,
            ClosedCount = closedCount,
            SourceBreakdown = sourceSlices,
            FollowUpsToday = followUpsToday,
            FollowUpsOverdue = followUpsOverdue,
            RecentLeads = recentLeads,
            RecentActivity = recentActivity
        };

        ViewBag.UnreadNotifications = unreadNotifs;
        ViewBag.OverdueBadge = overdueCount;

        // Fire-and-forget notification seeding
        _ = Task.Run(async () =>
        {
            try { await SeedFollowUpNotificationsAsync(userName); }
            catch { }
        });

        return View(vm);
    }

    private async Task SeedFollowUpNotificationsAsync(string userName)
    {
        using var scope = HttpContext.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notif = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var today = TodayIst();
        var dueToday = await db.Leads
            .Where(l => l.RemarkDate != null && l.RemarkDate.Value.Date == today)
            .Take(50)
            .ToListAsync();

        foreach (var lead in dueToday)
        {
            var targetUser = !string.IsNullOrWhiteSpace(lead.Staff) ? lead.Staff : userName;
            await notif.PushAsync(new Notification
            {
                UserName = targetUser,
                Category = NotificationCategory.Reminder,
                Type = NotificationType.FollowUpDue,
                Title = $"Follow-up due: {lead.FullName}",
                Message = lead.LatestRemark ?? $"Phone: {lead.Phone}",
                Url = $"/Leads?search={Uri.EscapeDataString(lead.Phone ?? "")}",
                EntityId = lead.Id,
                EntityType = "Lead",
                Priority = 5,
                DedupeKey = $"followup-today-{lead.Id}-{today:yyyyMMdd}"
            });
        }
    }

    private static string MapIcon(string action) => action switch
    {
        "Created" => "bi-plus-lg",
        "Updated" => "bi-pencil",
        "Deleted" => "bi-trash",
        "BulkDeleted" => "bi-trash-fill",
        "Imported" => "bi-upload",
        "TagUpdated" => "bi-tags",
        "FollowUpCompleted" => "bi-check2-circle",
        "FollowUpRescheduled" => "bi-calendar-event",
        _ => "bi-dot"
    };

    private static string MapColorClass(string action) => action switch
    {
        "Created" => "success",
        "Deleted" or "BulkDeleted" => "danger",
        "Updated" or "Imported" => "info",
        "TagUpdated" => "warning",
        "FollowUpCompleted" => "success",
        "FollowUpRescheduled" => "warning",
        _ => "info"
    };
}