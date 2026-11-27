using MultiverseCRM.Models;

namespace MultiverseCRM.ViewModels;

public class PropertyListViewModel
{
    public string? Search { get; set; }
    public string? StatusFilter { get; set; }
    public string? CityFilter { get; set; }
    public string? BHKFilter { get; set; }
    public int? ProjectFilter { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "desc";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;
    public int TotalFiltered { get; set; }
    public int TotalPages => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(TotalFiltered / (double)PageSize)) : 1;

    // Stats
    public int TotalCount { get; set; }
    public int AvailableCount { get; set; }
    public int BookedCount { get; set; }
    public int SoldCount { get; set; }

    public List<PropertyCard> Properties { get; set; } = new();
    public List<string> StatusOptions { get; set; } = new();
    public List<string> CityOptions { get; set; } = new();
    public List<string> BHKOptions { get; set; } = new();
    public List<ProjectOption> ProjectOptions { get; set; } = new();
}

public class PropertyCard
{
    public int Id { get; set; }
    public string UnitNumber { get; set; } = "";
    public string? Title { get; set; }
    public string? ProjectName { get; set; }
    public string? PropertyType { get; set; }
    public string? BHK { get; set; }
    public string? Furnished { get; set; }
    public decimal? CarpetArea { get; set; }
    public decimal? BuiltUpArea { get; set; }
    public decimal? TotalPrice { get; set; }
    public string? City { get; set; }
    public string? Location { get; set; }
    public string Status { get; set; } = "Available";
    public string? HeroImagePath { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public int ImageCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProjectOption
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class PropertyDetailViewModel
{
    public Property Property { get; set; } = null!;
    public Project? Project { get; set; }
    public List<PropertyImage> Images { get; set; } = new();
    public List<LeadMatch> MatchingLeads { get; set; } = new();   // ← new
}

public class PropertyCreateViewModel
{
    public Property Property { get; set; } = new();
    public List<IFormFile>? ImageFiles { get; set; }
    public List<ProjectOption> AvailableProjects { get; set; } = new();
}

public class PropertyEditViewModel
{
    public Property Property { get; set; } = new();
    public List<PropertyImage> ExistingImages { get; set; } = new();
    public List<int>? KeepImageIds { get; set; }
    public List<IFormFile>? ImageFiles { get; set; }
    public List<ProjectOption> AvailableProjects { get; set; } = new();
}