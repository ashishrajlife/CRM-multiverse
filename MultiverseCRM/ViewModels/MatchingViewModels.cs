using MultiverseCRM.Models;

namespace MultiverseCRM.ViewModels;

/// <summary>Property matched against a lead's requirement</summary>
public class PropertyMatch
{
    public int PropertyId { get; set; }
    public string UnitNumber { get; set; } = "";
    public string? Title { get; set; }
    public string? ProjectName { get; set; }
    public string? City { get; set; }
    public string? Location { get; set; }
    public string? BHK { get; set; }
    public string? Furnished { get; set; }
    public decimal? CarpetArea { get; set; }
    public decimal? TotalPrice { get; set; }
    public string? HeroImagePath { get; set; }
    public string Status { get; set; } = "";

    /// <summary>Match score 0–100</summary>
    public double Score { get; set; }

    /// <summary>Human-readable match reason (e.g., "3BHK ✓ · Raipur ✓ · Budget ✓")</summary>
    public List<string> MatchedOn { get; set; } = new();

    /// <summary>Color tier for badge (success/warning/info/secondary)</summary>
    public string Tier => Score >= 80 ? "success" : Score >= 60 ? "warning" : Score >= 40 ? "info" : "secondary";
}

/// <summary>Lead matched against a property</summary>
public class LeadMatch
{
    public int LeadId { get; set; }
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
    public string? Status { get; set; }
    public string? Staff { get; set; }
    public string? BHK { get; set; }
    public string? City { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinArea { get; set; }
    public decimal? MaxArea { get; set; }
    public string? LookingFor { get; set; }
    public string? Furnished { get; set; }
    public string? Project { get; set; }
    public DateTime CreatedAt { get; set; }

    public double Score { get; set; }
    public List<string> MatchedOn { get; set; } = new();
    public string Tier => Score >= 80 ? "success" : Score >= 60 ? "warning" : Score >= 40 ? "info" : "secondary";
}

/// <summary>Configurable weights (from appsettings)</summary>
public class PropertyMatchWeights
{
    public int BHK { get; set; } = 25;
    public int City { get; set; } = 20;
    public int Budget { get; set; } = 20;
    public int Area { get; set; } = 15;
    public int LookingFor { get; set; } = 10;
    public int Furnished { get; set; } = 5;
    public int Project { get; set; } = 5;
}

/// <summary>Result bundle returned to views</summary>
public class MatchingPanelViewModel
{
    public string Context { get; set; } = "";        // "lead" | "property"
    public int ContextId { get; set; }
    public string ContextLabel { get; set; } = "";
    public int TotalMatches { get; set; }
    public int StrongMatches { get; set; }           // score >= 80
    public int GoodMatches { get; set; }             // score >= 60
    public List<PropertyMatch> Properties { get; set; } = new();
    public List<LeadMatch> Leads { get; set; } = new();
}