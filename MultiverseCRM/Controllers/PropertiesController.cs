using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiverseCRM.Data;
using MultiverseCRM.Models;
using MultiverseCRM.Services;
using MultiverseCRM.ViewModels;

namespace MultiverseCRM.Controllers;

[Authorize]
public class PropertiesController : Controller
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _files;
    private readonly IActivityLogger _activity;
    private readonly ILogger<PropertiesController> _log;

    public PropertiesController(
        AppDbContext context,
        IFileStorageService files,
        IActivityLogger activity,
        ILogger<PropertiesController> log)
    {
        _context = context;
        _files = files;
        _activity = activity;
        _log = log;
    }

    private string CurrentUserDisplay =>
        User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "System";

    // ═══════════════════════════════════════════════════════
    // INDEX — SQL pagination, filters, search
    // ═══════════════════════════════════════════════════════
    public async Task<IActionResult> Index(
        string? search = null,
        string? status = null,
        string? city = null,
        string? bhk = null,
        int? projectId = null,
        string sortBy = "CreatedAt",
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 24,
        CancellationToken ct = default)
    {
        pageSize = new[] { 12, 24, 48, 96 }.Contains(pageSize) ? pageSize : 24;
        page = Math.Max(1, page);

        var q = _context.Properties.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p =>
                p.UnitNumber.Contains(s) ||
                (p.Title != null && p.Title.Contains(s)) ||
                (p.City != null && p.City.Contains(s)) ||
                (p.Location != null && p.Location.Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(city))
            q = q.Where(p => p.City == city);

        if (!string.IsNullOrWhiteSpace(bhk))
            q = q.Where(p => p.BHK == bhk);

        if (projectId.HasValue)
            q = q.Where(p => p.ProjectId == projectId.Value);

        bool asc = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);
        q = sortBy switch
        {
            "UnitNumber"  => asc ? q.OrderBy(p => p.UnitNumber) : q.OrderByDescending(p => p.UnitNumber),
            "TotalPrice"  => asc ? q.OrderBy(p => p.TotalPrice) : q.OrderByDescending(p => p.TotalPrice),
            "CarpetArea"  => asc ? q.OrderBy(p => p.CarpetArea) : q.OrderByDescending(p => p.CarpetArea),
            "Status"      => asc ? q.OrderBy(p => p.Status) : q.OrderByDescending(p => p.Status),
            _             => asc ? q.OrderBy(p => p.CreatedAt) : q.OrderByDescending(p => p.CreatedAt)
        };

        // SEQUENTIAL queries — same _context safety
        var total = await q.CountAsync(ct);

        var stats = await _context.Properties.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(p => p.IsActive),
                Available = g.Count(p => p.IsActive && p.Status == "Available"),
                Booked = g.Count(p => p.IsActive && p.Status == "Booked"),
                Sold = g.Count(p => p.IsActive && p.Status == "Sold")
            })
            .FirstOrDefaultAsync(ct);

        var cityOptions = await _context.Properties.AsNoTracking()
            .Where(p => p.City != null && p.City != "" && p.IsActive)
            .Select(p => p.City!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);

        var bhkOptions = await _context.Properties.AsNoTracking()
            .Where(p => p.BHK != null && p.BHK != "" && p.IsActive)
            .Select(p => p.BHK!)
            .Distinct()
            .OrderBy(b => b)
            .ToListAsync(ct);

        var projectOptions = await _context.Projects.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new ProjectOption { Id = p.Id, Name = p.Name })
            .ToListAsync(ct);

        int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, totalPages);

        var properties = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PropertyCard
            {
                Id = p.Id,
                UnitNumber = p.UnitNumber,
                Title = p.Title,
                ProjectName = p.Project != null ? p.Project.Name : null,
                PropertyType = p.PropertyType,
                BHK = p.BHK,
                Furnished = p.Furnished,
                CarpetArea = p.CarpetArea,
                BuiltUpArea = p.BuiltUpArea,
                TotalPrice = p.TotalPrice,
                City = p.City,
                Location = p.Location,
                Status = p.Status,
                HeroImagePath = p.HeroImagePath,
                IsActive = p.IsActive,
                IsFeatured = p.IsFeatured,
                ImageCount = p.Images.Count(i => i.IsActive),
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);

        var vm = new PropertyListViewModel
        {
            Search = search,
            StatusFilter = status,
            CityFilter = city,
            BHKFilter = bhk,
            ProjectFilter = projectId,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Page = page,
            PageSize = pageSize,
            TotalFiltered = total,
            TotalCount = stats?.Total ?? 0,
            AvailableCount = stats?.Available ?? 0,
            BookedCount = stats?.Booked ?? 0,
            SoldCount = stats?.Sold ?? 0,
            Properties = properties,
            StatusOptions = new List<string> { "Available", "Booked", "Sold", "Blocked", "Under Construction" },
            CityOptions = cityOptions,
            BHKOptions = bhkOptions,
            ProjectOptions = projectOptions
        };

        return View(vm);
    }

    // ═══════════════════════════════════════════════════════
    // DETAILS
    // ═══════════════════════════════════════════════════════
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var property = await _context.Properties.AsNoTracking()
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (property == null) return NotFound();

        var images = await _context.PropertyImages.AsNoTracking()
            .Where(i => i.PropertyId == id && i.IsActive)
            .OrderBy(i => i.DisplayOrder)
            .ThenBy(i => i.Id)
            .ToListAsync(ct);

        var vm = new PropertyDetailViewModel
        {
            Property = property,
            Project = property.Project,
            Images = images
        };

        return View(vm);
    }

    // ═══════════════════════════════════════════════════════
    // CREATE (GET)
    // ═══════════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Create(int? projectId, CancellationToken ct)
    {
        var projects = await _context.Projects.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new ProjectOption { Id = p.Id, Name = p.Name })
            .ToListAsync(ct);

        var vm = new PropertyCreateViewModel
        {
            Property = new Property { ProjectId = projectId },
            AvailableProjects = projects
        };

        // Pre-fill from project if selected
        if (projectId.HasValue)
        {
            var project = await _context.Projects.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId.Value, ct);
            if (project != null)
            {
                vm.Property.City = project.City;
                vm.Property.Location = project.Location;
            }
        }

        return View(vm);
    }

    // ═══════════════════════════════════════════════════════
    // CREATE (POST)
    // ═══════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Create(PropertyCreateViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.AvailableProjects = await LoadProjectOptionsAsync(ct);
            TempData["Error"] = "Please fill required fields.";
            return View(vm);
        }

        var property = vm.Property;
        property.Id = 0;
        property.CreatedAt = DateTime.UtcNow;
        property.UpdatedAt = DateTime.UtcNow;
        property.CreatedBy = CurrentUserDisplay;
        property.IsActive = true;

        _context.Properties.Add(property);
        await _context.SaveChangesAsync(ct);

        // Save images
        string? heroPath = null;
        if (vm.ImageFiles?.Any() == true)
        {
            int order = 0;
            foreach (var file in vm.ImageFiles)
            {
                if (!_files.IsValidImage(file, out _)) continue;
                var path = await _files.SavePropertyImageAsync(property.Id, file, ct);
                var isHero = heroPath == null;

                _context.PropertyImages.Add(new PropertyImage
                {
                    PropertyId = property.Id,
                    FilePath = path,
                    DisplayOrder = order++,
                    IsHero = isHero,
                    IsActive = true
                });

                if (isHero) heroPath = path;
            }
        }

        if (heroPath != null) property.HeroImagePath = heroPath;
        await _context.SaveChangesAsync(ct);

        await _activity.LogAsync(
            entity: "Property", action: "Created",
            title: $"{property.UnitNumber} — {property.Title ?? "Property"}",
            detail: $"{property.City ?? "—"} · {property.Status}",
            url: Url.Action("Index", "Properties"),
            entityId: property.Id);

        TempData["Success"] = $"Property '{property.UnitNumber}' created!";
        return RedirectToAction(nameof(Details), new { id = property.Id });
    }

    // ═══════════════════════════════════════════════════════
    // EDIT (GET)
    // ═══════════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var property = await _context.Properties.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (property == null) return NotFound();

        var images = await _context.PropertyImages.AsNoTracking()
            .Where(i => i.PropertyId == id)
            .OrderBy(i => i.DisplayOrder)
            .ToListAsync(ct);

        var projects = await LoadProjectOptionsAsync(ct);

        var vm = new PropertyEditViewModel
        {
            Property = property,
            ExistingImages = images,
            AvailableProjects = projects
        };

        return View(vm);
    }

    // ═══════════════════════════════════════════════════════
    // EDIT (POST)
    // ═══════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Edit(PropertyEditViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.AvailableProjects = await LoadProjectOptionsAsync(ct);
            vm.ExistingImages = await _context.PropertyImages.AsNoTracking()
                .Where(i => i.PropertyId == vm.Property.Id)
                .OrderBy(i => i.DisplayOrder)
                .ToListAsync(ct);
            TempData["Error"] = "Please fix validation errors.";
            return View(vm);
        }

        var property = await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == vm.Property.Id, ct);
        if (property == null) return NotFound();

        // Update fields
        property.UnitNumber = vm.Property.UnitNumber;
        property.Title = vm.Property.Title;
        property.ProjectId = vm.Property.ProjectId;
        property.PropertyType = vm.Property.PropertyType;
        property.BHK = vm.Property.BHK;
        property.Furnished = vm.Property.Furnished;
        property.Facing = vm.Property.Facing;
        property.FloorNumber = vm.Property.FloorNumber;
        property.TotalFloors = vm.Property.TotalFloors;
        property.CarpetArea = vm.Property.CarpetArea;
        property.BuiltUpArea = vm.Property.BuiltUpArea;
        property.SuperBuiltUpArea = vm.Property.SuperBuiltUpArea;
        property.AreaUnit = vm.Property.AreaUnit;
        property.BasePrice = vm.Property.BasePrice;
        property.TotalPrice = vm.Property.TotalPrice;
        property.PricePerSqft = vm.Property.PricePerSqft;
        property.City = vm.Property.City;
        property.Location = vm.Property.Location;
        property.Address = vm.Property.Address;
        property.Status = vm.Property.Status;
        property.OwnerName = vm.Property.OwnerName;
        property.OwnerPhone = vm.Property.OwnerPhone;
        property.BookingDate = vm.Property.BookingDate;
        property.SoldDate = vm.Property.SoldDate;
        property.BookingLeadId = vm.Property.BookingLeadId;
        property.DealId = vm.Property.DealId;
        property.Notes = vm.Property.Notes;
        property.IsActive = vm.Property.IsActive;
        property.IsFeatured = vm.Property.IsFeatured;
        property.UpdatedAt = DateTime.UtcNow;

        // Image deletions
        var keepIds = vm.KeepImageIds ?? new List<int>();
        var existingImages = await _context.PropertyImages
            .Where(i => i.PropertyId == property.Id)
            .ToListAsync(ct);

        foreach (var img in existingImages)
        {
            if (!keepIds.Contains(img.Id))
            {
                await _files.DeleteAsync(img.FilePath, ct);
                _context.PropertyImages.Remove(img);
            }
        }

        // New images
        if (vm.ImageFiles?.Any() == true)
        {
            var maxOrder = existingImages.Any() ? existingImages.Max(i => i.DisplayOrder) + 1 : 0;
            var hadHero = existingImages.Any(i => keepIds.Contains(i.Id) && i.IsHero);

            foreach (var file in vm.ImageFiles)
            {
                if (!_files.IsValidImage(file, out _)) continue;
                var path = await _files.SavePropertyImageAsync(property.Id, file, ct);
                var isHero = !hadHero;
                _context.PropertyImages.Add(new PropertyImage
                {
                    PropertyId = property.Id,
                    FilePath = path,
                    DisplayOrder = maxOrder++,
                    IsHero = isHero,
                    IsActive = true
                });
                if (isHero) hadHero = true;
            }
        }

        await _context.SaveChangesAsync(ct);

        // Update hero path
        var hero = await _context.PropertyImages.AsNoTracking()
            .Where(i => i.PropertyId == property.Id && i.IsActive)
            .OrderByDescending(i => i.IsHero)
            .ThenBy(i => i.DisplayOrder)
            .FirstOrDefaultAsync(ct);

        if (hero != null && property.HeroImagePath != hero.FilePath)
        {
            var tracked = await _context.Properties.FindAsync(new object[] { property.Id }, ct);
            if (tracked != null)
            {
                tracked.HeroImagePath = hero.FilePath;
                await _context.SaveChangesAsync(ct);
            }
        }

        await _activity.LogAsync(
            entity: "Property", action: "Updated",
            title: $"{property.UnitNumber} — {property.Title ?? "Property"}",
            detail: $"Status: {property.Status}",
            url: Url.Action("Index", "Properties"),
            entityId: property.Id);

        TempData["Success"] = $"Property '{property.UnitNumber}' updated!";
        return RedirectToAction(nameof(Details), new { id = property.Id });
    }

    // ═══════════════════════════════════════════════════════
    // DELETE
    // ═══════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var property = await _context.Properties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (property == null) return NotFound();

        foreach (var img in property.Images)
            await _files.DeleteAsync(img.FilePath, ct);

        _context.Properties.Remove(property);
        await _context.SaveChangesAsync(ct);

        await _activity.LogAsync(
            entity: "Property", action: "Deleted",
            title: property.UnitNumber,
            detail: property.City ?? "—",
            entityId: id);

        TempData["Success"] = $"Property '{property.UnitNumber}' deleted.";
        return RedirectToAction(nameof(Index));
    }

    // ═══════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════
    private async Task<List<ProjectOption>> LoadProjectOptionsAsync(CancellationToken ct)
    {
        return await _context.Projects.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new ProjectOption { Id = p.Id, Name = p.Name })
            .ToListAsync(ct);
    }
}