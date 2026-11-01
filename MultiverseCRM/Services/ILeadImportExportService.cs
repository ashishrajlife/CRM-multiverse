using MultiverseCRM.Models;

namespace MultiverseCRM.Services;

public interface ILeadImportExportService
{
    /// <summary>Builds an .xlsx of the given leads.</summary>
    byte[] Export(IReadOnlyList<Lead> leads);

    /// <summary>Parses an uploaded file and returns parsed rows (does not save).</summary>
    Task<ParseResult> ParseAsync(Stream stream, string fileName, CancellationToken ct = default);
}

public class ParseResult
{
    public List<LeadImportRow> Rows { get; set; } = new();
    public List<string> FileErrors { get; set; } = new();  // bad header, empty file, etc.
}

public class LeadImportRow
{
    public int RowNumber { get; set; }
    public Lead Lead { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public bool IsValid => Errors.Count == 0;
}