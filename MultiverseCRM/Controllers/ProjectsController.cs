using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiverseCRM.Data;
using MultiverseCRM.Models;
using MultiverseCRM.Services;
using MultiverseCRM.ViewModels;

namespace MultiverseCRM.Controllers;

[Authorize]
public class ProjectsController : Controller
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _files;
    private readonly IActivityLogger _activity;
    private readonly ILogger<ProjectsController> _log;

    public ProjectsController(
        AppDbContext context,
        IFileStorageService files,
        IActivityLogger activity,
        ILogger<ProjectsController> log)
    {
        _context = context;
        _files = files;
        _activity = activity;
        _log = log;
    }

    private string CurrentUserDisplay =>
        User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "System";

    // ═══════════════════════════════════════════════════════
    // LIST — SEQUENTIAL queries (DbContext is NOT thread-safe)
    // ═══════════════════════════════════════════════════════
    public async Task<IActionResult> Index(
        string? search = null,
        string? city = null,
        string? status = null,
        string sortBy = "CreatedAt",
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 24,
        CancellationToken ct = default)
    {
        pageSize = new[] { 12, 24, 48, 96 }.Contains(pageSize) ? pageSize : 24;
        page = Math.Max(1, page);

        var q = _context.Projects.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p =>
                p.Name.Contains(s) ||
                (p.City != null && p.City.Contains(s)) ||
                (p.Location != null && p.Location.Contains(s)) ||
                (p.Developer != null && p.Developer.Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(city))
            q = q.Where(p => p.City == city);

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(p => p.Status == status);

        bool asc = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);
        q = sortBy switch
        {
            "Name"       => asc ? q.OrderBy(p => p.Name) : q.OrderByDescending(p => p.Name),
            "City"       => asc ? q.OrderBy(p => p.City) : q.OrderByDescending(p => p.City),
            "MinPrice"   => asc ? q.OrderBy(p => p.MinPrice) : q.OrderByDescending(p => p.MinPrice),
            "Possession" => asc ? q.OrderBy(p => p.PossessionDate) : q.OrderByDescending(p => p.PossessionDate),
            _            => asc ? q.OrderBy(p => p.CreatedAt) : q.OrderByDescending(p => p.CreatedAt)
        };

        // ─── SEQUENTIAL awaits — same _context, safe ────────
        var total = await q.CountAsync(ct);

        var stats = await _context.Projects.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Active = g.Count(p => p.IsActive),
                Featured = g.Count(p => p.IsFeatured && p.IsActive),
                Total = g.Count()
            })
            .FirstOrDefaultAsync(ct);

        var cityOptions = await _context.Projects.AsNoTracking()
            .Where(p => p.City != null && p.City != "" && p.IsActive)
            .Select(p => p.City!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);

        var statusOptions = await _context.Projects.AsNoTracking()
            .Where(p => p.Status != null && p.Status != "" && p.IsActive)
            .Select(p => p.Status!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(ct);

        int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, totalPages);

        var projects = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProjectCard
            {
                Id = p.Id,
                Name = p.Name,
                City = p.City,
                Location = p.Location,
                Status = p.Status,
                ProjectType = p.ProjectType,
                Developer = p.Developer,
                HeroImagePath = p.HeroImagePath,
                MinPrice = p.MinPrice,
                MaxPrice = p.MaxPrice,
                PossessionDate = p.PossessionDate,
                IsActive = p.IsActive,
                IsFeatured = p.IsFeatured,
                ImageCount = p.Images.Count(i => i.IsActive),
                AmenityCount = p.Amenities.Count,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);

        var vm = new ProjectListViewModel
        {
            Search = search,
            CityFilter = city,
            StatusFilter = status,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Page = page,
            PageSize = pageSize,
            TotalFiltered = total,
            TotalCount = stats?.Total ?? 0,
            ActiveCount = stats?.Active ?? 0,
            FeaturedCount = stats?.Featured ?? 0,
            Projects = projects,
            CityOptions = cityOptions,
            StatusOptions = statusOptions
        };

        return View(vm);
    }

    // ═══════════════════════════════════════════════════════
    // DETAILS
    // ═══════════════════════════════════════════════════════
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var project = await _context.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (project == null) return NotFound();

        var images = await _context.ProjectImages.AsNoTracking()
            .Where(i => i.ProjectId == id && i.IsActive)
            .OrderBy(i => i.DisplayOrder)
            .ThenBy(i => i.Id)
            .ToListAsync(ct);

        var amenities = await _context.ProjectAmenities.AsNoTracking()
            .Where(a => a.ProjectId == id)
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync(ct);

        var documents = await _context.ProjectDocuments.AsNoTracking()
            .Where(d => d.ProjectId == id)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);

        var vm = new ProjectDetailViewModel
        {
            Project = project,
            Images = images,
            Amenities = amenities,
            Documents = documents
        };

        return View(vm);
    }

    // ═══════════════════════════════════════════════════════
    // CREATE (GET)
    // ═══════════════════════════════════════════════════════
    [HttpGet]
    public IActionResult Create()
    {
        return View(new ProjectCreateViewModel());
    }

    // ═══════════════════════════════════════════════════════
    // CREATE (POST)
    // ═══════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Create(ProjectCreateViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please fill required fields.";
            return View(vm);
        }

        var project = vm.Project;
        project.Id = 0;
        project.CreatedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;
        project.CreatedBy = CurrentUserDisplay;
        project.IsActive = true;

        _context.Projects.Add(project);
        await _context.SaveChangesAsync(ct);

        string? heroPath = null;

        if (vm.ImageFiles?.Any() == true)
        {
            int order = 0;
            foreach (var file in vm.ImageFiles)
            {
                if (!_files.IsValidImage(file, out _)) continue;

                var path = await _files.SaveProjectImageAsync(project.Id, file, ct);
                var isHero = heroPath == null;

                _context.ProjectImages.Add(new ProjectImage
                {
                    ProjectId = project.Id,
                    FilePath = path,
                    DisplayOrder = order++,
                    IsHero = isHero,
                    IsActive = true
                });

                if (isHero) heroPath = path;
            }
        }

        if (vm.AmenityNames?.Any() == true)
        {
            int order = 0;
            foreach (var name in ParseAmenities(vm.AmenityNames))
            {
                _context.ProjectAmenities.Add(new ProjectAmenity
                {
                    ProjectId = project.Id,
                    Name = name.Trim(),
                    DisplayOrder = order++
                });
            }
        }

        if (vm.DocumentFiles?.Any() == true)
        {
            foreach (var file in vm.DocumentFiles)
            {
                if (!_files.IsValidDocument(file, out _)) continue;

                var path = await _files.SaveProjectDocumentAsync(project.Id, file, ct);
                _context.ProjectDocuments.Add(new ProjectDocument
                {
                    ProjectId = project.Id,
                    Name = Path.GetFileNameWithoutExtension(file.FileName),
                    FilePath = path,
                    FileType = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant(),
                    FileSizeBytes = file.Length
                });
            }
        }

        if (heroPath != null)
            project.HeroImagePath = heroPath;

        await _context.SaveChangesAsync(ct);

        await _activity.LogAsync(
            entity: "Project", action: "Created",
            title: project.Name,
            detail: $"{project.City ?? "—"} · {project.Status ?? "—"}",
            url: Url.Action("Index", "Projects"),
            entityId: project.Id);

        TempData["Success"] = $"Project '{project.Name}' created successfully!";
        return RedirectToAction(nameof(Details), new { id = project.Id });
    }

    // ═══════════════════════════════════════════════════════
    // EDIT (GET)
    // ═══════════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var project = await _context.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (project == null) return NotFound();

        var images = await _context.ProjectImages.AsNoTracking()
            .Where(i => i.ProjectId == id)
            .OrderBy(i => i.DisplayOrder)
            .ThenBy(i => i.Id)
            .ToListAsync(ct);

        var amenities = await _context.ProjectAmenities.AsNoTracking()
            .Where(a => a.ProjectId == id)
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync(ct);

        var documents = await _context.ProjectDocuments.AsNoTracking()
            .Where(d => d.ProjectId == id)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);

        var vm = new ProjectEditViewModel
        {
            Project = project,
            ExistingImages = images,
            ExistingAmenities = amenities,
            ExistingDocuments = documents,
            AmenityNames = amenities.Select(a => a.Name).ToList()
        };

        return View(vm);
    }

    // ═══════════════════════════════════════════════════════
    // EDIT (POST)
    // ═══════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Edit(ProjectEditViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            // Reload existing data so form can render
            vm.ExistingImages = await _context.ProjectImages.AsNoTracking()
                .Where(i => i.ProjectId == vm.Project.Id)
                .OrderBy(i => i.DisplayOrder)
                .ToListAsync(ct);
            vm.ExistingAmenities = await _context.ProjectAmenities.AsNoTracking()
                .Where(a => a.ProjectId == vm.Project.Id)
                .OrderBy(a => a.DisplayOrder)
                .ToListAsync(ct);
            vm.ExistingDocuments = await _context.ProjectDocuments.AsNoTracking()
                .Where(d => d.ProjectId == vm.Project.Id)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync(ct);

            TempData["Error"] = "Please fix validation errors.";
            return View(vm);
        }

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == vm.Project.Id, ct);

        if (project == null) return NotFound();

        // Update core fields
        project.Name = vm.Project.Name;
        project.Description = vm.Project.Description;
        project.City = vm.Project.City;
        project.Location = vm.Project.Location;
        project.Status = vm.Project.Status;
        project.ProjectType = vm.Project.ProjectType;
        project.ReraNumber = vm.Project.ReraNumber;
        project.Developer = vm.Project.Developer;
        project.LaunchDate = vm.Project.LaunchDate;
        project.PossessionDate = vm.Project.PossessionDate;
        project.MinPrice = vm.Project.MinPrice;
        project.MaxPrice = vm.Project.MaxPrice;
        project.TotalArea = vm.Project.TotalArea;
        project.AreaUnit = vm.Project.AreaUnit;
        project.TotalUnits = vm.Project.TotalUnits;
        project.IsFeatured = vm.Project.IsFeatured;
        project.IsActive = vm.Project.IsActive;
        project.UpdatedAt = DateTime.UtcNow;

        // ─── Handle image deletions (keep only IDs in vm.KeepImageIds) ───
        var keepIds = vm.KeepImageIds ?? new List<int>();
        var existingImages = await _context.ProjectImages
            .Where(i => i.ProjectId == project.Id)
            .ToListAsync(ct);

        foreach (var img in existingImages)
        {
            if (!keepIds.Contains(img.Id))
            {
                await _files.DeleteAsync(img.FilePath, ct);
                _context.ProjectImages.Remove(img);
            }
        }

        // ─── Add new images ─────────────────────────────────
        if (vm.ImageFiles?.Any() == true)
        {
            var maxOrder = existingImages.Any() ? existingImages.Max(i => i.DisplayOrder) + 1 : 0;
            var hadHero = existingImages.Any(i => keepIds.Contains(i.Id) && i.IsHero);

            foreach (var file in vm.ImageFiles)
            {
                if (!_files.IsValidImage(file, out _)) continue;

                var path = await _files.SaveProjectImageAsync(project.Id, file, ct);
                var isHero = !hadHero;

                _context.ProjectImages.Add(new ProjectImage
                {
                    ProjectId = project.Id,
                    FilePath = path,
                    DisplayOrder = maxOrder++,
                    IsHero = isHero,
                    IsActive = true
                });

                if (isHero) hadHero = true;
            }
        }

        // ─── Reset hero if requested ───────────────────────
        if (vm.NewHeroImageId.HasValue)
        {
            foreach (var img in existingImages)
                img.IsHero = (img.Id == vm.NewHeroImageId.Value);
        }

        // ─── Update hero path on project ───────────────────
        // (will be set after SaveChanges if needed)
        // Simplification: keep existing hero path or set first image

        // ─── Amenities — full replace ──────────────────────
        var existingAmenities = await _context.ProjectAmenities
            .Where(a => a.ProjectId == project.Id)
            .ToListAsync(ct);
        _context.ProjectAmenities.RemoveRange(existingAmenities);

       int amenityOrder = 0;
        foreach (var name in ParseAmenities(vm.AmenityNames))
        {
            _context.ProjectAmenities.Add(new ProjectAmenity
            {
                ProjectId = project.Id,
                Name = name,
                DisplayOrder = amenityOrder++
            });
        }

        // ─── New documents ─────────────────────────────────
        if (vm.DocumentFiles?.Any() == true)
        {
            foreach (var file in vm.DocumentFiles)
            {
                if (!_files.IsValidDocument(file, out _)) continue;

                var path = await _files.SaveProjectDocumentAsync(project.Id, file, ct);
                _context.ProjectDocuments.Add(new ProjectDocument
                {
                    ProjectId = project.Id,
                    Name = Path.GetFileNameWithoutExtension(file.FileName),
                    FilePath = path,
                    FileType = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant(),
                    FileSizeBytes = file.Length
                });
            }
        }

        await _context.SaveChangesAsync(ct);

        // ─── Update HeroImagePath on project ───────────────
        var hero = await _context.ProjectImages.AsNoTracking()
            .Where(i => i.ProjectId == project.Id && i.IsActive)
            .OrderByDescending(i => i.IsHero)
            .ThenBy(i => i.DisplayOrder)
            .FirstOrDefaultAsync(ct);

        if (hero != null && project.HeroImagePath != hero.FilePath)
        {
            var trackedProject = await _context.Projects.FindAsync(new object[] { project.Id }, ct);
            if (trackedProject != null)
            {
                trackedProject.HeroImagePath = hero.FilePath;
                await _context.SaveChangesAsync(ct);
            }
        }

        await _activity.LogAsync(
            entity: "Project", action: "Updated",
            title: project.Name,
            detail: $"{project.City ?? "—"} · {project.Status ?? "—"}",
            url: Url.Action("Index", "Projects"),
            entityId: project.Id);

        TempData["Success"] = $"Project '{project.Name}' updated successfully!";
        return RedirectToAction(nameof(Details), new { id = project.Id });
    }

    // ═══════════════════════════════════════════════════════
    // DELETE
    // ═══════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var project = await _context.Projects
            .Include(p => p.Images)
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (project == null) return NotFound();

        foreach (var img in project.Images)
            await _files.DeleteAsync(img.FilePath, ct);
        foreach (var doc in project.Documents)
            await _files.DeleteAsync(doc.FilePath, ct);

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync(ct);

        await _activity.LogAsync(
            entity: "Project", action: "Deleted",
            title: project.Name,
            detail: project.City ?? "—",
            entityId: id);

        TempData["Success"] = $"Project '{project.Name}' deleted.";
        return RedirectToAction(nameof(Index));
    }

    // ═══════════════════════════════════════════════════════
    // DELETE IMAGE (AJAX, from edit page)
    // ═══════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int imageId, CancellationToken ct)
    {
        var img = await _context.ProjectImages.FindAsync(new object[] { imageId }, ct);
        if (img == null) return Json(new { ok = false, error = "Image not found" });

        await _files.DeleteAsync(img.FilePath, ct);
        _context.ProjectImages.Remove(img);
        await _context.SaveChangesAsync(ct);

        return Json(new { ok = true });
    }

    // ═══════════════════════════════════════════════════════
    // TOGGLE FEATURED
    // ═══════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFeatured(int id, CancellationToken ct)
    {
        var project = await _context.Projects.FindAsync(new object[] { id }, ct);
        if (project == null) return NotFound();

        project.IsFeatured = !project.IsFeatured;
        project.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return Json(new { ok = true, isFeatured = project.IsFeatured });
    }

    private static List<string> ParseAmenities(IEnumerable<string>? raw) =>
    (raw ?? Enumerable.Empty<string>())
        .SelectMany(s => s.Split(new[] { '\n', '\r', ',' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Select(s => s.Length > 100 ? s[..100] : s)   // 100 ko ProjectAmenity.Name ki MaxLength se match karo
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();


}