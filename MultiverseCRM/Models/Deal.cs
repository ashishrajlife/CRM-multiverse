using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MultiverseCRM.Models;

public class Deal
{
    public int Id { get; set; }

    // Origin lead (nullable — standalone deals allowed)
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    // Pipeline stage — LookupOptions category "DealStage"
    [MaxLength(64)]
    public string Stage { get; set; } = "New";

    // Property
    [MaxLength(256)] public string? Project { get; set; }
    [MaxLength(32)]  public string? BHK { get; set; }
    [MaxLength(128)] public string? Location { get; set; }
    [MaxLength(128)] public string? City { get; set; }
    public decimal? AreaSqft { get; set; }

    // Money
    [Column(TypeName = "decimal(18,2)")] public decimal Value { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? BookingAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? ReceivedAmount { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Win/Loss
    [MaxLength(32)]  public string Status { get; set; } = "Open";   // Open / Won / Lost
    [MaxLength(512)] public string? LostReason { get; set; }

    // Owner
    [MaxLength(128)] public string? OwnerUserName { get; set; }
    [MaxLength(128)] public string? OwnerDisplayName { get; set; }

    [MaxLength(512)] public string? Notes { get; set; }
    [MaxLength(256)] public string? Tags { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
} 