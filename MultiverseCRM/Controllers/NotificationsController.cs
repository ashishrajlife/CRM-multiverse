using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiverseCRM.Data;
using MultiverseCRM.Models;

namespace MultiverseCRM.Controllers;

[Authorize]
[Route("Notifications")]
public class NotificationsController : Controller
{
    private readonly AppDbContext _context;
    public NotificationsController(AppDbContext context) => _context = context;

    private string CurrentUser =>
        User.Identity?.Name ?? "unknown";

    // GET /Notifications/Recent?take=15
    [HttpGet("Recent")]
    public async Task<IActionResult> Recent(int take = 15)
    {
        take = Math.Clamp(take, 5, 50);

        var lastReadId = await _context.ActivityReads
            .Where(r => r.UserName == CurrentUser)
            .Select(r => (int?)r.LastReadEventId)
            .FirstOrDefaultAsync() ?? 0;

        var events = await _context.ActivityEvents
            .OrderByDescending(e => e.Id)
            .Take(take)
            .Select(e => new
            {
                e.Id,
                e.Entity,
                e.Action,
                e.Title,
                e.Detail,
                e.Url,
                e.ActorDisplayName,
                e.ActorUserName,
                e.CreatedUtc,
                e.EntityId
            })
            .ToListAsync();

        var unreadCount = await _context.ActivityEvents
            .Where(e => e.Id > lastReadId)
            .CountAsync();

        return Json(new
        {
            unreadCount,
            lastReadId,
            items = events.Select(e => new
            {
                e.Id,
                e.Entity,
                e.Action,
                e.Title,
                e.Detail,
                e.Url,
                actor = e.ActorDisplayName ?? e.ActorUserName ?? "System",
                e.EntityId,
                age = FormatAge(e.CreatedUtc),
                timestamp = FormatStamp(e.CreatedUtc)
            })
        });
    }

    // POST /Notifications/MarkAllRead
    [HttpPost("MarkAllRead")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var lastId = await _context.ActivityEvents
            .OrderByDescending(e => e.Id)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync() ?? 0;

        var row = await _context.ActivityReads
            .FirstOrDefaultAsync(r => r.UserName == CurrentUser);

        if (row == null)
        {
            _context.ActivityReads.Add(new ActivityRead
            {
                UserName = CurrentUser,
                LastReadEventId = lastId
            });
        }
        else if (lastId > row.LastReadEventId)
        {
            row.LastReadEventId = lastId;
        }

        await _context.SaveChangesAsync();
        return Json(new { ok = true, lastReadId = lastId });
    }

    // GET /Notifications/Poll?since=1234
    [HttpGet("Poll")]
    public async Task<IActionResult> Poll(int since = 0)
    {
        var newer = await _context.ActivityEvents
            .Where(e => e.Id > since)
            .OrderByDescending(e => e.Id)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Action,
                e.Entity
            })
            .Take(20)
            .ToListAsync();

        return Json(new
        {
            hasNew = newer.Count > 0,
            latestId = newer.Count > 0 ? newer[0].Id : since,
            preview = newer.FirstOrDefault()
        });
    }

    private static string FormatAge(DateTime utc)
    {
        var diff = DateTime.UtcNow - utc;
        if (diff.TotalSeconds < 60) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours   < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays    < 7)  return $"{(int)diff.TotalDays}d ago";
        return utc.ToString("dd MMM");
    }

    private static string FormatStamp(DateTime utc) =>
        utc.ToString("dd MMM yyyy, hh:mm tt") + " UTC";
}