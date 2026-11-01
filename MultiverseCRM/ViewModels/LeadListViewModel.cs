using MultiverseCRM.Models;

namespace MultiverseCRM.ViewModels;

public class LeadListViewModel
{
    public IEnumerable<Lead> Leads { get; set; } = new List<Lead>();
    public string ActiveStatus { get; set; } = "All";
    public string? SearchTerm { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "desc";
    
    // For tag dropdown suggestions (distinct tags across all leads)
    public List<string> AllTags { get; set; } = new();
    
    // Tab counts
    public int TotalCount { get; set; }
public Dictionary<string, int> StatusCounts { get; set; } = new();
public int FilteredCount { get; set; }
public int Page { get; set; } = 1;
public int PageSize { get; set; } = 25;

}