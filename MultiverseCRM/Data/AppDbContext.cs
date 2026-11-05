using Microsoft.EntityFrameworkCore;
using MultiverseCRM.Models;

namespace MultiverseCRM.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Lead> Leads { get; set; }
    public DbSet<LookupOption> LookupOptions => Set<LookupOption>();
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();
    public DbSet<ActivityRead> ActivityReads => Set<ActivityRead>();
    public DbSet<Deal> Deals => Set<Deal>();

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

        modelBuilder.Entity<ActivityEvent>()
        .HasIndex(e => e.CreatedUtc);

        modelBuilder.Entity<ActivityRead>()
            .HasIndex(r => r.UserName)
            .IsUnique();
        modelBuilder.Entity<Deal>()
        .HasOne(d => d.Lead)
        .WithMany()
        .HasForeignKey(d => d.LeadId)
        .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Deal>()
            .HasIndex(d => d.Stage);

        modelBuilder.Entity<Deal>()
            .HasIndex(d => d.Status);

        modelBuilder.Entity<Deal>()
            .HasIndex(d => d.OwnerUserName);       


    }
}