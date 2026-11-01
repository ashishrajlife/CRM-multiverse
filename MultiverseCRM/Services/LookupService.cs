using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MultiverseCRM.Data;
using MultiverseCRM.Models;

namespace MultiverseCRM.Services;

public interface ILookupService
{
    Task<List<LookupOption>> GetAsync(string category);
    Task<List<string>> StaffAsync();
    Task<List<string>> DistinctAsync(string field); // "City" | "Project"
}

public class LookupService : ILookupService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    public LookupService(AppDbContext db, IMemoryCache cache) { _db = db; _cache = cache; }

    private async Task<T> Cached<T>(string key, Func<Task<T>> load)
    {
        if (_cache.TryGetValue(key, out T? hit) && hit is not null) return hit;
        var v = await load();
        _cache.Set(key, v, TimeSpan.FromMinutes(5));
        return v;
    }

    public Task<List<LookupOption>> GetAsync(string category) =>
        Cached($"lk:{category}", () => _db.LookupOptions.AsNoTracking()
            .Where(x => x.Category == category && x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Value).ToListAsync());

    public Task<List<string>> StaffAsync() =>
        Cached("lk:staff", () => _db.Users.AsNoTracking()
            .OrderBy(u => u.FullName).Select(u => u.FullName).ToListAsync());

    public Task<List<string>> DistinctAsync(string field) =>
        Cached($"lk:distinct:{field}", () => (field == "City"
            ? _db.Leads.Where(l => l.City != null && l.City != "").Select(l => l.City!)
            : _db.Leads.Where(l => l.Project != null && l.Project != "").Select(l => l.Project!))
            .Distinct().OrderBy(x => x).ToListAsync());
}