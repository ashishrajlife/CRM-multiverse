using MultiverseCRM.Models;

namespace MultiverseCRM.ViewModels;

public class DashboardViewModel
{
    public int TotalLeads { get; set; }
    public int NewThisWeek { get; set; }
    public int Unassigned { get; set; }
    public int ClosedCount { get; set; }

    public List<LookupOption> Statuses { get; set; } = new();
    public Dictionary<string, int> StatusCounts { get; set; } = new();

    // Source breakdown (for donut)
    public List<SourceSlice> SourceBreakdown { get; set; } = new();

    // Tasks
    public List<FollowUpItem> FollowUpsToday { get; set; } = new();
    public List<FollowUpItem> FollowUpsOverdue { get; set; } = new();
    public List<Lead> RecentLeads { get; set; } = new();
    public List<ActivityItem> RecentActivity { get; set; } = new();

    public int FollowUpsTodayCount => FollowUpsToday.Count;
    public int FollowUpsOverdueCount => FollowUpsOverdue.Count;
}

public record SourceSlice(string Source, int Count, string Color, double Percent);

public record FollowUpItem(
    int LeadId,
    string FullName,
    string? Phone,
    string Status,
    string? AssignedTo,
    DateTime RemarkDate,
    string? LatestRemark,
    bool IsOverdue);

public record ActivityItem(
    int Id,
    string Action,
    string Entity,
    string Title,
    string? Detail,
    string? Url,
    string Actor,
    DateTime CreatedUtc,
    string Icon,
    string ColorClass);