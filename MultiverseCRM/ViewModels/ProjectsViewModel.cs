using MultiverseCRM.Models;

namespace MultiverseCRM.ViewModels;

public class ProjectListViewModel
{
    public string? Search { get; set; }
    public string? CityFilter { get; set; }
    public string? StatusFilter { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "desc";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;   // grid — more per page
    public int TotalFiltered { get; set; }
    public int TotalPages => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(TotalFiltered / (double)PageSize)) : 1;

    // Stats
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int FeaturedCount { get; set; }

    public List<ProjectCard> Projects { get; set; } = new();
    public List<string> CityOptions { get; set; } = new();
    public List<string> StatusOptions { get; set; } = new();
}

public class ProjectCard
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? City { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public string? ProjectType { get; set; }
    public string? Developer { get; set; }
    public string? HeroImagePath { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public DateTime? PossessionDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public int ImageCount { get; set; }
    public int AmenityCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProjectDetailViewModel
{
    public Project Project { get; set; } = null!;
    public List<ProjectImage> Images { get; set; } = new();
    public List<ProjectAmenity> Amenities { get; set; } = new();
    public List<ProjectDocument> Documents { get; set; } = new();
}

public class ProjectCreateViewModel
{
    public Project Project { get; set; } = new();
    public List<IFormFile>? ImageFiles { get; set; }
    public List<string>? AmenityNames { get; set; }
    public List<IFormFile>? DocumentFiles { get; set; }
}
public class ProjectEditViewModel
{
    public Project Project { get; set; } = new();

    // Existing data for display
    public List<ProjectImage> ExistingImages { get; set; } = new();
    public List<ProjectAmenity> ExistingAmenities { get; set; } = new();
    public List<ProjectDocument> ExistingDocuments { get; set; } = new();

    // Form inputs
    public List<IFormFile>? ImageFiles { get; set; }
    public List<IFormFile>? DocumentFiles { get; set; }
    public List<string>? AmenityNames { get; set; }

    // Image IDs to KEEP (unchecked ones get deleted)
    public List<int>? KeepImageIds { get; set; }

    // New hero image (optional)
    public int? NewHeroImageId { get; set; }
}