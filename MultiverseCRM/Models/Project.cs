using System.ComponentModel.DataAnnotations;

namespace MultiverseCRM.Models;

public class Project
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(100)]
    public string? Status { get; set; }              // Upcoming / Under Construction / Ready

    [MaxLength(100)]
    public string? ProjectType { get; set; }         // Residential / Commercial / Mixed

    [MaxLength(100)]
    public string? ReraNumber { get; set; }

    [MaxLength(200)]
    public string? Developer { get; set; }

    // Dates
    public DateTime? LaunchDate { get; set; }
    public DateTime? PossessionDate { get; set; }

    // Pricing
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    // Coverage
    public decimal? TotalArea { get; set; }
    [MaxLength(20)]
    public string? AreaUnit { get; set; }             // Sqft / Acres / SqM

    public int? TotalUnits { get; set; }

    // Hero image (denormalized for fast listing)
    [MaxLength(500)]
    public string? HeroImagePath { get; set; }

    // Metadata
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    // Navigation
    public ICollection<ProjectImage> Images { get; set; } = new List<ProjectImage>();
    public ICollection<ProjectAmenity> Amenities { get; set; } = new List<ProjectAmenity>();
    public ICollection<ProjectDocument> Documents { get; set; } = new List<ProjectDocument>();
}

public class ProjectImage
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = "";        // relative path: uploads/projects/1/abc.jpg

    [MaxLength(200)]
    public string? Caption { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsHero { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public class ProjectAmenity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";            // Swimming Pool, Gym, etc.

    [MaxLength(50)]
    public string? Icon { get; set; }                 // bi-water, bi-bicycle, etc.

    public int DisplayOrder { get; set; }
}

public class ProjectDocument
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";            // Brochure, Price List

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = "";

    [MaxLength(50)]
    public string? FileType { get; set; }             // pdf, docx, etc.

    public long FileSizeBytes { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}