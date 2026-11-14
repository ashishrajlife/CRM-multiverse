using ClosedXML.Excel;
using MultiverseCRM.Data;
using MultiverseCRM.Services;
using MultiverseCRM.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace MultiverseCRM.Services;

public class LeadImportExportService : ILeadImportExportService
{
    private readonly AppDbContext _db;
    public LeadImportExportService(AppDbContext db) => _db = db;

    // Per-parse lookup cache — loaded ONCE, no per-row DB hits
    private Dictionary<(string Category, string Value), bool>? _lookupCache;

    private static readonly string[] Columns = new[]
    {
        "FullName","Phone","AltPhone","Email","City","Location",
        "Priority","Status","Source","PrimarySource","Staff","RemarkAssignedTo",
        "LatestRemark","RemarkDate","Tags","AdditionalRequirements",
        "Project","BHK","Furnished","LookingFor","AreaUnit",
        "MinPrice","MaxPrice","Price","MinArea","MaxArea","AreaSqft",
        "CustomerType","CustomerCategory","ChannelPartner","BirthDate"
    };

    // ─── IST helper ──────────────────────────────────────────
    private static TimeZoneInfo GetIstTz()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
        catch
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
            catch { return TimeZoneInfo.Utc; }
        }
    }

    private static DateTime TodayIst() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetIstTz()).Date;

    // ============ EXPORT ============
    public byte[] Export(IReadOnlyList<Lead> leads)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Leads");

        for (int c = 0; c < Columns.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = Columns[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2FF");
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        int r = 2;
        foreach (var l in leads)
        {
            ws.Cell(r, 1).Value  = l.FullName ?? "";
            ws.Cell(r, 2).Value  = l.Phone ?? "";
            ws.Cell(r, 3).Value  = l.AltPhone ?? "";
            ws.Cell(r, 4).Value  = l.Email ?? "";
            ws.Cell(r, 5).Value  = l.City ?? "";
            ws.Cell(r, 6).Value  = l.Location ?? "";
            ws.Cell(r, 7).Value  = l.Priority ?? "";
            ws.Cell(r, 8).Value  = l.Status ?? "";
            ws.Cell(r, 9).Value  = l.Source ?? "";
            ws.Cell(r, 10).Value = l.PrimarySource ?? "";
            ws.Cell(r, 11).Value = l.Staff ?? "";
            ws.Cell(r, 12).Value = l.RemarkAssignedTo ?? "";
            ws.Cell(r, 13).Value = l.LatestRemark ?? "";
            if (l.RemarkDate.HasValue)
            {
                ws.Cell(r, 14).Value = l.RemarkDate.Value;
                ws.Cell(r, 14).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
            }
            ws.Cell(r, 15).Value = l.Tags ?? "";
            ws.Cell(r, 16).Value = l.AdditionalRequirements ?? "";
            ws.Cell(r, 17).Value = l.Project ?? "";
            ws.Cell(r, 18).Value = l.BHK ?? "";
            ws.Cell(r, 19).Value = l.Furnished ?? "";
            ws.Cell(r, 20).Value = l.LookingFor ?? "";
            ws.Cell(r, 21).Value = l.AreaUnit ?? "";
            if (l.MinPrice.HasValue) ws.Cell(r, 22).Value = l.MinPrice.Value;
            if (l.MaxPrice.HasValue) ws.Cell(r, 23).Value = l.MaxPrice.Value;
            if (l.Price.HasValue)    ws.Cell(r, 24).Value = l.Price.Value;
            if (l.MinArea.HasValue)  ws.Cell(r, 25).Value = l.MinArea.Value;
            if (l.MaxArea.HasValue)  ws.Cell(r, 26).Value = l.MaxArea.Value;
            if (l.AreaSqft.HasValue) ws.Cell(r, 27).Value = l.AreaSqft.Value;
            ws.Cell(r, 28).Value = l.CustomerType ?? "";
            ws.Cell(r, 29).Value = l.CustomerCategory ?? "";
            ws.Cell(r, 30).Value = l.ChannelPartner ?? "";
            if (l.BirthDate.HasValue)
            {
                ws.Cell(r, 31).Value = l.BirthDate.Value;
                ws.Cell(r, 31).Style.DateFormat.Format = "yyyy-mm-dd";
            }
            r++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ============ PARSE IMPORT ============
    public async Task<ParseResult> ParseAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        var result = new ParseResult();
        _lookupCache = null;

        var isCsv = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);

        XLWorkbook wb;
        try
        {
            if (isCsv)
            {
                using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, 1024, leaveOpen: true);
                var lines = new List<string[]>();
                string? line;
                while ((line = await reader.ReadLineAsync(ct)) != null)
                    lines.Add(ParseCsvLine(line));

                if (lines.Count == 0)
                {
                    result.FileErrors.Add("The file is empty.");
                    return result;
                }

                wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Leads");
                for (int r = 0; r < lines.Count; r++)
                    for (int c = 0; c < lines[r].Length; c++)
                        ws.Cell(r + 1, c + 1).Value = lines[r][c];
            }
            else
            {
                wb = new XLWorkbook(stream);
            }
        }
        catch (Exception ex)
        {
            result.FileErrors.Add("Could not read file: " + ex.Message);
            return result;
        }

        using (wb)
        {
            var ws = wb.Worksheet(1);
            var used = ws.RangeUsed();
            if (used == null)
            {
                result.FileErrors.Add("The file is empty.");
                return result;
            }

            var headerRow = used.FirstRow();
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
                headerMap[cell.GetString().Trim()] = cell.Address.ColumnNumber;

            if (!headerMap.ContainsKey("FullName") || !headerMap.ContainsKey("Phone"))
            {
                result.FileErrors.Add("Header must contain at least 'FullName' and 'Phone' columns.");
                return result;
            }

            var defaultStatus   = await _db.LookupOptions.Where(x => x.Category == "Status"   && x.IsDefault && x.IsActive).Select(x => x.Value).FirstOrDefaultAsync(ct) ?? "Fresh Lead";
            var defaultPriority = await _db.LookupOptions.Where(x => x.Category == "Priority" && x.IsDefault && x.IsActive).Select(x => x.Value).FirstOrDefaultAsync(ct) ?? "Medium";
            var defaultAreaUnit = await _db.LookupOptions.Where(x => x.Category == "AreaUnit" && x.IsDefault && x.IsActive).Select(x => x.Value).FirstOrDefaultAsync(ct) ?? "Sqft";

            // Pre-load lookup cache ONCE
            await GetLookupCacheAsync(ct);
            var cache = _lookupCache!;

            var todayIst = TodayIst();

            string Get(IXLRangeRow row, string col) =>
                headerMap.TryGetValue(col, out var c) ? row.Cell(c).GetString().Trim() : "";

            decimal? Dec(string s) =>
                decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
            DateTime? Dt(string s) =>
                DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d
                : DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out var d2) ? d2 : null;

            foreach (var row in used.RowsUsed().Skip(1))
            {
                var rn = row.RowNumber();
                var lead = new Lead
                {
                    FullName               = Get(row, "FullName"),
                    Phone                  = Get(row, "Phone"),
                    AltPhone               = NullIfEmpty(Get(row, "AltPhone")),
                    Email                  = NullIfEmpty(Get(row, "Email")),
                    City                   = NullIfEmpty(Get(row, "City")),
                    Location               = NullIfEmpty(Get(row, "Location")),
                    Priority               = NullIfEmpty(Get(row, "Priority")),
                    Status                 = NullIfEmpty(Get(row, "Status")),
                    Source                 = NullIfEmpty(Get(row, "Source")),
                    PrimarySource          = NullIfEmpty(Get(row, "PrimarySource")),
                    Staff                  = NullIfEmpty(Get(row, "Staff")),
                    RemarkAssignedTo       = NullIfEmpty(Get(row, "RemarkAssignedTo")),
                    LatestRemark           = NullIfEmpty(Get(row, "LatestRemark")),
                    RemarkDate             = Dt(Get(row, "RemarkDate")),
                    Tags                   = NullIfEmpty(Get(row, "Tags")),
                    AdditionalRequirements = NullIfEmpty(Get(row, "AdditionalRequirements")),
                    Project                = NullIfEmpty(Get(row, "Project")),
                    BHK                    = NullIfEmpty(Get(row, "BHK")),
                    Furnished              = NullIfEmpty(Get(row, "Furnished")),
                    LookingFor             = NullIfEmpty(Get(row, "LookingFor")),
                    AreaUnit               = NullIfEmpty(Get(row, "AreaUnit")),
                    MinPrice               = Dec(Get(row, "MinPrice")),
                    MaxPrice               = Dec(Get(row, "MaxPrice")),
                    Price                  = Dec(Get(row, "Price")),
                    MinArea                = Dec(Get(row, "MinArea")),
                    MaxArea                = Dec(Get(row, "MaxArea")),
                    AreaSqft               = (int?)Dec(Get(row, "AreaSqft")),
                    CustomerType           = NullIfEmpty(Get(row, "CustomerType")),
                    CustomerCategory       = NullIfEmpty(Get(row, "CustomerCategory")),
                    ChannelPartner         = NullIfEmpty(Get(row, "ChannelPartner")),
                    BirthDate              = Dt(Get(row, "BirthDate")),
                    CreatedAt              = DateTime.UtcNow,
                    UpdatedAt              = DateTime.UtcNow,
                };

                // ═══════════════════════════════════════════════════════
                // PRODUCTION FIX: Roll past RemarkDate forward to today
                // ═══════════════════════════════════════════════════════
                if (lead.RemarkDate.HasValue)
                {
                    var originalDate = lead.RemarkDate.Value.Date;
                    if (originalDate < todayIst)
                    {
                        lead.RemarkDate = todayIst;

                        var suffix = $" · imported (adjusted from {originalDate:dd MMM yyyy})";
                        lead.LatestRemark = string.IsNullOrWhiteSpace(lead.LatestRemark)
                            ? $"Auto-adjusted from {originalDate:dd MMM yyyy}"
                            : lead.LatestRemark + suffix;
                    }
                }

                var ir = new LeadImportRow { RowNumber = rn, Lead = lead };

                if (string.IsNullOrWhiteSpace(lead.FullName))
                    ir.Errors.Add("FullName is required.");

                if (string.IsNullOrWhiteSpace(lead.Phone))
                    ir.Errors.Add("Phone is required.");
                else if (!System.Text.RegularExpressions.Regex.IsMatch(lead.Phone, @"^\d{6,15}$"))
                    ir.Errors.Add("Phone must be 6–15 digits.");

                if (lead.Email is { Length: > 0 } && !lead.Email.Contains('@'))
                    ir.Errors.Add("Email is not valid.");

                lead.Status   ??= defaultStatus;
                lead.Priority ??= defaultPriority;
                lead.AreaUnit ??= defaultAreaUnit;

                if (ir.IsValid)
                {
                    EnsureLookupSync(cache, "Status",           lead.Status);
                    EnsureLookupSync(cache, "Priority",         lead.Priority);
                    EnsureLookupSync(cache, "BHK",              lead.BHK);
                    EnsureLookupSync(cache, "Furnished",        lead.Furnished);
                    EnsureLookupSync(cache, "LookingFor",       lead.LookingFor);
                    EnsureLookupSync(cache, "AreaUnit",         lead.AreaUnit);
                    EnsureLookupSync(cache, "CustomerType",     lead.CustomerType);
                    EnsureLookupSync(cache, "CustomerCategory", lead.CustomerCategory);
                    EnsureLookupSync(cache, "Source",           lead.Source);
                    EnsureLookupSync(cache, "PrimarySource",    lead.PrimarySource);
                }

                result.Rows.Add(ir);
            }
        }

        return result;
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    // ============ LOOKUP CACHE ============
    private async Task<Dictionary<(string Category, string Value), bool>> GetLookupCacheAsync(CancellationToken ct)
    {
        if (_lookupCache != null) return _lookupCache;

        var existing = await _db.LookupOptions
            .AsNoTracking()
            .Select(x => new { x.Category, x.Value })
            .ToListAsync(ct);

        _lookupCache = new Dictionary<(string, string), bool>(
            existing.Count,
            new LookupKeyComparer());

        foreach (var x in existing)
            _lookupCache[(x.Category ?? "", x.Value ?? "")] = true;

        return _lookupCache;
    }

    private void EnsureLookupSync(
        Dictionary<(string Category, string Value), bool> cache,
        string category,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        var key = (category, value.Trim());
        if (cache.ContainsKey(key)) return;

        _db.LookupOptions.Add(new LookupOption
        {
            Category  = category,
            Value     = value.Trim(),
            Label     = null,
            Color     = null,
            SortOrder = 999,
            IsDefault = false,
            IsActive  = true
        });

        cache[key] = true;
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(ch);
            }
            else
            {
                if (ch == '"') inQuotes = true;
                else if (ch == ',') { fields.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(ch);
            }
        }
        fields.Add(sb.ToString());
        return fields.ToArray();
    }
}

internal sealed class LookupKeyComparer : IEqualityComparer<(string Category, string Value)>
{
    public bool Equals((string Category, string Value) x, (string Category, string Value) y) =>
        string.Equals(x.Category, y.Category, StringComparison.OrdinalIgnoreCase)
        && string.Equals(x.Value, y.Value, StringComparison.OrdinalIgnoreCase);

    public int GetHashCode((string Category, string Value) obj) =>
        HashCode.Combine(
            (obj.Category ?? "").ToLowerInvariant(),
            (obj.Value    ?? "").ToLowerInvariant());
}