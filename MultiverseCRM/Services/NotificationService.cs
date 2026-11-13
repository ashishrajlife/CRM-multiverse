using Microsoft.EntityFrameworkCore;
using MultiverseCRM.Data;
using MultiverseCRM.Models;

namespace MultiverseCRM.Services;

public interface INotificationService
{
    Task PushAsync(Notification n, CancellationToken ct = default);
    Task PushToUsersAsync(IEnumerable<string> userNames, Notification template, CancellationToken ct = default);
    Task MarkAllReadAsync(string userName, CancellationToken ct = default);
    Task MarkReadAsync(int id, string userName, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(string userName, CancellationToken ct = default);
    Task<List<Notification>> GetRecentAsync(string userName, int take = 20, CancellationToken ct = default);
    Task<int> GetLatestIdAsync(string userName, CancellationToken ct = default);
    Task<List<Notification>> GetNewerThanAsync(string userName, int sinceId, CancellationToken ct = default);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    public NotificationService(AppDbContext db) => _db = db;

    public async Task PushAsync(Notification n, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(n.DedupeKey))
        {
            var exists = await _db.Notifications
                .AnyAsync(x => x.DedupeKey == n.DedupeKey && x.UserName == n.UserName, ct);
            if (exists) return;
        }
        n.CreatedUtc = DateTime.UtcNow;
        _db.Notifications.Add(n);
        await _db.SaveChangesAsync(ct);
    }

    public async Task PushToUsersAsync(IEnumerable<string> userNames, Notification template, CancellationToken ct = default)
    {
        var list = userNames.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList();
        if (list.Count == 0) return;

        foreach (var u in list)
        {
            _db.Notifications.Add(new Notification
            {
                UserName = u,
                Category = template.Category,
                Type = template.Type,
                Title = template.Title,
                Message = template.Message,
                Url = template.Url,
                EntityId = template.EntityId,
                EntityType = template.EntityType,
                ActorName = template.ActorName,
                Priority = template.Priority,
                DedupeKey = template.DedupeKey != null ? $"{template.DedupeKey}:{u}" : null,
                CreatedUtc = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(string userName, CancellationToken ct = default)
    {
        var rows = await _db.Notifications
            .Where(n => (n.UserName == userName || n.UserName == null) && !n.IsRead)
            .ToListAsync(ct);
        if (rows.Count == 0) return;
        var now = DateTime.UtcNow;
        foreach (var r in rows) { r.IsRead = true; r.ReadUtc = now; }
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkReadAsync(int id, string userName, CancellationToken ct = default)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x =>
            x.Id == id && (x.UserName == userName || x.UserName == null), ct);
        if (n == null || n.IsRead) return;
        n.IsRead = true;
        n.ReadUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public Task<int> GetUnreadCountAsync(string userName, CancellationToken ct = default) =>
        _db.Notifications.CountAsync(n => (n.UserName == userName || n.UserName == null) && !n.IsRead, ct);

    public Task<List<Notification>> GetRecentAsync(string userName, int take = 20, CancellationToken ct = default) =>
        _db.Notifications
            .Where(n => n.UserName == userName || n.UserName == null)
            .OrderByDescending(n => n.Priority)
            .ThenByDescending(n => n.CreatedUtc)
            .Take(Math.Clamp(take, 5, 100))
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<int> GetLatestIdAsync(string userName, CancellationToken ct = default) =>
        await _db.Notifications
            .Where(n => n.UserName == userName || n.UserName == null)
            .MaxAsync(n => (int?)n.Id, ct) ?? 0;

    public Task<List<Notification>> GetNewerThanAsync(string userName, int sinceId, CancellationToken ct = default) =>
        _db.Notifications
            .Where(n => (n.UserName == userName || n.UserName == null) && n.Id > sinceId)
            .OrderByDescending(n => n.Id)
            .Take(20)
            .AsNoTracking()
            .ToListAsync(ct);
}