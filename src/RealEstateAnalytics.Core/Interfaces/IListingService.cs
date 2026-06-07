using RealEstateAnalytics.Core.Models;

namespace RealEstateAnalytics.Core.Interfaces;

public interface IListingService
{
    Task<IEnumerable<AgentListingsResponseModel>> GetAgentListingsOrderedByCountAsync(ListingRequestModel requestModel, CancellationToken ct = default);
}
