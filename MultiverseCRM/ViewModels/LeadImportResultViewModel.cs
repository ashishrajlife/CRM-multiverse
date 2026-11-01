namespace MultiverseCRM.ViewModels;

public class LeadImportResultViewModel
{
    public int TotalRows { get; set; }
    public int InsertedCount { get; set; }
    public int SkippedCount { get; set; }     // duplicates by Phone
    public int ErrorCount { get; set; }
    public List<ImportRowResult> Rows { get; set; } = new();
}

public class ImportRowResult
{
    public int RowNumber { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Status { get; set; } = "";  // "Inserted" | "Skipped (duplicate)" | "Error"
    public string Message { get; set; } = "";
}