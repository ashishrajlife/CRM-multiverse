using System.ComponentModel.DataAnnotations;

namespace MultiverseCRM.Models;

public class ActivityEvent
{
    public int Id { get; set; }

    /// <summary>"Lead" / "Project" / "Deal" — used for icon + colour.</summary>
    [MaxLength(32)]
    public string Entity { get; set; } = "Lead";

    /// <summary>"Created" / "Updated" / "Deleted" / "Imported" / "TagUpdated"</summary>
    [MaxLength(32)]
    public string Action { get; set; } = "";

    /// <summary>Short title shown in the row, e.g. "Ramesh Kumar".</summary>
    [MaxLength(256)]
    public string Title { get; set; } = "";

    /// <summary>Optional longer body (e.g. "Phone: 98765 43210").</summary>
    [MaxLength(512)]
    public string? Detail { get; set; }

    /// <summary>Link to click through to.</summary>
    [MaxLength(512)]
    public string? Url { get; set; }

    /// <summary>Username of the person who did it.</summary>
    [MaxLength(128)]
    public string? ActorUserName { get; set; }

    [MaxLength(128)]
    public string? ActorDisplayName { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Optional — id of the related row (Lead.Id, etc.).</summary>
    public int? EntityId { get; set; }
}