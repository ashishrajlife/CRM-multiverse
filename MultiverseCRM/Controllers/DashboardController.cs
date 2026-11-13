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

    public DashboardController(AppDbContext context, ILookupService lookups, INotificationService notifications)
    {
        _context = context;
        _lookups = lookups;
        _notifications = notifications;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userName = User.Identity?.Name ?? "unknown";
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var weekAgo = DateTime.UtcNow.AddDays(-7);

        // ── Status counts ────────────────────────────────────
        var counts = await _context.Leads
            .GroupBy(l => l.Status)
            .Select(g => new { g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.C, ct);

        var statuses = await _lookups.GetAsync("Status");
        var last = statuses.LastOrDefault();
        int closedCount = last != null ? counts.GetValueOrDefault(last.Value) : 0;

        // ── Source breakdown (for donut) ────────────────────
        var sourceGroups = await _context.Leads
            .Where(l => l.Source != null && l.Source != "")
            .GroupBy(l => l.Source!)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(6)
            .ToListAsync(ct);

        var sourceColors = new[]
        {
            "#2F5BEA", "#B08A4A", "#14b8a6", "#f59e0b", "#ef4444", "#8b5cf6"
        };
        int sourceTotal = sourceGroups.Sum(x => x.Count);
        var sourceSlices = sourceGroups.Select((s, i) => new SourceSlice(
            s.Source,
            s.Count,
            sourceColors[i % sourceColors.Length],
            sourceTotal == 0 ? 0 : Math.Round(s.Count * 100.0 / sourceTotal, 1)
        )).ToList();

        // ── Follow-ups due today / overdue ──────────────────
        var followUpsRaw = await _context.Leads
            .Where(l => l.RemarkDate != null
                     && l.RemarkDate.Value >= today.AddDays(-30)
                     && l.RemarkDate.Value < tomorrow.AddDays(1))
            .OrderBy(l => l.RemarkDate)
            .Select(l => new
            {
                l.Id,
                l.FullName,
                l.Phone,
                l.Status,
                l.Staff,
                l.RemarkDate,
                l.LatestRemark
            })
            .ToListAsync(ct);

        var followUpsToday = followUpsRaw
            .Where(l => l.RemarkDate!.Value.Date == today)
            .Select(l => new FollowUpItem(
                l.Id, l.FullName ?? "—", l.Phone, l.Status ?? "—",
                l.Staff, l.RemarkDate!.Value, l.LatestRemark, false))
            .ToList();

        var followUpsOverdue = followUpsRaw
            .Where(l => l.RemarkDate!.Value.Date < today)
            .Select(l => new FollowUpItem(
                l.Id, l.FullName ?? "—", l.Phone, l.Status ?? "—",
                l.Staff, l.RemarkDate!.Value, l.LatestRemark, true))
            .Take(8)
            .ToList();

        // ── Recent activity (already logged by ActivityLogger) ──
        var recentActivity = await _context.ActivityEvents
            .OrderByDescending(e => e.Id)
            .Take(8)
            .Select(e => new ActivityItem(
                e.Id,
                e.Action,
                e.Entity,
                e.Title,
                e.Detail,
                e.Url,
                e.ActorDisplayName ?? e.ActorUserName ?? "System",
                e.CreatedUtc,
                MapIcon(e.Action),
                MapColorClass(e.Action)
            ))
            .AsNoTracking()
            .ToListAsync(ct);

        var vm = new DashboardViewModel
        {
            Statuses = statuses,
            StatusCounts = counts,
            TotalLeads = counts.Values.Sum(),
            NewThisWeek = await _context.Leads.CountAsync(l => l.CreatedAt >= weekAgo, ct),
            Unassigned = await _context.Leads.CountAsync(l => l.Staff == null || l.Staff == "", ct),
            ClosedCount = closedCount,
            SourceBreakdown = sourceSlices,
            FollowUpsToday = followUpsToday,
            FollowUpsOverdue = followUpsOverdue,
            RecentLeads = await _context.Leads
                .OrderByDescending(l => l.CreatedAt)
                .Take(5)
                .AsNoTracking()
                .ToListAsync(ct),
            RecentActivity = recentActivity
        };

        // Fire-and-forget: seed follow-up notifications for today (safe to call repeatedly thanks to DedupeKey)
        _ = Task.Run(async () =>
        {
            try { await SeedFollowUpNotificationsAsync(userName); }
            catch { /* swallow */ }
        });

        ViewBag.UnreadNotifications = await _notifications.GetUnreadCountAsync(userName, ct);
        return View(vm);
    }

    private async Task SeedFollowUpNotificationsAsync(string userName)
    {
        using var scope = HttpContext.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notif = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var today = DateTime.UtcNow.Date;
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
        _ => "bi-dot"
    };

    private static string MapColorClass(string action) => action switch
    {
        "Created" => "success",
        "Deleted" or "BulkDeleted" => "danger",
        "Updated" or "Imported" => "info",
        "TagUpdated" => "warning",
        _ => "info"
    };
}