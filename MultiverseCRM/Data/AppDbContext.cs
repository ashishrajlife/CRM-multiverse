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
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<ProjectImage> ProjectImages { get; set; } = null!;
    public DbSet<ProjectAmenity> ProjectAmenities { get; set; } = null!;
    public DbSet<ProjectDocument> ProjectDocuments { get; set; } = null!;
    public DbSet<Property> Properties { get; set; } = null!;
    public DbSet<PropertyImage> PropertyImages { get; set; } = null!;

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
            
        modelBuilder.Entity<Notification>(b =>
        {
            b.HasIndex(n => new { n.UserName, n.IsRead, n.CreatedUtc });
            b.HasIndex(n => n.DedupeKey).IsUnique().HasFilter("[DedupeKey] IS NOT NULL");
            b.Property(n => n.Category).HasConversion<int>();
            b.Property(n => n.Type).HasConversion<int>();
        });
        modelBuilder.Entity<Project>(b =>
        {
            b.HasIndex(p => p.Name);
            b.HasIndex(p => p.City);
            b.HasIndex(p => p.Status);
            b.HasIndex(p => p.IsActive);
            b.HasIndex(p => new { p.IsActive, p.CreatedAt });

            b.HasMany(p => p.Images)
                .WithOne(i => i.Project)
                .HasForeignKey(i => i.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(p => p.Amenities)
                .WithOne(a => a.Project)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(p => p.Documents)
                .WithOne(d => d.Project)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectImage>(b =>
        {
            b.HasIndex(i => new { i.ProjectId, i.DisplayOrder });
            b.HasIndex(i => new { i.ProjectId, i.IsHero });
        });

        modelBuilder.Entity<ProjectAmenity>(b =>
        {
            b.HasIndex(a => new { a.ProjectId, a.DisplayOrder });
        });

        modelBuilder.Entity<ProjectDocument>(b =>
        {
            b.HasIndex(d => d.ProjectId);
        });   
        modelBuilder.Entity<Property>(b =>
        {
            // Primary lookup indexes
            b.HasIndex(p => p.UnitNumber);
            b.HasIndex(p => p.Status);
            b.HasIndex(p => p.City);
            b.HasIndex(p => p.ProjectId);
            b.HasIndex(p => new { p.IsActive, p.Status });
            b.HasIndex(p => new { p.IsActive, p.CreatedAt });

            // Unique constraint: no duplicate unit numbers per project
            b.HasIndex(p => new { p.ProjectId, p.UnitNumber })
                .IsUnique()
                .HasFilter("[ProjectId] IS NOT NULL");

            b.HasOne(p => p.Project)
                .WithMany()
                .HasForeignKey(p => p.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasMany(p => p.Images)
                .WithOne(i => i.Property)
                .HasForeignKey(i => i.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PropertyImage>(b =>
        {
            b.HasIndex(i => new { i.PropertyId, i.DisplayOrder });
            b.HasIndex(i => new { i.PropertyId, i.IsHero });
        });       


    }
}