using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiverseCRM.Services;

namespace MultiverseCRM.Controllers;

[Authorize]
[Route("Notifications")]
public class NotificationsController : Controller
{
    private readonly INotificationService _service;
    public NotificationsController(INotificationService service) => _service = service;

    private string UserName => User.Identity?.Name ?? "unknown";

    [HttpGet("Recent")]
    public async Task<IActionResult> Recent(int take = 20, CancellationToken ct = default)
    {
        var items = await _service.GetRecentAsync(UserName, take, ct);
        var unread = await _service.GetUnreadCountAsync(UserName, ct);

        return Json(new
        {
            unreadCount = unread,
            items = items.Select(n => new
            {
                n.Id,
                n.Category,
                n.Type,
                n.Title,
                n.Message,
                n.Url,
                n.EntityId,
                n.EntityType,
                n.ActorName,
                n.IsRead,
                age = FormatAge(n.CreatedUtc),
                timestamp = FormatStamp(n.CreatedUtc),
                icon = MapIcon(n.Type),
                tone = MapTone(n.Type)
            })
        });
    }

    [HttpPost("MarkAllRead")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _service.MarkAllReadAsync(UserName, ct);
        return Json(new { ok = true });
    }

    [HttpPost("MarkRead/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        await _service.MarkReadAsync(id, UserName, ct);
        return Json(new { ok = true });
    }

    [HttpGet("Poll")]
    public async Task<IActionResult> Poll(int since = 0, CancellationToken ct = default)
    {
        var newer = await _service.GetNewerThanAsync(UserName, since, ct);
        var unread = await _service.GetUnreadCountAsync(UserName, ct);
        return Json(new
        {
            hasNew = newer.Count > 0,
            unreadCount = unread,
            latestId = newer.Count > 0 ? newer[0].Id : since,
            preview = newer.FirstOrDefault() is { } n
                ? new { n.Id, n.Title, n.Message, n.Type }
                : null
        });
    }

    private static string MapIcon(Models.NotificationType t) => t switch
    {
        Models.NotificationType.Created => "bi-plus-lg",
        Models.NotificationType.Updated => "bi-pencil",
        Models.NotificationType.Deleted => "bi-trash",
        Models.NotificationType.Assigned => "bi-person-check",
        Models.NotificationType.FollowUpDue => "bi-alarm",
        Models.NotificationType.FollowUpOverdue => "bi-exclamation-triangle",
        Models.NotificationType.Imported => "bi-upload",
        Models.NotificationType.Mentioned => "bi-at",
        Models.NotificationType.SystemAlert => "bi-info-circle",
        _ => "bi-dot"
    };

    private static string MapTone(Models.NotificationType t) => t switch
    {
        Models.NotificationType.Created => "success",
        Models.NotificationType.Deleted => "danger",
        Models.NotificationType.FollowUpOverdue => "danger",
        Models.NotificationType.FollowUpDue => "warning",
        Models.NotificationType.Updated or Models.NotificationType.Imported => "info",
        _ => "info"
    };

    private static string FormatAge(DateTime utc)
    {
        var diff = DateTime.UtcNow - utc;
        if (diff.TotalSeconds < 60) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
        return utc.ToString("dd MMM");
    }

    private static string FormatStamp(DateTime utc) =>
        utc.ToString("dd MMM yyyy, hh:mm tt") + " UTC";
}