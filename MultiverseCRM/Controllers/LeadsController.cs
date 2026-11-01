using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiverseCRM.Data;
using MultiverseCRM.Services;
using MultiverseCRM.Models;
using MultiverseCRM.ViewModels;

namespace MultiverseCRM.Controllers;

[Authorize]
public class LeadsController : Controller
{
    private readonly AppDbContext _context;
private readonly ILeadImportExportService _importExport;
    public LeadsController(AppDbContext context, ILeadImportExportService importExport)
    {
        _context = context;
        _importExport = importExport;
    }

    private IQueryable<Lead> BuildFilteredQuery(
    string? status, string? search, string? sortBy, string? sortOrder)
{
    var query = _context.Leads.AsNoTracking().AsQueryable();

    // Search
    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(l =>
            l.FullName.Contains(search) ||
            l.Phone.Contains(search) ||
            (l.Email != null && l.Email.Contains(search)));

    // Status
    if (!string.IsNullOrWhiteSpace(status) && status != "All")
        query = query.Where(l => l.Status == status);

    // Sort (match the exact strings Index uses)
    sortBy ??= "CreatedAt";
    sortOrder ??= "desc";
    var asc = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);

    query = sortBy switch
    {
        "CreatedAt"  => asc ? query.OrderBy(l => l.CreatedAt)  : query.OrderByDescending(l => l.CreatedAt),
        "UpdatedAt"  => asc ? query.OrderBy(l => l.UpdatedAt)  : query.OrderByDescending(l => l.UpdatedAt),
        "RemarkDate" => asc ? query.OrderBy(l => l.RemarkDate) : query.OrderByDescending(l => l.RemarkDate),
        "AreaSqft"   => asc ? query.OrderBy(l => l.AreaSqft)   : query.OrderByDescending(l => l.AreaSqft),
        _            => query.OrderByDescending(l => l.CreatedAt)
    };

    return query;
}

   public async Task<IActionResult> Index(
    string? search, string? status,
    string sortBy = "CreatedAt", string sortOrder = "desc",
    int page = 1, int pageSize = 25)
    {
        // Base query
        var query = _context.Leads.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(l =>
                l.FullName.Contains(search) ||
                l.Phone.Contains(search) ||
                (l.Email != null && l.Email.Contains(search)));

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
            query = query.Where(l => l.Status == status);

        // Sorting
        query = sortBy switch
        {
            "CreatedAt" => sortOrder == "asc"
                ? query.OrderBy(l => l.CreatedAt) : query.OrderByDescending(l => l.CreatedAt),
            "UpdatedAt" => sortOrder == "asc"
                ? query.OrderBy(l => l.UpdatedAt) : query.OrderByDescending(l => l.UpdatedAt),
            "RemarkDate" => sortOrder == "asc"
                ? query.OrderBy(l => l.RemarkDate) : query.OrderByDescending(l => l.RemarkDate),
            "AreaSqft" => sortOrder == "asc"
                ? query.OrderBy(l => l.AreaSqft) : query.OrderByDescending(l => l.AreaSqft),
            _ => query.OrderByDescending(l => l.CreatedAt)
        };

        // Get all distinct tags for Tom Select suggestions
        var allTags = await _context.Leads
            .Where(l => !string.IsNullOrEmpty(l.Tags))
            .Select(l => l.Tags!)
            .ToListAsync();

        var tagSet = new HashSet<string>();
        foreach (var t in allTags)
            foreach (var tag in t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                tagSet.Add(tag);

                pageSize = new[] { 10, 25, 50, 100 }.Contains(pageSize) ? pageSize : 25;
var filteredCount = await query.CountAsync();
var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)pageSize));
page = Math.Clamp(page, 1, totalPages);

var statusCounts = await _context.Leads.GroupBy(l => l.Status)
    .Select(g => new { g.Key, Count = g.Count() })
    .ToDictionaryAsync(x => x.Key, x => x.Count);


       var vm = new LeadListViewModel
{
    ActiveStatus = status ?? "All",
    SearchTerm = search,
    SortBy = sortBy,
    SortOrder = sortOrder,
    AllTags = tagSet.OrderBy(x => x).ToList(),
    StatusCounts = statusCounts,
    TotalCount = statusCounts.Values.Sum(),
    FilteredCount = filteredCount,
    Page = page,
    PageSize = pageSize,
    Leads = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync()
};

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lead lead)
    {
        if (ModelState.IsValid)
        {
            lead.CreatedAt = DateTime.UtcNow;
            lead.UpdatedAt = DateTime.UtcNow;
            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Lead created successfully!";
        }
        else
        {
            TempData["Error"] = "Please fill all required fields correctly.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Lead lead)
    {
        if (ModelState.IsValid)
        {
            var existing = await _context.Leads.FindAsync(lead.Id);
            if (existing == null)
            {
                TempData["Error"] = "Lead not found.";
                return RedirectToAction(nameof(Index));
            }

            existing.FullName = lead.FullName;
         existing.Phone = CleanPhone(lead.Phone);
existing.AltPhone = string.IsNullOrWhiteSpace(lead.AltPhone) ? null : CleanPhone(lead.AltPhone);
            existing.Email = lead.Email;
            existing.Priority = lead.Priority;
            existing.Status = lead.Status;
            existing.Source = lead.Source;
            existing.PrimarySource = lead.PrimarySource;
            existing.Staff = lead.Staff;
            existing.ChannelPartner = lead.ChannelPartner;
            existing.LatestRemark = lead.LatestRemark;
            existing.RemarkAssignedTo = lead.RemarkAssignedTo;
            existing.RemarkDate = lead.RemarkDate;
            existing.Tags = lead.Tags;
            existing.AdditionalRequirements = lead.AdditionalRequirements;
            existing.Project = lead.Project;
            existing.BHK = lead.BHK;
            existing.Location = lead.Location;
            existing.City = lead.City;
            existing.Price = lead.Price;
            existing.MinPrice = lead.MinPrice;
            existing.MaxPrice = lead.MaxPrice;
            existing.MinArea = lead.MinArea;
            existing.MaxArea = lead.MaxArea;
            existing.AreaSqft = lead.AreaSqft;
            existing.AreaUnit = lead.AreaUnit;
            existing.Furnished = lead.Furnished;
            existing.BirthDate = lead.BirthDate;
            existing.LookingFor = lead.LookingFor;
            existing.CustomerType = lead.CustomerType;
            existing.CustomerCategory = lead.CustomerCategory;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Lead updated successfully!";
        }
        else
        {
            TempData["Error"] = "Please fill all required fields correctly.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead != null)
        {
            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Lead deleted successfully!";
        }
        return RedirectToAction(nameof(Index));
    }

    // AJAX endpoint to create a new tag globally
    [HttpPost]
    public IActionResult AddTag([FromBody] string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return Json(new { success = false });
        // Tags are stored per-lead; this endpoint just confirms tag acceptance.
        // Frontend adds it to the select for the current lead.
        return Json(new { success = true, tag = tag.Trim() });
    }
    
    private static string CleanPhone(string? p)
{
    var d = new string((p ?? "").Where(char.IsDigit).ToArray());
    return d.Length > 10 ? d[^10..] : d;
}

public record UpdateTagsRequest(int Id, string? Tags);

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateTags([FromBody] UpdateTagsRequest req)
{
    var lead = await _context.Leads.FindAsync(req.Id);
    if (lead == null) return NotFound();
    lead.Tags = req.Tags;
    lead.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
    return Json(new { success = true });
}
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteMany(string ids)
{
    if (string.IsNullOrWhiteSpace(ids))
        return RedirectToAction(nameof(Index));

    var idList = ids
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(int.Parse)
        .ToList();

    var leads = await _context.Leads.Where(l => idList.Contains(l.Id)).ToListAsync();
    _context.Leads.RemoveRange(leads);
    await _context.SaveChangesAsync();

    TempData["Success"] = $"{leads.Count} lead(s) deleted.";
    return RedirectToAction(nameof(Index));
}
[HttpGet]
public IActionResult Export(string? status, string? search, string? sortBy, string? sortOrder)
{
    // Reuse whatever your Index does to load Model.Leads — ideally factor the query into a private helper.
    var leads = BuildFilteredQuery(status, search, sortBy, sortOrder).ToList();  // <-- your existing filter logic
    var bytes = _importExport.Export(leads);
    var fileName = $"leads_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
    return File(bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        fileName);
}

[HttpPost]
[RequestSizeLimit(20_000_000)] // 20 MB
[ValidateAntiForgeryToken]
public async Task<IActionResult> Import(IFormFile file)
{
    if (file == null || file.Length == 0)
        return BadRequest("No file uploaded.");

    using var stream = file.OpenReadStream();
    var parsed = await _importExport.ParseAsync(stream, file.FileName);

    if (parsed.FileErrors.Any())
        return BadRequest(string.Join(" | ", parsed.FileErrors));

    // Insert valid rows; skip duplicates by Phone (existing in DB OR seen earlier in this file)
    var existingPhones = await _context.Leads
        .Where(l => parsed.Rows.Select(r => r.Lead.Phone).Contains(l.Phone))
        .Select(l => l.Phone)
        .ToListAsync();
    var seen = new HashSet<string>(existingPhones);

    var vm = new LeadImportResultViewModel { TotalRows = parsed.Rows.Count };

    foreach (var row in parsed.Rows)
    {
        var res = new ImportRowResult
        {
            RowNumber = row.RowNumber,
            FullName  = row.Lead.FullName,
            Phone     = row.Lead.Phone
        };

        if (!row.IsValid)
        {
            res.Status  = "Error";
            res.Message = string.Join("; ", row.Errors);
            vm.ErrorCount++;
        }
        else if (seen.Contains(row.Lead.Phone))
        {
            res.Status  = "Skipped (duplicate)";
            res.Message = "Phone already exists.";
            vm.SkippedCount++;
        }
        else
        {
            _context.Leads.Add(row.Lead);
            seen.Add(row.Lead.Phone);
            res.Status  = "Inserted";
            res.Message = "OK";
            vm.InsertedCount++;
        }
        vm.Rows.Add(res);
    }

    await _context.SaveChangesAsync();
    return Json(vm);
}


}