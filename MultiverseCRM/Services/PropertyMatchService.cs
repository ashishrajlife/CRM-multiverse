using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MultiverseCRM.Data;
using MultiverseCRM.Models;
using MultiverseCRM.ViewModels;

namespace MultiverseCRM.Services;

public class PropertyMatchService : IPropertyMatchService
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly PropertyMatchWeights _weights;
    private readonly ILogger<PropertyMatchService> _log;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public PropertyMatchService(
        AppDbContext context,
        IMemoryCache cache,
        IOptions<PropertyMatchWeights> weights,
        ILogger<PropertyMatchService> log)
    {
        _context = context;
        _cache = cache;
        _weights = weights.Value;
        _log = log;
    }

    // ═══════════════════════════════════════════════════════
    // LEAD → PROPERTIES
    // ═══════════════════════════════════════════════════════
    public async Task<List<PropertyMatch>> MatchLeadToPropertiesAsync(
        int leadId, int topN = 10, CancellationToken ct = default)
    {
        var cacheKey = $"match:lead:{leadId}:{topN}";
        if (_cache.TryGetValue(cacheKey, out List<PropertyMatch>? cached) && cached != null)
            return cached;

        var lead = await _context.Leads.AsNoTracking()
            .Include(l => l.ProjectEntity)
            .FirstOrDefaultAsync(l => l.Id == leadId, ct);

        if (lead == null) return new List<PropertyMatch>();

        // Pre-filter — only available properties, prioritize by city or BHK
        var candidateQuery = _context.Properties.AsNoTracking()
            .Include(p => p.Project)
            .Where(p => p.IsActive && p.Status == "Available");

        if (!string.IsNullOrEmpty(lead.City))
            candidateQuery = candidateQuery.Where(p => p.City == lead.City || p.BHK == lead.BHK || p.ProjectId == lead.ProjectId);
        else if (!string.IsNullOrEmpty(lead.BHK))
            candidateQuery = candidateQuery.Where(p => p.BHK == lead.BHK || p.ProjectId == lead.ProjectId);

        // Limit candidates — safety against huge tables
        var candidates = await candidateQuery
            .OrderByDescending(p => p.CreatedAt)
            .Take(200)
            .Select(p => new
            {
                p.Id,
                p.UnitNumber,
                p.Title,
                p.City,
                p.Location,
                p.BHK,
                p.Furnished,
                p.CarpetArea,
                p.TotalPrice,
                p.HeroImagePath,
                p.Status,
                p.ProjectId,
                ProjectName = p.Project != null ? p.Project.Name : null
            })
            .ToListAsync(ct);

        var results = new List<PropertyMatch>(candidates.Count);

        foreach (var c in candidates)
        {
            var (score, reasons) = CalculateLeadMatchScore(lead, c.BHK, c.City, c.TotalPrice, c.CarpetArea,
                c.Furnished, c.ProjectId, c.ProjectName);

            results.Add(new PropertyMatch
            {
                PropertyId = c.Id,
                UnitNumber = c.UnitNumber,
                Title = c.Title,
                ProjectName = c.ProjectName,
                City = c.City,
                Location = c.Location,
                BHK = c.BHK,
                Furnished = c.Furnished,
                CarpetArea = c.CarpetArea,
                TotalPrice = c.TotalPrice,
                HeroImagePath = c.HeroImagePath,
                Status = c.Status,
                Score = Math.Round(score, 1),
                MatchedOn = reasons
            });
        }

        var top = results
            .OrderByDescending(x => x.Score)
            .Take(topN)
            .ToList();

        _cache.Set(cacheKey, top, CacheDuration);
        return top;
    }

    // ═══════════════════════════════════════════════════════
    // PROPERTY → LEADS
    // ═══════════════════════════════════════════════════════
    public async Task<List<LeadMatch>> MatchPropertyToLeadsAsync(
        int propertyId, int topN = 10, CancellationToken ct = default)
    {
        var cacheKey = $"match:property:{propertyId}:{topN}";
        if (_cache.TryGetValue(cacheKey, out List<LeadMatch>? cached) && cached != null)
            return cached;

        var property = await _context.Properties.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == propertyId, ct);

        if (property == null) return new List<LeadMatch>();

        // Pre-filter — recent active leads, prioritize by city or BHK
        var candidateQuery = _context.Leads.AsNoTracking()
            .Where(l => l.Status != "Won" && l.Status != "Lost");

        if (!string.IsNullOrEmpty(property.City))
            candidateQuery = candidateQuery.Where(l => l.City == property.City || l.BHK == property.BHK);
        else if (!string.IsNullOrEmpty(property.BHK))
            candidateQuery = candidateQuery.Where(l => l.BHK == property.BHK);

        var candidates = await candidateQuery
            .OrderByDescending(l => l.CreatedAt)
            .Take(200)
            .Select(l => new
            {
                l.Id,
                l.FullName,
                l.Phone,
                l.Status,
                l.Staff,
                l.BHK,
                l.City,
                l.MinPrice,
                l.MaxPrice,
                l.MinArea,
                l.MaxArea,
                l.LookingFor,
                l.Furnished,
                l.ProjectId,
                l.Project,
                l.CreatedAt
            })
            .ToListAsync(ct);

        var results = new List<LeadMatch>(candidates.Count);

        foreach (var l in candidates)
        {
            var (score, reasons) = CalculatePropertyMatchScore(
                property.BHK, property.City, property.TotalPrice, property.CarpetArea,
                property.Furnished, property.ProjectId, property.Project?.Name,
                l.BHK, l.City, l.MinPrice, l.MaxPrice, l.MinArea, l.MaxArea,
                l.LookingFor, l.Furnished, l.ProjectId, l.Project);

            results.Add(new LeadMatch
            {
                LeadId = l.Id,
                FullName = l.FullName,
                Phone = l.Phone,
                Status = l.Status,
                Staff = l.Staff,
                BHK = l.BHK,
                City = l.City,
                MinPrice = l.MinPrice,
                MaxPrice = l.MaxPrice,
                MinArea = l.MinArea,
                MaxArea = l.MaxArea,
                LookingFor = l.LookingFor,
                Furnished = l.Furnished,
                Project = l.Project,
                CreatedAt = l.CreatedAt,
                Score = Math.Round(score, 1),
                MatchedOn = reasons
            });
        }

        var top = results
            .OrderByDescending(x => x.Score)
            .Take(topN)
            .ToList();

        _cache.Set(cacheKey, top, CacheDuration);
        return top;
    }

    // ═══════════════════════════════════════════════════════
    // PANEL WRAPPERS
    // ═══════════════════════════════════════════════════════
    public async Task<MatchingPanelViewModel> GetLeadMatchesPanelAsync(int leadId, CancellationToken ct = default)
    {
        var lead = await _context.Leads.AsNoTracking()
            .Where(l => l.Id == leadId)
            .Select(l => new { l.Id, l.FullName })
            .FirstOrDefaultAsync(ct);

        if (lead == null) return new MatchingPanelViewModel();

        var matches = await MatchLeadToPropertiesAsync(leadId, 10, ct);

        return new MatchingPanelViewModel
        {
            Context = "lead",
            ContextId = leadId,
            ContextLabel = lead.FullName,
            TotalMatches = matches.Count,
            StrongMatches = matches.Count(m => m.Score >= 80),
            GoodMatches = matches.Count(m => m.Score >= 60),
            Properties = matches
        };
    }

    public async Task<MatchingPanelViewModel> GetPropertyMatchesPanelAsync(int propertyId, CancellationToken ct = default)
    {
        var prop = await _context.Properties.AsNoTracking()
            .Where(p => p.Id == propertyId)
            .Select(p => new { p.Id, p.UnitNumber })
            .FirstOrDefaultAsync(ct);

        if (prop == null) return new MatchingPanelViewModel();

        var matches = await MatchPropertyToLeadsAsync(propertyId, 10, ct);

        return new MatchingPanelViewModel
        {
            Context = "property",
            ContextId = propertyId,
            ContextLabel = prop.UnitNumber,
            TotalMatches = matches.Count,
            StrongMatches = matches.Count(m => m.Score >= 80),
            GoodMatches = matches.Count(m => m.Score >= 60),
            Leads = matches
        };
    }

    // ═══════════════════════════════════════════════════════
    // SCORING — Lead → Property
    // ═══════════════════════════════════════════════════════
    private (double score, List<string> reasons) CalculateLeadMatchScore(
        Lead lead,
        string? propBHK, string? propCity, decimal? propPrice, decimal? propArea,
        string? propFurnished, int? propProjectId, string? propProjectName)
    {
        double score = 0;
        var reasons = new List<string>();

        // BHK
        if (!string.IsNullOrEmpty(lead.BHK) && !string.IsNullOrEmpty(propBHK))
        {
            if (string.Equals(lead.BHK, propBHK, StringComparison.OrdinalIgnoreCase))
            {
                score += _weights.BHK;
                reasons.Add($"{propBHK} ✓");
            }
        }

        // City
        if (!string.IsNullOrEmpty(lead.City) && !string.IsNullOrEmpty(propCity))
        {
            if (string.Equals(lead.City, propCity, StringComparison.OrdinalIgnoreCase))
            {
                score += _weights.City;
                reasons.Add("Same city ✓");
            }
        }

        // Budget — overlap between [lead.MinPrice, lead.MaxPrice] and prop.TotalPrice
        if (propPrice.HasValue && (lead.MinPrice.HasValue || lead.MaxPrice.HasValue))
        {
            var leadMin = lead.MinPrice ?? 0;
            var leadMax = lead.MaxPrice ?? decimal.MaxValue;
            if (propPrice.Value >= leadMin && propPrice.Value <= leadMax)
            {
                score += _weights.Budget;
                reasons.Add("Budget ✓");
            }
        }

        // Area — overlap
        if (propArea.HasValue && (lead.MinArea.HasValue || lead.MaxArea.HasValue))
        {
            var leadMin = lead.MinArea ?? 0;
            var leadMax = lead.MaxArea ?? decimal.MaxValue;
            if (propArea.Value >= leadMin && propArea.Value <= leadMax)
            {
                score += _weights.Area;
                reasons.Add("Area ✓");
            }
        }

        // LookingFor (Rent/Buy) — heuristic: property has no explicit field so skip for now
        // If you add property.LookingFor later, add here

        // Furnished
        if (!string.IsNullOrEmpty(lead.Furnished) && !string.IsNullOrEmpty(propFurnished))
        {
            if (string.Equals(lead.Furnished, propFurnished, StringComparison.OrdinalIgnoreCase))
            {
                score += _weights.Furnished;
                reasons.Add("Furnishing ✓");
            }
        }

        // Project (ID or Name match)
        if (propProjectId.HasValue && lead.ProjectId.HasValue)
        {
            if (propProjectId.Value == lead.ProjectId.Value)
            {
                score += _weights.Project;
                reasons.Add("Same project ✓");
            }
        }
        else if (!string.IsNullOrEmpty(lead.Project) && !string.IsNullOrEmpty(propProjectName))
        {
            if (string.Equals(lead.Project, propProjectName, StringComparison.OrdinalIgnoreCase))
            {
                score += _weights.Project;
                reasons.Add("Project name match ✓");
            }
        }

        return (score, reasons);
    }

    // ═══════════════════════════════════════════════════════
    // SCORING — Property → Lead
    // ═══════════════════════════════════════════════════════
    private (double score, List<string> reasons) CalculatePropertyMatchScore(
        string? propBHK, string? propCity, decimal? propPrice, decimal? propArea,
        string? propFurnished, int? propProjectId, string? propProjectName,
        string? leadBHK, string? leadCity, decimal? leadMinPrice, decimal? leadMaxPrice,
        decimal? leadMinArea, decimal? leadMaxArea,
        string? leadLookingFor, string? leadFurnished,
        int? leadProjectId, string? leadProjectName)
    {
        double score = 0;
        var reasons = new List<string>();

        // BHK
        if (!string.IsNullOrEmpty(leadBHK) && !string.IsNullOrEmpty(propBHK)
            && string.Equals(leadBHK, propBHK, StringComparison.OrdinalIgnoreCase))
        {
            score += _weights.BHK;
            reasons.Add($"{leadBHK} ✓");
        }

        // City
        if (!string.IsNullOrEmpty(leadCity) && !string.IsNullOrEmpty(propCity)
            && string.Equals(leadCity, propCity, StringComparison.OrdinalIgnoreCase))
        {
            score += _weights.City;
            reasons.Add("Same city ✓");
        }

        // Budget — prop.Price within lead's [min, max]
        if (propPrice.HasValue && (leadMinPrice.HasValue || leadMaxPrice.HasValue))
        {
            var min = leadMinPrice ?? 0;
            var max = leadMaxPrice ?? decimal.MaxValue;
            if (propPrice.Value >= min && propPrice.Value <= max)
            {
                score += _weights.Budget;
                reasons.Add("Budget ✓");
            }
        }

        // Area
        if (propArea.HasValue && (leadMinArea.HasValue || leadMaxArea.HasValue))
        {
            var min = leadMinArea ?? 0;
            var max = leadMaxArea ?? decimal.MaxValue;
            if (propArea.Value >= min && propArea.Value <= max)
            {
                score += _weights.Area;
                reasons.Add("Area ✓");
            }
        }

        // Furnished
        if (!string.IsNullOrEmpty(leadFurnished) && !string.IsNullOrEmpty(propFurnished)
            && string.Equals(leadFurnished, propFurnished, StringComparison.OrdinalIgnoreCase))
        {
            score += _weights.Furnished;
            reasons.Add("Furnishing ✓");
        }

        // Project
        if (propProjectId.HasValue && leadProjectId.HasValue && propProjectId.Value == leadProjectId.Value)
        {
            score += _weights.Project;
            reasons.Add("Same project ✓");
        }
        else if (!string.IsNullOrEmpty(leadProjectName) && !string.IsNullOrEmpty(propProjectName)
                 && string.Equals(leadProjectName, propProjectName, StringComparison.OrdinalIgnoreCase))
        {
            score += _weights.Project;
            reasons.Add("Project name match ✓");
        }

        return (score, reasons);
    }
}