using MultiverseCRM.Models;
using MultiverseCRM.ViewModels;

namespace MultiverseCRM.Services;

public interface IPropertyMatchService
{
    Task<List<PropertyMatch>> MatchLeadToPropertiesAsync(int leadId, int topN = 10, CancellationToken ct = default);
    Task<List<LeadMatch>> MatchPropertyToLeadsAsync(int propertyId, int topN = 10, CancellationToken ct = default);
    Task<MatchingPanelViewModel> GetLeadMatchesPanelAsync(int leadId, CancellationToken ct = default);
    Task<MatchingPanelViewModel> GetPropertyMatchesPanelAsync(int propertyId, CancellationToken ct = default);
}