using System.ComponentModel.DataAnnotations;

namespace MultiverseCRM.Models;

public class Lead
{
    public int Id { get; set; }

    // Basic Info
    [Required] public string FullName { get; set; } = string.Empty;
    [Required] public string Phone { get; set; } = string.Empty;
    public string? AltPhone { get; set; }
    public string? Email { get; set; }
    
    // Status & Priority
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Fresh Lead";
    
    // Source & Tracking
    public string? Source { get; set; }
    public string? PrimarySource { get; set; }
    public string? Staff { get; set; }
    public string? ChannelPartner { get; set; }
    public string? LatestRemark { get; set; }
    public string? RemarkAssignedTo { get; set; }
    public DateTime? RemarkDate { get; set; }
    public string? Tags { get; set; } // Comma-separated: "hot,3bhk,investor"
    
    // Requirements
    public string? AdditionalRequirements { get; set; }
    public string? Project { get; set; }
    public string? ListingType { get; set; }
    public string? PropertyType { get; set; }
    public string? SpecificPropertyType { get; set; }
    public string? BHK { get; set; }
    public string? Location { get; set; }
    public string? City { get; set; }
    public decimal? Price { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxArea { get; set; }
    public decimal? MinArea { get; set; }
    public int? AreaSqft { get; set; }           // Always store normalized in Sqft
    public string? AreaUnit { get; set; } = "Sqft"; // Display unit: Sqft, SqM, Bigha, Angul
    public string? Furnished { get; set; }
    
    // Site & Follow-ups
    public bool SiteVisitCompleted { get; set; }
    public int FollowUps { get; set; }
    public int ReengagedCount { get; set; }
    public bool Duplicate { get; set; }
    public DateTime? DuplicateAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string? SecondaryStaff { get; set; }
    
    // Meta Tracking
    public string? PageId { get; set; }
    public string? FormId { get; set; }
    public string? AdId { get; set; }
    public string? Campaign { get; set; }
    public string? AdSet { get; set; }
    
    // Customer Profile
    public DateTime? BirthDate { get; set; }
    public string? LookingFor { get; set; }
    public string? CustomerType { get; set; }
    public string? CustomerCategory { get; set; }

    // Dates
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}