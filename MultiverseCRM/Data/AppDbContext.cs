using Microsoft.EntityFrameworkCore;
using MultiverseCRM.Models;

namespace MultiverseCRM.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Lead> Leads { get; set; }
    public DbSet<LookupOption> LookupOptions => Set<LookupOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed default Admin (static values only!)
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Username = "admin",
            Password = "admin123",
            FullName = "System Admin",
            Role = "Admin",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        modelBuilder.Entity<LookupOption>().HasIndex(x => new { x.Category, x.Value }).IsUnique();
    }
}