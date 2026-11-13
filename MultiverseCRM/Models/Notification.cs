using System.ComponentModel.DataAnnotations;

namespace MultiverseCRM.Models;

public enum NotificationCategory
{
    Lead = 0,
    Deal = 1,
    System = 2,
    Reminder = 3
}

public enum NotificationType
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
    Assigned = 3,
    FollowUpDue = 4,
    FollowUpOverdue = 5,
    Imported = 6,
    Mentioned = 7,
    SystemAlert = 8
}

public class Notification
{
    public int Id { get; set; }

    /// <summary>Target user name (User.Identity.Name). Null = broadcast to all.</summary>
    [MaxLength(256)]
    public string? UserName { get; set; }

    public NotificationCategory Category { get; set; }
    public NotificationType Type { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(500)]
    public string? Message { get; set; }

    /// <summary>Deep-link URL to navigate on click.</summary>
    [MaxLength(500)]
    public string? Url { get; set; }

    /// <summary>Optional entity reference (e.g. Lead id).</summary>
    public int? EntityId { get; set; }

    [MaxLength(64)]
    public string? EntityType { get; set; }

    /// <summary>Actor who triggered this notification (display name).</summary>
    [MaxLength(200)]
    public string? ActorName { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadUtc { get; set; }

    /// <summary>Higher = shown first (e.g. urgent reminders).</summary>
    public int Priority { get; set; } = 0;

    /// <summary>Dedupe key to prevent duplicate system notifications.</summary>
    [MaxLength(128)]
    public string? DedupeKey { get; set; }
}