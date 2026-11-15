using System.ComponentModel.DataAnnotations;

namespace MultiverseCRM.Models;

public class Property
{
    public int Id { get; set; }

    // ─── Identification ─────────────────────────────────
    [Required, MaxLength(50)]
    public string UnitNumber { get; set; } = "";       // A-402, B-101

    [MaxLength(200)]
    public string? Title { get; set; }                 // "3BHK Premium Corner Unit"

    // ─── Project Relation (optional — standalone properties allowed) ───
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    // ─── Property Details ───────────────────────────────
    [MaxLength(50)]
    public string? PropertyType { get; set; }          // Flat / Villa / Plot / Shop

    [MaxLength(50)]
    public string? BHK { get; set; }                   // 1 BHK, 2 BHK, 3 BHK, 4 BHK

    [MaxLength(50)]
    public string? Furnished { get; set; }             // Unfurnished / Semi / Fully

    [MaxLength(50)]
    public string? Facing { get; set; }                // North / East / etc.

    public int? FloorNumber { get; set; }
    public int? TotalFloors { get; set; }

    // ─── Area ───────────────────────────────────────────
    public decimal? CarpetArea { get; set; }
    public decimal? BuiltUpArea { get; set; }
    public decimal? SuperBuiltUpArea { get; set; }

    [MaxLength(20)]
    public string? AreaUnit { get; set; } = "Sqft";

    // ─── Pricing ────────────────────────────────────────
    public decimal? BasePrice { get; set; }
    public decimal? TotalPrice { get; set; }
    public decimal? PricePerSqft { get; set; }

    // ─── Location (denormalized for fast queries) ───────
    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(200)]
    public string? Address { get; set; }

    // ─── Status & Ownership ─────────────────────────────
    [MaxLength(50)]
    public string Status { get; set; } = "Available";   // Available / Booked / Sold / Blocked / Under Construction

    [MaxLength(200)]
    public string? OwnerName { get; set; }

    [MaxLength(20)]
    public string? OwnerPhone { get; set; }

    // ─── Booking Info ───────────────────────────────────
    public DateTime? BookingDate { get; set; }
    public DateTime? SoldDate { get; set; }

    public int? BookingLeadId { get; set; }             // FK to Leads (who booked)
    public int? DealId { get; set; }                    // FK to Deals

    [MaxLength(500)]
    public string? Notes { get; set; }

    // ─── Hero Image (denormalized) ──────────────────────
    [MaxLength(500)]
    public string? HeroImagePath { get; set; }

    // ─── Metadata ───────────────────────────────────────
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    // ─── Navigation ─────────────────────────────────────
    public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
}

public class PropertyImage
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = "";

    [MaxLength(200)]
    public string? Caption { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsHero { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}