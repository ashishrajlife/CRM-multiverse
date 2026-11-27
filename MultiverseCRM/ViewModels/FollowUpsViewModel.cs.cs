using MultiverseCRM.Models;

namespace MultiverseCRM.ViewModels;

public class FollowUpsViewModel
{
    public string ActiveTab { get; set; } = "today";
    public string? Search { get; set; }
    public string SortBy { get; set; } = "RemarkDate";
    public string SortOrder { get; set; } = "asc";

    // Stats
    public int OverdueCount { get; set; }
    public int TodayCount { get; set; }
    public int UpcomingCount { get; set; }
    public int AllCount { get; set; }

    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalFiltered { get; set; }
    public int TotalPages => PageSize > 0
        ? Math.Max(1, (int)Math.Ceiling(TotalFiltered / (double)PageSize))
        : 1;

    public List<FollowUpRow> Rows { get; set; } = new();

    public List<string> StaffOptions { get; set; } = new();
    public string? StaffFilter { get; set; }
    public int ClosedCount { get; set; }
}

public class FollowUpRow
{
    public int LeadId { get; set; }
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = "";
    public string? Priority { get; set; }
    public string? Staff { get; set; }
    public string? Project { get; set; }
    public string? City { get; set; }
    public string? LatestRemark { get; set; }
    public DateTime RemarkDate { get; set; }
    public int DaysFromToday { get; set; }
    public string Bucket { get; set; } = "";
    public bool IsClosed { get; set; }
}