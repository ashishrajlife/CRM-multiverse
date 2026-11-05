using Microsoft.EntityFrameworkCore;
using MultiverseCRM.Data;
using MultiverseCRM.Models;

namespace MultiverseCRM.Services;

public interface IActivityLogger
{
    Task LogAsync(
        string entity,
        string action,
        string title,
        string? detail = null,
        string? url = null,
        int? entityId = null,
        string? actorUserName = null,
        string? actorDisplayName = null);
}

public class ActivityLogger : IActivityLogger
{
    private readonly AppDbContext _context;

    public ActivityLogger(AppDbContext context) => _context = context;

    public async Task LogAsync(
        string entity, string action, string title,
        string? detail = null, string? url = null, int? entityId = null,
        string? actorUserName = null, string? actorDisplayName = null)
    {
        var evt = new ActivityEvent
        {
            Entity = entity,
            Action = action,
            Title = title.Length > 256 ? title[..256] : title,
            Detail = detail is { Length: > 512 } ? detail[..512] : detail,
            Url = url,
            EntityId = entityId,
            ActorUserName = actorUserName,
            ActorDisplayName = actorDisplayName,
            CreatedUtc = DateTime.UtcNow
        };
        _context.ActivityEvents.Add(evt);
        await _context.SaveChangesAsync();
    }
}