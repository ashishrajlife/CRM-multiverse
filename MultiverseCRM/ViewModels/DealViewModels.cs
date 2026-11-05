using MultiverseCRM.Models;

namespace MultiverseCRM.ViewModels;

public class DealListViewModel
{
    public string ActiveStage { get; set; } = "All";
    public string ActiveStatus { get; set; } = "All";
    public string? SearchTerm { get; set; }
    public string? OwnerFilter { get; set; }
    public string SortBy { get; set; } = "UpdatedAt";
    public string SortOrder { get; set; } = "desc";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int FilteredCount { get; set; }

    public Dictionary<string, int> StageCounts { get; set; } = new();
    public Dictionary<string, decimal> StageValues { get; set; } = new();
    public int TotalCount { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal TotalWonValue { get; set; }

    public List<Deal> Deals { get; set; } = new();
    public List<string> AllOwners { get; set; } = new();
    public List<string> AllTags { get; set; } = new();

    // For the Lead dropdown on Create / Edit
    public List<LeadOption> AllLeads { get; set; } = new();
}

public record LeadOption(int Id, string Display, bool Assigned);

public record MoveStageRequest(int Id, string Stage);
public record CloseDealRequest(int Id, string? LostReason);