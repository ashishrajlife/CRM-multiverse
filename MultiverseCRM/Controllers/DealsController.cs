using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiverseCRM.Data;
using MultiverseCRM.Models;
using MultiverseCRM.Services;
using MultiverseCRM.ViewModels;

namespace MultiverseCRM.Controllers;

[Authorize]
public class DealsController : Controller
{
    private readonly AppDbContext _context;
    private readonly IActivityLogger _activity;

    public DealsController(AppDbContext context, IActivityLogger activity)
    {
        _context = context;
        _activity = activity;
    }

    // ---- helpers ----
    private IQueryable<Deal> FilteredQuery(
        string? stage, string? status, string? search, string? owner,
        string sortBy = "UpdatedAt", string sortOrder = "desc")
    {
        var q = _context.Deals.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(d =>
                (d.Project != null && d.Project.Contains(search)) ||
                (d.ProjectEntity != null && d.ProjectEntity.Name.Contains(search)) ||
                (d.PropertyEntity != null &&
                    (d.PropertyEntity.Title != null && d.PropertyEntity.Title.Contains(search))) ||
                (d.Location != null && d.Location.Contains(search)) ||
                (d.Notes != null && d.Notes.Contains(search)) ||
                (d.Lead != null && d.Lead.FullName.Contains(search)));

        if (!string.IsNullOrWhiteSpace(stage) && stage != "All")
            q = q.Where(d => d.Stage == stage);

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
            q = q.Where(d => d.Status == status);

        if (!string.IsNullOrWhiteSpace(owner) && owner != "All")
            q = q.Where(d => d.OwnerDisplayName == owner || d.OwnerUserName == owner);

        var asc = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);
        q = sortBy switch
        {
            "Value"              => asc ? q.OrderBy(d => d.Value)              : q.OrderByDescending(d => d.Value),
            "ExpectedCloseDate"  => asc ? q.OrderBy(d => d.ExpectedCloseDate)  : q.OrderByDescending(d => d.ExpectedCloseDate),
            "CreatedAt"          => asc ? q.OrderBy(d => d.CreatedAt)          : q.OrderByDescending(d => d.CreatedAt),
            _                    => asc ? q.OrderBy(d => d.UpdatedAt)          : q.OrderByDescending(d => d.UpdatedAt)
        };
        return q;
    }

    private static string ActorDisplayName(System.Security.Claims.ClaimsPrincipal user) =>
        user.FindFirst("FullName")?.Value
        ?? user.Identity?.Name
        ?? "System";

    public async Task<IActionResult> Index(
        string? stage, string? search, string? owner,
        string? status = "All",
        string sortBy = "UpdatedAt", string sortOrder = "desc",
        int page = 1, int pageSize = 10)
    {
        pageSize = new[] { 10, 25, 50, 100 }.Contains(pageSize) ? pageSize : 10;

        var query = FilteredQuery(stage, status, search, owner, sortBy, sortOrder);

        var filteredCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var pageDeals = await query
            .Include(d => d.Lead)
            .Include(d => d.ProjectEntity)
            .Include(d => d.PropertyEntity)
                .ThenInclude(p => p.Project)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var stageCounts = await _context.Deals
            .GroupBy(d => d.Stage)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var stageValues = await _context.Deals
            .Where(d => d.Status == "Open")
            .GroupBy(d => d.Stage)
            .Select(g => new { g.Key, Value = g.Sum(d => d.Value) })
            .ToDictionaryAsync(x => x.Key, x => x.Value);

        var owners = await _context.Deals
            .Where(d => d.OwnerDisplayName != null)
            .Select(d => d.OwnerDisplayName!)
            .Distinct()
            .OrderBy(o => o)
            .ToListAsync();

        var allTags = await _context.Deals
            .Where(d => !string.IsNullOrEmpty(d.Tags))
            .Select(d => d.Tags!)
            .ToListAsync();

        var tagSet = new HashSet<string>();
        foreach (var t in allTags)
            foreach (var tag in t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                tagSet.Add(tag);

        var boardDeals = await FilteredQuery(stage, status, search, owner, sortBy, sortOrder)
            .Include(d => d.Lead)
            .Take(500)
            .ToListAsync();

        // ---- Leads dropdown for the create/edit modal ----
        // Only leads with no deal attached yet OR the deal currently editing.
        var leadsWithDeals = await _context.Deals
            .Where(d => d.LeadId != null)
            .Select(d => d.LeadId!.Value)
            .ToHashSetAsync();

        var allLeads = await _context.Leads
            .OrderByDescending(l => l.CreatedAt)
            .Take(1000)
            .Select(l => new { l.Id, l.FullName, l.Phone, l.Status })
            .ToListAsync();

        var leadOptions = allLeads
            .Select(l => new LeadOption(
                l.Id,
                $"{l.FullName} · {l.Phone}" + (string.IsNullOrEmpty(l.Status) ? "" : $" ({l.Status})"),
                leadsWithDeals.Contains(l.Id)
            ))
            .ToList();
        
        var projectOptions = await LoadProjectOptionsAsync();
        var propertyOptions = await LoadPropertyOptionsAsync();    

        var vm = new DealListViewModel
        {
            ActiveStage = stage ?? "All",
            ActiveStatus = status ?? "All",
            SearchTerm = search,
            OwnerFilter = owner,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Page = page,
            PageSize = pageSize,
            FilteredCount = filteredCount,
            StageCounts = stageCounts,
            StageValues = stageValues,
            TotalCount = stageCounts.Values.Sum(),
            TotalPipelineValue = await _context.Deals.Where(d => d.Status == "Open").SumAsync(d => d.Value),
            TotalWonValue = await _context.Deals.Where(d => d.Status == "Won").SumAsync(d => d.Value),
            Deals = pageDeals,
            AllOwners = owners,
            AllTags = tagSet.OrderBy(x => x).ToList(),
            AllLeads = leadOptions,
            AllProjects = projectOptions,
            AllProperties = propertyOptions,
        };

        return View(vm);
    }

    // ---- BOARD (kanban JSON) ----
    [HttpGet]
    public async Task<IActionResult> Board(string? owner, string? search)
    {
        var q = _context.Deals
            .AsNoTracking()
            .Where(d => d.Status == "Open")
            .Include(d => d.Lead)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(owner) && owner != "All")
            q = q.Where(d => d.OwnerDisplayName == owner);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(d => d.Project!.Contains(search) || d.Lead!.FullName.Contains(search));

        var deals = await q.OrderByDescending(d => d.UpdatedAt).Take(500).ToListAsync();

        var grouped = deals.GroupBy(d => d.Stage)
            .ToDictionary(g => g.Key, g => g.Select(d => new
            {
                d.Id,
                d.Stage,
                d.Project,
                d.BHK,
                d.Location,
                d.Value,
                d.ExpectedCloseDate,
                lead = d.Lead?.FullName,
                owner = d.OwnerDisplayName ?? d.OwnerUserName,
                updatedAt = d.UpdatedAt,
                age = FormatAge(d.UpdatedAt)
            }).ToList());

        return Json(grouped);
    }

    // ---- CREATE ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Deal deal, int? ProjectId, int? PropertyId)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please fill all required fields correctly.";
            return RedirectToAction(nameof(Index));
        }

        // Project and Property are independent selections.
        // A standalone Property must not implicitly overwrite ProjectId.
        var selectionsValid = await ApplyProjectPropertySelectionsAsync(
            deal, ProjectId, PropertyId, usePropertyPriceWhenValueMissing: true);

        if (!selectionsValid)
        {
            TempData["Error"] = "The selected project or property is no longer available. Please refresh and try again.";
            return RedirectToAction(nameof(Index));
        }

        deal.CreatedAt = DateTime.UtcNow;
        deal.UpdatedAt = DateTime.UtcNow;
        deal.OwnerUserName ??= User.Identity?.Name;
        deal.OwnerDisplayName ??= ActorDisplayName(User);

        _context.Deals.Add(deal);
        await _context.SaveChangesAsync();

        await _activity.LogAsync(
            entity: "Deal",
            action: "Created",
            title: deal.Project ?? $"Deal #{deal.Id}",
            detail: $"₹{deal.Value:N0} · {deal.Stage}",
            url: Url.Action("Index", "Deals"),
            entityId: deal.Id);

        TempData["Success"] = "Deal created successfully!";
        return RedirectToAction(nameof(Index));
    }

    // ---- EDIT ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Deal deal, int? ProjectId, int? PropertyId)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please check the submitted deal details and try again.";
            return RedirectToAction(nameof(Index));
        }

        var existing = await _context.Deals
            .FirstOrDefaultAsync(d => d.Id == deal.Id);

        if (existing == null)
        {
            TempData["Error"] = "Deal not found.";
            return RedirectToAction(nameof(Index));
        }

        // Resolve both selectors independently and refresh the Property snapshot.
        // Do not copy null BHK/Location/City/Area values from the edit form:
        // those fields are not posted by _EditDealModal.
        var selectionsValid = await ApplyProjectPropertySelectionsAsync(
            existing, ProjectId, PropertyId);

        if (!selectionsValid)
        {
            TempData["Error"] = "The selected project or property is no longer available. Please refresh and try again.";
            return RedirectToAction(nameof(Index));
        }

        existing.LeadId = deal.LeadId;
        existing.Stage = deal.Stage;
        existing.Value = deal.Value;
        existing.BookingAmount = deal.BookingAmount;
        existing.ReceivedAmount = deal.ReceivedAmount;
        existing.ExpectedCloseDate = deal.ExpectedCloseDate;
        existing.Status = deal.Status;
        existing.OwnerUserName = deal.OwnerUserName ?? existing.OwnerUserName;
        existing.OwnerDisplayName = deal.OwnerDisplayName ?? existing.OwnerDisplayName;
        existing.Notes = deal.Notes;
        existing.Tags = deal.Tags;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activity.LogAsync(
            entity: "Deal",
            action: "Updated",
            title: existing.Project ?? $"Deal #{existing.Id}",
            detail: $"₹{existing.Value:N0} · {existing.Stage}",
            url: Url.Action("Index", "Deals"),
            entityId: existing.Id);

        TempData["Success"] = "Deal updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Applies the independently selected Project and Property to a Deal.
    /// ProjectId always comes from the Project dropdown; PropertyId always
    /// comes from the Property dropdown. A Property's own ProjectId is never
    /// used to silently replace the user's Project selection.
    /// </summary>
    private async Task<bool> ApplyProjectPropertySelectionsAsync(
        Deal target,
        int? projectId,
        int? propertyId,
        bool usePropertyPriceWhenValueMissing = false)
    {
        var selectedProject = projectId.HasValue
            ? await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId.Value &&
                    (p.IsActive || _context.Deals.Any(d => d.ProjectId == p.Id)))
            : null;

        if (projectId.HasValue && selectedProject == null)
            return false;

        var selectedProperty = propertyId.HasValue
            ? await _context.Properties
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == propertyId.Value &&
                    (p.IsActive || _context.Deals.Any(d => d.PropertyId == p.Id)))
            : null;

        if (propertyId.HasValue && selectedProperty == null)
            return false;

        // Independent Project relationship + display snapshot.
        target.ProjectId = projectId;
        target.Project = selectedProject?.Name;

        // Property relationship + denormalized display snapshot.
        target.PropertyId = propertyId;

        if (selectedProperty != null)
        {
            target.BHK = selectedProperty.BHK;
            target.Location = selectedProperty.Location;
            target.City = selectedProperty.City;
            target.AreaSqft = selectedProperty.CarpetArea ?? selectedProperty.BuiltUpArea;

            if (usePropertyPriceWhenValueMissing &&
                target.Value <= 0 &&
                selectedProperty.TotalPrice > 0)
            {
                target.Value = (decimal)selectedProperty.TotalPrice;
            }
        }
        else
        {
            // Clearing Property must also clear its old snapshot values.
            target.BHK = null;
            target.Location = null;
            target.City = null;
            target.AreaSqft = null;
        }

        return true;
    }

    // ---- DELETE ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var d = await _context.Deals.FindAsync(id);
        if (d != null)
        {
            _context.Deals.Remove(d);
            await _context.SaveChangesAsync();

            await _activity.LogAsync(
                entity: "Deal",
                action: "Deleted",
                title: d.Project ?? $"Deal #{d.Id}",
                detail: $"₹{d.Value:N0}",
                entityId: d.Id);

            TempData["Success"] = "Deal deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ---- MOVE STAGE (kanban drag-drop) ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveStage([FromBody] MoveStageRequest req)
    {
        var d = await _context.Deals.FindAsync(req.Id);
        if (d == null) return NotFound();

        var oldStage = d.Stage;
        d.Stage = req.Stage;
        d.UpdatedAt = DateTime.UtcNow;

        if (req.Stage == "Closed Won" || req.Stage == "Closed Lost")
        {
            d.Status = req.Stage == "Closed Won" ? "Won" : "Lost";
            d.ClosedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        await _activity.LogAsync(
            entity: "Deal",
            action: "MovedStage",
            title: d.Project ?? $"Deal #{d.Id}",
            detail: $"{oldStage} → {req.Stage}",
            url: Url.Action("Index", "Deals"),
            entityId: d.Id);

        return Json(new { ok = true });
    }

    // ---- CLOSE WON ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseWon(int id)
    {
        var d = await _context.Deals.FindAsync(id);
        if (d == null) return NotFound();

        d.Status = "Won";
        d.Stage = "Closed Won";
        d.ClosedAt = DateTime.UtcNow;
        d.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _activity.LogAsync(
            entity: "Deal",
            action: "ClosedWon",
            title: d.Project ?? $"Deal #{d.Id}",
            detail: $"₹{d.Value:N0}",
            url: Url.Action("Index", "Deals"),
            entityId: d.Id);

        return Json(new { ok = true });
    }

    // ---- CLOSE LOST ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseLost([FromBody] CloseDealRequest req)
    {
        var d = await _context.Deals.FindAsync(req.Id);
        if (d == null) return NotFound();

        d.Status = "Lost";
        d.Stage = "Closed Lost";
        d.LostReason = req.LostReason;
        d.ClosedAt = DateTime.UtcNow;
        d.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _activity.LogAsync(
            entity: "Deal",
            action: "ClosedLost",
            title: d.Project ?? $"Deal #{d.Id}",
            detail: req.LostReason ?? "No reason given",
            url: Url.Action("Index", "Deals"),
            entityId: d.Id);

        return Json(new { ok = true });
    }

    private static string FormatAge(DateTime utc)
    {
        var diff = DateTime.UtcNow - utc;
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
        return utc.ToString("dd MMM");
    }

    // ═══════════════════════════════════════════════════════
    // HELPERS — options for Create/Edit modals
    // ═══════════════════════════════════════════════════════
    private async Task<List<ProjectOption>> LoadProjectOptionsAsync(CancellationToken ct = default)
    {
        return await _context.Projects.AsNoTracking()
            // Keep inactive records that are already referenced by a Deal so
            // the edit modal can display and preserve existing associations.
            .Where(p => p.IsActive || _context.Deals.Any(d => d.ProjectId == p.Id))
            .OrderBy(p => p.Name)
            .Select(p => new ProjectOption { Id = p.Id, Name = p.Name })
            .ToListAsync(ct);
    }

    private async Task<List<PropertyOption>> LoadPropertyOptionsAsync(CancellationToken ct = default)
    {
        return await _context.Properties.AsNoTracking()
            // Keep inactive records that are already referenced by a Deal so
            // the edit modal can display and preserve existing associations.
            .Where(p => p.IsActive || _context.Deals.Any(d => d.PropertyId == p.Id))
            .OrderBy(p => p.UnitNumber)
            .Select(p => new PropertyOption
            {
                Id = p.Id,
                UnitNumber = p.UnitNumber,
                Title = p.Title,
                ProjectId = p.ProjectId,
                ProjectName = p.Project != null ? p.Project.Name : null,
                BHK = p.BHK,
                City = p.City,
                Location = p.Location,
                TotalPrice = p.TotalPrice,
                CarpetArea = p.CarpetArea,
                BuiltUpArea = p.BuiltUpArea,
                Status = p.Status,
                PropertyType = p.PropertyType,
                AreaUnit = p.AreaUnit
            })
            .ToListAsync(ct);
    }   

    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ToggleClose(int id, string password)
{
    const string closeDealPassword = "1234";

    if (!string.Equals(password, closeDealPassword, StringComparison.Ordinal))
    {
        return BadRequest(new
        {
            ok = false,
            message = "Incorrect password."
        });
    }

    var deal = await _context.Deals.FindAsync(id);

    if (deal == null)
    {
        return NotFound(new
        {
            ok = false,
            message = "Deal not found."
        });
    }

    bool isClosed = deal.ClosedAt.HasValue
                    || deal.Status == "Won"
                    || deal.Status == "Lost";

    if (isClosed)
    {
        // Reopen deal
        deal.Status = "Open";
        deal.ClosedAt = null;
    }
    else
    {
        // Close deal
        deal.Status = "Won";
        deal.ClosedAt = DateTime.UtcNow;
    }

    deal.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    await _activity.LogAsync(
        entity: "Deal",
        action: isClosed ? "Reopened" : "Closed",
        title: deal.Project ?? $"Deal #{deal.Id}",
        detail: isClosed ? "Deal reopened" : $"Deal closed - ₹{deal.Value:N0}",
        url: Url.Action("Index", "Deals"),
        entityId: deal.Id
    );

    return Json(new
    {
        ok = true,
        message = isClosed
            ? "Deal reopened successfully."
            : "Deal closed successfully."
    });
}

}